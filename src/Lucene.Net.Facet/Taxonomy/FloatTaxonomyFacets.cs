using System;
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
    /// to a per-ords <see cref="float[]"/>. 
    /// </summary>
    public abstract class FloatTaxonomyFacets : TaxonomyFacets
    {
        /// <summary>
        /// Per-ordinal value. </summary>
        protected readonly float[] values;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        protected internal FloatTaxonomyFacets(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config)
            : base(indexFieldName, taxoReader, config)
        {
            values = new float[taxoReader.Count];
        }

        /// <summary>
        /// Rolls up any single-valued hierarchical dimensions.
        /// </summary>
        protected virtual void Rollup()
        {
            // Rollup any necessary dims:
            foreach (KeyValuePair<string, FacetsConfig.DimConfig> ent in config.DimConfigs)
            {
                string dim = ent.Key;
                FacetsConfig.DimConfig ft = ent.Value;
                if (ft.Hierarchical && ft.MultiValued == false)
                {
                    int dimRootOrd = taxoReader.GetOrdinal(new FacetLabel(dim));
                    Debug.Assert(dimRootOrd > 0);
                    values[dimRootOrd] += Rollup(children[dimRootOrd]);
                }
            }
        }

        private float Rollup(int ord)
        {
            float sum = 0;
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                float childValue = values[ord] + Rollup(children[ord]);
                values[ord] = childValue;
                sum += childValue;
                ord = siblings[ord];
            }
            return sum;
        }

        public override float GetSpecificValue(string dim, params string[] path)
        {
            FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
            if (path.Length == 0)
            {
                if (dimConfig.Hierarchical && dimConfig.MultiValued == false)
                {
                    // ok: rolled up at search time
                }
                else if (dimConfig.RequireDimCount && dimConfig.MultiValued)
                {
                    // ok: we indexed all ords at index time
                }
                else
                {
                    throw new System.ArgumentException("cannot return dimension-level value alone; use getTopChildren instead");
                }
            }
            int ord = taxoReader.GetOrdinal(new FacetLabel(dim, path));
            if (ord < 0)
            {
                return -1;
            }
            return values[ord];
        }

        public override FacetResult GetTopChildren(int topN, string dim, params string[] path)
        {
            if (topN <= 0)
            {
                throw new System.ArgumentException("topN must be > 0 (got: " + topN + ")");
            }
            FacetsConfig.DimConfig dimConfig = VerifyDim(dim);
            FacetLabel cp = new FacetLabel(dim, path);
            int dimOrd = taxoReader.GetOrdinal(cp);
            if (dimOrd == -1)
            {
                return null;
            }

            TopOrdAndFloatQueue q = new TopOrdAndFloatQueue(Math.Min(taxoReader.Count, topN));
            float bottomValue = 0;

            int ord = children[dimOrd];
            float sumValues = 0;
            int childCount = 0;

            TopOrdAndFloatQueue.OrdAndValue reuse = null;
            while (ord != TaxonomyReader.INVALID_ORDINAL)
            {
                if (values[ord] > 0)
                {
                    sumValues += values[ord];
                    childCount++;
                    if (values[ord] > bottomValue)
                    {
                        if (reuse == null)
                        {
                            reuse = new TopOrdAndFloatQueue.OrdAndValue();
                        }
                        reuse.Ord = ord;
                        reuse.Value = values[ord];
                        reuse = q.InsertWithOverflow(reuse);
                        if (q.Size() == topN)
                        {
                            bottomValue = q.Top().Value;
                        }
                    }
                }

                ord = siblings[ord];
            }

            if (sumValues == 0)
            {
                return null;
            }

            if (dimConfig.MultiValued)
            {
                if (dimConfig.RequireDimCount)
                {
                    sumValues = values[dimOrd];
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

            LabelAndValue[] labelValues = new LabelAndValue[q.Size()];
            for (int i = labelValues.Length - 1; i >= 0; i--)
            {
                TopOrdAndFloatQueue.OrdAndValue ordAndValue = q.Pop();
                FacetLabel child = taxoReader.GetPath(ordAndValue.Ord);
                labelValues[i] = new LabelAndValue(child.Components[cp.Length], ordAndValue.Value);
            }

            return new FacetResult(dim, path, sumValues, labelValues, childCount);
        }
    }
}