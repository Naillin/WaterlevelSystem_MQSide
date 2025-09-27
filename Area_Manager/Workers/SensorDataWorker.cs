using Area_Manager.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ;
using System.Text.Json;

namespace Area_Manager.Workers
{
	internal class SensorDataWorker : IHostedService
	{
		private readonly ILogger<SensorDataWorker> _logger;
		private readonly IMessageConsumer _messageConsumer;
		private readonly string _queue;

		private string _tag = string.Empty;

		private readonly Dictionary<string, SensorDataDto> _sensorDatas = new ();

		public SensorDataWorker(string queue, IMessageConsumer messageConsumer, ILogger<SensorDataWorker> logger)
		{
			_queue = queue;
			_messageConsumer = messageConsumer;

			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting sensor data worker");

			_tag = await _messageConsumer.StartConsumingAsync(
				_queue,
				HandleResponseMessage,
				cancellationToken
			);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping sensor data worker");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task HandleResponseMessage(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				//расшифрвка сообщения. должн быть какой то словарь где все топики будут иметь
				if(context != null && string.IsNullOrWhiteSpace(context.Body))
					return;

				var sensorEvent = JsonSerializer.Deserialize<SensorDataReceivedEvent>(context!.Body);

				if (sensorEvent == null)
					throw new InvalidOperationException($"Failed to deserialize sensor event.");


				await ToSensorDataDto(sensorEvent);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling response message in sensor data worker");
			}
		}

		private async Task ToSensorDataDto(SensorDataReceivedEvent sensorEvent)
		{
			if (string.IsNullOrWhiteSpace(sensorEvent.TopicPath))
				return;

			if (_sensorDatas.TryGetValue(sensorEvent.TopicPath, out var sensorData))
				sensorData.Data.Add((sensorEvent.Value, sensorEvent.Timestamp));
			else
			{
				var sensorDataNew = new SensorDataDto
				{
					TopicPath = sensorEvent.TopicPath,
					Coordinate = Coord, // Создать стратегию и кинуть запрос к mqgateway. попросить координаты и высоту топика по path_topic
					Altitude = 0
				};
				// лучше один раз при запуске воркера (в StartAsync) запросить данные как GetTopics и записать их здесь в память!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				sensorDataNew.Data.Add((sensorEvent.Value, sensorEvent.Timestamp));
				_sensorDatas.Add(sensorEvent.TopicPath, sensorDataNew);
			}
		}

		public IReadOnlyDictionary<string, SensorDataDto> GetSensorData() => _sensorDatas.AsReadOnly();
	}
}
