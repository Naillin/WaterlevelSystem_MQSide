using Microsoft.Extensions.Logging;
using MQTT_Data_Сollector.Core.Models;
using RabbitMQManager.Core.Interfaces.MQ;

namespace MQTT_Data_Сollector.Services
{
	internal class RabbitMQService
	{
		private readonly IMessageProducer _messageProducer;
		private readonly ILogger<RabbitMQService> _logger;

		public RabbitMQService(IMessageProducer messageProducer, ILogger<RabbitMQService> logger)
		{
			_messageProducer = messageProducer;
			_logger = logger;
		}

		public async Task PublishDataAsync(string topic, string value)
		{
			_logger.LogInformation($"Publish data in queue.");
			long time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			var message = new SensorDataReceivedEvent
			{
				TopicPath = topic,
				Value = double.Parse(value),
				Timestamp = time
			};
			await _messageProducer.PublishAsync<SensorDataReceivedEvent>(message);

			_logger.LogInformation($"Publish value {value} at {time.ToString()} time.");
		}
	}
}
