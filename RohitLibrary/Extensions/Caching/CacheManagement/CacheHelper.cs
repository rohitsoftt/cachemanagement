// Author: Rohit Jadhav
// Date: 18 April 2020
/// <summary>
/// This class is use to for cache management
/// Limitations:
/// 1. Not for Lifetime Cache or permanant cache
/// 2. Only use for Normal priority cache mangement
/// </summary>
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace RohitLibrary.Extensions.Caching.CacheManagement
{
    class CacheHelper
    {
        private readonly string _uniqueKeyGeneartor;
        private readonly int _cacheExpityTime;
        private readonly IMemoryCache _cache;
        public CacheHelper(int cacheExpiryTimeInMin, IMemoryCache cache, string uniqueKeyGeneartor = null)
        {
            if (string.IsNullOrEmpty(uniqueKeyGeneartor))
            {
                _uniqueKeyGeneartor = string.Empty;

            }
            else
            {
                _uniqueKeyGeneartor = uniqueKeyGeneartor;
            }
            _cache = cache ?? throw new ArgumentNullException("Memory cache object should not null");
            _cacheExpityTime = cacheExpiryTimeInMin;
        }
        public string GetCacheKey(string key)
        {
            return $"{key ?? throw new ArgumentNullException("key value should not null")}_{_uniqueKeyGeneartor}";
        }
        public DateTimeOffset? GetCacheExpiryTime(string key)
        {
            if (_cache.TryGetValue(key, out CacheModel<object> cacheModel))
            {
                return cacheModel.AbsoluteExpiration;
            }
            else
            {
                return DateTimeOffset.Now.AddMinutes(0);
            }
        }
        public bool TryGetCache<T>(string key, out T data)
        {
            if (_cache.TryGetValue(key, out CacheModel<T> cacheModel))
            {
                data = cacheModel.Data;
                return true;
            }
            else
            {
                _cache.TryGetValue(key, out data);
                return false;
            }
        }
        public bool SetCache<T>(string key, T obj)
        {
            var options = GetCacheEntryOptions();
            var cacheModel = GetCacheModel<T>(options.AbsoluteExpiration, obj);
            try
            {
                _cache.Set(key, cacheModel, options);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public async Task<bool> SetCacheAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            var data = await createAction();
            bool result = SetCache(key, data);
            return result;
        }
        public async Task<bool> UpdateCache<T>(string key, T obj)
        {
            if (_cache.TryGetValue(key, out CacheModel<T> data))
            {
                DateTimeOffset? dateTime = data.AbsoluteExpiration;
                var options = GetCacheEntryOptions(dateTime);
                var cacheModel = GetCacheModel(dateTime, obj);
                _cache.Set(key, cacheModel, options);
                return true;
            }
            return false;
        }
        public async Task<bool> UpdateCache<TItem>(string key, Func<Task<TItem>> createAction)
        {
            if (_cache.TryGetValue(key, out CacheModel<TItem> data))
            {
                var newData = await createAction();
                DateTimeOffset? dateTime = data.AbsoluteExpiration;
                var options = GetCacheEntryOptions(dateTime);
                var cacheModel = GetCacheModel(dateTime, newData);
                _cache.Set(key, cacheModel, options);
                return true;
            }
            return false;
        }
        public bool RefreshCache<T>(string key, T obj)
        {
            var options = GetCacheEntryOptions();
            var data = GetCacheModel<T>(options.AbsoluteExpiration, obj);
            try
            {
                _cache.Set(key, data, options);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public bool ExtendCacheLife<TItem>(string key, DateTimeOffset? dateTime = null)
        {
            if (TryGetCache(key, out CacheModel<TItem> cacheModel))
            {
                var options = GetCacheEntryOptions();
                var newDateTime = dateTime != null ? dateTime : options.AbsoluteExpiration;
                options.AbsoluteExpiration = newDateTime;
                cacheModel.AbsoluteExpiration = newDateTime;
                _cache.Set(key, cacheModel, options);
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<bool> CreateAndRefreshAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            var data = await createAction();
            bool result = SetCache(key, data);
            return result;
        }
        public async Task<TItem> GetOrCreateAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            CacheModel<TItem> response;
            var options = GetCacheEntryOptions();
            if (_cache.TryGetValue(key, out response))
            {
                return response.Data;
            }
            else
            {
                var data = await createAction();
                var cacheModel = GetCacheModel<TItem>(options.AbsoluteExpiration, data);
                _cache.Set(key, cacheModel, options);
                return cacheModel.Data;
            }
        }
        public async Task<bool> CreateAndUpdateAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            DateTimeOffset? dateTime;
            if (_cache.TryGetValue(key, out CacheModel<TItem> response))
            {
                dateTime = response.AbsoluteExpiration;
                var data = await createAction();
                var options = GetCacheEntryOptions(dateTime);
                var cacheModel = GetCacheModel<TItem>(options.AbsoluteExpiration, data);
                _cache.Set(key, cacheModel, options);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool RemoveCache(string key)
        {
            try
            {
                _cache.Remove(key);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private CacheModel<T> GetCacheModel<T>(DateTimeOffset? dateTime, T obj)
        {

            return new CacheModel<T>
            {
                AbsoluteExpiration = dateTime ?? DateTimeOffset.Now.AddMinutes(_cacheExpityTime),
                Data = obj
            };
        }
        private MemoryCacheEntryOptions GetCacheEntryOptions(DateTimeOffset? dateTime = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = dateTime ?? DateTimeOffset.Now.AddMinutes(_cacheExpityTime),
                Priority = CacheItemPriority.Normal
            };
            return options;
        }
    }
}
