namespace MQTT_Data_Сollector.Core.Configs
{
	internal class AppConfig
	{
		public RabbitConfig Rabbit { get; set; }

		public MQTTConfig MQTT { get; set; }
	}
}
