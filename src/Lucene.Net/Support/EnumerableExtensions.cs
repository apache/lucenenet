using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    /*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

    /// <summary>
    /// .NET Specific Helper Extensions for IEnumerable
    /// </summary>
    //Note: LUCENENET specific
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Enumerates a sequence in pairs  
        /// </summary>
        /// <remarks>
        /// In the case of an uneven amount of elements, the list call to <paramref name="join" /> pases <code>default(T)</code> as the second parameter.
        /// </remarks>
        /// <typeparam name="T">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TOut">The type of the elements returned from <paramref name="join" />.</typeparam>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to enumerate in pairs.</param>
        /// <param name="join">A function that is invoked for each pair of elements.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="source" /> or <paramref name="join" /> is <see langword="null" />.</exception>
        /// <returns>A new <see cref="T:System.Collections.Generic.IEnumerable`1" /> containing the results from each pair.</returns>
        public static IEnumerable<TOut> InPairs<T, TOut>(this IEnumerable<T> source, Func<T, T, TOut> join)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (join == null)
                throw new ArgumentNullException("join");

            using (IEnumerator<T> enumerator = source.GetEnumerator())
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