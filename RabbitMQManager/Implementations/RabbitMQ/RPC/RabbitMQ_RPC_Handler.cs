using RabbitMQManager.Core.Implementations;
using RabbitMQManager.Core.Interfaces.MQ.RPC;
using RabbitMQManager.Core.Models;
using System.Text;

namespace RabbitMQManager.Implementations.RabbitMQ.RPC
{
	public class RabbitMQ_RPC_Handler : IRPC_Handler
	{
		private readonly ICommandRegistry _registry;

		public RabbitMQ_RPC_Handler(ICommandRegistry registry)
		{
			_registry = registry;
		}

		public async Task<ResponsePack> Handle(MessageContext context, CancellationToken cancellationToken = default)
		{
			// Получаем значение заголовка и преобразуем его в строку
			if (!context.Headers.TryGetValue("RequestType", out var headerValue))
				throw new InvalidOperationException("RequestType header is missing");

			string requestType = ConvertHeaderValueToString(headerValue!);

			if (!_registry.TryGet(requestType, out var strategy))
				throw new InvalidOperationException($"Unknown command: {requestType}");

			return await strategy!.Use(context.Body, cancellationToken);
		}

		private string ConvertHeaderValueToString(object headerValue)
		{
			if (headerValue == null)
				return string.Empty;

			if (headerValue is string stringValue)
				return stringValue;

			if (headerValue is byte[] byteArray)
				return Encoding.UTF8.GetString(byteArray);

			if (headerValue is ReadOnlyMemory<byte> memory)
				return Encoding.UTF8.GetString(memory.Span);

			return headerValue.ToString() ?? string.Empty;
		}
	}
}
