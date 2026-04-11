using System.Collections.Concurrent;
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
		private readonly ConcurrentDictionary<string, SensorDataDto> _sensorData = new();

		public FloodWorker(ISensorDataService sensorDataService, IFloodDataService floodDataService, IMessageProducer messageProducer, ILogger<FloodWorker> logger)
		{
			_sensorDataService = sensorDataService;
			_floodDataService = floodDataService;
			_messageProducer = messageProducer;

			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation($"Checking sensors");
				await GetAndAnalysis(cancellationToken);
				DeleteExpiredTopics();

				await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
			}
		}

		private async Task GetAndAnalysis(CancellationToken cancellationToken = default)
		{
			var sensorDatas = _sensorDataService.GetSensorData()
				.Select(s => s.Value)
				.Where(sensor => sensor.Data.Count() >= 10)
				.ToList();

			// Параметры параллелизма: максимум 5 задач одновременно
			var parallelOptions = new ParallelOptions
			{
				MaxDegreeOfParallelism = 5,
				CancellationToken = cancellationToken
			};

			// Parallel.ForEachAsync сам распределит задачи и дождется их завершения
			await Parallel.ForEachAsync(sensorDatas, parallelOptions, async (sensor, ct) =>
			{
				Guid sessionGuid = Guid.NewGuid(); // возможно стоит передавать этот гуид прямиком до GDALPython
        
				try
				{
					await TryAnalysisAndSendAsync(sensor, sessionGuid, ct);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, $"Error analyzing sensor {sensor.TopicPath}");
				}
			});

			_logger.LogInformation($"Completed analysis of {sensorDatas.Count} sensors");
		}

		private async Task TryAnalysisAndSendAsync(SensorDataDto sensor, Guid guid, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation($"Analysis sensor - [{sensor.TopicPath}]. Guid - {guid}");

			// Кэш, что бы не считать еще раз, если новых данных в Data нет
			if (_sensorData.TryGetValue(sensor.TopicPath, out var sensorData) 
			    && sensorData.Data.SequenceEqual(sensor.Data))
				return;
			
			try
			{
				var coords = await _floodDataService.Analysis(sensor, cancellationToken);

				_sensorData[sensor.TopicPath] = new SensorDataDto 
				{
					TopicPath = sensor.TopicPath,
					Data = sensor.Data.ToList()
				};
				//_sensorData[sensor.TopicPath] = sensor with { Data = sensor.Data.ToList() };
				
				var floodAreaCalculatedEvent = new FloodAreaCalculatedEvent
				{
					TopicPath = sensor.TopicPath,
					Coordinates = coords.Any() // если координат нет, значит и затопления нет - отправялем null.
						? string.Join(',', coords)
						: null
				};

				await _messageProducer.PublishAsync<FloodAreaCalculatedEvent>(
					floodAreaCalculatedEvent,
					"",
					cancellationToken
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error in FloodWorker! Guid - {guid}");
			}
		}

		private void DeleteExpiredTopics()
		{
			long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			long threshold = currentUnixTime - (48 * 60 * 60); // топики не получающие данные уже более 48 часов. УБРАТЬ ХАРДКОД!!!!!!

			var expiredSensors = _sensorData
				.Where(kvp => 
					!kvp.Value.Data.Any() ||
					kvp.Value.Data.LastOrDefault().Timestamp < threshold)
				.Select(kvp => kvp.Key)
				.ToList();
			
			foreach (var sensor in expiredSensors)
				_sensorData.TryRemove(sensor, out _);
		}
	}
}
