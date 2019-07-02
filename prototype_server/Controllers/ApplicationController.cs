using System;
using Microsoft.Extensions.DependencyInjection;

using prototype_config;
using prototype_server.DB;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected readonly bool IsClientDebug;
        protected readonly bool IsSerialized;

        protected ApplicationController(IServiceScope scope, IRedisCache redis) : base(scope, redis)
        {
            IsClientDebug = Config.IsConfigActive("clientDebug");
            IsSerialized = Config.IsConfigActive("serializePackets");
            
            LogService.Log("Client Debug: " + IsClientDebug);
            LogService.Log("Serialize Packets: " + IsSerialized);
        }
    }
}