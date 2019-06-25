using System;
using Microsoft.Extensions.DependencyInjection;

using prototype_config;
using prototype_server.DB;

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

        protected static Guid ConvertBytesToGuid(byte[] valueBytes)
        {
            const int guidByteSize = 16;

            if (valueBytes.Length != guidByteSize)
            {
                Array.Resize(ref valueBytes, guidByteSize);
            }

            return new Guid(valueBytes);
        }
    }
}