using System;

namespace Lucene.Net.Search
{
    using Lucene.Net.Util;

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
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Holds all implementations of classes in the o.a.l.search package as a
    /// back-compatibility test. It does not run any tests per-se, however if
    /// someone adds a method to an interface or abstract method to an abstract
    /// class, one of the implementations here will fail to compile and so we know
    /// back-compat policy was violated.
    /// </summary>
    internal sealed class JustCompileSearch
    {
        private const string UNSUPPORTED_MSG = "unsupported: used for back-compat testing only !";

        internal sealed class JustCompileCollector : Collector
        {
            public override void Collect(int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileDocIdSet : DocIdSet
        {
            public override DocIdSetIterator GetIterator()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileDocIdSetIterator : DocIdSetIterator
        {
            public override int DocID()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int NextDoc()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Advance(int target)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long Cost()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileExtendedFieldCacheLongParser : FieldCache.ILongParser
        {
            public long ParseLong(BytesRef @string)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileExtendedFieldCacheDoubleParser : FieldCache.IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFieldComparator : FieldComparator<object>
        {
            public override int Compare(int slot1, int slot2)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int CompareBottom(int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override void Copy(int slot, int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Bottom
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override object TopValue
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override FieldComparator SetNextReader(AtomicReaderContext context)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override IComparable Value(int slot)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int CompareTop(int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFieldComparatorSource : FieldComparatorSource
        {
            public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFilter : Filter
        {
            // Filter is just an abstract class with no abstract methods. However it is
            // still added here in case someone will add abstract methods in the future.

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                return null;
            }
        }

        internal sealed class JustCompileFilteredDocIdSet : FilteredDocIdSet
        {
            public JustCompileFilteredDocIdSet(DocIdSet innerSet)
                : base(innerSet)
            {
            }

            protected internal override bool Match(int docid)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileFilteredDocIdSetIterator : FilteredDocIdSetIterator
        {
            public JustCompileFilteredDocIdSetIterator(DocIdSetIterator innerIter)
                : base(innerIter)
            {
            }

            protected internal override bool Match(int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long Cost()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileQuery : Query
        {
            public override string ToString(string field)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileScorer : Scorer
        {
            internal JustCompileScorer(Weight weight)
                : base(weight)
            {
            }

            public override float Score()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Freq()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int DocID()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int NextDoc()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Advance(int target)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long Cost()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileSimilarity : Similarity
        {
            public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override SimScorer DoSimScorer(SimWeight stats, AtomicReaderContext context)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long ComputeNorm(FieldInvertState state)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileTopDocsCollector : TopDocsCollector<ScoreDoc>
        {
            internal JustCompileTopDocsCollector(PriorityQueue<ScoreDoc> pq)
                : base(pq)
            {
            }

            public override void Collect(int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override TopDocs TopDocs()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override TopDocs TopDocs(int start)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override TopDocs TopDocs(int start, int end)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileWeight : Weight
        {
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override Query Query
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override float ValueForNormalization
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }
    }
}