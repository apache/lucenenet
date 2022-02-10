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

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using MatchingDocs = FacetsCollector.MatchingDocs;

    /// <summary>
    /// Aggregates sum of <see cref="float"/> values previously indexed with
    /// <see cref="SingleAssociationFacetField"/>, assuming the default
    /// encoding.
    /// <para/>
    /// NOTE: This was TaxonomyFacetSumFloatAssociations in Lucene
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class TaxonomyFacetSumSingleAssociations : SingleTaxonomyFacets
    {
        /// <summary>
        /// Create <see cref="TaxonomyFacetSumSingleAssociations"/> against
        /// the default index field. 
        /// </summary>
        public TaxonomyFacetSumSingleAssociations(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
            : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME, taxoReader, config, fc)
        {
        }

        /// <summary>
        /// Create <see cref="TaxonomyFacetSumSingleAssociations"/> against
        /// the specified index field. 
        /// </summary>
        public TaxonomyFacetSumSingleAssociations(string indexFieldName, TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
            : base(indexFieldName, taxoReader, config)
        {
            SumValues(fc.GetMatchingDocs());
        }

        private void SumValues(IList<FacetsCollector.MatchingDocs> matchingDocs)
        {
            //System.out.println("count matchingDocs=" + matchingDocs + " facetsField=" + facetsFieldName);
            foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
            {
                BinaryDocValues dv = hits.Context.AtomicReader.GetBinaryDocValues(m_indexFieldName);
                if (dv is null) // this reader does not have DocValues for the requested category list
                {
                    continue;
                }

                BytesRef scratch = new BytesRef();
                DocIdSetIterator docs = hits.Bits.GetIterator();

                int doc;
                while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    //System.out.println("  doc=" + doc);
                    // TODO: use OrdinalsReader?  we'd need to add a
                    // BytesRef getAssociation()?
                    dv.Get(doc, scratch);
                    byte[] bytes = scratch.Bytes;
                    int end = scratch.Offset + scratch.Length;
                    int offset = scratch.Offset;
                    while (offset < end)
                    {
                        int ord = ((bytes[offset] & 0xFF) << 24) | ((bytes[offset + 1] & 0xFF) << 16) | 
                            ((bytes[offset + 2] & 0xFF) << 8) | (bytes[offset + 3] & 0xFF);
                        offset += 4;
                        int value = ((bytes[offset] & 0xFF) << 24) | ((bytes[offset + 1] & 0xFF) << 16) | 
                            ((bytes[offset + 2] & 0xFF) << 8) | (bytes[offset + 3] & 0xFF);
                        offset += 4;
                        m_values[ord] += J2N.BitConversion.Int32BitsToSingle(value);
                    }
                }
            }
        }
    }
}