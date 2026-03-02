namespace Area_Manager.Core.Models
{
	internal class SensorDataDto //переехать на структуру(?)
	{
		public string TopicPath { get; set; } = string.Empty;

		public Coordinate Coordinate { get; set; }

		public double Altitude { get; set; } = 0.0;

		public List<(double Value, long Timestamp)> Data { get; set; } = new();
	}
}
