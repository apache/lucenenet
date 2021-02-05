// Lucene version compatibility level 4.8.1
using System.Collections.Generic;

namespace Lucene.Net.Facet
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
    /// Common base class for all facets implementations.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public abstract class Facets
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected Facets() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Returns the topN child labels under the specified
        /// path.  Returns null if the specified path doesn't
        /// exist or if this dimension was never seen. 
        /// </summary>
        public abstract FacetResult GetTopChildren(int topN, string dim, params string[] path);

        /// <summary>
        /// Return the count or value
        /// for a specific path.  Returns -1 if
        /// this path doesn't exist, else the count. 
        /// </summary>
        public abstract float GetSpecificValue(string dim, params string[] path);

        /// <summary>
        /// Returns topN labels for any dimension that had hits,
        /// sorted by the number of hits that dimension matched;
        /// this is used for "sparse" faceting, where many
        /// different dimensions were indexed, for example
        /// depending on the type of document. 
        /// </summary>
        public abstract IList<FacetResult> GetAllDims(int topN);
    }
}