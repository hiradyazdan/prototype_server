using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.Models;

namespace prototype_server
{
    public class DependencyResolver
    {
        public IServiceProvider ServiceProvider { get; }
        
        public DependencyResolver()
        {
            IServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient(provider =>
            {
                const string configFileName = "appsettings";
                const string configFileExtension = "json";
            
                var appEnvironmentVariable = Environment.GetEnvironmentVariable("NETCOREAPP_ENVIRONMENT") ?? "Production";
                
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"{configFileName}.{configFileExtension}")
                    .AddJsonFile($"{configFileName}.{appEnvironmentVariable}.{configFileExtension}",
                        optional: true, reloadOnChange: true)
                    .Build();

                var optionsBuilder = new DbContextOptionsBuilder<GameDbContext>();

                optionsBuilder.UseMySQL(configuration.GetConnectionString("DefaultConnection"));
                
                return new GameDbContext(optionsBuilder.Options);
            });

            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = "127.0.0.1";
                option.InstanceName = "master";
            });
        }
    }
}