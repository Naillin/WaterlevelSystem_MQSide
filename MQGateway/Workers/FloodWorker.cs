using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQGateway.Core.Interfaces;
using MQGateway.Core.Models;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ;
using System.Text.Json;

namespace MQGateway.Workers
{
	internal class FloodWorker : IHostedService
	{
		private readonly ILogger<FloodWorker> _logger;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly string _queue;
		private string _tag = string.Empty;

		public FloodWorker(
			ILogger<FloodWorker> logger,
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

			_tag = await _messageConsumer.StartConsumingAsync(
				_queue,
				MessageHandler,
				cancellationToken
			);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping collector worker");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task MessageHandler(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var dataReceivedEvent = JsonSerializer.Deserialize<FloodAreaCalculatedEvent>(context.Body);

				if (dataReceivedEvent == null)
					throw new InvalidOperationException($"Failed to deserialize request.");

				if (string.IsNullOrWhiteSpace(dataReceivedEvent.TopicPath) ||
					string.IsNullOrWhiteSpace(dataReceivedEvent.Coordinates))
					throw new InvalidOperationException($"TopicPath or Coordinates is null.");

				using (var scope = _scopeFactory.CreateScope())
				{
					var dataRepository = scope.ServiceProvider.GetRequiredService<IDataRepository>();

					await dataRepository.UpsertAreaPoints(
						dataReceivedEvent.TopicPath!,
						dataReceivedEvent.Coordinates!
					);

					_logger.LogDebug($"Data saved for topic: {dataReceivedEvent.TopicPath}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in collector worker");
			}
		}
	}
}
