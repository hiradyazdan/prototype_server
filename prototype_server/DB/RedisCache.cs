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
        private static IDatabase _cache;
        
        public RedisCache(string connectionString)
        {
            var connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString));
 
            _cache = connection.Value.GetDatabase();
        }
 
        public virtual string GetCache(string key)
        {            
            try
            {
                return _cache.StringGet(key);
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }
 
        public virtual void SetCache(string key, string value)
        {            
            _cache.StringSet(key, value);
        }
 
        public virtual void RemoveCache(string key)
        {
            try
            {
                _cache.KeyDelete(key);
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
            }
        }
    }
}