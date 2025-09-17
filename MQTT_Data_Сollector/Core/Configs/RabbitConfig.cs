namespace MQTT_Data_Сollector.Core.Configs
{
	internal class RabbitConfig
	{
		public required string Address { get; set; }

		public required int Port { get; set; }

		public required string Login { get; set; }

		public required string Password { get; set; }

		public required string VirtualHost { get; set; }

		public required string MQTTExchange { get; set; }

		public required string RPC_Exchange { get; set; }

		public required string RPC_Routing { get; set; }
	}
}
