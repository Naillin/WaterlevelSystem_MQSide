namespace RabbitMQManager.Core.Interfaces.MQ.RPC
{
	public interface IRPC_Client : IDisposable
	{
		Task<TResponse> SendRequestAsync<TRequest, TResponse>(
			TRequest request,
			string requestType,
			TimeSpan timeout,
			CancellationToken cancellationToken = default)
			where TRequest : IMQRequest
			where TResponse : IMQResponse;
	}
}
