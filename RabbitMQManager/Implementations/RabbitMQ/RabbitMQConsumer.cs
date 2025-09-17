using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Implementations;
using System.Text;

namespace RabbitMQManager.Core.Implementations.RabbitMQ
{
	public class RabbitMQConsumer : MQConnector, IMessageConsumer
	{
		private readonly ILogger<RabbitMQConsumer> _consumerLogger;
		private Dictionary<string, string> _tags = new ();

		public RabbitMQConsumer(ILogger<RabbitMQConsumer> logger, MQConnectionContext connectionContext) : base(logger, connectionContext)
		{
			_consumerLogger = logger;
		}

		public override async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			await base.ConnectAsync(cancellationToken);
			_channel = await _connection!.CreateChannelAsync(null, cancellationToken);
			_consumerLogger.LogInformation($"Channel for RabbitMQ attached.");
		}

		/// <summary>
		/// Запуск потребления сообщений из очереди
		/// </summary>
		public async Task<string> StartConsumingAsync(string queueName, Func<MessageContext, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default)
		{
			if (!IsConnected || _channel == null)
				throw new InvalidOperationException("Not connected to RabbitMQ");

			var consumer = new AsyncEventingBasicConsumer(_channel);

			consumer.ReceivedAsync += async (sender, ea) =>
			{
				try
				{
					var ctx = new MessageContext(
						Encoding.UTF8.GetString(ea.Body.ToArray()),
						ea.RoutingKey,
						ea.Exchange,
						ea.BasicProperties?.Headers ?? new Dictionary<string, object?>(),
						ea.DeliveryTag);

					await messageHandler(ctx, cancellationToken);

					// подтверждаем доставку
					await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
				}
				catch (Exception ex)
				{
					_consumerLogger.LogError($"Error processing message: {ex.Message}");

					// отклоняем сообщение без повторной доставки
					await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
				}
			};

			_consumerLogger.LogInformation($"Started consuming from queue '{queueName}'");

			// запускаем consumer
			string _consumerTag = await _channel.BasicConsumeAsync(
				queue: queueName,
				autoAck: false,
				consumer: consumer,
				cancellationToken: cancellationToken);

			_tags[queueName] = _consumerTag;
			return _consumerTag;
		}

		/// <summary>
		/// Остановка потребления
		/// </summary>
		public async Task StopConsumingAsync(string tag, CancellationToken cancellationToken = default)
		{
			if (_tags.Count() == 0 || string.IsNullOrWhiteSpace(tag))
				return;

			if (!IsConnected || _channel == null)
				return;

			try
			{
				await _channel.BasicCancelAsync(tag, false, cancellationToken);
				_consumerLogger.LogInformation("Stopped consuming");
			}
			catch (Exception ex)
			{
				_consumerLogger.LogError($"Error stopping consumer: {ex.Message}");
			}
		}

		public IReadOnlyDictionary<string, string> GetTags()
		{
			return _tags.AsReadOnly();
		}
	}
}
