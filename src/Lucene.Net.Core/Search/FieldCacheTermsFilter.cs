namespace Lucene.Net.Search
{
    using Lucene.Net.Index;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsEnum = Lucene.Net.Index.DocsEnum; // javadoc @link
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;

    /// <summary>
    /// A <seealso cref="Filter"/> that only accepts documents whose single
    /// term value in the specified field is contained in the
    /// provided set of allowed terms.
    ///
    /// <p/>
    ///
    /// this is the same functionality as TermsFilter (from
    /// queries/), except this filter requires that the
    /// field contains only a single term for all documents.
    /// Because of drastically different implementations, they
    /// also have different performance characteristics, as
    /// described below.
    ///
    /// <p/>
    ///
    /// The first invocation of this filter on a given field will
    /// be slower, since a <seealso cref="SortedDocValues"/> must be
    /// created.  Subsequent invocations using the same field
    /// will re-use this cache.  However, as with all
    /// functionality based on <seealso cref="FieldCache"/>, persistent RAM
    /// is consumed to hold the cache, and is not freed until the
    /// <seealso cref="IndexReader"/> is closed.  In contrast, TermsFilter
    /// has no persistent RAM consumption.
    ///
    ///
    /// <p/>
    ///
    /// With each search, this filter translates the specified
    /// set of Terms into a private <seealso cref="FixedBitSet"/> keyed by
    /// term number per unique <seealso cref="IndexReader"/> (normally one
    /// reader per segment).  Then, during matching, the term
    /// number for each docID is retrieved from the cache and
    /// then checked for inclusion using the <seealso cref="FixedBitSet"/>.
    /// Since all testing is done using RAM resident data
    /// structures, performance should be very fast, most likely
    /// fast enough to not require further caching of the
    /// DocIdSet for each possible combination of terms.
    /// However, because docIDs are simply scanned linearly, an
    /// index with a great many small documents may find this
    /// linear scan too costly.
    ///
    /// <p/>
    ///
    /// In contrast, TermsFilter builds up an <seealso cref="FixedBitSet"/>,
    /// keyed by docID, every time it's created, by enumerating
    /// through all matching docs using <seealso cref="DocsEnum"/> to seek
    /// and scan through each term's docID list.  While there is
    /// no linear scan of all docIDs, besides the allocation of
    /// the underlying array in the <seealso cref="FixedBitSet"/>, this
    /// approach requires a number of "disk seeks" in proportion
    /// to the number of terms, which can be exceptionally costly
    /// when there are cache misses in the OS's IO cache.
    ///
    /// <p/>
    ///
    /// Generally, this filter will be slower on the first
    /// invocation for a given field, but subsequent invocations,
    /// even if you change the allowed set of Terms, should be
    /// faster than TermsFilter, especially as the number of
    /// Terms being matched increases.  If you are matching only
    /// a very small number of terms, and those terms in turn
    /// match a very small number of documents, TermsFilter may
    /// perform faster.
    ///
    /// <p/>
    ///
    /// Which filter is best is very application dependent.
    /// </summary>

    public class FieldCacheTermsFilter : Filter
    {
        private string Field;// LUCENENET TODO: Rename (private)
        private BytesRef[] Terms;// LUCENENET TODO: Rename (private)

        public FieldCacheTermsFilter(string field, params BytesRef[] terms)
        {
            this.Field = field;
            this.Terms = terms;
        }

        public FieldCacheTermsFilter(string field, params string[] terms)
        {
            this.Field = field;
            this.Terms = new BytesRef[terms.Length];
            for (int i = 0; i < terms.Length; i++)
            {
                this.Terms[i] = new BytesRef(terms[i]);
            }
        }

        public virtual IFieldCache FieldCache
        {
            get
            {
                return Search.FieldCache.DEFAULT;
            }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
        {
            SortedDocValues fcsi = FieldCache.GetTermsIndex((context.AtomicReader), Field);
            FixedBitSet bits = new FixedBitSet(fcsi.ValueCount);
            for (int i = 0; i < Terms.Length; i++)
            {
                int ord = fcsi.LookupTerm(Terms[i]);
                if (ord >= 0)
                {
                    bits.Set(ord);
                }
            }
            return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.Reader.MaxDoc, acceptDocs, fcsi, bits);
        }

        private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
        {
            private readonly FieldCacheTermsFilter OuterInstance;

            private SortedDocValues Fcsi;
            private FixedBitSet Bits;

            public FieldCacheDocIdSetAnonymousInnerClassHelper(FieldCacheTermsFilter outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, FixedBitSet bits)
                : base(maxDoc, acceptDocs)
            {
                this.OuterInstance = outerInstance;
                this.Fcsi = fcsi;
                this.Bits = bits;
            }

            protected internal override sealed bool MatchDoc(int doc)
            {
                int ord = Fcsi.GetOrd(doc);
                if (ord == -1)
                {
                    // missing
                    return false;
                }
                else
                {
                    return Bits.Get(ord);
                }
            }
        }
    }
}