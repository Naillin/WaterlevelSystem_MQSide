using MQTT_Data_Сollector.Core.Models;
using RabbitMQManager.Core.Interfaces;

namespace MQTT_Data_Сollector.Core.Interfaces
{
	internal interface IMqttClient : IConnector, IDisposable
	{
		bool IsConnected();

		Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);

		Task SubscribeAsync(string[] topics, CancellationToken cancellationToken = default);

		Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);

		Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default);

		Task UnsubscribeAllAsync(CancellationToken cancellationToken = default);

		IReadOnlyCollection<string> GetSubscriptions();

		Task Publish(string topic, string payload, CancellationToken cancellationToken = default);

		event EventHandler<MqttMessageReceivedEventArgs> MessageReceived;
	}
}
