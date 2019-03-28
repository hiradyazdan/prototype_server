using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using prototype_server.Config;
using prototype_server.Models;

namespace prototype_server.DB
{
    public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
    {
        public GameDbContext CreateDbContext(string[] args)
        {
            var appConfig = new ServiceConfiguration();
            return appConfig.ServiceProvider.GetService(typeof(GameDbContext)) as GameDbContext;
        }
    }
    
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions options) : base(options)
        {}
        
        public GameDbContext()
        {}

        public DbSet<Player> Players { get; set; }
    }
}