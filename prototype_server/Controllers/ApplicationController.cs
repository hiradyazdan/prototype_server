using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using prototype_server.DB;
using prototype_services.Interfaces;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected readonly bool IsSerialized;

        protected ApplicationController(IServiceScope scope, IRedisCache redis) : base(scope, redis)
        {
            IsSerialized = Config.IsConfigActive("serializePackets");

#if DEBUG
            LogService.Log("Serialize Packets: " + IsSerialized);
#endif
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