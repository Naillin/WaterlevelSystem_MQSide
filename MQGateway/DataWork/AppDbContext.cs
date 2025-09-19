using Microsoft.EntityFrameworkCore;
using MQGateway.Core.Entities;

namespace MQGateway.DataWork
{
	internal class AppDbContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Topic> Topics { get; set; }
		public DbSet<Data> Data { get; set; }
		public DbSet<AreaPoint> AreaPoints { get; set; }

		//private readonly string _connectionString;

		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

		//public AppDbContext(string connectionString) => _connectionString = connectionString;

		//protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		//{
		//	optionsBuilder.UseNpgsql(_connectionString);
		//}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// Настройка User
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(u => u.ID_User);
				entity.Property(u => u.Login_User).IsRequired();
				entity.Property(u => u.Password_User).IsRequired();
				entity.HasIndex(u => u.Login_User).IsUnique();
			});

			// Настройка Topic
			modelBuilder.Entity<Topic>(entity =>
			{
				entity.HasKey(t => t.ID_Topic);
				entity.Property(t => t.Name_Topic).IsRequired();
				entity.Property(t => t.Path_Topic).IsRequired();
				entity.Property(t => t.Latitude_Topic).IsRequired();
				entity.Property(t => t.Longitude_Topic).IsRequired();
				entity.Property(t => t.Altitude_Topic).IsRequired();
				entity.Property(t => t.AltitudeSensor_Topic).IsRequired();
			});

			// Настройка Data (каскад уже определен в SQL)
			modelBuilder.Entity<Data>(entity =>
			{
				entity.HasKey(d => d.ID_Data);
				entity.Property(d => d.Time_Data).IsRequired();
				entity.HasOne(d => d.Topic)
					  .WithMany(t => t.Data)
					  .HasForeignKey(d => d.ID_Topic)
					  .IsRequired();
			});

			// Настройка AreaPoint (каскад уже определен в SQL)
			modelBuilder.Entity<AreaPoint>(entity =>
			{
				entity.HasKey(a => a.ID_AreaPoint);
				entity.Property(a => a.Depression_AreaPoint).IsRequired();
				entity.HasOne(a => a.Topic)
					  .WithMany(t => t.AreaPoints)
					  .HasForeignKey(a => a.ID_Topic)
					  .IsRequired();
			});
		}
	}
}
