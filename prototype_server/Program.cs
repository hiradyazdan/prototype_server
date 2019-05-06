using System;
using System.Threading;
using LiteNetLib;
using Microsoft.Extensions.Configuration;

using prototype_server.Config;

namespace prototype_server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = Configuration.Initialize(args);
            var routes = new Routes(config);
            var server = new NetManager(routes);
            
            routes.ServerInstance = server;
            
            Run(server, routes);
            
            Console.ReadKey();
        }

        private static void Run(NetManager server, RoutesBase routes)
        {
            if (server.Start(15000))
            {
                Console.WriteLine("Server started listening on port 15000");
            }
            else
            {
                Console.Error.WriteLine("Server could not start!");
                return;
            }

            while (server.IsRunning)
            {
                server.PollEvents();
                routes.SyncWithConnectedPeers();

                Thread.Sleep(15);
            }
        }
    }
    
    internal static class Extensions
    {
        internal static bool CaseInsensitiveContains(this string text, string value, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }

        internal static bool IsConfigActive(this IConfiguration configuration, string key)
        {
            var configVars = configuration.GetSection("config:vars");
            
            return configuration.GetValue<string>(key)?.CaseInsensitiveContains("true") ?? 
                   configVars.GetSection(key).Value.CaseInsensitiveContains("true");
        }
    }
}
