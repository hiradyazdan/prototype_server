using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace prototype_server.DB
{
    public interface IRedisCache
    {
        void FlushAllDatabases();
        string GetCache(string key);
        void SetCache(string key, string value);
        void RemoveCache(string key);
    }
    
    public class RedisCache : IRedisCache
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly string _connectionString;
        private static IDatabase _cache;
        
        public RedisCache(string connectionString)
        {
            _connectionString = connectionString;
            
            var options = ConfigurationOptions.Parse(_connectionString);
#if DEBUG
            options.AllowAdmin = true;
#endif
            _connection = ConnectionMultiplexer.Connect(options);
            
            var connection = new Lazy<ConnectionMultiplexer>(() => (ConnectionMultiplexer) _connection);
            
            _cache = connection.Value.GetDatabase();
        }
        
        public virtual void FlushAllDatabases()
        {
#if DEBUG
            _connection.GetServer(_connectionString).FlushAllDatabases();
#endif
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