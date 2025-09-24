namespace Area_Manager.Core.Interfaces
{
	internal interface ICoordinate : IEquatable<Coordinate>
	{
		public int RoundDigits { get; set; }

		public double Latitude { get; set; }

		public double Longitude { get; set; }

		bool AreNeighbors(Coordinate coordinate, double checkDistance = 200);

		List<Coordinate> GetNeighbors(double distance = 200);
	}
}
