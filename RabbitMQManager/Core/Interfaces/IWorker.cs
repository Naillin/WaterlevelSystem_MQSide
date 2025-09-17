namespace RabbitMQManager.Core.Interfaces
{
	public interface IWorker : IDisposable
	{
		Task StartAsync(CancellationToken cancellationToken = default);

		Task StopAsync(CancellationToken cancellationToken = default);
	}
}
