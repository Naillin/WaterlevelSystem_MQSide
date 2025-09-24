using MQTT_Data_Сollector.Core.Interfaces;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Implementations;

namespace MQTT_Data_Сollector.Workers
{
	internal class ConnectorWorker : MQConnectorWorker
	{
		private readonly IMqttClient _mqttClient;

		public ConnectorWorker(
			IMessageConsumer messageConsumer,
			IMessageProducer messageProducer,
			IMessageQueueManager messageQueueManager,
			IMqttClient mqttClient) : base(
				messageConsumer,
				messageProducer,
				messageQueueManager) => _mqttClient = mqttClient;

		public override async Task StartAsync(CancellationToken cancellationToken = default)
		{
			await base.StartAsync(cancellationToken);

			await _mqttClient.ConnectAsync(cancellationToken);
		}

		public override async Task StopAsync(CancellationToken cancellationToken = default)
		{
			await base.StopAsync(cancellationToken);

			await _mqttClient.DisconnectAsync(cancellationToken);
		}
	}
}
