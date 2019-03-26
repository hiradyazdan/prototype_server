using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

#if DEBUG
    using prototype_server.Libs.LiteNetLib.Utils;
#else
    using LiteNetLib.Utils;
#endif

namespace prototype_server.Controllers
{       
    public abstract class _BaseController
    {
        protected readonly RedisCache Redis;
        public readonly NetDataWriter DataWriter;

        protected _BaseController(IServiceScope scope, RedisCache redis)
        {            
            Redis = redis;
            DataWriter = new NetDataWriter();
        }
    }
}