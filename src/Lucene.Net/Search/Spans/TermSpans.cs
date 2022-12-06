using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Expert:
    /// Public for extension only
    /// </summary>
    public class TermSpans : Spans
    {
        protected readonly DocsAndPositionsEnum m_postings;
        protected readonly Term m_term;
        protected int m_doc;
        protected int m_freq;
        protected int m_count;
        protected int m_position;
        protected bool m_readPayload;

        public TermSpans(DocsAndPositionsEnum postings, Term term)
        {
            this.m_postings = postings;
            this.m_term = term;
            m_doc = -1;
        }

        // only for EmptyTermSpans (below)
        internal TermSpans()
        {
            m_term = null;
            m_postings = null;
        }

        public override bool MoveNext()
        {
            if (m_count == m_freq)
            {
                if (m_postings is null)
                {
                    return false;
                }
                m_doc = m_postings.NextDoc();
                if (m_doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return false;
                }
                m_freq = m_postings.Freq;
                m_count = 0;
            }
            m_position = m_postings.NextPosition();
            m_count++;
            m_readPayload = false;
            return true;
        }

        public override bool SkipTo(int target)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(target > m_doc);
            m_doc = m_postings.Advance(target);
            if (m_doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }

            m_freq = m_postings.Freq;
            m_count = 0;
            m_position = m_postings.NextPosition();
            m_count++;
            m_readPayload = false;
            return true;
        }

        public override int Doc => m_doc;

        public override int Start => m_position;

        public override int End => m_position + 1;

        public override long GetCost()
        {
            return m_postings.GetCost();
        }

        // TODO: Remove warning after API has been finalized
        public override ICollection<byte[]> GetPayload()
        {
            var payload = m_postings.GetPayload();
            m_readPayload = true;
            byte[] bytes;
            if (payload != null)
            {
                bytes = new byte[payload.Length];
                Arrays.Copy(payload.Bytes, payload.Offset, bytes, 0, payload.Length);
            }
            else
            {
                bytes = null;
            }
            return new[] { bytes };
        }

        // TODO: Remove warning after API has been finalized
        public override bool IsPayloadAvailable => m_readPayload == false && m_postings.GetPayload() != null;

        public override string ToString()
        {
            return "spans(" + m_term.ToString() + ")@" + (m_doc == -1 ? "START" : (m_doc == int.MaxValue) ? "END" : m_doc + "-" + m_position);
        }

        public virtual DocsAndPositionsEnum Postings => m_postings;

        private sealed class EmptyTermSpans : TermSpans
        {
            public override bool MoveNext()
            {
                return false;
            }

            public override bool SkipTo(int target)
            {
                return false;
            }

            public override int Doc => DocIdSetIterator.NO_MORE_DOCS;

            public override int Start => -1;

            public override int End => -1;

            public override ICollection<byte[]> GetPayload()
            {
                return null;
            }

            public override bool IsPayloadAvailable => false;

            public override long GetCost()
            {
                return 0;
            }
        }

        public static readonly TermSpans EMPTY_TERM_SPANS = new EmptyTermSpans();
    }
}