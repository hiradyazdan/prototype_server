using LiteNetLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

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