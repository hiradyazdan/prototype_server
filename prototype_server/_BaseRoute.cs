using prototype_config;
using prototype_services.Interfaces;

namespace prototype_server
{
    public class _BaseRoutes
    {
        protected readonly ILogService LogService;
        protected readonly IRelayService RelayService;
        
        protected _BaseRoutes(ServiceConfiguration serviceConfig)
        {
            var services = serviceConfig.SharedServices;
            
            LogService = services.Log;
            RelayService = services.Relay;
            
            LogService.LogScope = this;
        }
    }
}