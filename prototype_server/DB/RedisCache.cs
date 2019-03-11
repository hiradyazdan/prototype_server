using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace prototype_server.DB
{
    public class RedisCache
    {
        private static IDatabase _cache;
        
        public RedisCache(string connectionString)
        {
            var connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connectionString));
 
            _cache = connection.Value.GetDatabase();
        }
 
        public string GetCache(string key)
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
 
        public void SetCache(string key, string value)
        {            
            _cache.StringSet(key, value);
        }
 
        public void RemoveCache(string key)
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