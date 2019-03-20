using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.Config;
using prototype_server.DB;

namespace prototype_server.Controllers
{       
    public abstract class _BaseController
    {
        protected readonly NetDataWriter DataWriter;
        protected readonly RedisCache RedisCache;
        protected readonly IServiceScope Scope;

        protected _BaseController()
        {
            var appConfig = new ServiceConfiguration();
            
            Scope = appConfig.ServiceProvider.CreateScope();
            RedisCache = appConfig.ServiceProvider.GetRequiredService<RedisCache>();     
            
            DataWriter = new NetDataWriter();
        }
    }
}