using DispatcherAreaManager.Core.Interfaces;
using DispatcherAreaManager.Core.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQManager.Core.Interfaces.MQ;

namespace DispatcherAreaManager.Implementations.Services
{
	internal class QueueManagerService : IQueueManagerService
	{
		private readonly ILogger<QueueManagerService> _logger;
		private readonly IMessageQueueManager _queueManager;
		private readonly IMessageProducer _messageProducer;

		//private readonly Dictionary<string, Queue<SensorDataReceivedEvent>> _sensorData = new();
		private readonly Dictionary<string, QueueDeclareOk> _sensorToQueue = new();

		public QueueManagerService(ILogger<QueueManagerService> logger, IMessageQueueManager queueManager, IMessageProducer messageProducer)
		{
			_queueManager = queueManager;
			_messageProducer = messageProducer;

			_logger = logger;
		}

		//Возможно дополнить метод нужно
		//поду через env передается имя очереди
		public async Task AddData(SensorDataReceivedEvent? sensorData, CancellationToken cancellationToken = default)
		{
			if (sensorData is null)
				throw new NullReferenceException("SensorData is null!");
			
			string? topicPath = sensorData.TopicPath;
			if (string.IsNullOrWhiteSpace(topicPath))
				throw new NullReferenceException("TopicPath is null!");

			if (_sensorToQueue.TryGetValue(topicPath, out var queue))
			{
				try
				{
					await _messageProducer.PublishAsync<SensorDataReceivedEvent>(sensorData,
						"",
						queue.QueueName,
						cancellationToken
					);
				}
				catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) // очереди больше нет, удаляем такую запись о топике
				{
					_sensorToQueue.Remove(topicPath);
				}
			}
			else
				_sensorToQueue.Add(topicPath, await _queueManager.AnonymousQueueDeclareAsync(cancellationToken));
		}

		public IReadOnlyDictionary<string, QueueDeclareOk> GetSensors() => _sensorToQueue.AsReadOnly();
	}
}
