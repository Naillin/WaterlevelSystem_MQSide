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
		private readonly IMovingAverage _movingAverage;

		private readonly double _addNumber = 0.5;

		public FloodDataService(IPredictor predictor, IMetric metric, IAreaCalculator areaCalculator, IMovingAverage movingAverage, ILogger<FloodDataService> logger)
		{
			_predictor = predictor;
			_metric = metric;
			_areaCalculator = areaCalculator;
			_movingAverage = movingAverage;

			_logger = logger;
		}
		
		public async Task<(IList<Coordinate> coordinates, IList<double> smoothed, IList<ValueAtTime> predictions)> Analysis(SensorDataDto sensorData, CancellationToken cancellationToken = default)
		{
			using var timeoutCts = GetCombineCancellationToken(cancellationToken);
			try
			{
				// Данные только уровня
				var data = sensorData.Data
					.Select(dataUnit => dataUnit.Value)
					.ToList();

				// EMA - список. По задумке этот список должен получиться меньше, чем список данных с фактическими значениями
				var smoothed = _movingAverage.Calculate(data);
				if (!smoothed.Any()) 
					return new(Array.Empty<Coordinate>(), Array.Empty<double>(), Array.Empty<ValueAtTime>());
				
				// EMA - список склееный с временным рядом фактических значений
				var smoothedAtTime = sensorData.Data
					.TakeLast(smoothed.Count)
					.Zip(smoothed, (sensorUnit, smoothValue) => new ValueAtTime(smoothValue, sensorUnit.Date))
					.ToList();
				// Список предсказанных значений данные-время
				var predictions = _predictor.Predict(smoothedAtTime, 3);
				if (!predictions.Any())
					return new(Array.Empty<Coordinate>(), smoothed, Array.Empty<ValueAtTime>());
				
				double metric = _metric.Calculate(data, smoothed);
				double p3baff = predictions.Last().Value + metric;
				double buffedNumber = smoothed.Last() + _addNumber;

				var allValues = new[] { smoothed.Last() }.Concat(predictions.Select(pred => pred.Value));
				_logger.LogInformation(string.Join("\n", allValues.Select((v, i) => $"p{i} = {v}")) +
				                       $"Metric = {metric}, p3_buffed = {p3baff}, buffNumber = {buffedNumber}");

				// F_last > (E_last + addNumber) && (predict3 + MAE) >= height
				if (data.Last() >= buffedNumber && p3baff >= sensorData.Altitude)
				{
					_logger.LogInformation($"Conditions met for topic {sensorData.TopicPath}: f1 = {data.Last()} >= buffNumber = {buffedNumber} and p3_buffed = {p3baff} >= altitude = {sensorData.Altitude}.");
					var coordinates = await _areaCalculator.FindArea(sensorData.Coordinate, predictions.Last().Value, timeoutCts.Token);
					return (coordinates, smoothed, predictions);
				}
				else
				{
					_logger.LogInformation($"Conditions NOT met for topic {sensorData.TopicPath}: f1 = {data.Last()} >= buffNumber = {buffedNumber} and p3_buffed = {p3baff} >= altitude = {sensorData.Altitude}.");
					return (Array.Empty<Coordinate>(), smoothed, predictions);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Analysis is cancelled.");
				return new(Array.Empty<Coordinate>(), Array.Empty<double>(), Array.Empty<ValueAtTime>());
			}
		}
		
		private CancellationTokenSource GetCombineCancellationToken(CancellationToken globalToken) =>
			CancellationTokenSource.CreateLinkedTokenSource(
				globalToken,
				new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token // todo: убрать хардкод
			);
	}
}
