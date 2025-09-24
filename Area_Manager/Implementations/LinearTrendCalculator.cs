using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations
{
	internal class LinearTrendCalculator : ITrendCalculator
	{
		public double CalculateTrend(List<double> values)
		{
			if (values == null || values.Count < 2)
				return 0;

			return (values.Last() - values.First()) / (values.Count - 1);
		}
	}
}
