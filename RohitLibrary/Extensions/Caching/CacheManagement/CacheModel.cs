// Author: Rohit Jadhav
// Date: 18 April 2020
using System;
using System.Collections.Generic;
using System.Text;

namespace RohitLibrary.Extensions.Caching.CacheManagement
{
    internal class CacheModel<T>
    {
        public T Data { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
    }
}
