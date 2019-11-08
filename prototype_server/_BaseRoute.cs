using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using prototype_config;
using prototype_storage;
using prototype_services;
using prototype_services.Interfaces;

namespace prototype_server
{
    public class _BaseRoutes
    {
        protected readonly ISharedServiceCollection Services;
        
        protected readonly ILogService LogService;
        protected readonly IRelayService RelayService;
        
        protected readonly IConfiguration Config;
        
        public _BaseRoutes(ServiceConfiguration serviceConfig)
        {
            Services = serviceConfig.SharedServices;
            
            LogService = Services.Log;
            RelayService = Services.Relay;
            
            Config = AppConfiguration.SharedInstance;
            
            LogService.LogScope = this;
        }
    }
}