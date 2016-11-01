using System.Collections.Generic;

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
    using Bits = Lucene.Net.Util.Bits;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Holds all implementations of classes in the o.a.l.s.spans package as a
    /// back-compatibility test. It does not run any tests per-se, however if
    /// someone adds a method to an interface or abstract method to an abstract
    /// class, one of the implementations here will fail to compile and so we know
    /// back-compat policy was violated.
    /// </summary>
    internal sealed class JustCompileSearchSpans
    {
        private const string UNSUPPORTED_MSG = "unsupported: used for back-compat testing only !";

        internal sealed class JustCompileSpans : Spans
        {
            public override int Doc()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int End()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override bool Next()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override bool SkipTo(int target)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Start()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override bool PayloadAvailable
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override long Cost()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileSpanQuery : SpanQuery
        {
            public override string Field
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override string ToString(string field)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompilePayloadSpans : Spans
        {
            public override ICollection<byte[]> Payload
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override bool PayloadAvailable
            {
                get
                {
                    throw new System.NotSupportedException(UNSUPPORTED_MSG);
                }
            }

            public override int Doc()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int End()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override bool Next()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override bool SkipTo(int target)
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override int Start()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }

            public override long Cost()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }

        internal sealed class JustCompileSpanScorer : SpanScorer
        {
            internal JustCompileSpanScorer(Spans spans, Weight weight, Similarity.SimScorer docScorer)
                : base(spans, weight, docScorer)
            {
            }

            protected internal override bool SetFreqCurrentDoc()
            {
                throw new System.NotSupportedException(UNSUPPORTED_MSG);
            }
        }
    }
}