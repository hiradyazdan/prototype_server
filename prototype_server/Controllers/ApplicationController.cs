namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected ApplicationController()
        {
            LogService.Log("Serialize Packets: " + SerializerConfig.IsActive);
        }
    }
}