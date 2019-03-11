using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using prototype_server.Models;

namespace prototype_server.DB
{
    public class GameDbContextFactory : IDesignTimeDbContextFactory<GameDbContext>
    {
        public GameDbContext CreateDbContext(string[] args)
        {
            var resolver = new DependencyResolver();
            return resolver.ServiceProvider.GetService(typeof(GameDbContext)) as GameDbContext;
        }
    }
    
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions options) : base(options)
        { }

        public DbSet<Player> Player { get; set; }
    }
}