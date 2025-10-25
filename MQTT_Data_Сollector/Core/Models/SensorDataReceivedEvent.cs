namespace MQTT_Data_Сollector.Core.Models
{
	internal class SensorDataReceivedEvent
	{
		public string? TopicPath { get; set; }

		public double Value { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
