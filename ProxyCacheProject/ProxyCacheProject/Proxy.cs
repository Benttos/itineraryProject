using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ComponentModel;
using System.Net.Http;

namespace ProxyCacheProject
{
    enum TypeOfCache
    {
        [Description("Contract")]
        Contract,
        [Description("Station")]
        Station
    }

    public class Proxy : IProxy
    {
        // Dictionnaire de caches distincts par type
        private readonly Dictionary<TypeOfCache, GenericProxyCache<string>> _caches;
        private int nb = 0;
        HttpClient _httpClient = new HttpClient();
        public Proxy()
        {
            _caches = new Dictionary<TypeOfCache, GenericProxyCache<string>>
            {
                { TypeOfCache.Contract, new GenericProxyCache<string>(5) },
                { TypeOfCache.Station, new GenericProxyCache<string>(10) },
            };
        }

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public string LookForData(string link, string type)
        {
            CompositeType element = new CompositeType(link,type);
            return ConsultCache(element);
        }

        private string ConsultCache(CompositeType elementName)
        {
            string key = elementName.Link;

            var cache = GetCacheFor(elementName);

            var cached = cache.Get(key);
            if (cached != null)
            {
                return cached ;
            }

            cached = cache.GetOrAdd(key, () => callApi(elementName));
            return cached ;
        }

        private GenericProxyCache<string> GetCacheFor(CompositeType element)
        {
            TypeOfCache type = TypeOfCache.Contract;

            if (!string.IsNullOrWhiteSpace(element.Type))
            {
                TypeOfCache parsed;
                if (Enum.TryParse<TypeOfCache>(element.Type, true, out parsed))
                {
                    type = parsed;
                }
            }

            return _caches[type];
        }

        private string callApi(CompositeType elementName)
        {

            var asyncResult = _httpClient.GetStringAsync(elementName.Link);
            if(!asyncResult.Wait(5000))
            {
                return "Timeout calling API for "+elementName.Type;
            }
            return asyncResult.Result;
        }
    }
}