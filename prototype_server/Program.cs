using System;
using Microsoft.Extensions.DependencyInjection;
using prototype_server.DB;

namespace prototype_server
{
    public class Program
    {   
        public static void Main(string[] args)
        {
            var resolver = new DependencyResolver();
            var redisCache = resolver.ServiceProvider.GetRequiredService(typeof(RedisCache)) as RedisCache;
            var app = new App(redisCache);
            
            app.Run();
            Console.ReadKey();
        }
    }
}
