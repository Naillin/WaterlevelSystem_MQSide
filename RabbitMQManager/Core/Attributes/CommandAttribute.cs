namespace RabbitMQManager.Core.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class CommandAttribute : Attribute
	{
		public string Name { get; }

		public CommandAttribute(string name)
		{
			Name = name;
		}
	}
}
