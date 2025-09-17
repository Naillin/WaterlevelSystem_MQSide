using Microsoft.EntityFrameworkCore;
using MQGateway.Core.Entities;
using MQGateway.Core.Interfaces;

namespace MQGateway.DataWork.Repositories
{
	internal class DatabaseRepository : IDataRepository
	{
		private readonly IDbContextFactory<AppDbContext> _factory;

		public DatabaseRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

		public async Task<User?> GetUserAsync(string login)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Login_User == login);
		}

		public async Task AddTopicAsync(Topic topic)
		{
			await using var db = await _factory.CreateDbContextAsync();
			await db.Topics.AddAsync(topic);
			await db.SaveChangesAsync();
		}

		public async Task<int?> RemoveTopicAsync(int topicId)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics
				.Where(t => t.ID_Topic == topicId)
				.ExecuteDeleteAsync();
		}

		public async Task<List<Topic>> GetTopicsAsync()
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Topic?> GetTopicAsync(int topicId)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics
				.AsNoTracking()
				.FirstOrDefaultAsync(t => t.ID_Topic == topicId);
		}

		public async Task<Topic?> GetTopicAsync(string topicPath)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Topics
				.AsNoTracking()
				.FirstOrDefaultAsync(t => t.Path_Topic == topicPath);
		}

		public async Task<List<Data>> GetDataAsync(int topicId)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Data
				.Where(d => d.ID_Topic == topicId)
				.OrderByDescending(d => d.Time_Data)
				.ToListAsync();
		}

		public async Task<List<Data>> GetDataAsync(int topicId, int limit)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.Data
				.Where(d => d.ID_Topic == topicId)
				.OrderByDescending(d => d.Time_Data)
				.Take(limit)
				.ToListAsync();
		}

		public async Task<AreaPoint?> GetAreaPointsAsync(int topicId)
		{
			await using var db = await _factory.CreateDbContextAsync();
			return await db.AreaPoints
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.ID_Topic == topicId);
		}

		public async Task AddDataAsync(Data data)
		{
			await using var db = await _factory.CreateDbContextAsync();
			await db.Data.AddAsync(data);
			await db.SaveChangesAsync();
		}
	}
}
