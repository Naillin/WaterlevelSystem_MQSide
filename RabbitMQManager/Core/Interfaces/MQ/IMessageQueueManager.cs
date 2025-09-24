using RabbitMQ.Client;

namespace RabbitMQManager.Core.Interfaces.MQ
{
	public interface IMessageQueueManager : IConnector, IDisposable
	{
		Task<QueueDeclareOk> CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, bool noWait = false, CancellationToken cancellationToken = default);

		Task<QueueDeclareOk> AnonymousQueueDeclareAsync(CancellationToken cancellationToken = default);

		Task CreateExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Direct, bool durable = true, bool autoDelete = false, bool noWait = false, CancellationToken cancellationToken = default);

		Task BindQueueAsync(string queueName, string exchangeName, string routingKey = "", CancellationToken cancellationToken = default);

		Task DeleteQueue(string queueName);

		Task DeleteExchange(string exchangeName);
	}
}
