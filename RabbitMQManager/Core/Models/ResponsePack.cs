namespace RabbitMQManager.Core.Models
{
	public class ResponsePack
	{
		public string _message { get; set; }

		public string _queue { get; set; }

		public string _type { get; set; }


		public Dictionary<string, object> _headers = new();

		public ResponsePack(string Message, string Queue, string Type, Dictionary<string, object> headers)
		{
			_message = Message;
			_queue = Queue;
			_type = Type;
			_headers = headers;
		}
	}
}
