namespace MQTT_Data_Сollector.Core.Interfaces
{
	internal interface IMQService
	{
		Task PublishDataAsync(string topic, string value);
	}
}
