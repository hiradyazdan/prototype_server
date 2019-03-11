using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

namespace prototype_server
{
    internal class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }
        
        private const string ConfigFileName = "appsettings";
        private const string ConfigFileExtension = "json";
        private readonly (string Redis, string MySql) _connectionStrings;
        
        public DependencyResolver()
        {
            var appEnvironmentVariable = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "Production";
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{ConfigFileName}.{ConfigFileExtension}")
                .AddJsonFile($"{ConfigFileName}.{appEnvironmentVariable}.{ConfigFileExtension}",
                    optional: true, reloadOnChange: true)
                .Build();

            _connectionStrings = (
                Redis: configuration.GetConnectionString("RedisConnection"),
                MySql: configuration.GetConnectionString("MySqlConnection")
            );
            
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            var redisOptions = _connectionStrings.Redis;
            var mysqlOptions = _connectionStrings.MySql;
            
            if (redisOptions == null || mysqlOptions == null)
            {
                Console.Error.WriteLine("Missing ConnectionString values!");
                return;
            }
            
            /**
             * Redis
             */
            services.AddTransient(provider => new RedisCache(redisOptions));
            
            /**
             * MySQL
             */
            services.AddTransient(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<GameDbContext>();

                optionsBuilder.UseMySQL(mysqlOptions);
                
                return new GameDbContext(optionsBuilder.Options);
            });
        }
    }
}