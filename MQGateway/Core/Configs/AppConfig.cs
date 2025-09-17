using MQGateway.Core.Configs;

namespace MQTT_Data_Сollector.Core.Configs
{
	internal class AppConfig
	{
		public RabbitConfig Rabbit { get; set; }

		public DatabaseConfig Database { get; set; }
	}
}
