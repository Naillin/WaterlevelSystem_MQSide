using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations.Metrics
{
	internal class MetricMAE : IMetric
	{
		public double Calculate(List<double> actualData, List<double> maData) => actualData
			.Zip(maData, (a, b) => Math.Abs(a - b))
			.Sum() / actualData.Count();
	}
}
