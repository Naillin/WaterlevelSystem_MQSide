namespace Contracts.Models;

public class SensorDataDto //todo: переехать на структуру(?)
{
	public string TopicPath { get; set; } = string.Empty;

	public Coordinate Coordinate { get; set; }

	public double Altitude { get; set; } = 0.0;

	public List<ValueAtTime> Data { get; set; } = new();
}
