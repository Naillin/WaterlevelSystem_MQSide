namespace Area_Manager
{
	internal class ExponentialMovingAverage
	{
		private readonly double _alpha;
		private readonly double _windowSize;

		public ExponentialMovingAverage(double smoothing, double windowSize)
		{
			_windowSize = windowSize;

			_alpha = smoothing / (_windowSize + 1);
		}

		public List<double> Calculate(List<double> values) //возможно возвращать стоит List<double?>
		{
			if (values == null || !values.Any())
				return new List<double>();

			double lastEMA = values[0];

			return values
				.Select((value, index) =>
				{
					if (index == 0)
						return value;
					else
					{
						lastEMA = CalculateActual(value, lastEMA);
						return lastEMA;
					}
				})
				.ToList();
		}

		private double CalculateActual(double value, double last) => _alpha * value + (1 - _alpha) * last;
	}
}
