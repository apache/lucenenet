using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search.Spans
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
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    ///
    /// <summary>
    /// A wrapper to perform span operations on a non-leaf reader context
    /// <p>
    /// NOTE: this should be used for testing purposes only
    /// @lucene.internal
    /// </summary>
    public class MultiSpansWrapper : Spans // can't be package private due to payloads
    {
        private readonly SpanQuery Query;
        private readonly IList<AtomicReaderContext> Leaves;
        private int LeafOrd = 0;
        private Spans Current;
        private readonly IDictionary<Term, TermContext> TermContexts;
        private readonly int NumLeaves;

        private MultiSpansWrapper(IList<AtomicReaderContext> leaves, SpanQuery query, IDictionary<Term, TermContext> termContexts)
        {
            this.Query = query;
            this.Leaves = leaves;
            this.NumLeaves = leaves.Count;
            this.TermContexts = termContexts;
        }

        public static Spans Wrap(IndexReaderContext topLevelReaderContext, SpanQuery query)
        {
            IDictionary<Term, TermContext> termContexts = new Dictionary<Term, TermContext>();
            SortedSet<Term> terms = new SortedSet<Term>();
            query.ExtractTerms(terms);
            foreach (Term term in terms)
            {
                termContexts[term] = TermContext.Build(topLevelReaderContext, term);
            }
            IList<AtomicReaderContext> leaves = topLevelReaderContext.Leaves;
            if (leaves.Count == 1)
            {
                AtomicReaderContext ctx = leaves[0];
                return query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
            }
            return new MultiSpansWrapper(leaves, query, termContexts);
        }

        public override bool Next()
        {
            if (LeafOrd >= NumLeaves)
            {
                return false;
            }
            if (Current == null)
            {
                AtomicReaderContext ctx = Leaves[LeafOrd];
                Current = Query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, TermContexts);
            }
            while (true)
            {
                if (Current.Next())
                {
                    return true;
                }
                if (++LeafOrd < NumLeaves)
                {
                    AtomicReaderContext ctx = Leaves[LeafOrd];
                    Current = Query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, TermContexts);
                }
                else
                {
                    Current = null;
                    break;
                }
            }
            return false;
        }

        public override bool SkipTo(int target)
        {
            if (LeafOrd >= NumLeaves)
            {
                return false;
            }

            int subIndex = ReaderUtil.SubIndex(target, Leaves);
            Debug.Assert(subIndex >= LeafOrd);
            if (subIndex != LeafOrd)
            {
                AtomicReaderContext ctx = Leaves[subIndex];
                Current = Query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, TermContexts);
                LeafOrd = subIndex;
            }
            else if (Current == null)
            {
                AtomicReaderContext ctx = Leaves[LeafOrd];
                Current = Query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, TermContexts);
            }
            while (true)
            {
                if (target < Leaves[LeafOrd].DocBase)
                {
                    // target was in the previous slice
                    if (Current.Next())
                    {
                        return true;
                    }
                }
                else if (Current.SkipTo(target - Leaves[LeafOrd].DocBase))
                {
                    return true;
                }
                if (++LeafOrd < NumLeaves)
                {
                    AtomicReaderContext ctx = Leaves[LeafOrd];
                    Current = Query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, TermContexts);
                }
                else
                {
                    Current = null;
                    break;
                }
            }

            return false;
        }

        public override int Doc
        {
            get
            {
                if (Current == null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return Current.Doc + Leaves[LeafOrd].DocBase;
            }
        }

        public override int Start
        {
            get
            {
                if (Current == null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return Current.Start;
            }
        }

        public override int End
        {
            get
            {
                if (Current == null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return Current.End;
            }
        }

        public override ICollection<byte[]> GetPayload()
        {
            if (Current == null)
            {
                return new List<byte[]>();
            }
            return Current.GetPayload();
        }

        public override bool IsPayloadAvailable
        {
            get
            {
                if (Current == null)
                {
                    return false;
                }
                return Current.IsPayloadAvailable;
            }
        }

        public override long Cost()
        {
            return int.MaxValue; // just for tests
        }
    }
}