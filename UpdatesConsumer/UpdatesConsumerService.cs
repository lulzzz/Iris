using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace UpdatesConsumer
{
    public class UpdatesConsumerService : BackgroundService
    {
        private readonly RabbitMqConfig _config;
        private readonly IUpdateConsumer _consumer;
        private readonly ILogger<UpdatesConsumerService> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public UpdatesConsumerService(
            RabbitMqConfig config, 
            IUpdateConsumer consumer,
            ILogger<UpdatesConsumerService> logger)
        {
            _config = config;
            _consumer = consumer;
            _logger = logger;

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters =
                {
                    new MediaJsonConverter()
                }
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new RabbitMqConsumer(_config, OnMessage);

            // Dispose the consumer when service is stopped
            stoppingToken.Register(() => consumer.Dispose());

            return Task.CompletedTask;
        }

        private async Task OnMessage(BasicDeliverEventArgs message)
        {
            try
            {
                var update = JsonSerializer.Deserialize<Update>(message.Body.Span, _jsonSerializerOptions);
                await _consumer.OnUpdateAsync(update);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "");
            }
        }
    }
}