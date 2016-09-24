using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Base class for all taxonomy-based facets impls. </summary>
    public abstract class TaxonomyFacets : Facets
    {
        private static readonly IComparer<FacetResult> BY_VALUE_THEN_DIM = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<FacetResult>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(FacetResult a, FacetResult b)
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
                    return a.Dim.CompareTo(b.Dim);
                }
            }
        }

        /// <summary>
        /// Index field name provided to the constructor. </summary>
        protected readonly string IndexFieldName;

        /// <summary>
        /// {@code TaxonomyReader} provided to the constructor. </summary>
        protected readonly TaxonomyReader TaxoReader;

        /// <summary>
        /// {@code FacetsConfig} provided to the constructor. </summary>
        protected readonly FacetsConfig Config;

        /// <summary>
        /// Maps parent ordinal to its child, or -1 if the parent
        ///  is childless. 
        /// </summary>
        protected readonly int[] Children;

        /// <summary>
        /// Maps an ordinal to its sibling, or -1 if there is no
        ///  sibling. 
        /// </summary>
        protected readonly int[] Siblings;

        /// <summary>
        /// Sole constructor. 
        /// </summary>
        protected internal TaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config)
        {
            this.IndexFieldName = indexFieldName;
            this.TaxoReader = taxoReader;
            this.Config = config;
            ParallelTaxonomyArrays pta = taxoReader.ParallelTaxonomyArrays;
            Children = pta.Children();
            Siblings = pta.Siblings();
        }

        /// <summary>
        /// Throws {@code IllegalArgumentException} if the
        ///  dimension is not recognized.  Otherwise, returns the
        ///  <seealso cref="DimConfig"/> for this dimension. 
        /// </summary>
        protected internal virtual DimConfig VerifyDim(string dim)
        {
            DimConfig dimConfig = Config.GetDimConfig(dim);
            if (!dimConfig.IndexFieldName.Equals(IndexFieldName))
            {
                throw new System.ArgumentException("dimension \"" + dim + "\" was not indexed into field \"" + IndexFieldName);
            }
            return dimConfig;
        }

        public override IList<FacetResult> GetAllDims(int topN)
        {
            int ord = Children[TaxonomyReader.ROOT_ORDINAL];
            IList<FacetResult> results = new List<FacetResult>();
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                string dim = TaxoReader.GetPath(ord).Components[0];
                DimConfig dimConfig = Config.GetDimConfig(dim);
                if (dimConfig.IndexFieldName.Equals(IndexFieldName))
                {
                    FacetResult result = GetTopChildren(topN, dim);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                ord = Siblings[ord];
            }

            // Sort by highest value, tie break by dim:
            var resultArray = results.ToArray();
            Array.Sort(resultArray, BY_VALUE_THEN_DIM);
            return resultArray;
        }
    }
}