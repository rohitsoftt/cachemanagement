// Author: Rohit Jadhav
// Date: 18 April 2020
using System;
using System.Collections.Generic;
using System.Text;

namespace RohitLibrary.Extensions.Caching.CacheManagement
{
    internal class CacheModel<T>
    {
        /// <summary>
        /// Value to add as a cache
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Expiry time of cache
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }
    }
}
