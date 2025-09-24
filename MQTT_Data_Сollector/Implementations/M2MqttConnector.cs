using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Interfaces;
using uPLibrary.Networking.M2Mqtt;

namespace MQTT_Data_Сollector.Implementations
{
	internal class M2MqttConnector : IConnector
	{
		private readonly ILogger<M2MqttConnector> _logger;

		protected string _clientId;
		protected string _username;
		protected string _password;

		protected MqttClient _mqttClient;

		public bool IsConnected => _mqttClient.IsConnected;

		private bool disconnectMode = false;

		public M2MqttConnector(string brokerAddress, int port, string username, string password, ILogger<M2MqttConnector> logger)
		{
			_clientId = Guid.NewGuid().ToString();
			_username = username;
			_password = password;
			_logger = logger;

			// Создание клиента MQTT
			_mqttClient = new MqttClient(brokerAddress, port, false, null, null, MqttSslProtocols.None);
		}

		public virtual async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			if (!IsConnected && !disconnectMode)
			{
				try
				{
					_logger.LogInformation("Connecting to MQTT broker...");
					await Task.Delay(2000);
					_mqttClient.Connect(_clientId, _username, _password);
					_logger.LogInformation("Connected to MQTT broker.");
				}
				catch (Exception ex)
				{
					_logger.LogError($"Connection failed: {ex.Message}");
				}
				finally
				{
					await Task.Delay(5000);
				}
			}
		}

		public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
			if (IsConnected)
			{
				_logger.LogInformation("Disconnecting from MQTT broker...");
				disconnectMode = true;
				_mqttClient.Disconnect();

				_logger.LogInformation("Disconnected from MQTT broker.");
			}
		}

		public virtual async Task ReconnectAsync(CancellationToken cancellationToken = default)
		{
			while (!IsConnected && !disconnectMode)
			{
				try
				{
					_logger.LogInformation("Reconnecting to MQTT broker...");
					_mqttClient.Connect(_clientId);
					_logger.LogInformation("Reconnected to MQTT broker.");
				}
				catch (Exception ex)
				{
					_logger.LogError($"Reconnection failed: {ex.Message}");
				}
				finally
				{
					await Task.Delay(5000);
				}
			}
		}

		public void Dispose()
		{
			DisconnectAsync().GetAwaiter();

			GC.SuppressFinalize(this);
		}
	}
}
