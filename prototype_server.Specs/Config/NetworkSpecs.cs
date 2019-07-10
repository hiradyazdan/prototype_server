using NSpec;
using prototype_config;
using prototype_services.Interfaces;

namespace prototype_server.Specs.Config
{
    public class NetworkSpecs : nspec
    {
        protected ILogService LogService;
        protected INetworkService NetworkService;
        
        protected void SetUp()
        {
            var services = ServiceConfiguration.SharedInstance.SharedServices;
            
            LogService = services.Log;
            NetworkService = services.Network;
        }
    }
}