/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Search.Spans
{
    /// <summary> Public for extension only.</summary>
    public class SpanScorer : Scorer
    {
        protected Spans spans;

        protected bool more = true;

        protected int doc;
        protected float freq;
        protected int numMatches;
        protected Similarity.SloppySimScorer docScorer;



        public SpanScorer(Spans spans, Weight weight, Similarity.SloppySimScorer docScorer)
            : base(weight)
        {
            this.docScorer = docScorer;
            this.spans = spans;

            doc = -1;
            more = spans.Next();
        }

        public override int NextDoc()
        {
            if (!SetFreqCurrentDoc())
            {
                doc = NO_MORE_DOCS;
            }
            return doc;
        }

        public override int Advance(int target)
        {
            if (!more)
            {
                return doc = NO_MORE_DOCS;
            }
            if (spans.Doc() < target)
            {
                // setFreqCurrentDoc() leaves spans.doc() ahead
                more = spans.SkipTo(target);
            }
            if (!SetFreqCurrentDoc())
            {
                doc = NO_MORE_DOCS;
            }
            return doc;
        }

        protected virtual bool SetFreqCurrentDoc()
        {
            if (!more)
            {
                return false;
            }
            doc = spans.Doc();
            freq = 0.0f;
            numMatches = 0;
            do
            {
                int matchLength = spans.End() - spans.Start();
                freq += docScorer.ComputeSlopFactor(matchLength);
                numMatches++;
                more = spans.Next();
            } while (more && (doc == spans.Doc()));
            return true;
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override float Score()
        {
            return docScorer.Score(doc, freq);
        }

        public override int Freq()
        {
            return numMatches;
        }

        public float SloppyFreq()
        {
            return freq;
        }

        public override long Cost
        {
            get { return spans.Cost(); }
        }
    }
}