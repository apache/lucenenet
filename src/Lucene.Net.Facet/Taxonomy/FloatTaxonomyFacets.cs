// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

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
    /// to a per-ords <see cref="T:float[]"/>.
    /// <para/>
    /// NOTE: This was FloatTaxonomyFacets in Lucene
    /// </summary>
    public abstract class SingleTaxonomyFacets : TaxonomyFacets
    {
        private const int OrdAndSingleByteSize = sizeof(int) + sizeof(float); // LUCENENET specific - so we can calculate stack size

        /// <summary>
        /// Per-ordinal value. </summary>
        protected readonly float[] m_values;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        protected SingleTaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config)
            : base(indexFieldName, taxoReader, config)
        {
            m_values = new float[taxoReader.Count];
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(dimRootOrd > 0);
                    m_values[dimRootOrd] += Rollup(m_children[dimRootOrd]);
                }
            }
        }

        private float Rollup(int ord)
        {
            float sum = 0;
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                float childValue = m_values[ord] + Rollup(m_children[ord]);
                m_values[ord] = childValue;
                sum += childValue;
                ord = m_siblings[ord];
            }
            return sum;
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
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
            FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
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
            bool usePool = OrdAndSingleByteSize * bufferSize > Constants.MaxStackByteLimit;
            OrdAndValue<float>[]? arrayToReturnToPool = usePool ? ArrayPool<OrdAndValue<float>>.Shared.Rent(bufferSize) : null;
            try
            {
                Span<OrdAndValue<float>> buffer = usePool ? arrayToReturnToPool : stackalloc OrdAndValue<float>[bufferSize];
                var q = new ValuePriorityQueue<OrdAndValue<float>>(buffer, TopOrdAndSingleComparer.Default);

                float bottomValue = 0;

                int ord = m_children[dimOrd];
                float sumValues = 0;
                int childCount = 0;

                while (ord != TaxonomyReader.INVALID_ORDINAL)
                {
                    if (m_values[ord] > 0)
                    {
                        sumValues += m_values[ord];
                        childCount++;
                        if (m_values[ord] > bottomValue)
                        {
                            // LUCENENET specific - use struct instead of reusing class instance for better performance
                            q.Insert(new OrdAndValue<float>(ord, m_values[ord]));
                            if (q.Count == topN)
                            {
                                bottomValue = q.Top.Value;
                            }
                        }
                    }

                    ord = m_siblings[ord];
                }

                if (sumValues == 0)
                {
                    return null;
                }

                if (dimConfig.IsMultiValued)
                {
                    if (dimConfig.RequireDimCount)
                    {
                        sumValues = m_values[dimOrd];
                    }
                    else
                    {
                        // Our sum'd count is not correct, in general:
                        sumValues = -1;
                    }
                }
                else
                {
                    // Our sum'd dim count is accurate, so we keep it
                }

                LabelAndValue[] labelValues = new LabelAndValue[q.Count];
                for (int i = labelValues.Length - 1; i >= 0; i--)
                {
                    var ordAndValue = q.Pop();
                    FacetLabel child = m_taxoReader.GetPath(ordAndValue.Ord);
                    labelValues[i] = new LabelAndValue(child.Components[cp.Length], ordAndValue.Value);
                }

                return new FacetResult(dim, path, sumValues, labelValues, childCount);
            }
            finally
            {
                if (arrayToReturnToPool is not null)
                    ArrayPool<OrdAndValue<float>>.Shared.Return(arrayToReturnToPool);
            }
        }
    }
}