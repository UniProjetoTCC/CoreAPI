using Business.Services.Base;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Business.Services
{
    public class MessageBrokerService : IMessageBrokerService, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<MessageBrokerService> _logger;
        private readonly HashSet<string> _activeSubscriptions;
        private bool _disposed;

        public MessageBrokerService(IConnectionMultiplexer redis, ILogger<MessageBrokerService> logger)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _subscriber = redis.GetSubscriber();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSubscriptions = new HashSet<string>();
        }

        public async Task PublishAsync<T>(string channel, T message)
        {
            ThrowIfDisposed();

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
            ThrowIfDisposed();

            // Prevent duplicate subscriptions
            if (_activeSubscriptions.Contains(channel))
            {
                _logger.LogWarning("Already subscribed to channel {Channel}", channel);
                return;
            }

            try
            {
                await _subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (_, value) =>
                {
                    if (value.IsNullOrEmpty) return;

                    try
                    {
                        var message = JsonSerializer.Deserialize<T>(value!);
                        if (message != null)
                        {
                            await handler(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from channel {Channel}", channel);
                    }
                });

                _activeSubscriptions.Add(channel);
                _logger.LogInformation("Successfully subscribed to channel {Channel}", channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to channel {Channel}", channel);
                throw;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MessageBrokerService));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                foreach (var channel in _activeSubscriptions)
                {
                    _subscriber.Unsubscribe(RedisChannel.Literal(channel));
                }
                _activeSubscriptions.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MessageBrokerService disposal");
            }

            _disposed = true;
        }
    }
}
