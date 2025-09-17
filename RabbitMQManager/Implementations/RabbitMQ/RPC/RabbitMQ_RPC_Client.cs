using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using System.Text.Json;

namespace RabbitMQManager.Implementations.RabbitMQ.RPC
{
	public class RabbitMQ_RPC_Client : IRPC_Client, IDisposable
	{
		private readonly ILogger<RabbitMQ_RPC_Client> _RPClogger;
		private readonly Dictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
		private readonly object _lock = new();

		private readonly IMessageQueueManager _queueManager;
		private readonly IMessageConsumer _messageConsumer;
		private readonly IMessageProducer _messageProducer;

		private readonly string _requestExchange;
		private readonly string _requestRoutingKey;

		public RabbitMQ_RPC_Client(
			ILogger<RabbitMQ_RPC_Client> logger,
			IMessageQueueManager queueManager,
			IMessageConsumer messageConsumer,
			IMessageProducer messageProducer,
			string requestExchange,
			string requestRoutingKey)
		{
			_queueManager = queueManager;
			_messageConsumer = messageConsumer;
			_messageProducer = messageProducer;

			_requestExchange = requestExchange;
			_requestRoutingKey = requestRoutingKey;

			_RPClogger = logger;
		}

		public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
			TRequest request,
			string requestType,
			TimeSpan timeout,
			CancellationToken cancellationToken = default)
			where TRequest : IMQRequest
			where TResponse : IMQResponse
		{
			var requestId = Guid.NewGuid().ToString();
			var responseQueue = await SetupResponseQueueAsync(cancellationToken);
			var tcs = new TaskCompletionSource<string>();

			lock (_lock)
			{
				_pendingRequests[requestId] = tcs;
			}

			try
			{
				// Добавляем ID запроса и очередь для ответа
				request.RequestId = requestId;
				request.QueueName = responseQueue.QueueName;

				await _messageProducer.PublishAsync(
					JsonSerializer.Serialize(request),
					_requestExchange,
					_requestRoutingKey,
					requestType,
					new Dictionary<string, object> {
						["RequestId"] = requestId,
						["RequestType"] = requestType
					},
					cancellationToken
				);

				_RPClogger.LogInformation($"Sent request {requestId} to {_requestExchange}");

				// Ждем ответа с таймаутом
				var responseTask = tcs.Task;
				var timeoutTask = Task.Delay(timeout, cancellationToken);

				var completedTask = await Task.WhenAny(responseTask, timeoutTask);

				if (completedTask == timeoutTask)
					throw new TimeoutException($"Request {requestId} timed out after {timeout.TotalSeconds}s");

				var responseJson = await responseTask;
				var response = JsonSerializer.Deserialize<TResponse>(responseJson);

				if (response == null)
					throw new InvalidOperationException($"Failed to deserialize response for request {requestId}");

				return response;
			}
			finally
			{
				lock (_lock)
				{
					_pendingRequests.Remove(requestId);
				}
				await _queueManager.DeleteQueue(responseQueue.QueueName);
			}
		}

		private async Task<QueueDeclareOk> SetupResponseQueueAsync(CancellationToken cancellationToken = default)
		{
			var queue = await _queueManager.AnonymousQueueDeclareAsync(cancellationToken);

			// Подписываемся на ответы один раз при первом запросе
			if (!_pendingRequests.Any())
			{
				await _messageConsumer.StartConsumingAsync(
					queue.QueueName,
					HandleResponseMessage,
					cancellationToken
				);
			}

			return queue;
		}

		private async Task HandleResponseMessage(MessageContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				var requestId = context.Headers.TryGetValue("RequestId", out var headerValue)
					? headerValue?.ToString()
					: null;

				if (string.IsNullOrEmpty(requestId))
				{
					_RPClogger.LogWarning("Received response without RequestId header");
					return;
				}

				TaskCompletionSource<string>? tcs;
				lock (_lock)
				{
					_pendingRequests.TryGetValue(requestId, out tcs);
				}

				if (tcs != null)
				{
					tcs.TrySetResult(context.Body);
					_RPClogger.LogInformation($"Received response for request {requestId}");
				}
				else
					_RPClogger.LogWarning($"Received response for unknown request {requestId}");
			}
			catch (Exception ex)
			{
				_RPClogger.LogError(ex, "Error handling response message");
			}
		}

		public void Dispose()
		{
			lock (_lock)
			{
				foreach (var tcs in _pendingRequests.Values)
				{
					tcs.TrySetCanceled();
				}
				_pendingRequests.Clear();
			}

			_messageConsumer.Dispose();
			_messageProducer.Dispose();
			_queueManager.Dispose();
		}
	}
}
