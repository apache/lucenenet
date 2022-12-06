// Lucene version compatibility level 4.8.1
using J2N.Text;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy
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

    using DimConfig = Lucene.Net.Facet.FacetsConfig.DimConfig; // javadocs

    /// <summary>
    /// Base class for all taxonomy-based facets impls.
    /// </summary>
    public abstract class TaxonomyFacets : Facets
    {
        private static readonly IComparer<FacetResult> BY_VALUE_THEN_DIM = Comparer<FacetResult>.Create((a, b) =>
        {
            if (a.Value > b.Value)
            {
                return -1;
            }
            else if (b.Value > a.Value)
            {
                return 1;
            }
            else
            {
                return a.Dim.CompareToOrdinal(b.Dim);
            }
        });
                
        /// <summary>
        /// Index field name provided to the constructor.
        /// </summary>
        protected readonly string m_indexFieldName;

        /// <summary>
        /// <see cref="TaxonomyReader"/> provided to the constructor.
        /// </summary>
        protected readonly TaxonomyReader m_taxoReader;

        /// <summary>
        /// <see cref="FacetsConfig"/> provided to the constructor.
        /// </summary>
        protected readonly FacetsConfig m_config;

        /// <summary>
        /// Maps parent ordinal to its child, or -1 if the parent
        /// is childless. 
        /// </summary>
        protected readonly int[] m_children;

        /// <summary>
        /// Maps an ordinal to its sibling, or -1 if there is no
        /// sibling. 
        /// </summary>
        protected readonly int[] m_siblings;

        /// <summary>
        /// Sole constructor. 
        /// </summary>
        protected TaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config)
        {
            this.m_indexFieldName = indexFieldName;
            this.m_taxoReader = taxoReader;
            this.m_config = config;
            ParallelTaxonomyArrays pta = taxoReader.ParallelTaxonomyArrays;
            m_children = pta.Children;
            m_siblings = pta.Siblings;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if the
        /// dimension is not recognized.  Otherwise, returns the
        /// <see cref="DimConfig"/> for this dimension. 
        /// </summary>
        protected virtual DimConfig VerifyDim(string dim)
        {
            DimConfig dimConfig = m_config.GetDimConfig(dim);
            if (!dimConfig.IndexFieldName.Equals(m_indexFieldName, StringComparison.Ordinal))
            {
                throw new ArgumentException("dimension \"" + dim + "\" was not indexed into field \"" + m_indexFieldName);
            }
            return dimConfig;
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            int ord = m_children[TaxonomyReader.ROOT_ORDINAL];
            JCG.List<FacetResult> results = new JCG.List<FacetResult>();
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                string dim = m_taxoReader.GetPath(ord).Components[0];
                DimConfig dimConfig = m_config.GetDimConfig(dim);
                if (dimConfig.IndexFieldName.Equals(m_indexFieldName, StringComparison.Ordinal))
                {
                    FacetResult result = GetTopChildren(topN, dim);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                ord = m_siblings[ord];
            }

            // Sort by highest value, tie break by dim:
            results.Sort(BY_VALUE_THEN_DIM);
            return results;
        }
    }
}