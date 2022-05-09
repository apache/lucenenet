// Lucene version compatibility level 4.8.1
using System.Collections.Generic;

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

    using MatchingDocs = FacetsCollector.MatchingDocs;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    /// <summary>
    /// Computes facets counts, assuming the default encoding
    /// into DocValues was used.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class FastTaxonomyFacetCounts : Int32TaxonomyFacets
    {
        /// <summary>
        /// Create <see cref="FastTaxonomyFacetCounts"/>, which also
        /// counts all facet labels. 
        /// </summary>
        public FastTaxonomyFacetCounts(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
            : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME, taxoReader, config, fc)
        {
        }

        /// <summary>
        /// Create <see cref="FastTaxonomyFacetCounts"/>, using the
        /// specified <paramref name="indexFieldName"/> for ordinals.  Use
        /// this if you had set <see cref="FacetsConfig.SetIndexFieldName"/>
        /// to change the index
        /// field name for certain dimensions. 
        /// </summary>
        public FastTaxonomyFacetCounts(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
            : base(indexFieldName, taxoReader, config)
        {
            Count(fc.GetMatchingDocs());
        }

        private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
        {
            // LUCENENET specific - performance is significantly better if we instantiate
            // this outside of the outer loop.
            BytesRef bytesRef = new BytesRef();
            foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
            {
                BinaryDocValues dv = hits.Context.AtomicReader.GetBinaryDocValues(m_indexFieldName);
                if (dv is null) // this reader does not have DocValues for the requested category list
                {
                    continue;
                }

                DocIdSetIterator docs = hits.Bits.GetIterator();

                int doc;
                while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    dv.Get(doc, bytesRef);
                    var bytes = bytesRef.Bytes;
                    int end = bytesRef.Offset + bytesRef.Length;
                    int ord = 0;
                    int offset = bytesRef.Offset;
                    int prev = 0;
                    while (offset < end)
                    {
                        byte b = bytes[offset++];
                        if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                        {
                            prev = ord = ((ord << 7) | b) + prev;
                            ++m_values[ord];
                            ord = 0;
                        }
                        else
                        {
                            ord = (ord << 7) | (b & 0x7F);
                        }
                    }
                }
            }

            Rollup();
        }
    }
}