using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations.Metrics
{
	internal class MetricMSE : IMetric
	{
		public double Calculate(List<double> actualData, List<double> maData) => actualData
			.Zip(maData, (a, b) => Math.Pow(a - b, 2))
			.Sum() / actualData.Count();
	}
}
