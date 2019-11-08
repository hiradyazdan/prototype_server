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
            var config = AppConfiguration.Initialize(args);
            var serviceConfig = ServiceConfiguration.Initialize(config);
            
            var program = new Program(serviceConfig);
            
            program.Start();
            program.FixedUpdate();
            
            Console.ReadKey();
        }
        
        private Program(ServiceConfiguration serviceConfig)
        {
            _logService = serviceConfig.SharedServices.Log;
            _relayService = serviceConfig.SharedServices.Relay;
            _storageService = serviceConfig.SharedServices.Storage;
            
            _storageService.ConfigureStorage();
            
            _routes = new Routes(serviceConfig);
            
            _logService.LogScope = this;
        }
        
        private void Start()
        {
            _routes.Start();
        }
        
        private void FixedUpdate()
        {
            while (_relayService.NetManager.IsRunning)
            {
                _routes.FixedUpdate();
                
                Thread.Sleep(15);
            }
        }
    }
}
