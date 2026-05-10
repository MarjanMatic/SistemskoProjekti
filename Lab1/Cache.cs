using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Projekat1
{
    struct CacheEntry
    {
        public DateTimeOffset timestamp;
        public JObject json;
    }
    internal class Cache
    {

        private readonly ConcurrentDictionary<string, CacheEntry> cache;
        private const int CACHE_TIMELIMIT_SECONDS = 60;

        public Cache()
        {
            cache = new ConcurrentDictionary<string, CacheEntry>();
        }

        public JObject? get(string key)
        {
            if (cache.TryGetValue(key, out CacheEntry entry))
            {
                TimeSpan timeInCache = DateTimeOffset.UtcNow - entry.timestamp;
                if (timeInCache.TotalSeconds < CACHE_TIMELIMIT_SECONDS)
                {
                    return entry.json;
                }
            }

            return null;
        }

        public void set(string key, JObject value)
        {
            CacheEntry newEntry = new CacheEntry();
            newEntry.timestamp = DateTimeOffset.UtcNow;
            newEntry.json = value;

            cache[key] = newEntry;
        }

    }
}
