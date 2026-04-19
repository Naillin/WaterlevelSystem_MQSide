namespace Area_Manager.Core.Interfaces.EMA
{
	internal interface IMovingAverage
	{
		IList<double> Calculate(List<double> values);
	}
}
