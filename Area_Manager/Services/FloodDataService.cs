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
		
		public async Task<IList<Coordinate>> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default)
		{
			using var timeoutCts = GetCombineCancellationToken(cancellationToken);
			try
			{
				// Берем только данные уровня
				var data = sensorData.Data
					.Select(s => s.Value)
					.ToList();

				var (smoothedValues, predictions) = _predictor.Predict(data, 3);

				double metric = _metric.Calculate(data, smoothedValues);
				double p3baff = predictions.Last() + metric;
				double buffedNumber = smoothedValues.Last() + _addNumber;

				var allValues = new[] { smoothedValues.Last() }.Concat(predictions);
				_logger.LogInformation(string.Join("\n", allValues.Select((v, i) => $"p{i} = {v}")) +
				                       $"Metric = {metric}, p3_buffed = {p3baff}, buffNumber = {buffedNumber}");

				// F_last > (E_last + addNumber) && (predict3 + MAE) >= height
				if (data.Last() >= buffedNumber && p3baff >= sensorData.Altitude)
				{
					_logger.LogInformation($"Conditions met for topic {sensorData.TopicPath}: f1 = {data.Last()} >= buffNumber = {buffedNumber} and p3_buffed = {p3baff} >= altitude = {sensorData.Altitude}.");
					return await _areaCalculator.FindArea(sensorData.Coordinate, predictions.Last(), timeoutCts.Token);
				}

				_logger.LogInformation($"Conditions NOT met for topic {sensorData.TopicPath}: f1 = {data.Last()} >= buffNumber = {buffedNumber} and p3_buffed = {p3baff} >= altitude = {sensorData.Altitude}.");
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Analysis is cancelled.");
				return new List<Coordinate>();
			}
			
			return new List<Coordinate>();
		}
		
		private CancellationTokenSource GetCombineCancellationToken(CancellationToken globalToken) =>
			CancellationTokenSource.CreateLinkedTokenSource(
				globalToken,
				new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token // todo: убрать хардкод
			);
	}
}
