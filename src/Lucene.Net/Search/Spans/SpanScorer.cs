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

    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Public for extension only.
    /// </summary>
    public class SpanScorer : Scorer
    {
        protected Spans m_spans;

        protected bool m_more = true;

        protected int m_doc;
        protected float m_freq;
        protected int m_numMatches;
        protected readonly Similarity.SimScorer m_docScorer;

        protected internal SpanScorer(Spans spans, Weight weight, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.m_docScorer = docScorer;
            this.m_spans = spans;

            m_doc = -1;
            m_more = spans.MoveNext();
        }

        public override int NextDoc()
        {
            if (!SetFreqCurrentDoc())
            {
                m_doc = NO_MORE_DOCS;
            }
            return m_doc;
        }

        public override int Advance(int target)
        {
            if (!m_more)
            {
                return m_doc = NO_MORE_DOCS;
            }
            if (m_spans.Doc < target) // setFreqCurrentDoc() leaves spans.doc() ahead
            {
                m_more = m_spans.SkipTo(target);
            }
            if (!SetFreqCurrentDoc())
            {
                m_doc = NO_MORE_DOCS;
            }
            return m_doc;
        }

        protected virtual bool SetFreqCurrentDoc()
        {
            if (!m_more)
            {
                return false;
            }
            m_doc = m_spans.Doc;
            m_freq = 0.0f;
            m_numMatches = 0;
            do
            {
                int matchLength = m_spans.End - m_spans.Start;
                m_freq += m_docScorer.ComputeSlopFactor(matchLength);
                m_numMatches++;
                m_more = m_spans.MoveNext();
            } while (m_more && (m_doc == m_spans.Doc));
            return true;
        }

        public override int DocID => m_doc;

        public override float GetScore()
        {
            return m_docScorer.Score(m_doc, m_freq);
        }

        public override int Freq => m_numMatches;

        /// <summary>
        /// Returns the intermediate "sloppy freq" adjusted for edit distance
        /// <para/>
        /// @lucene.internal
        /// </summary>
        // only public so .payloads can see it.
        public virtual float SloppyFreq => m_freq;

        public override long GetCost()
        {
            return m_spans.GetCost();
        }
    }
}