namespace MQTT_Data_Сollector.Core.Models
{
	internal class MqttMessageReceivedEventArgs : EventArgs
	{
		public string? Topic { get; set; }

		public string? Payload { get; set; }
	}
}
