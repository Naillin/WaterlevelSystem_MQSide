using DispatcherAreaManager.Core.Interfaces;
using DispatcherAreaManager.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ;
using System.Text.Json;

namespace DispatcherAreaManager.Implementations.Workers
{
	internal class SensorDataWorker : IHostedService
	{
		private readonly ILogger<SensorDataWorker> _logger;
		private readonly IQueueManagerService _queueManager;
		private readonly IMessageConsumer _messageConsumer;
		private readonly string _queue;
		private string _tag = string.Empty;

		public SensorDataWorker(
			ILogger<SensorDataWorker> logger,
			IMessageConsumer messageConsumer,
			IQueueManagerService queueManager,
			string queue)
		{
			_messageConsumer = messageConsumer;
			_queueManager = queueManager;
			_queue = queue;

			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting kuber worker");

			_tag = await _messageConsumer.StartConsumingAsync(
				_queue,
				MessageHandler,
				cancellationToken
			);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping kuber worker");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task MessageHandler(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				if (context != null && string.IsNullOrWhiteSpace(context.Body))
					return;

				var sensorEvent = JsonSerializer.Deserialize<SensorDataReceivedEvent>(context!.Body); // сделать расшифровку заголовка сообщения в нем получать имя топика

				if (sensorEvent == null)
					throw new InvalidOperationException($"Failed to deserialize sensor event.");

				_queueManager.AddData(sensorEvent);

				_logger.LogInformation($"Added data for topic [{sensorEvent.TopicPath}]. Data: {sensorEvent.Value}; {sensorEvent.Timestamp}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling response message in sensor data worker");
			}
		}
	}
}
