using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector.Core.Interfaces;
using MQTT_Data_Сollector.Core.Models;

namespace MQTT_Data_Сollector.Workers
{
	internal class MqttClientWorker : BackgroundService
	{
		private readonly IMqttClient _mqttClient;
		private readonly IMQService _mqService;
		private readonly ILogger<MqttClientWorker> _logger;

		public MqttClientWorker(IMqttClient mqttClient, IMQService mqService, ILogger<MqttClientWorker> logger)
		{
			_mqttClient = mqttClient;
			_mqService = mqService;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken = default)
		{
			// Подписываемся на событие
			_mqttClient.MessageReceived += OnMessageReceived;

			try
			{
				// Ждем сигнала остановки, но периодически проверяем соединение
				while (!stoppingToken.IsCancellationRequested)
				{
					// Проверяем состояние подключения (если есть такая возможность)
					if (!_mqttClient.IsConnected()) // Проверьте правильное свойство для вашего клиента
					{
						_logger.LogWarning("MQTT client is disconnected. Attempting to handle...");
						// Здесь можно добавить логику переподключения
					}

					await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in MQTT message listener!");
			}
			finally
			{
				// ВАЖНО: отписываемся от события при остановке
				_mqttClient.MessageReceived -= OnMessageReceived;
				_logger.LogInformation("MQTT message listener stopped.");
			}
		}

		private async void OnMessageReceived(object? sender, MqttMessageReceivedEventArgs eventArgs)
		{
			try
			{
				_logger.LogInformation($"Get data - Topic: [{eventArgs.Topic}] Message: [{eventArgs.Payload}]");

				if (!string.IsNullOrWhiteSpace(eventArgs.Topic) && !string.IsNullOrWhiteSpace(eventArgs.Payload))
					await _mqService.PublishDataAsync(eventArgs.Topic, eventArgs.Payload);
				else
					_logger.LogWarning("MQTT message or topic is empty!");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in MQTT message receive!");
			}
		}
	}
}
