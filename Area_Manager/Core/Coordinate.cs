using Area_Manager.Core.Interfaces;

namespace Area_Manager.Core
{
	internal struct Coordinate : ICoordinate
	{
		public int RoundDigits { get; set; } = 6;

		private double _latitude;
		private double _longitude;

		public double Latitude
		{
			get => _latitude;
			set => _latitude = Math.Round(value, RoundDigits);
		}

		public double Longitude
		{
			get => _longitude;
			set => _longitude = Math.Round(value, RoundDigits);
		}

		public Coordinate(double latitude, double longitude)
		{
			_latitude = Math.Round(latitude, RoundDigits);
			_longitude = Math.Round(longitude, RoundDigits);
			RoundDigits = 6;
		}

		public Coordinate(double latitude, double longitude, int roundDigits)
		{
			_latitude = Math.Round(latitude, roundDigits);
			_longitude = Math.Round(longitude, roundDigits);
			RoundDigits = roundDigits;
		}

		public bool AreNeighbors(Coordinate coordinate, double checkDistance = 200)
		{
			double latitude = coordinate.Latitude;
			double longitude = coordinate.Longitude;

			double distance = Math.Sqrt(Math.Pow(Latitude - latitude, 2) + Math.Pow(Longitude - longitude, 2));
			return distance <= checkDistance / 111320;
		}

		public List<Coordinate> GetNeighbors(double distance = 200)
		{
			List<Coordinate> result = new List<Coordinate>();

			for (int dLat = -1; dLat < 2; dLat++)
			{
				for (int dLon = -1; dLon < 2; dLon++)
				{
					if (dLat == 0 && dLon == 0) { continue; }

					double newLat = Latitude + dLat * (distance / 111320);
					double latInRadians = Latitude * Math.PI / 180;
					double newLon = Longitude + dLon * (distance / (111320 * Math.Cos(latInRadians)));

					result.Add(new Coordinate(newLat, newLon));
				}
			}

			return result;
		}

		public override string ToString() => $"[{Latitude}, {Longitude}]";

		// Реализация IEquatable<T> - полностью избегает боксинга
		public bool Equals(Coordinate other) => Latitude == other.Latitude && Longitude == other.Longitude;

		// Перегрузка для object (вызывается редко)
		public override bool Equals(object? obj) => obj is Coordinate other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);

		// Операторы для удобства
		public static bool operator ==(Coordinate left, Coordinate right) => left.Equals(right);
		public static bool operator !=(Coordinate left, Coordinate right) => !left.Equals(right);
	}
}
