#region Assembly Microsoft.Extensions.Caching.Abstractions, Version=5.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
// C:\Users\xxx\.nuget\packages\microsoft.extensions.caching.abstractions\5.0.0-preview.2.20160.3\lib\netstandard2.0\Microsoft.Extensions.Caching.Abstractions.dll
#endregion
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
    /// <summary>
    /// Dependancy on 5.0.0-preview.2.20160.3\lib\netstandard2.0\Microsoft.Extensions.Caching.Abstractions.dll
    /// nuget package, make sure IMemoryCache value should same as current used version.
    /// </summary>
    public class CacheHelper
    {
        /// <summary>
        /// unique key generator use to generate unique
        /// </summary>
        private readonly string _uniqueKeyGenerator;

        /// <summary>
        /// Cache expity time in minutes
        /// </summary>
        private readonly int _cacheExpityTime;

        /// <summary>
        /// IMemoryCache value
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// CacheHelper Constructor 
        /// IMemoryCache should not null.
        /// </summary>
        /// <param name="cacheExpiryTimeInMin"></param>
        /// <param name="cache"></param>
        /// <param name="uniqueKeyGenerator"></param>
        public CacheHelper(int cacheExpiryTimeInMin, IMemoryCache cache, string uniqueKeyGeneartor = null)
        {
            if (string.IsNullOrEmpty(uniqueKeyGeneartor))
            {
                _uniqueKeyGenerator = string.Empty;
            }
            else
            {
                _uniqueKeyGenerator = uniqueKeyGeneartor;
            }
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _cacheExpityTime = cacheExpiryTimeInMin;
        }

        /// <summary>
        /// To get cache key with the combination of uniqueKeyGeneartor and passed key value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetCacheKey(string key)
        {
            return $"{key ?? throw new ArgumentNullException(nameof(key))}_{_uniqueKeyGenerator}";
        }

        /// <summary>
        /// To get cache value expiry time by key
        /// if cache value already expired, method will return null value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DateTimeOffset? GetCacheExpiryTime(string key)
        {
            if (_cache.TryGetValue(key, out CacheModel<object> cacheModel))
            {
                return cacheModel.AbsoluteExpiration;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Try to get value by key
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool TryGetCache<TItem>(string key, out TItem value)
        {
            if (_cache.TryGetValue(key, out CacheModel<TItem> cacheModel))
            {
                value = cacheModel.Data;
                return true;
            }
            else
            {
                _cache.TryGetValue(key, out value);
                return false;
            }
        }

        /// <summary>
        /// Set cache by key : 
        /// returns boolean value
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns>
        /// </returns>
        public bool SetCache<TItem>(string key, TItem value)
        {
            var options = GetCacheEntryOptions();
            var cacheModel = GetCacheModel<TItem>(options.AbsoluteExpiration, value);
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

        /// <summary>
        /// Set cache value by key
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="createAction"></param>
        /// <returns></returns>
        public async Task<bool> SetCacheAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            var data = await createAction();
            bool result = SetCache(key, data);
            return result;
        }

        /// <summary>
        /// Updates cache by key if present in memory
        /// else will not update. cache expiry time will be same after update
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool UpdateCache<TItem>(string key, TItem value)
        {
            if (_cache.TryGetValue(key, out CacheModel<TItem> data))
            {
                DateTimeOffset? dateTime = data.AbsoluteExpiration;
                var options = GetCacheEntryOptions(dateTime);
                var cacheModel = GetCacheModel(dateTime, value);
                _cache.Set(key, cacheModel, options);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates cache by key if present in memory.
        /// if cache already expired then passed method will not execute
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="createAction"></param>
        /// <returns></returns>
        public async Task<bool> UpdateCacheAsync<TItem>(string key, Func<Task<TItem>> createAction)
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

        /// <summary>
        /// Updates existing cache with new time and new value
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool RefreshCache<TItem>(string key, TItem value)
        {
            var options = GetCacheEntryOptions();
            var data = GetCacheModel<TItem>(options.AbsoluteExpiration, value);
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

        /// <summary>
        /// Set value if cache already exist in memory with old value
        /// It's set new value and extends expiry time.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="createAction"></param>
        /// <returns></returns>
        public async Task<bool> RefreshCacheAsync<TItem>(string key, Func<Task<TItem>> createAction)
        {
            if(_cache.TryGetValue(key, out CacheModel<TItem> cacheModel))
            {
                var options = GetCacheEntryOptions();
                var result = await createAction();
                var data = GetCacheModel<TItem>(options.AbsoluteExpiration, result);
                _cache.Set(key, data, options);
                return true;
            }
            return false;
        }

        /// <summary>
        /// extends existing cache life if present in memory.
        /// uses default expiry time if not passed to the method
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets cache if present in memory else creates new one
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="key"></param>
        /// <param name="createAction"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Removes cache by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
        private CacheModel<TItem> GetCacheModel<TItem>(DateTimeOffset? dateTime, TItem value)
        {

            return new CacheModel<TItem>
            {
                AbsoluteExpiration = dateTime ?? DateTimeOffset.Now.AddMinutes(_cacheExpityTime),
                Data = value
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
