using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

namespace prototype_server.Controllers
{
    public class ApplicationController : _BaseController
    {
        protected ApplicationController(IServiceScope scope, RedisCache redis) : base(scope, redis)
        {}
    }
}