using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ;
using System.Text.Json;
using RabbitMQManager.Implementations;

namespace Area_Manager.Workers
{
	internal class SensorDataWorker : IHostedService
	{
		private readonly ILogger<SensorDataWorker> _logger;
		private readonly IMessageConsumer _messageConsumer;
		private readonly ISensorDataService _sensorDataService;
		private readonly string _queue;

		private string _tag = string.Empty;

		public SensorDataWorker(string queue, IMessageConsumer messageConsumer, ISensorDataService sensorDataService, ILogger<SensorDataWorker> logger)
		{
			_queue = queue;
			_messageConsumer = messageConsumer;
			_sensorDataService = sensorDataService;

			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting sensor data worker");

			_tag = await _messageConsumer.StartConsumingAsync(
				_queue,
				HandleResponseMessage,
				cancellationToken
			);
			
			while (!cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation("Checking sensors for expiring");
				CheckExpiredTopics();

				await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
			}
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping sensor data worker");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task HandleResponseMessage(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				if(context != null && string.IsNullOrWhiteSpace(context.Body))
					return;

				var sensorEvent = JsonSerializer.Deserialize<SensorDataReceivedEvent>(context!.Body);

				if (sensorEvent == null)
					throw new InvalidOperationException($"Failed to deserialize sensor event.");

				await _sensorDataService.AddData(sensorEvent, cancellationToken);

				_logger.LogInformation($"Added data for topic [{sensorEvent.TopicPath}]. Data: {sensorEvent.Value}; {sensorEvent.Date}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling response message in sensor data worker");
			}
		}
		
		private void CheckExpiredTopics()
		{
			// Выносим в переменную (в будущем это легко забрать из appsettings.json)
			TimeSpan expirationThreshold = TimeSpan.FromHours(48);// топики не получающие данные уже более 48 часов. todo: УБРАТЬ ХАРДКОД!!!!!!
			DateTimeOffset cutoffTime = DateTimeOffset.UtcNow - expirationThreshold;

			var expiredSensors = _sensorDataService.GetSensorData()
				.Where(kvp => kvp.Value.Data.Any() && kvp.Value.Data.LastOrDefault().Date < cutoffTime)
				.Select(kvp => kvp.Key)
				.ToList();
    
			if (expiredSensors.Any())
			{
				_logger.LogInformation($"Found {expiredSensors.Count} expired sensors. Deleting...");
				_sensorDataService.DeleteSensorsAsync(expiredSensors);
			}
		}
	}
}
