using Area_Manager.Core.Interfaces;
using Area_Manager.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Area_Manager.Workers
{
	internal class FloodWorker : BackgroundService
	{
		private readonly ILogger<FloodWorker> _logger;
		private readonly SensorDataWorker _sensorDataWorker; //писать IHostedService делает не очевидным что класть в конструктор
		private readonly IPredictor _predictor;
		private readonly IMetric _metric;
		private readonly FloodDataService _floodDataService;
		private readonly IAreaCalculator _areaCalculator;

		private readonly double _addNumber = 0.5;

		public FloodWorker(SensorDataWorker sensorDataWorker, IPredictor predictor, IMetric metric, ILogger<FloodWorker> logger)
		{
			_sensorDataWorker = sensorDataWorker;
			_predictor = predictor;
			_metric = metric;

			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
		{
			_logger.LogInformation("Starting flood worker");

			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					await Analysis();
				}

				await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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

		private async Task Analysis()
		{
			var sensorData = _sensorDataWorker.GetSensorData();

			foreach (var sensor in sensorData)
			{
				if (sensor.Value.Count() < 10)
					continue;

				var data = sensor.Value
					.Select(s => s.Value)
					.ToList();

				var (smoothedValues, predictions) = _predictor.Predict(data, 3);

				double metric = _metric.Calculate(data, smoothedValues);
				double p3baff = predictions.Last() + metric;
				double buffNumber = predictions.First() + _addNumber;

				// F_last > (E_last + buffNumber) & (predict3 + MAE) >= height 
				if (smoothedValues.Last() >= buffNumber && p3baff >= altitude)
				{
					var area = _areaCalculator.FindArea(coordinate, predictions.Last());
				}
			}
		}
	}
}
