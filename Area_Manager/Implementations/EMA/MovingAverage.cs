using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations.EMA
{
	internal class ExponentialMovingAverage : IMovingAverage
	{
		private readonly double _alpha;

		public ExponentialMovingAverage(double smoothing, double windowSize) => _alpha = smoothing / (windowSize + 1);

		public List<double> Calculate(List<double> values) //возможно возвращать стоит List<double?>
		{
			if (values == null || !values.Any())
				return new List<double>();

			double lastEMA = values[0];

			return values
				.Skip(1)
				.Select(value =>
				{
					lastEMA = CalculateActual(value, lastEMA);
					return lastEMA;
				})
				.ToList();
		}

		private double CalculateActual(double value, double last) => _alpha * value + (1 - _alpha) * last;
	}
}
