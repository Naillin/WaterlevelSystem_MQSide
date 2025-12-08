using DispatcherAreaManager.Core.Models;
using RabbitMQ.Client;

namespace DispatcherAreaManager.Core.Interfaces
{
	internal interface IQueueManagerService
	{
		public Task AddData(SensorDataReceivedEvent? sensorData, CancellationToken cancellationToken = default);

		public IReadOnlyDictionary<string, QueueDeclareOk> GetSensors();
	}
}
