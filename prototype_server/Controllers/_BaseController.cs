using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using MessagePack.Resolvers;

using prototype_config;
using prototype_services;
using prototype_storage;
using prototype_services.Interfaces;

namespace prototype_server.Controllers
{
    public interface IController
    {
        
    }
    
    public abstract class _BaseController : IController
    {
        protected Contexts Contexts;
        protected ISharedServiceCollection Services;
        
        protected readonly ILogService LogService;
        protected readonly IRelayService RelayService;
        protected readonly ICrudService CrudService;
        
        protected readonly IConfiguration Config;
        
        protected _BaseController()
        {
            Contexts = Contexts.sharedInstance;
            Services = ServiceConfiguration.Initialize().SharedServices;
            
            LogService = Services.Log;
            CrudService = Services.Crud;
            RelayService = Services.Relay;
            
            Config = AppConfiguration.SharedInstance;
            
            LogService.LogScope = this;
            
            SerializerConfiguration.RegisterMessagePackResolvers(
                false,
                CustomTypeResolver.Instance
            );
        }
    }
}