using MQGateway.Core.Entities;

namespace MQGateway.Core.Interfaces
{
	internal interface IDataRepository
	{
		Task<User?> GetUserAsync(string login);

		Task AddTopicAsync(Topic topic);

		Task<int?> RemoveTopicAsync(int topicId);

		Task<List<Topic>> GetTopicsAsync();

		Task<Topic?> GetTopicAsync(int id);

		Task<Topic?> GetTopicAsync(string topicPath);

		Task<List<Data>> GetDataAsync(int topicId);

		Task<List<Data>> GetDataAsync(int topicId, int limit);

		Task<AreaPoint?> GetAreaPointsAsync(int topicId);

		Task AddDataAsync(Data data);
	}
}
