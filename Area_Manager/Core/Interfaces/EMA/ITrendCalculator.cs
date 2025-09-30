namespace Area_Manager.Core.Interfaces.EMA
{
	internal interface ITrendCalculator
	{
		double CalculateTrend(List<double> values);
	}
}
