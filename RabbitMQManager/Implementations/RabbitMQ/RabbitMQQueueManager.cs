using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Implementations;

namespace RabbitMQManager.Core.Implementations.RabbitMQ
{
	public class RabbitMQQueueManager : MQConnector, IMessageQueueManager
	{
		private readonly ILogger<RabbitMQQueueManager> _consumerLogger;

		public RabbitMQQueueManager(ILogger<RabbitMQQueueManager> logger, MQConnectionContext connectionContext) : base(logger, connectionContext) => _consumerLogger = logger;

		public override async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			await base.ConnectAsync(cancellationToken);
			_channel = await _connection!.CreateChannelAsync(null, cancellationToken);
			_consumerLogger.LogInformation($"Channel for RabbitMQ attached.");
		}

		public async Task<QueueDeclareOk> CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, bool noWait = false, CancellationToken cancellationToken = default)
		{
			try
			{
				return await _channel!.QueueDeclareAsync(
					queue: queueName,
					durable: durable,
					exclusive: exclusive,
					autoDelete: autoDelete,
					arguments: null,
					noWait: noWait,
					cancellationToken
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}

		public async Task<QueueDeclareOk> AnonymousQueueDeclareAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				return await _channel!.QueueDeclareAsync(
					queue: string.Empty,
					durable: false,
					exclusive: false,
					autoDelete: true,
					arguments: null,
					noWait: false,
					cancellationToken
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}

		public async Task CreateExchangeAsync(string exchangeName, string exchangeType = "direct", bool durable = true, bool autoDelete = false, bool noWait = false, CancellationToken cancellationToken = default)
		{
			try
			{
				await _channel!.ExchangeDeclareAsync(
					exchange: exchangeName,
					type: exchangeType,
					durable: durable,
					autoDelete: autoDelete,
					arguments: null,
					noWait: noWait,
					cancellationToken
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}

		public async Task BindQueueAsync(string queueName, string exchangeName, string routingKey = "", CancellationToken cancellationToken = default)
		{
			try
			{
				await _channel!.QueueBindAsync(
					queue: queueName,
					exchange: exchangeName,
					routingKey: routingKey,
					arguments: null
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}

		public async Task DeleteQueue(string queueName)
		{
			try
			{
				await _channel!.QueueDeleteAsync(
					queue: queueName,
					ifUnused: false,
					ifEmpty: false
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}

		public async Task DeleteExchange(string exchangeName)
		{
			try
			{
				await _channel!.ExchangeDeleteAsync(
					exchange: exchangeName,
					ifUnused: false
				);
			}
			catch (BrokerUnreachableException ex)
			{
				_consumerLogger.LogError(ex, "Cannot reach RabbitMQ broker.");
				throw;
			}
		}
	}
}
