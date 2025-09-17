using RabbitMQManager.Core.Interfaces.MQ.RPC;
using RabbitMQManager.Core.Models;
using System.Text.Json;

namespace RabbitMQManager.Implementations.RabbitMQ.RPC.Strategies
{
	//[Command("BasicCommand")]
	public abstract class MQStrategy<TRequest, TResponse> : IMQStrategy
	where TRequest : IMQRequest
	where TResponse : IMQResponse, new()
	{
		public async Task<ResponsePack> Use(string body, CancellationToken cancellationToken = default)
		{
			var request = JsonSerializer.Deserialize<TRequest>(body);

			if (request == null)
				throw new InvalidOperationException($"Failed to deserialize request.");

			var response = await Handle(request, cancellationToken);

			var headers = new Dictionary<string, object>
			{
				["RequestId"] = request.RequestId!,
				["RequestType"] = request.Type!
			};

			return new ResponsePack(JsonSerializer.Serialize<TResponse>(response),
				request.QueueName!,
				request.Type!,
				headers
			);
		}

		protected virtual async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default)
		{
			var response = new TResponse
			{
				RequestId = request.RequestId!,
				Type = "BasicCommand",
				Success = true,
				ErrorMessage = string.Empty
			};

			return response;
		}
	}
}
