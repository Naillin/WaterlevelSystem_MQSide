namespace RabbitMQManager.Core.Interfaces
{
	public interface IConnector : IDisposable
	{
		Task ConnectAsync(CancellationToken cancellationToken = default);

		Task DisconnectAsync(CancellationToken cancellationToken = default);

		Task ReconnectAsync(CancellationToken cancellationToken = default);
	}
}
