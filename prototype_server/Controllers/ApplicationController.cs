using System;
using Microsoft.Extensions.DependencyInjection;

using prototype_config;
using prototype_storage;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected readonly bool IsSerialized;

        protected ApplicationController(IServiceScope scope, IRedisCache redis) : base(scope, redis)
        {
            IsSerialized = Config.IsConfigActive("serializePackets");
            
            LogService.Log("Serialize Packets: " + IsSerialized);
        }
    }
}