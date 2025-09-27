namespace Area_Manager.Core.Models
{
	internal class SensorDataDto
	{
		public string TopicPath { get; set; } = string.Empty;

		public Coordinate Coordinate { get; set; }

		public double Altitude = 0.0;

		public List<(double, long)> Data = new();
	}
}
