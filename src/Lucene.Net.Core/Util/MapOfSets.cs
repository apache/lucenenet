using System.Collections.Generic;

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
    /// @lucene.internal
    /// </summary>
    public class MapOfSets<K, V>// LUCENENET TODO: change to Key, Value
    {
        private readonly IDictionary<K, HashSet<V>> TheMap;

        /// <param name="m"> the backing store for this object </param>
        public MapOfSets(IDictionary<K, HashSet<V>> m)
        {
            TheMap = m;
        }

        /// <returns> direct access to the map backing this object. </returns>
        public virtual IDictionary<K, HashSet<V>> Map
        {
            get
            {
                return TheMap;
            }
        }

        /// <summary>
        /// Adds val to the Set associated with key in the Map.  If key is not
        /// already in the map, a new Set will first be created. </summary>
        /// <returns> the size of the Set associated with key once val is added to it. </returns>
        public virtual int Put(K key, V val)
        {
            HashSet<V> theSet;
            if (TheMap.ContainsKey(key))
            {
                theSet = TheMap[key];
            }
            else
            {
                theSet = new HashSet<V>();
                TheMap[key] = theSet;
            }
            theSet.Add(val);
            return theSet.Count;
        }

        /// <summary>
        /// Adds multiple vals to the Set associated with key in the Map.
        /// If key is not
        /// already in the map, a new Set will first be created. </summary>
        /// <returns> the size of the Set associated with key once val is added to it. </returns>
        public virtual int PutAll(K key, IEnumerable<V> vals)
        {
            HashSet<V> theSet;
            if (TheMap.ContainsKey(key))
            {
                theSet = TheMap[key];
            }
            else
            {
                theSet = new HashSet<V>();
                TheMap[key] = theSet;
            }
            theSet.UnionWith(vals);
            return theSet.Count;
        }
    }
}