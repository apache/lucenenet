using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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
    /// Helper class for keeping Lists of Objects associated with keys. <b>WARNING: this CLASS IS NOT THREAD SAFE</b>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class MapOfSets<TKey, TValue>
    {
        private readonly IDictionary<TKey, ISet<TValue>> theMap;

        /// <param name="m"> The backing store for this object. </param>
        public MapOfSets(IDictionary<TKey, ISet<TValue>> m) 
        {
            theMap = m;
        }

        /// <returns> Direct access to the map backing this object. </returns>
        public virtual IDictionary<TKey, ISet<TValue>> Map => theMap;

        /// <summary>
        /// Adds <paramref name="val"/> to the <see cref="ISet{T}"/> associated with key in the <see cref="IDictionary{TKey, TValue}"/>.  
        /// If <paramref name="key"/> is not
        /// already in the map, a new <see cref="ISet{T}"/> will first be created. </summary>
        /// <returns> The size of the <see cref="ISet{T}"/> associated with key once val is added to it. </returns>
        public virtual int Put(TKey key, TValue val)
        {
            // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
            if (!theMap.TryGetValue(key, out ISet<TValue> theSet))
            {
                theMap[key] = theSet = new JCG.HashSet<TValue>();
            }
            theSet.Add(val);
            return theSet.Count;
        }

        /// <summary>
        /// Adds multiple <paramref name="vals"/> to the <see cref="ISet{T}"/> associated with key in the <see cref="IDictionary{TKey, TValue}"/>.
        /// If <paramref name="key"/> is not
        /// already in the map, a new <see cref="ISet{T}"/> will first be created. </summary>
        /// <returns> The size of the <see cref="ISet{T}"/> associated with key once val is added to it. </returns>
        public virtual int PutAll(TKey key, IEnumerable<TValue> vals)
        {
            // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
            if (!theMap.TryGetValue(key, out ISet<TValue> theSet))
            {
                theMap[key] = theSet = new JCG.HashSet<TValue>();
            }
            theSet.UnionWith(vals);
            return theSet.Count;
        }
    }
}