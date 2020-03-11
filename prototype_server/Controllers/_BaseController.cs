using System;

using MessagePack.Resolvers;

using prototype_config;
using prototype_models;
using prototype_serializers.JSON;
using prototype_services;
using prototype_services.Interfaces;

namespace prototype_server.Controllers
{
    public interface IController
    {
        
    }
    
    public abstract class _BaseController : IController
    {
        protected readonly Contexts Contexts;
        protected readonly ISharedServiceCollection Services;
        protected readonly SerializerConfiguration SerializerConfig;
        
        protected readonly ILogService LogService;
        protected readonly IRelayService RelayService;
        protected readonly ICrudService CrudService;
        
        protected _BaseController()
        {
            Contexts = Contexts.sharedInstance;
            Services = ServiceConfiguration.SharedInstance.SharedServices;
            
            SerializerConfig = SerializerConfiguration.Initialize();
            
            LogService = Services.Log;
            CrudService = Services.Crud;
            RelayService = Services.Relay;
            
            LogService.LogScope = this;
            
            SerializerConfiguration.RegisterMessagePackResolvers(
                false,
                CustomTypeResolver.Instance
            );
        }
        
        public void StoreAppData()
        {
            var relayServer = new RelayServerModel
            {
                UdpHostIpv4 = RelayService.UdpHostIpv4,
                UdpHostIpv6 = RelayService.UdpHostIpv6,
                UdpPort = RelayService.UdpPort
            };
            
            var reqData = JsonSerializer.ToJson(relayServer, new []
            {
                "action_type",
                "object_type",
                "id",
                "is_local"
            });
            
            var postRes = CrudService.RequestStringAsync(
                $"{ContentApiEndpoints.RELAY_SERVERS}",
                reqData
            ).Result;
            
            RelayService.UdpConnKey = JsonSerializer.FromJson<RelayServerModel>(postRes)?.UdpConnKey;

            if (RelayService.UdpConnKey != null) return;
            
            LogService.LogError("Server is shutting down as \"UDPConnectionKey\" was not generated!");
            Environment.Exit(6);
        }
    }
}