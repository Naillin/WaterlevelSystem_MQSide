namespace RabbitMQManager.Implementations
{
	public class MQConnectionContext
	{
		public readonly string _brokerAddress;

		public readonly string _userName;

		public readonly string _password;

		public readonly int _port;

		public readonly string _virtualHost;

		public MQConnectionContext(
			string brokerAddress = "127.0.0.1",
			int port = 5672,
			string userName = "guest",
			string password = "guest",
			string virtualHost = "/")
		{
			_brokerAddress = brokerAddress;
			_port = port;
			_userName = userName;
			_password = password;
			_virtualHost = virtualHost;
		}

		public MQConnectionContext(
			MQConnectionContext connectionContext)
		{
			_brokerAddress = connectionContext._brokerAddress;
			_port = connectionContext._port;
			_userName = connectionContext._userName;
			_password = connectionContext._password;
			_virtualHost = connectionContext._virtualHost;
		}
	}
}
