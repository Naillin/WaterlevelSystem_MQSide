namespace RabbitMQManager.Core.Implementations
{
	public class MessageContext
	{
		public string Body { get; }

		public string RoutingKey { get; }

		public string Exchange { get; }

		public IDictionary<string, object?> Headers { get; }

		public ulong DeliveryTag { get; }

		public MessageContext(string body, string routingKey, string exchange, IDictionary<string, object?> headers, ulong deliveryTag)
		{
			Body = body;
			RoutingKey = routingKey;
			Exchange = exchange;
			Headers = headers;
			DeliveryTag = deliveryTag;
		}
	}
}
