using Spatial4n.Core.Shapes;
using System.Collections.Generic;

namespace Lucene.Net.Spatial.Util
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
    /// Bounded Cache of Shapes associated with docIds.  Note, multiple Shapes can be
    /// associated with a given docId
    /// <para/>
    /// WARNING: This class holds the data in an extremely inefficient manner as all Points are in memory as objects and they
    /// are stored in many Lists (one per document).  So it works but doesn't scale.  It will be replaced in the future.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ShapeFieldCache<T> where T : IShape
    {
        private readonly IList<T>[] cache;
        public int defaultLength;

        public ShapeFieldCache(int length, int defaultLength)
        {
            cache = new IList<T>[length];
            this.defaultLength = defaultLength;
        }

        public virtual void Add(int docid, T s)
        {
            IList<T> list = cache[docid];
            if (list == null)
            {
                list = cache[docid] = new List<T>(defaultLength);
            }
            list.Add(s);
        }

        public virtual IList<T> GetShapes(int docid)
        {
            return cache[docid];
        }
    }
}
