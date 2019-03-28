using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace prototype_server.DB
{
    public interface IRedisCache
    {
        string GetCache(string key);
        void SetCache(string key, string value);
        void RemoveCache(string key);
    }
    
    public class RedisCache : IRedisCache
    {
        protected static IDatabase Cache;
        
        public RedisCache(string connectionString)
        {
            var connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString));
 
            Cache = connection.Value.GetDatabase();
        }
 
        public virtual string GetCache(string key)
        {            
            try
            {
                return Cache.StringGet(key);
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }
 
        public virtual void SetCache(string key, string value)
        {            
            Cache.StringSet(key, value);
        }
 
        public virtual void RemoveCache(string key)
        {
            try
            {
                Cache.KeyDelete(key);
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
            }
        }
    }
}