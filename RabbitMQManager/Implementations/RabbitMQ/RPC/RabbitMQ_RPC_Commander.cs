using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Core.Interfaces.MQ.RPC;

namespace RabbitMQManager.Implementations.RabbitMQ.RPC
{
	public class RabbitMQ_RPC_Commander : IHostedService
	{
		private readonly ILogger<RabbitMQ_RPC_Commander> _logger;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IMessageProducer _messageProducer;
		private readonly IRPC_Handler _handler;
		private readonly string _queue;

		private string _tag = string.Empty;

		private Dictionary<string, Func<IMQStrategy>> _strategyFactories = new();

		public RabbitMQ_RPC_Commander(
			ILogger<RabbitMQ_RPC_Commander> logger,
			IMessageConsumer messageConsumer,
			IMessageProducer messageProducer,
			IRPC_Handler handler,
			string Queue)
		{
			_messageConsumer = messageConsumer;
			_messageProducer = messageProducer;
			_handler = handler;

			_logger = logger;

			_queue = Queue;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Starting commander");

			_tag = await _messageConsumer.StartConsumingAsync(
					_queue,
					HandleResponseMessage,
					cancellationToken
			);
		}
		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Stopping commander");

			await _messageConsumer.StopConsumingAsync(_tag, cancellationToken);
			_tag = string.Empty;
		}

		private async Task HandleResponseMessage(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var pack = await _handler.Handle(context, cancellationToken);
				await _messageProducer.PublishAsync(
					message: pack._message,
					exchangeName: "",
					routingKey: pack._queue,
					pack._type,
					pack._headers
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling response message in commander");
			}
		}

		public void Dispose()
		{
			//_messageConsumer?.Dispose();
			//_messageProducer?.Dispose();

			_strategyFactories?.Clear();
		}
	}
}
