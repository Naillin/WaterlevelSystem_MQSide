namespace RabbitMQManager.Core.Interfaces.MQ
{
	public interface IMessageProducer : IConnector, IDisposable
	{
		Task PublishAsync<T>(T message, string routingKey = "", CancellationToken cancellationToken = default);


		Task PublishAsync(string message, string routingKey, string messageType, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default);

		Task PublishAsync<T>(T message, string exchangeName, string routingKey = "", CancellationToken cancellationToken = default);


		Task PublishAsync(string message, string exchangeName, string routingKey, string messageType, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default);
	}
}
