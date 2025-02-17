using System;
using System.Text.Json;
using System.Threading.Tasks;
using Business.Services.Base;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Business.Services
{
    public class MessageBrokerService : IMessageBrokerService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<MessageBrokerService> _logger;
        private bool _disposed;

        public MessageBrokerService(IConnectionMultiplexer redis, ILogger<MessageBrokerService> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _subscriber = redis.GetSubscriber();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync<T>(string channel, T message)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBrokerService));

            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                await _subscriber.PublishAsync(RedisChannel.Literal(channel), jsonMessage);
                _logger.LogInformation("Message published to channel {Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to channel {Channel}", channel);
                throw;
            }
        }

        public async Task SubscribeAsync<T>(string channel, Func<T, Task> handler)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessageBrokerService));

            try
            {
                await _subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (_, value) =>
                {
                    if (value.IsNullOrEmpty) return;
                    
                    try
                    {
                        var typedMessage = JsonSerializer.Deserialize<T>(value!);
                        if (typedMessage != null)
                        {
                            await handler(typedMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from channel {Channel}", channel);
                    }
                });
                
                _logger.LogInformation("Subscribed to channel {Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to channel {Channel}", channel);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _redis?.Dispose();
                _disposed = true;
            }
        }
    }
}
