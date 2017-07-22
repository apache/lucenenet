using System;
using System.Collections.Generic;

namespace Lucene.Net.Replicator.Http
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// .NET Specific Helper Extensions for IEnumerable
    /// </remarks>
    //Note: LUCENENET specific
    public static class EnumerableExtensions
    {
        public static IEnumerable<TOut> InPairs<T, TOut>(this IEnumerable<T> list, Func<T, T, TOut> join)
        {
            using (var enumerator = list.GetEnumerator())
            {
                while (true)
                {
                    if (!enumerator.MoveNext())
                        yield break;

                    T x = enumerator.Current;
                    if (!enumerator.MoveNext())
                        yield return join(x, default(T));
                    yield return join(x, enumerator.Current);
                }
            }
        }
    }
}