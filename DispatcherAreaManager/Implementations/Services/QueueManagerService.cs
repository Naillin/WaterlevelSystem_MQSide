using DispatcherAreaManager.Core.Interfaces;
using DispatcherAreaManager.Core.Models;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ;

namespace DispatcherAreaManager.Implementations.Services
{
	internal class QueueManagerService : IQueueManagerService
	{
		private readonly ILogger<QueueManagerService> _logger;
		private readonly IMessageQueueManager _queueManager;
		private readonly IKubernetesService _kubernetes;

		private readonly Dictionary<string, Queue<SensorDataReceivedEvent>> _sensorData = new();
		private readonly Dictionary<string, string> _sensorToQueue = new();

		public QueueManagerService(ILogger<QueueManagerService> logger, IMessageQueueManager queueManager, IKubernetesService kubernetes)
		{
			_queueManager = queueManager;
			_kubernetes = kubernetes;

			_logger = logger;
		}

		//переделать метод!!!!!!!!!!!!!!!!!!!!!!!!
		//если нет такого топика в списке, то должна появиться очередь и новый pod для этого дачтика
		//очередь - исчезает если слушатели остуствуют
		//поду через env передается имя очереди
		public void AddData(SensorDataReceivedEvent sensorData)
		{
			if (string.IsNullOrWhiteSpace(sensorData.TopicPath))
				throw new InvalidOperationException("TopicPath is null!");

			_sensorData[sensorData.TopicPath].Append(sensorData);
		}

		public IReadOnlyDictionary<string, Queue<SensorDataReceivedEvent>> GetSensors() => _sensorData.AsReadOnly();
	}
}
