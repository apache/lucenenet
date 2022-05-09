using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Spans
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
        private readonly SpanQuery query;
        private readonly IList<AtomicReaderContext> leaves;
        private int leafOrd = 0;
        private Spans current;
        private readonly IDictionary<Term, TermContext> termContexts;
        private readonly int numLeaves;

        private MultiSpansWrapper(IList<AtomicReaderContext> leaves, SpanQuery query, IDictionary<Term, TermContext> termContexts)
        {
            this.query = query;
            this.leaves = leaves;
            this.numLeaves = leaves.Count;
            this.termContexts = termContexts;
        }

        public static Spans Wrap(IndexReaderContext topLevelReaderContext, SpanQuery query)
        {
            IDictionary<Term, TermContext> termContexts = new Dictionary<Term, TermContext>();
            JCG.SortedSet<Term> terms = new JCG.SortedSet<Term>();
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

        public override bool MoveNext()
        {
            if (leafOrd >= numLeaves)
            {
                return false;
            }
            if (current is null)
            {
                AtomicReaderContext ctx = leaves[leafOrd];
                current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
            }
            while (true)
            {
                if (current.MoveNext())
                {
                    return true;
                }
                if (++leafOrd < numLeaves)
                {
                    AtomicReaderContext ctx = leaves[leafOrd];
                    current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
                }
                else
                {
                    current = null;
                    break;
                }
            }
            return false;
        }

        public override bool SkipTo(int target)
        {
            if (leafOrd >= numLeaves)
            {
                return false;
            }

            int subIndex = ReaderUtil.SubIndex(target, leaves);
            if (Debugging.AssertsEnabled) Debugging.Assert(subIndex >= leafOrd);
            if (subIndex != leafOrd)
            {
                AtomicReaderContext ctx = leaves[subIndex];
                current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
                leafOrd = subIndex;
            }
            else if (current is null)
            {
                AtomicReaderContext ctx = leaves[leafOrd];
                current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
            }
            while (true)
            {
                if (target < leaves[leafOrd].DocBase)
                {
                    // target was in the previous slice
                    if (current.MoveNext())
                    {
                        return true;
                    }
                }
                else if (current.SkipTo(target - leaves[leafOrd].DocBase))
                {
                    return true;
                }
                if (++leafOrd < numLeaves)
                {
                    AtomicReaderContext ctx = leaves[leafOrd];
                    current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts);
                }
                else
                {
                    current = null;
                    break;
                }
            }

            return false;
        }

        public override int Doc
        {
            get
            {
                if (current is null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return current.Doc + leaves[leafOrd].DocBase;
            }
        }

        public override int Start
        {
            get
            {
                if (current is null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return current.Start;
            }
        }

        public override int End
        {
            get
            {
                if (current is null)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                return current.End;
            }
        }

        public override ICollection<byte[]> GetPayload()
        {
            if (current is null)
            {
                return Collections.EmptyList<byte[]>();
            }
            return current.GetPayload();
        }

        public override bool IsPayloadAvailable
        {
            get
            {
                if (current is null)
                {
                    return false;
                }
                return current.IsPayloadAvailable;
            }
        }

        public override long GetCost()
        {
            return int.MaxValue; // just for tests
        }
    }
}