using System;
using Microsoft.Extensions.DependencyInjection;
using LiteNetLib.Utils;
using Microsoft.Extensions.Configuration;

using prototype_server.Config;
using prototype_server.DB;
using prototype_server.Models;

namespace prototype_server.Controllers
{
    public abstract class _BaseController
    {
        public NetDataWriter DataWriter { get; }

        protected readonly IConfiguration Config;
        protected readonly IConfigurationSection ConfigVars;
        protected readonly RedisCache Redis;

        protected readonly bool IsClearDatabaseActive;
        protected readonly IServiceScope Scope;
        
        protected _BaseController(IServiceScope scope, RedisCache redis)
        {
            Scope = scope;
            Config = Configuration.SharedInstance;
            ConfigVars = Config.GetSection("config:vars");

            Redis = redis;
            DataWriter = new NetDataWriter();
        }
    }
}