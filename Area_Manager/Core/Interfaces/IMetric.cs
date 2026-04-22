namespace Area_Manager.Core.Interfaces;

internal interface IMetric
{
	double Calculate(IList<double> actualData, IList<double> maData);
}