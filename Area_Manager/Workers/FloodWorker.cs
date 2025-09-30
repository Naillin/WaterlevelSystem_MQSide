using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Interfaces.EMA;
using Area_Manager.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Area_Manager.Workers
{
	internal class FloodWorker : BackgroundService
	{
		private readonly ILogger<FloodWorker> _logger;
		private readonly ISensorDataService _sensorDataService; //писать IHostedService делает не очевидным что класть в конструктор
		private readonly IPredictor _predictor;
		private readonly IMetric _metric;
		private readonly FloodDataService _floodDataService;
		private readonly IAreaCalculator _areaCalculator;

		private readonly double _addNumber = 0.5;

		public FloodWorker(ISensorDataService sensorDataService, IPredictor predictor, IMetric metric, ILogger<FloodWorker> logger)
		{
			_sensorDataService = sensorDataService;
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
					await Analysis(stoppingToken);
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

		private async Task Analysis(CancellationToken cancellationToken = default)
		{
			var sensorData = _sensorDataService.GetSensorData().Select(s => s.Value).ToList();

			foreach (var sensor in sensorData)
			{
				if (sensor.Data.Count() < 10)
					continue;

				// Берем только данные уровня
				var data = sensor.Data
					.Select(s => s.Item1)
					.ToList();

				var (smoothedValues, predictions) = _predictor.Predict(data, 3);

				double metric = _metric.Calculate(data, smoothedValues);
				double p3baff = predictions.Last() + metric;
				double buffNumber = predictions.First() + _addNumber;

				// F_last > (E_last + buffNumber) & (predict3 + MAE) >= height 
				if (smoothedValues.Last() >= buffNumber && p3baff >= sensor.Altitude)
				{
					var area = _areaCalculator.FindArea(sensor.Coordinate, predictions.Last());
				}
			}
		}
	}
}
