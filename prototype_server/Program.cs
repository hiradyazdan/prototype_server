using System;
using System.Threading;

using prototype_config;
using prototype_services.Interfaces;

namespace prototype_server
{
    public class Program
    {
        private readonly ILogService _logService;
        private readonly IRelayService _relayService;
        private readonly IStorageService _storageService;
        
        private readonly Routes _routes;
        
        public static void Main(string[] args)
        {
            var serviceConfig = ServiceConfiguration.Initialize(args);
            
            var program = new Program(serviceConfig);
            
            program.Start();
            program.FixedUpdate();
            
            Console.ReadKey();
        }
        
        private Program(ServiceConfiguration serviceConfig)
        {
            var sharedServices = serviceConfig.SharedServices;
            
            _logService = sharedServices.Log;
            _relayService = sharedServices.Relay;
            _storageService = sharedServices.Storage;
            
            _routes = new Routes(serviceConfig);
            
            _logService.LogScope = this;
        }
        
        private void Start()
        {
            _storageService.ConfigureStorage();
            _routes.Start();
        }
        
        private void FixedUpdate()
        {
            while (_relayService.IsNetManagerRunning)
            {
                _routes.FixedUpdate();
                
                Thread.Sleep(15);
            }
        }
    }
}
