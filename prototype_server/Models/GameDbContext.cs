using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace prototype_server.Models
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