using System;
using System.Runtime.InteropServices;
using System.Threading;
using LiteNetLib;
using Microsoft.Extensions.Configuration;

using prototype_config;
using prototype_services;
using prototype_services.Interfaces;
using prototype_services.Common;

namespace prototype_server
{
    public class Program
    {
        private readonly ILogService _logService;
        
        public static void Main(string[] args)
        {
            var config = Configuration.Initialize(args);
            var serviceConfig = ServiceConfiguration.Initialize(config);
            
            var program = new Program(serviceConfig);
            
            var routes = new Routes(serviceConfig);
            var server = new NetManager(routes);
            
            routes.ServerInstance = server;
            
            program.Run(server, routes);
            
            Console.ReadKey();
        }
        
        private Program(ServiceConfiguration serviceConfig)
        {
            _logService = serviceConfig.SharedServices.Log;
            _logService.LogScope = this;
        }
        
        private void Run(NetManager server, RoutesBase routes)
        {
            if (server.Start(15000))
            {
                _logService.Log("Server started listening on port 15000");
            }
            else
            {
                _logService.LogError("Server could not start!");
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
        
        internal static int GetSize(this object obj)
        {
            var objectType = obj.GetType();
            
            objectType = obj is Enum ? Enum.GetUnderlyingType(objectType) : objectType;
            
            // Unmanaged object
            return Marshal.SizeOf(objectType);
        }
    }
}
