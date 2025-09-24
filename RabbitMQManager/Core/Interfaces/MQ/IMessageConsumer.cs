using RabbitMQManager.Core.Implementations;

namespace RabbitMQManager.Core.Interfaces.MQ
{
	public interface IMessageConsumer : IConnector, IDisposable
	{
		Task<string> StartConsumingAsync(string queueName, Func<MessageContext, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default);

		Task StopConsumingAsync(string tag, CancellationToken cancellationToken = default);
	}
}
