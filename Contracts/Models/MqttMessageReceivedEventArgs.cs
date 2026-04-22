namespace Contracts.Models;

public class MqttMessageReceivedEventArgs : EventArgs
{
	public string? Topic { get; set; }

	public string? Payload { get; set; }
}