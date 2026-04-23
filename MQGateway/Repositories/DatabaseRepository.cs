using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MQGateway.Core.Interfaces;
using WaterlevelSystem_DataBaseStructure;
using WaterlevelSystem_DataBaseStructure.Entities;

namespace MQGateway.Repositories;

public class DatabaseRepository : IDataRepository
{
	private readonly IDbContextFactory<AppDbContext> _factory;

	public DatabaseRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	public async Task<User?> GetUserAsync(string login, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Login_User == login, cancellationToken);
	}

	public async Task AddTopicAsync(Topic topic, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		await db.Topics.AddAsync(topic);
		await db.SaveChangesAsync();
	}

	public async Task<int?> RemoveTopicAsync(int topicId, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Topics
			.Where(t => t.ID_Topic == topicId)
			.ExecuteDeleteAsync(cancellationToken);
	}

	public async Task<List<Topic>> GetTopicsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Topics
			.AsNoTracking()
			.ToListAsync(cancellationToken);
	}

	public async Task<Topic?> GetTopicAsync(int topicId, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Topics
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.ID_Topic == topicId, cancellationToken);
	}

	public async Task<Topic?> GetTopicAsync(string topicPath, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Topics
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.Path_Topic == topicPath, cancellationToken);
	}

	public async Task<List<Data>> GetDataAsync(int topicId, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Data
			.Where(d => d.ID_Topic == topicId)
			.OrderBy(d => d.Time_Data)
			.ToListAsync(cancellationToken);
	}

	public async Task<List<Data>> GetDataAsync(int topicId, int limit, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.Data
			.Where(d => d.ID_Topic == topicId)
			.OrderBy(d => d.Time_Data)
			.Take(limit)
			.ToListAsync(cancellationToken);
	}

	public async Task<AreaPoint?> GetAreaPointsAsync(int topicId, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		return await db.AreaPoints
			.AsNoTracking()
			.FirstOrDefaultAsync(d => d.ID_Topic == topicId, cancellationToken);
	}

	public async Task AddDataAsync(Data data, CancellationToken cancellationToken = default)
	{
		await using var db = await _factory.CreateDbContextAsync(cancellationToken);
		await db.Data.AddAsync(data, cancellationToken);
		await db.SaveChangesAsync(cancellationToken);
	}

	public async Task UpsertPredictionData(int topicId, string? points, IList<Ema>? emas, IList<Prediction>? predictions, CancellationToken cancellationToken = default)
	{
		var emasJson = emas != null
			? JsonSerializer.Serialize(emas)
			: null;
		var predsJson = predictions != null
			? JsonSerializer.Serialize(predictions)
			: null;

		await using var db = await _factory.CreateDbContextAsync(cancellationToken);

		await db.Database.ExecuteSqlInterpolatedAsync(
			$"SELECT public.upsert_flood_data_package({topicId}, {points}, {emasJson}::jsonb, {predsJson}::jsonb)",
			cancellationToken
		);
	}
}