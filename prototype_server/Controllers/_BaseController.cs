using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using MessagePack.Resolvers;

using prototype_config;
using prototype_storage;
using prototype_services.Interfaces;

namespace prototype_server.Controllers
{
    public interface IController
    {
        
    }
    
    public abstract class _BaseController : IController
    {
        protected readonly ILogService LogService;
        protected readonly INetworkService NetworkService;
        protected readonly IHttpService HttpService;
        protected readonly IConfiguration Config;
        protected readonly IRedisCache Redis;
        protected readonly IServiceScope Scope;
        
        protected _BaseController(IServiceScope scope, IRedisCache redis)
        {
            Scope = scope;
            Config = AppConfiguration.SharedInstance;
            
            var services = ServiceConfiguration.SharedInstance.SharedServices;
            
            LogService = services.Log;
            NetworkService = services.Network;
            HttpService = services.Http;
            
            LogService.LogScope = this;
            
            Redis = redis;
            
            SerializerConfiguration.RegisterMessagePackResolvers(
                false,
                CustomTypeResolver.Instance
            );
        }
    }
}