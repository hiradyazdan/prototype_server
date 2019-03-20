using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using prototype_server.DB;

namespace prototype_server.Config
{
    internal class ServiceConfiguration
    {
        public IServiceProvider ServiceProvider { get; }
        
        private const string ConfigFileName = "appsettings";
        private const string ConfigFileExtension = "json";
        private readonly (string Redis, string MySql) _connectionStrings;
        private static readonly LoggerFactory MySqlLoggerFactory = new LoggerFactory(new[]
        {
            new ConsoleLoggerProvider((category, level) => 
                category == DbLoggerCategory.Database.Command.Name && 
                level == LogLevel.Information, true)
        });
        
        public ServiceConfiguration()
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
            services.AddDbContext<GameDbContext>(options =>
            {
                options.UseMySQL(mysqlOptions);
                options.UseLoggerFactory(MySqlLoggerFactory);

#if DEBUG
                options.EnableSensitiveDataLogging();
#endif                
            });
            
            services.AddScoped(typeof(IRepository<>), typeof(ModelRepository<>));  
        }
    }
}