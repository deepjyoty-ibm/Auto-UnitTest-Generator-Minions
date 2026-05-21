using System;
using System.Collections.Generic;
using System.Text;

namespace SampleConsoleApp.Utility
{
    public class GenericCache<T>
    {
        private readonly Dictionary<string, T> _cache = new();

        public void Add(string key, T value)
        {
            _cache[key] = value;
        }

        public T? Get(string key)
        {
            _cache.TryGetValue(key, out var value);

            return value;
        }
    }
}
