namespace Area_Manager.Core.Interfaces
{
	internal interface IAreaCalculator
	{
		Task<IList<Coordinate>> FindArea(Coordinate coordinate, double initialHeight = 100,
			CancellationToken cancellationToken = default);
	}
}
