using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.Config;
using prototype_server.Models;

namespace prototype_server.DB
{
    public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
    {
        public GameDbContext CreateDbContext(string[] args)
        {
            var config = Configuration.Initialize(args);
            var svcConfig = ServiceConfiguration.Initialize(config);
            
            return svcConfig.ServiceProvider.GetService<GameDbContext>();
        }
    }
    
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions options) : base(options)
        {}
        
        public GameDbContext()
        {}

        public DbSet<Player> Players { get; set; }
        
        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }
 
        public async Task<int> SaveChangesAsync()
        {
            AddTimestamps();
            return await base.SaveChangesAsync();
        }

        private void AddTimestamps()
        {
            var entities = ChangeTracker.Entries().Where(x => x.Entity is _BaseModel && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    ((_BaseModel)entity.Entity).CreatedAt = DateTime.UtcNow;
                }

                ((_BaseModel)entity.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TODO: should change to binary(16)/varbinary(16)
            var converter = new GuidToStringConverter();

            modelBuilder.Entity<Player>()
                        .Property(p => p.GUID)
                        .HasConversion(converter);
            
            modelBuilder.Entity<Player>()
                        .HasIndex(p => new { p.GUID })
                        .IsUnique();
        }
    }
}