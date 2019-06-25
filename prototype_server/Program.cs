﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var config = AppConfiguration.Initialize(args);
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
}
