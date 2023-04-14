// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

    using DimConfig = Lucene.Net.Facet.FacetsConfig.DimConfig;

    /// <summary>
    /// Base class for all taxonomy-based facets that aggregate
    /// to a per-ords <see cref="T:int[]"/>.
    /// <para/>
    /// NOTE: This was IntTaxonomyFacets in Lucene
    /// </summary>
    public abstract class Int32TaxonomyFacets : TaxonomyFacets
    {
        private const int OrdAndInt32ByteSize = sizeof(int) + sizeof(int); // LUCENENET specific - so we can calculate stack size

        /// <summary>
        /// Per-ordinal value. </summary>
        protected readonly int[] m_values;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        protected Int32TaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config)
            : base(indexFieldName, taxoReader, config)
        {
            m_values = new int[taxoReader.Count];
        }

        /// <summary>
        /// Rolls up any single-valued hierarchical dimensions.
        /// </summary>
        protected virtual void Rollup()
        {
            // Rollup any necessary dims:
            foreach (KeyValuePair<string, FacetsConfig.DimConfig> ent in m_config.DimConfigs)
            {
                string dim = ent.Key;
                FacetsConfig.DimConfig ft = ent.Value;
                if (ft.IsHierarchical && ft.IsMultiValued == false)
                {
                    int dimRootOrd = m_taxoReader.GetOrdinal(new FacetLabel(dim));
                    // It can be -1 if this field was declared in the
                    // config but never indexed:
                    if (dimRootOrd > 0)
                    {
                        m_values[dimRootOrd] += Rollup(m_children[dimRootOrd]);
                    }
                }
            }
        }

        private int Rollup(int ord)
        {
            int sum = 0;
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                int childValue = m_values[ord] + Rollup(m_children[ord]);
                m_values[ord] = childValue;
                sum += childValue;
                ord = m_siblings[ord];
            }
            return sum;
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            var dimConfig = VerifyDim(dim);
            if (path.Length == 0)
            {
                if (dimConfig.IsHierarchical && dimConfig.IsMultiValued == false)
                {
                    // ok: rolled up at search time
                }
                else if (dimConfig.RequireDimCount && dimConfig.IsMultiValued)
                {
                    // ok: we indexed all ords at index time
                }
                else
                {
                    throw new ArgumentException("cannot return dimension-level value alone; use getTopChildren instead");
                }
            }
            int ord = m_taxoReader.GetOrdinal(new FacetLabel(dim, path));
            if (ord < 0)
            {
                return -1;
            }
            return m_values[ord];
        }

#nullable enable

        public override FacetResult? GetTopChildren(int topN, string dim, params string[] path)
        {
            if (topN <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topN), "topN must be > 0 (got: " + topN + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            var dimConfig = VerifyDim(dim);
            FacetLabel cp = new FacetLabel(dim, path);
            int dimOrd = m_taxoReader.GetOrdinal(cp);
            if (dimOrd == -1)
            {
                return null;
            }

            // LUCENENET: Refactored PriorityQueue<T> subclass into PriorityComparer<T>
            // implementation, which can be passed into ValuePriorityQueue. ValuePriorityQueue
            // lives on the stack, and if the array size is small enough, we also allocate the
            // array on the stack. Fallback to the array pool if it is beyond MaxStackByteLimit.
            int bufferSize = PriorityQueue.GetArrayHeapSize(Math.Min(m_taxoReader.Count, topN));
            bool usePool = OrdAndInt32ByteSize * bufferSize > Constants.MaxStackByteLimit;
            OrdAndValue<int>[]? arrayToReturnToPool = usePool ? ArrayPool<OrdAndValue<int>>.Shared.Rent(bufferSize) : null;
            try
            {
                Span<OrdAndValue<int>> buffer = usePool ? arrayToReturnToPool : stackalloc OrdAndValue<int>[bufferSize];
                var q = new ValuePriorityQueue<OrdAndValue<int>>(buffer, TopOrdAndInt32Comparer.Default);

                int bottomValue = 0;

                int ord = m_children[dimOrd];
                int totValue = 0;
                int childCount = 0;

                while (ord != TaxonomyReader.INVALID_ORDINAL)
                {
                    if (m_values[ord] > 0)
                    {
                        totValue += m_values[ord];
                        childCount++;
                        if (m_values[ord] > bottomValue)
                        {
                            // LUCENENET specific - use struct instead of reusing class instance for better performance
                            q.Insert(new OrdAndValue<int>(ord, m_values[ord]));
                            if (q.Count == topN)
                            {
                                bottomValue = q.Top.Value;
                            }
                        }
                    }

                    ord = m_siblings[ord];
                }

                if (totValue == 0)
                {
                    return null;
                }

                if (dimConfig.IsMultiValued)
                {
                    if (dimConfig.RequireDimCount)
                    {
                        totValue = m_values[dimOrd];
                    }
                    else
                    {
                        // Our sum'd value is not correct, in general:
                        totValue = -1;
                    }
                }
                else
                {
                    // Our sum'd dim value is accurate, so we keep it
                }

                LabelAndValue[] labelValues = new LabelAndValue[q.Count];
                for (int i = labelValues.Length - 1; i >= 0; i--)
                {
                    var ordAndValue = q.Pop();
                    FacetLabel child = m_taxoReader.GetPath(ordAndValue.Ord);
                    labelValues[i] = new LabelAndValue(child.Components[cp.Length], ordAndValue.Value);
                }

                return new FacetResult(dim, path, totValue, labelValues, childCount);
            }
            finally
            {
                if (arrayToReturnToPool is not null)
                    ArrayPool<OrdAndValue<int>>.Shared.Return(arrayToReturnToPool);
            }
        }
    }
}