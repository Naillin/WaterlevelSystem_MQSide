namespace Area_Manager.Core.Interfaces.EMA
{
	internal interface IMovingAverage
	{
		List<double> Calculate(List<double> values);
	}
}
