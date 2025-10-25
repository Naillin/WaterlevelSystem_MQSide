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
			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation($"Checking sensors");
				await GetAndAnalysis(stoppingToken);

				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}

		private async Task GetAndAnalysis(CancellationToken cancellationToken = default)
		{
			var sensorDatas = _sensorDataService.GetSensorData().Select(s => s.Value).ToList();

			// СОЗДАЕМ задачи для всех сенсоров
			var analysisTasks = sensorDatas
				.Where(sensor => sensor.Data.Count() >= 10)
				.Select(sensor => TryAnalysisAndSendAsync(sensor, cancellationToken))
				.ToList();

			// ЖДЕМ завершения ВСЕХ задач перед выходом из метода
			await Task.WhenAll(analysisTasks);

			_logger.LogInformation($"Completed analysis of {analysisTasks.Count} sensors");
		}

		private async Task TryAnalysisAndSendAsync(SensorDataDto sensor, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation($"Analysis sensor - [{sensor.TopicPath}]");

			try
			{
				var coords = await _floodDataService.Analysis(sensor, cancellationToken);

				if (!coords.Any())
					return;

				var floodAreaCalculatedEvent = new FloodAreaCalculatedEvent
				{
					TopicPath = sensor.TopicPath,
					Coordinates = string.Join(',', coords)
				};

				await _messageProducer.PublishAsync<FloodAreaCalculatedEvent>(
					floodAreaCalculatedEvent,
					"",
					cancellationToken
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in FloodWorker!");
			}
		}
	}
}
