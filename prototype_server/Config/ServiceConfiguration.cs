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
    public static class Configuration
    {
        public static IConfiguration SharedInstance { get; private set; }
        
        private const string ConfigFileName = "appsettings";
        private const string ConfigFileExtension = "json";

        private static IConfiguration BuildConfig(string[] args = null)
        {
            var appEnvironmentVariable = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "Production";
            
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{ConfigFileName}.{ConfigFileExtension}")
                .AddJsonFile($"{ConfigFileName}.{appEnvironmentVariable}.{ConfigFileExtension}",
                    optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .Build();
        }
        
        public static IConfiguration Initialize(string[] args = null)
        {
            return SharedInstance ?? (SharedInstance = BuildConfig(args));
        }
    }
    
    internal class ServiceConfiguration
    {
        public IServiceProvider ServiceProvider { get; }
        public static ServiceConfiguration SharedInstance { get; private set; }

        private bool _isClearRedisCache;
        private bool _isClearDatabase;
        private readonly (string Redis, string MySql) _connectionStrings;
        
        private ServiceConfiguration(IConfiguration configuration)
        {
            _connectionStrings = (
                Redis: configuration.GetConnectionString("RedisConnection"),
                MySql: configuration.GetConnectionString("MySqlConnection")
            );
            
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
#if DEBUG
            ConfigureDevelopment(configuration);
#endif
        }
        
        public static ServiceConfiguration Initialize(IConfiguration configuration)
        {
            return SharedInstance ?? (SharedInstance = new ServiceConfiguration(configuration));
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
             * DB Logging
             */
            services.AddLogging(logOptions => {
#if DEBUG
                logOptions.AddConsole().AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
#endif
            });

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
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif                
            });
            
            services.AddScoped(typeof(IRepository<>), typeof(ModelRepository<>));
        }

        private void ConfigureDevelopment(IConfiguration configuration)
        {
            _isClearDatabase = configuration.IsConfigActive("clearDatabase");
            _isClearRedisCache = configuration.IsConfigActive("clearRedisCache") || _isClearDatabase;

            if(!_isClearRedisCache && !_isClearDatabase) return;

            ResetRedisCache();
            
            if (!_isClearDatabase) return;
            
            ResetDevelopmentDatabases();
        }

        private void ResetRedisCache()
        {
            Console.WriteLine("Clear Redis Cache: " + _isClearRedisCache);
            
            ServiceProvider.GetService<RedisCache>().FlushAllDatabases();
            _isClearRedisCache = false;
        }
        
        private void ResetDevelopmentDatabases()
        {
            if (_isClearRedisCache)
            {
                ResetRedisCache();
            }

            Console.WriteLine("Clear Database: " + _isClearDatabase);
            
            ServiceProvider.GetService<GameDbContext>().Database.EnsureDeleted();
            ServiceProvider.GetService<GameDbContext>().Database.Migrate();
            _isClearDatabase = false;
        }
    }
}