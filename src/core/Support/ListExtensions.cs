using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class ListExtensions
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> values)
        {
            var lt = list as List<T>;

            if (lt != null)
                lt.AddRange(values);
            else
            {
                foreach (var item in values)
                {
                    lt.Add(item);
                }
            }
        }
    }
}
