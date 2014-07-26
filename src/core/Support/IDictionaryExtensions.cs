using System.Collections.Generic;

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

        public static ISet<KeyValuePair<TKey, TValue>> EntrySet<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            ISet<KeyValuePair<TKey, TValue>> iset = new HashSet<KeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                iset.Add(kvp);
            }
            return iset;
        }
    }
}