using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class SetExtensions
    {
        public static void RemoveAll<T>(this ICollection<T> theSet, IEnumerable<T> removeList)
        {
            foreach (var elt in removeList)
            {
                theSet.Remove(elt);
            }
        }

        public static void AddAll<T>(this ICollection<T> set, IEnumerable<T> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
            {
                set.Add(item);
            }
        }
    }
}
