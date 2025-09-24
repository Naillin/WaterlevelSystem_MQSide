using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Implementations;
using System.Text;
using System.Text.Json;

namespace RabbitMQManager.Core.Implementations.RabbitMQ
{
	public class RabbitMQProducer : MQConnector, IMessageProducer
	{
		private readonly ILogger<RabbitMQProducer> _producerLogger;
		private readonly string _exchangeName = "";

		public RabbitMQProducer(ILogger<RabbitMQProducer> logger, MQConnectionContext connectionContext, string exchangeName = "") : base(logger, connectionContext)
		{
			_exchangeName = exchangeName;
			_producerLogger = logger;
		}

		public override async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			await base.ConnectAsync(cancellationToken);
			
			if(_channel == null || _channel.IsClosed)
				_channel = await _connection!.CreateChannelAsync(null, cancellationToken);

			_producerLogger.LogInformation($"Channel for RabbitMQ attached. {_channel.ToString()}");
		}

		/// <summary>
		/// Публикация типизированного сообщения
		/// </summary>
		public async Task PublishAsync<T>(T message, string routingKey = "", CancellationToken cancellationToken = default)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!IsConnected)
				throw new InvalidOperationException("Not connected to RabbitMQ");

			var json = JsonSerializer.Serialize(message);
			await PublishAsync(json, routingKey, typeof(T).Name, null, cancellationToken);
		}

		/// <summary>
		/// Публикация строкового сообщения
		/// </summary>
		public async Task PublishAsync(string message, string routingKey, string messageType, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
		{
			await PublishAsync(message, _exchangeName, routingKey, messageType, headers, cancellationToken);
		}

		public async Task PublishAsync<T>(T message, string exchangeName, string routingKey = "", CancellationToken cancellationToken = default)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!IsConnected)
				throw new InvalidOperationException("Not connected to RabbitMQ");

			var json = JsonSerializer.Serialize(message);
			await PublishAsync(json, exchangeName, routingKey, typeof(T).Name, null, cancellationToken);
		}

		public async Task PublishAsync(string message, string exchangeName, string routingKey, string messageType, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(message))
				throw new ArgumentException("Message cannot be null or empty", nameof(message));

			if (!IsConnected || _channel == null)
				throw new InvalidOperationException("Not connected to RabbitMQ");

			try
			{
				var body = Encoding.UTF8.GetBytes(message);

				var properties = new BasicProperties
				{
					Persistent = true,
					MessageId = Guid.NewGuid().ToString(),
					Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
				};

				if (!string.IsNullOrEmpty(messageType))
					properties.Type = messageType;

				if (headers != null)
					properties.Headers = headers!;

				await _channel.BasicPublishAsync(
					exchange: exchangeName,
					routingKey: routingKey,
					mandatory: false,
					basicProperties: properties,
					body: body,
					cancellationToken: cancellationToken);

				_producerLogger.LogInformation($"Message published to {_exchangeName} with routing key '{routingKey}'");
			}
			catch (AlreadyClosedException ex)
			{
				throw new InvalidOperationException("RabbitMQ connection is closed", ex);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to publish message", ex);
			}
		}
	}
}
