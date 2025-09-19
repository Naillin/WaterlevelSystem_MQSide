namespace Area_Manager
{
	internal class Predictor
	{
		private readonly double _windowSize;
		private readonly double _slopeFactor;
		private readonly ExponentialMovingAverage _exponentialMovingAverage;

		public Predictor(double smoothing, double windowSize, double slopeFactor)
		{
			_windowSize = windowSize;
			_slopeFactor = slopeFactor;

			_exponentialMovingAverage = new ExponentialMovingAverage(smoothing, _windowSize);
		}
		//все это какая то хуета!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		public (List<double>, List<double>) Calculate(List<double> values, int iter = 4)
		{
			List<double> emaValues = _exponentialMovingAverage.Calculate(values);
			List<double> lastValues = emaValues.TakeLast((int)_windowSize).ToList();
			double slope = CalculateSlope(lastValues);

			List<double> predictedValues = new List<double>();
			for (int i = 1; i < iter; i++)
				predictedValues.Add(emaValues.Last() + slope * i * _slopeFactor);

			return (emaValues, predictedValues);
		}

		private double CalculateSlope(List<double> values) => (values.Last() - values.First()) / (_windowSize - 1);
	}
}
