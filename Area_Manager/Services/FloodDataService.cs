using Area_Manager.Core;
using Area_Manager.Core.Interfaces;
using Area_Manager.Core.Interfaces.EMA;
using Area_Manager.Core.Models;
using Microsoft.Extensions.Logging;

namespace Area_Manager.Services
{
	internal class FloodDataService : IFloodDataService
	{
		private readonly ILogger<FloodDataService> _logger;
		private readonly IPredictor _predictor;
		private readonly IMetric _metric;
		private readonly IAreaCalculator _areaCalculator;

		private readonly double _addNumber = 0.5;

		public FloodDataService(IPredictor predictor, IMetric metric, IAreaCalculator areaCalculator, ILogger<FloodDataService> logger)
		{
			_predictor = predictor;
			_metric = metric;
			_areaCalculator = areaCalculator;

			_logger = logger;
		}

		public async Task<List<Coordinate>> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default)
		{
			// Берем только данные уровня
			var data = sensorData.Data
				.Select(s => s.Item1)
				.ToList();

			var (smoothedValues, predictions) = _predictor.Predict(data, 3);

			double metric = _metric.Calculate(data, smoothedValues);
			double p3baff = predictions.Last() + metric;
			double buffNumber = predictions.First() + _addNumber;

			// F_last > (E_last + buffNumber) & (predict3 + MAE) >= height 
			if (smoothedValues.Last() >= buffNumber && p3baff >= sensorData.Altitude)
				return _areaCalculator.FindArea(sensorData.Coordinate, predictions.Last());
			else
				return new List<Coordinate>();
		}
	}
}
