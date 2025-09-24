using Area_Manager.Core.Interfaces;

namespace Area_Manager.Implementations.Metrics
{
	internal class MetricR2 : IMetric
	{
		public double Calculate(List<double> actualData, List<double> maData)
		{
			// Вычисляем среднее значение actual
			double meanActual = actualData.Average();

			// Вычисляем общую сумму квадратов (SS Total)
			double ssTotal = actualData.Sum(a => Math.Pow(a - meanActual, 2));

			// Вычисляем сумму квадратов ошибок (SS Residual)
			double ssResidual = actualData
				.Zip(maData, (a, b) => Math.Pow(a - b, 2))
				.Sum();

			return 1 - (ssResidual / ssTotal);
		}
	}
}
