namespace Area_Manager.Core.Interfaces
{
	internal interface IAreaCalculator
	{
		List<Coordinate> FindArea(Coordinate coordinate, double initialHeight = 100);
	}
}
