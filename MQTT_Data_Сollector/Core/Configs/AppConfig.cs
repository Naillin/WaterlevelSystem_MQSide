namespace MQTT_Data_Сollector.Core.Configs
{
	internal class AppConfig
	{
		public required RabbitConfig Rabbit { get; set; }

		public required MQTTConfig MQTT { get; set; }
	}
}
