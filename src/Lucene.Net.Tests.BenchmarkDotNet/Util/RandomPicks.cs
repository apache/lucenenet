using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.BenchmarkDotNet.Util
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
    /// Random selections of objects.
    /// </summary>
    public class RandomPicks
    {
        /// <summary>
        /// Pick a random element from the <paramref name="list"/> (which may be an array).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="list"/> contains no items.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="list"/> is <c>null</c>.</exception>
        public static T RandomFrom<T>(Random random, IList<T> list)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
            {
                throw new ArgumentException("Can't pick a random object from an empty list.");
            }
            return list[random.Next(0, list.Count)];
        }

        /// <summary>
        /// Pick a random element from the <paramref name="collection"/>.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="collection"/> contains no items.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static T RandomFrom<T>(Random random, ICollection<T> collection)
        {
            if (random is null)
                throw new ArgumentNullException(nameof(random));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (collection.Count == 0)
            {
                throw new ArgumentException("Can't pick a random object from an empty collection.");
            }
            return collection.ElementAt(random.Next(0, collection.Count));
        }
    }
}
