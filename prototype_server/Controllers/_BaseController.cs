using Microsoft.Extensions.DependencyInjection;
using LiteNetLib.Utils;
using Microsoft.Extensions.Configuration;

using prototype_config;
using prototype_server.DB;
using prototype_services.Interfaces;

namespace prototype_server.Controllers
{
    public interface IController
    {
        
    }
    
    public abstract class _BaseController : IController
    {
        public NetDataWriter DataWriter { get; }

        protected readonly ILogService LogService;
        protected readonly IConfiguration Config;
        protected readonly IConfigurationSection ConfigVars;
        protected readonly IRedisCache Redis;

        protected readonly bool IsClearDatabaseActive;
        protected readonly IServiceScope Scope;
        
        protected _BaseController(IServiceScope scope, IRedisCache redis)
        {
            var services = ServiceConfiguration.SharedInstance.SharedServices;
            
            LogService = services.Log;
            LogService.LogScope = this;
            
            Scope = scope;
            Config = Configuration.SharedInstance;
            ConfigVars = Config.GetSection("config:vars");

            Redis = redis;
            DataWriter = new NetDataWriter();
        }
    }
}