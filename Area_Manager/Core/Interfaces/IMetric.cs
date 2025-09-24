namespace Area_Manager.Core.Interfaces
{
	internal interface IMetric
	{
		double Calculate(List<double> actualData, List<double> maData);
	}
}
