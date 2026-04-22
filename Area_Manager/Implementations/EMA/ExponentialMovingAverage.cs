using Area_Manager.Core.Interfaces.EMA;

namespace Area_Manager.Implementations.EMA;

internal class ExponentialMovingAverage : IMovingAverage
{
	private readonly double _alpha;

	public ExponentialMovingAverage(double smoothing = 2.0, double windowSize = 7.0) => _alpha = smoothing / (windowSize + 1);

	public IList<double> Calculate(List<double> values)
	{
		if (!values.Any())
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