using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using prototype_server.DB;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected readonly bool IsSerialized;

        protected ApplicationController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {
            IsSerialized = Config.IsConfigActive("serializePackets");

#if DEBUG
            Console.WriteLine("Serialize Packets: " + IsSerialized);
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