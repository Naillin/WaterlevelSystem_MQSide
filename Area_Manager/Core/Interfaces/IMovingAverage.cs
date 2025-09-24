namespace Area_Manager.Core.Interfaces
{
	internal interface IMovingAverage
	{
		List<double> Calculate(List<double> values);
	}
}
