using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces.MQ;

namespace Area_Manager.Workers
{
	internal class FloodWorker : BackgroundService
	{
		private readonly ILogger<FloodWorker> _logger;
		private readonly ISensorDataService _sensorDataService;
		private readonly IFloodDataService _floodDataService;
		private readonly IMessageProducer _messageProducer;

		public FloodWorker(ISensorDataService sensorDataService, IFloodDataService floodDataService, IMessageProducer messageProducer, ILogger<FloodWorker> logger)
		{
			_sensorDataService = sensorDataService;
			_floodDataService = floodDataService;
			_messageProducer = messageProducer;

			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
		{
			_logger.LogInformation("Starting flood worker");

			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					await AnalysisAndSend(stoppingToken);

					await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in flood worker!");
			}
			finally
			{
				_logger.LogInformation("Flood worker stopped.");
			}
		}

		private async Task AnalysisAndSend(CancellationToken cancellationToken = default)
		{
			var sensorDatas = _sensorDataService.GetSensorData().Select(s => s.Value).ToList();

			foreach (var sensor in sensorDatas)
			{
				_logger.LogInformation($"Analysis of topic - [{sensor.TopicPath}].");

				if (sensor.Data.Count() < 10)
					continue;

				var coords = await _floodDataService.Analysis(sensor, cancellationToken);

				if (!coords.Any())
					continue;

				var coordsList = string.Join(",", coords);
				var floodAreaCalculatedEvent = new FloodAreaCalculatedEvent
				{
					TopicPath = sensor.TopicPath,
					Coordinates = coordsList
				};

				await _messageProducer.PublishAsync<FloodAreaCalculatedEvent>(
					floodAreaCalculatedEvent,
					"",
					cancellationToken
				);
			}
		}
	}
}
