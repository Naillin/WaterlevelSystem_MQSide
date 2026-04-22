namespace Contracts.Models.RabbitMQ;

public class FloodAreaCalculatedEvent
{
	public string? TopicPath { get; set; }

	public string? Coordinates { get; set; }
	
	public List<double>? EmaData { get; set; }
	
	public List<ValueAtTime>? PredictionData { get; set; }
}

