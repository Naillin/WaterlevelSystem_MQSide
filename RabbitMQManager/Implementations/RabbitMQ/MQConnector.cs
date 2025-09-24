using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQManager.Core.Interfaces;
using RabbitMQManager.Implementations;

namespace RabbitMQManager.Core.Implementations.RabbitMQ
{
	public abstract class MQConnector : IConnector
	{
		private readonly ILogger<MQConnector> _logger;
		private readonly MQConnectionContext _connectionContext;

		protected static IConnection? _connection;
		protected IChannel? _channel;
		protected readonly object _connectionLock = new();
		protected bool _disposed = false;

		public static bool IsConnected => _connection?.IsOpen == true;

		public MQConnector(ILogger<MQConnector> logger, MQConnectionContext connectionContext)
		{
			_connectionContext = connectionContext;
			_logger = logger;
		}

		/// <summary>
		/// Установка подключения к RabbitMQ брокеру
		/// </summary>
		public virtual async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			if (IsConnected)
				return;

			lock (_connectionLock)
			{
				if (IsConnected)
					return;
			}

			try
			{
				var factory = new ConnectionFactory
				{
					HostName = _connectionContext._brokerAddress,
					UserName = _connectionContext._userName,
					Password = _connectionContext._password,
					Port = _connectionContext._port,
					VirtualHost = _connectionContext._virtualHost,
					AutomaticRecoveryEnabled = true,
					RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
					ContinuationTimeout = TimeSpan.FromSeconds(30)
				};

				_connection = await factory.CreateConnectionAsync(cancellationToken);

				_logger.LogInformation($"Connected to RabbitMQ at {_connectionContext._brokerAddress}");
			}
			catch (BrokerUnreachableException ex)
			{
				throw new InvalidOperationException($"Cannot connect to RabbitMQ at {_connectionContext._brokerAddress}", ex);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to connect to RabbitMQ", ex);
			}
		}

		/// <summary>
		/// Отключение от RabbitMQ брокера
		/// </summary>
		public virtual async Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
			lock (_connectionLock)
			{
				if (!IsConnected)
					return;
			}

			try
			{
				if (_channel != null)
				{
					await _channel.CloseAsync(cancellationToken);
					await _channel.DisposeAsync();
					_channel = null;
				}

				if (_connection != null)
				{
					await _connection.CloseAsync(cancellationToken);
					await _connection.DisposeAsync();
					_connection = null;
				}

				_logger.LogInformation("Disconnected from RabbitMQ");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error during disconnect: {ex.Message}");
			}
		}

		/// <summary>
		/// Переподключение к RabbitMQ брокеру
		/// </summary>
		public async Task ReconnectAsync(CancellationToken cancellationToken = default)
		{
			await DisconnectAsync(cancellationToken);
			await ConnectAsync(cancellationToken);
		}

		/// <summary>
		/// Освобождение ресурсов
		/// </summary>
		public void Dispose()
		{
			DisposeAsync().GetAwaiter().GetResult();
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;

			try
			{
				if (_channel != null)
				{
					await _channel.CloseAsync();
					await _channel.DisposeAsync();
				}

				if (_connection != null)
				{
					await _connection.CloseAsync();
					await _connection.DisposeAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error during disposal: {ex.Message}");
			}

			_disposed = true;
		}

		~MQConnector() => DisposeAsync().GetAwaiter().GetResult();
	}
}
