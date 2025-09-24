using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector.Core.Interfaces;
using MQTT_Data_Сollector.Core.Models;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTT_Data_Сollector.Implementations
{
	internal class M2MqttClient : M2MqttConnector, IMqttClient
	{
		private readonly ILogger<M2MqttClient> _logger;

		public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;
		private readonly HashSet<string> _subscriptions = new();

		public M2MqttClient(string brokerAddress, int port, string username, string password, ILogger<M2MqttClient> logger) : base(brokerAddress, port, username, password, logger)
		{
			// Настройка обработчиков событий
			_mqttClient.MqttMsgPublishReceived += OnMqttMsgPublishReceived;
			_mqttClient.ConnectionClosed += OnConnectionClosed;

			_logger = logger;
		}

		bool IMqttClient.IsConnected() => IsConnected;

		public override async Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
			if (IsConnected)
				await UnsubscribeAllAsync();

			await base.DisconnectAsync(cancellationToken);
		}

		public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				if (_subscriptions.Contains(topic))
					return;

				_logger.LogInformation($"Subscribing to topic: {topic}");
				_mqttClient.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
				_subscriptions.Add(topic);
				_logger.LogInformation($"Subscribed to topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot subscribe.");
				await ReconnectAsync();
			}
		}

		public async Task SubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				var topicsToSubscribe = topics
					.Where(t => !_subscriptions.Contains(t))
					.ToArray();

				byte[] qosLevels = new byte[topicsToSubscribe.Length];
				for (int i = 0; i < topicsToSubscribe.Length; i++)
				{
					qosLevels[i] = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
					_subscriptions.Add(topicsToSubscribe[i]);
					_logger.LogInformation($"Subscribing to topic: {topicsToSubscribe[i]}");
				}

				_mqttClient.Subscribe(topicsToSubscribe, qosLevels);
				_logger.LogInformation("Subscribed to all topics.");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot subscribe.");
				await ReconnectAsync();
			}
		}

		public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				_logger.LogInformation($"Unsubscribing from topic: {topic}");
				_subscriptions.Remove(topic);
				_mqttClient.Unsubscribe(new string[] { topic });
				_logger.LogInformation($"Unsubscribed from topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
				await ReconnectAsync();
			}
		}

		public async Task UnsubscribeAsync(string[] topics, CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				_logger.LogInformation("Unsubscribing from topics:");
				foreach (var topic in topics)
				{
					_logger.LogInformation($"- {topic}");
					_subscriptions.Remove(topic);
				}

				_mqttClient.Unsubscribe(topics);
				_logger.LogInformation("Unsubscribed from all specified topics.");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
				await ReconnectAsync();
			}
		}

		public async Task UnsubscribeAllAsync(CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				_subscriptions.ToList().ForEach(s => _logger.LogInformation($", {s}"));
				_mqttClient.Unsubscribe(_subscriptions.ToArray());
				_subscriptions.Clear();
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot unsubscribe.");
				await ReconnectAsync();
			}
		}

		public async Task Publish(string topic, string payload, CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				_mqttClient.Publish(topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
				_logger.LogInformation($"Message published to topic: {topic}");
			}
			else
			{
				_logger.LogError("Client is not connected. Cannot publish.");
				await ReconnectAsync();
			}
		}

		public IReadOnlyCollection<string> GetSubscriptions()
		{
			return _subscriptions.ToList().AsReadOnly();
		}

		// Обработчик события получения сообщения
		private void OnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			var payload = Encoding.UTF8.GetString(e.Message);
			_logger.LogInformation($"Received message: {payload} from topic: {e.Topic}");

			// Вызов события для обработки сообщения
			MessageReceived?.Invoke(this, new MqttMessageReceivedEventArgs
			{
				Topic = e.Topic,
				Payload = payload
			});
		}

		// Обработчик события закрытия соединения
		private void OnConnectionClosed(object sender, EventArgs e)
		{
			_logger.LogInformation("Disconnected from MQTT broker.");
			if (!IsConnected)
			{
				ReconnectAsync().GetAwaiter();
			}
		}
	}
}
