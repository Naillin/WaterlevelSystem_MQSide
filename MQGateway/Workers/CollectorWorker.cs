using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQGateway.Core.Entities;
using MQGateway.Core.Interfaces;
using MQGateway.Core.Models;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces;
using RabbitMQManager.Core.Interfaces.MQ;
using System.Text.Json;

namespace MQGateway.Workers
{
	internal class CollectorWorker : IWorker
	{
		private readonly ILogger<CollectorWorker> _logger;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly string _queue;
		private string _tag = string.Empty;

		public CollectorWorker(
			ILogger<CollectorWorker> logger,
			IMessageConsumer messageConsumer,
			IServiceScopeFactory scopeFactory,
			string queue)
		{
			_messageConsumer = messageConsumer;
			_scopeFactory = scopeFactory;
			_queue = queue;

			_logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting collector worker");

			_tag = await _messageConsumer.StartConsumingAsync(_queue, RunAsync, cancellationToken);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping collector worker");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task RunAsync(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var dataReceivedEvent = JsonSerializer.Deserialize<SensorDataReceivedEvent>(context.Body);

				if (dataReceivedEvent == null)
					throw new InvalidOperationException($"Failed to deserialize request.");

				using (var scope = _scopeFactory.CreateScope())
				{
					var dataRepository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

					var topic = await dataRepository.GetTopicAsync(dataReceivedEvent.TopicPath!);

					if (topic == null)
						throw new InvalidOperationException($"Failed to get topic.");

					var data = new Data
					{
						ID_Topic = topic.ID_Topic,
						Value_Data = dataReceivedEvent.Value.ToString(),
						Time_Data = dataReceivedEvent.Timestamp,
						Topic = topic,
					};

					await dataRepository.AddDataAsync(data);
					_logger.LogDebug($"Data saved for topic: {dataReceivedEvent.TopicPath}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in collector worker");
			}
		}

		public void Dispose()
		{
			StopAsync().GetAwaiter();
			_messageConsumer.Dispose();
		}
	}
}
