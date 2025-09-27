using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations.EMA
{
	internal class EMAPredictor : IPredictor
	{
		private readonly IMovingAverage _movingAverage;
		private readonly ITrendCalculator _trendCalculator;
		private readonly double _trendFactor;

		public EMAPredictor(IMovingAverage movingAverage, ITrendCalculator trendCalculator, double trendFactor = 1.0)
		{
			_movingAverage = movingAverage;
			_trendCalculator = trendCalculator;
			_trendFactor = trendFactor;
		}

		public (List<double> smoothedValues, List<double> predictions) Predict(List<double> values, int predictionSteps = 3)
		{
			var smoothedValues = _movingAverage.Calculate(values);
			var recentValues = smoothedValues.TakeLast(Math.Min(10, smoothedValues.Count)).ToList();

			var trend = _trendCalculator.CalculateTrend(recentValues) * _trendFactor;
			var lastValue = smoothedValues.Last();

			var predictions = new List<double>();
			for (int i = 1; i <= predictionSteps; i++)
				predictions.Add(lastValue + trend * i);

			return (smoothedValues, predictions);
		}
	}
}
