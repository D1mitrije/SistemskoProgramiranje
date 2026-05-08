namespace SistemskoProgramiranje
{
    public class ExpiringCache
    {
        private class CacheItem
        {
            public WordCountResult? Value { get; set; }

            public DateTime CreatedAt { get; set; }

            public bool IsLoading { get; set; }

            public Exception? Error { get; set; }
        }

        private readonly Dictionary<string, CacheItem> cache =
            new Dictionary<string, CacheItem>();

        private readonly object cacheLock =
            new object();

        private readonly TimeSpan expirationTime;

        private readonly SafeLogger logger;

        public ExpiringCache(
            TimeSpan expirationTime,
            SafeLogger logger)
        {
            this.expirationTime = expirationTime;
            this.logger = logger;
        }

        public WordCountResult GetOrCreate(
            string key,
            Func<WordCountResult> factory)
        {
            CacheItem item;

            bool shouldLoad = false;

            lock (cacheLock)
            {
                if (cache.TryGetValue(key, out item!))
                {
                    bool expired =
                        DateTime.Now - item.CreatedAt >
                        expirationTime;

                    if (!expired &&
                        item.Value != null)
                    {
                        logger.Log($"CACHE HIT: {key}");

                        return item.Value.CloneAsCached();
                    }

                    if (item.IsLoading)
                    {
                        logger.Log($"CACHE WAIT: {key}");

                        while (item.IsLoading)
                        {
                            Monitor.Wait(cacheLock);
                        }

                        if (item.Value != null)
                        {
                            return item.Value.CloneAsCached();
                        }
                    }

                    item.IsLoading = true;

                    shouldLoad = true;
                }
                else
                {
                    logger.Log($"CACHE MISS: {key}");

                    item = new CacheItem
                    {
                        IsLoading = true,
                        CreatedAt = DateTime.Now
                    };

                    cache[key] = item;

                    shouldLoad = true;
                }
            }

            if (shouldLoad)
            {
                try
                {
                    WordCountResult result =
                        factory();

                    lock (cacheLock)
                    {
                        item.Value = result;

                        item.CreatedAt =
                            DateTime.Now;

                        item.IsLoading = false;

                        Monitor.PulseAll(cacheLock);
                    }

                    return result;
                }
                catch
                {
                    lock (cacheLock)
                    {
                        cache.Remove(key);

                        item.IsLoading = false;

                        Monitor.PulseAll(cacheLock);
                    }

                    throw;
                }
            }

            throw new Exception("Cache greska.");
        }
    }
}