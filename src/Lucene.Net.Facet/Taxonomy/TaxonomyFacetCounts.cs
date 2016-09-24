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
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using MatchingDocs = FacetsCollector.MatchingDocs;

    /// <summary>
    /// Reads from any <seealso cref="OrdinalsReader"/>; use {@link
    ///  FastTaxonomyFacetCounts} if you are using the
    ///  default encoding from <seealso cref="BinaryDocValues"/>.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class TaxonomyFacetCounts : IntTaxonomyFacets
    {
        private readonly OrdinalsReader ordinalsReader;

        /// <summary>
        /// Create {@code TaxonomyFacetCounts}, which also
        ///  counts all facet labels.  Use this for a non-default
        ///  <seealso cref="OrdinalsReader"/>; otherwise use {@link
        ///  FastTaxonomyFacetCounts}. 
        /// </summary>
        public TaxonomyFacetCounts(OrdinalsReader ordinalsReader, TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
            : base(ordinalsReader.IndexFieldName, taxoReader, config)
        {
            this.ordinalsReader = ordinalsReader;
            Count(fc.GetMatchingDocs);
        }

        private void Count(IList<FacetsCollector.MatchingDocs> matchingDocs)
        {
            IntsRef scratch = new IntsRef();
            foreach (FacetsCollector.MatchingDocs hits in matchingDocs)
            {
                OrdinalsReader.OrdinalsSegmentReader ords = ordinalsReader.GetReader(hits.context);
                DocIdSetIterator docs = hits.bits.GetIterator();

                int doc;
                while ((doc = docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    ords.Get(doc, scratch);
                    for (int i = 0; i < scratch.Length; i++)
                    {
                        values[scratch.Ints[scratch.Offset + i]]++;
                    }
                }
            }

            Rollup();
        }
    }
}