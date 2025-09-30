using Area_Manager.Core.Interfaces;
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
		private readonly ISensorDataService _sensorDataService;
		private readonly string _queue;

		private string _tag = string.Empty;

		public SensorDataWorker(string queue, IMessageConsumer messageConsumer, ISensorDataService sensorDataService, ILogger<SensorDataWorker> logger)
		{
			_queue = queue;
			_messageConsumer = messageConsumer;
			_sensorDataService = sensorDataService;

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

				await _sensorDataService.AddData(sensorEvent, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling response message in sensor data worker");
			}
		}
	}
}
