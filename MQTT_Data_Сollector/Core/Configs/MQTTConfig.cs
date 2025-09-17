namespace MQTT_Data_Сollector.Core.Configs
{
	internal class MQTTConfig
	{
		public required string Address { get; set; }

		public required int Port { get; set; }

		public required string Login { get; set; }

		public required string Password { get; set; }
	}
}
