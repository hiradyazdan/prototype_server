using prototype_config;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected readonly bool IsSerialized;
        
        protected ApplicationController()
        {
            IsSerialized = Config.IsConfigActive("serializePackets");
            
            LogService.Log("Serialize Packets: " + IsSerialized);
        }
    }
}