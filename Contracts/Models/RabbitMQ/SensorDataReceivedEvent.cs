namespace Contracts.Models.RabbitMQ;

public class SensorDataReceivedEvent
{
	public string? TopicPath { get; set; }

	public double Value { get; set; }

	public DateTimeOffset Date { get; set; }
}

