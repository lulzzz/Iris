﻿using System;
using System.Collections.Generic;
using ConsumerTelegramBot.Configuration;
using Microsoft.Extensions.Logging;
using ProducerApi;
using Telegram.Bot;
using TwitterProducer;

namespace ConsumerTelegramBot
{
    internal class TelegramBot
    {
        private readonly ConsumerTelegramBotConfig _config;
        private readonly ILogger<TelegramBot> _logger;
        private readonly ITelegramBotClient _client;

        public TelegramBot(
            ConsumerTelegramBotConfig config,
            ILogger<TelegramBot> logger)
        {
            _config = config;
            _logger = logger;

            _client = new TelegramBotClient(config.Token);
            _client.StartReceiving();
            
            _logger.LogInformation("Starting to receive Telegram events");

            RegisterProducers();
            
            _logger.LogInformation("Completed construction");
        }

        private void RegisterProducers()
        {
            foreach ((IProducer producer, long[] watchedUsersIds) in GetProducers())
            {
                var usersWatcher = new UsersWatcher(
                    TimeSpan.FromSeconds(_config.TwitterConfig.PollIntervalSeconds),
                    producer,
                    watchedUsersIds);
                
                usersWatcher
                    .Updates
                    .Subscribe(OnProducerUpdate);
                
                _logger.LogInformation($"Subscribed to updates of the producer `{producer.GetType().Name}`");
            }
        }

        private async void OnProducerUpdate(IUpdate update)
        {
            _logger.LogInformation($"Caught new update #{update.Id} by {update.Author.Name}");
            foreach (long chatId in _config.PostChatIds)
            {
                await _client.SendTextMessageAsync(chatId, update.Url);
                _logger.LogInformation($"Posted update #{update.Id} to chat {chatId}");
            }
        }

        private IEnumerable<(IProducer, long[])> GetProducers()
        {
            TwitterConfig twitterConfig = _config.TwitterConfig;

            yield return
            (
                new Twitter(
                    twitterConfig.ConsumerKey,
                    twitterConfig.ConsumerSecret,
                    twitterConfig.AccessToken,
                    twitterConfig.AccessTokenSecret
                ),
                    twitterConfig.WatchedUsersIds
            );
        }
    }
}