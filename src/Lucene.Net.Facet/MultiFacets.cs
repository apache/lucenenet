// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Maps specified dims to provided <see cref="Facets"/> impls; else, uses
    /// the default <see cref="Facets"/> impl. 
    /// </summary>
    public class MultiFacets : Facets
    {
        private readonly IDictionary<string, Facets> dimToFacets;
        private readonly Facets defaultFacets;

        /// <summary>
        /// Create this, with the specified default <see cref="Facets"/>
        /// for fields not included in <paramref name="dimToFacets"/>. 
        /// </summary>
        public MultiFacets(IDictionary<string, Facets> dimToFacets, Facets defaultFacets = null)
        {
            this.dimToFacets = dimToFacets;
            this.defaultFacets = defaultFacets;
        }

        public override FacetResult GetTopChildren(int topN, string dim, params string[] path)
        {
            if (!dimToFacets.TryGetValue(dim, out Facets facets))
            {
                if (defaultFacets is null)
                {
                    throw new ArgumentException("invalid dim \"" + dim + "\"");
                }
                facets = defaultFacets;
            }
            return facets.GetTopChildren(topN, dim, path);
        }

        
        public override float GetSpecificValue(string dim, params string[] path)
        {
            if (!dimToFacets.TryGetValue(dim, out Facets facets))
            {
                if (defaultFacets is null)
                {
                    throw new ArgumentException("invalid dim \"" + dim + "\"");
                }
                facets = defaultFacets;
            }
            return facets.GetSpecificValue(dim, path);
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            IList<FacetResult> results = new JCG.List<FacetResult>();

            // First add the specific dim's facets:
            foreach (KeyValuePair<string, Facets> ent in dimToFacets)
            {
                results.Add(ent.Value.GetTopChildren(topN, ent.Key));
            }

            if (defaultFacets != null)
            {

                // Then add all default facets as long as we didn't
                // already add that dim:
                foreach (FacetResult result in defaultFacets.GetAllDims(topN))
                {
                    if (dimToFacets.ContainsKey(result.Dim) == false)
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}