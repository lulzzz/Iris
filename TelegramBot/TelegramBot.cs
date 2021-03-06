using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Extensions;
using Microsoft.Extensions.Logging;
using Optional;
using Optional.Unsafe;
using Telegram.Bot;
using Telegram.Bot.Types;
using UpdatesConsumer;
using Update = Common.Update;

namespace TelegramBot
{
    public class TelegramBot : IUpdateConsumer
    {
        private readonly ITelegramBotClientProvider _clientProvider;
        private readonly MessageBuilder _messageBuilder;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<TelegramBot> _logger;
        private readonly ConcurrentDictionary<ChatId, ActionBlock<Task>> _chatSenders;
        
        private readonly object _configLock = new object();
        private MessageSender _sender;
        private TelegramConfig _config;

        public TelegramBot(
            IConfigProvider configProvider,
            MessageBuilder messageBuilder,
            ITelegramBotClientProvider clientProvider,
            ILoggerFactory loggerFactory)
        {
            _clientProvider = clientProvider;
            _messageBuilder = messageBuilder;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<TelegramBot>();
            _chatSenders = new ConcurrentDictionary<ChatId, ActionBlock<Task>>();
            
            configProvider.Configs.SubscribeAsync(HandleConfig);
        }

        private Task HandleConfig(Result<TelegramConfig> result)
        {
            Task OnEmptyReceivedAsync()
            {
                _logger.LogInformation("Received empty config that will not be used");

                return Task.CompletedTask;
            }

            return result.DoAsync(
                OnConfigReceivedAsync,
                OnEmptyReceivedAsync);
        }

        private async Task OnConfigReceivedAsync(TelegramConfig config)
        {
            _logger.LogInformation(
                "Received new config {}, trying to create new TelegramBotClient with it",
                config);
            
            var client = await CreateNewTelegramBotClient(config);
            
            if (client.HasValue)
            {
                lock (_configLock) 
                {
                    _sender = new MessageSender(client.ValueOrDefault(), _loggerFactory);
                    _config = config;
                }
            }
        }

        private async Task<Option<ITelegramBotClient>> CreateNewTelegramBotClient(TelegramConfig config)
        {
            try
            {
                return Option.Some(
                    await _clientProvider.CreateAsync(config));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create TelegramBotClient with new config");
                return Option.None<ITelegramBotClient>();
            }
        }

        public async Task OnUpdateAsync(Update update)
        {
            (TelegramConfig config, MessageSender sender) = GetState();

            if (!TryGetUser(config, update.AuthorId, out User user))
            {
                _logger.LogError("User {} is not in config. Leaving.", update.AuthorId);
                return;
            }

            _logger.LogInformation("Got update {}", update);
            
            foreach (ChatId chatId in user.ChatIds)
            {
                try
                {
                    MessageInfo messageInfo = _messageBuilder.Build(update, user, chatId);

                    await SendChatUpdate(update, sender, messageInfo, chatId);
                }
                catch (FilterRuleSkipException)
                {
                    _logger.LogInformation("Skipping update because of filter skip rule");
                }
                
            }
        }

        private (TelegramConfig config, MessageSender sender) GetState()
        {
            lock (_configLock)
            {
                return (_config, _sender);
            }
        }

        private async Task SendChatUpdate(
            Update originalUpdate,
            MessageSender sender,
            MessageInfo message,
            ChatId chatId)
        {
            _logger.LogInformation("Sending update {} to chat id {}", originalUpdate, chatId.Username ?? chatId.Identifier.ToString());
            
            ActionBlock<Task> chatSender = _chatSenders
                .GetOrAdd(chatId, id => new ActionBlock<Task>(task => task));

            await chatSender.SendAsync(
                sender.SendAsync(message));
                
            _logger.LogInformation("Successfully sent update {} to chat id {}", originalUpdate, chatId.Username ?? chatId.Identifier.ToString());
        }

        private bool TryGetUser(TelegramConfig config, string authorId, out User user)
        {
            if (config == null)
            {
                _logger.LogError("Update request received, but no config present");
            }
            
            user = config?.Users.FirstOrDefault(u => u.UserNames.Contains(authorId));
            return user != null;
        }

        public async Task FlushAsync()
        {
            Task[] completions = _chatSenders
                .Select(CompleteAsync)
                .ToArray();

            await Task.WhenAll(completions);
        }

        private Task CompleteAsync(KeyValuePair<ChatId, ActionBlock<Task>> pair)
        {
            (ChatId chatId, ActionBlock<Task> chatSender) = pair;

            _logger.LogInformation("Completing chat sender for chat id: {}", chatId);

            return chatSender.CompleteAsync();
        }
    }
}
