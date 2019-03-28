using System;
using System.Collections.Generic;
using prototype_server.DB;

namespace prototype_server.Specs.Config.Utils
{
    public class RedisCacheAdapter : RedisCache
    {
        private readonly Dictionary<string, string> _cache;
        
        public RedisCacheAdapter(string connectionString) : base(connectionString)
        {
            _cache = new Dictionary<string, string>();
        }

        public override string GetCache(string key)
        {
            try
            {
                return _cache[key];
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
                return null;
            }
        }

        public override void SetCache(string key, string value)
        {
            _cache.Add(key, value);
        }

        public override void RemoveCache(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch (KeyNotFoundException exc)
            {
                Console.WriteLine(exc);
            }
        }
    }
}