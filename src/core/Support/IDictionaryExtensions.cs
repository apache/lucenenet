using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class IDictionaryExtensions
    {
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }
    }
}