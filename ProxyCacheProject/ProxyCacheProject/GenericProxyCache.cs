using System;
using System.Runtime.Caching;



namespace ProxyCacheProject
{
    internal class GenericProxyCache<T> where T : class
    {
        private readonly MemoryCache _cache = MemoryCache.Default;
        private readonly int _timeToKeepMinutes;
        private readonly object _sync = new object();

        public GenericProxyCache(int timeToKeepInMinutes)
        {
            if (timeToKeepInMinutes <= 0) timeToKeepInMinutes = 5;
            _timeToKeepMinutes = timeToKeepInMinutes;
        }

        private CacheItemPolicy CreatePolicy()
        {
            return new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_timeToKeepMinutes)
            };
        }

        public T Get(string key)
        {
            return _cache.Get(key) as T;
        }

        public T GetOrAdd(string key, Func<T> valueFactory)
        {
            var existing = Get(key);
            if (existing != null) return existing;

            lock (_sync)
            {
                existing = Get(key);
                if (existing != null) return existing;

                var value = valueFactory();
                if (value == null) return null;

                _cache.Set(key, value, CreatePolicy());
                return value;
            }
        }
    }
}