using System.Collections.Generic;

namespace Lucene.Net.Support
{
    public static class DictionaryExtensions
    {
        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            foreach (var kvp in kvps)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        public static ISet<KeyValuePair<TKey, TValue>> EntrySet<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            ISet<KeyValuePair<TKey, TValue>> iset = new HashSet<KeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                iset.Add(kvp);
            }
            return iset;
        }

        public static TValue Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict == null)
                return default(TValue);

            var oldValue = dict.ContainsKey(key) ? dict[key] : default(TValue);
            dict[key] = value;
            return oldValue;
        }
    }
}