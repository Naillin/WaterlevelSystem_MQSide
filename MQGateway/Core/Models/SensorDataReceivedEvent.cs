
namespace MQGateway.Core.Models
{
	internal class SensorDataReceivedEvent
	{
		public string? TopicPath { get; set; }

		public double Value { get; set; }

		public long Timestamp { get; set; }
	}
}
