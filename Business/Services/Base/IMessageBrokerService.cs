using System;
using System.Threading.Tasks;

namespace Business.Services.Base
{
    public interface IMessageBrokerService : IDisposable
    {
        Task PublishAsync<T>(string channel, T message);
        Task SubscribeAsync<T>(string channel, Func<T, Task> handler);
    }
}
