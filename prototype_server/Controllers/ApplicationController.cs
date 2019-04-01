using System;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected ApplicationController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {}

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