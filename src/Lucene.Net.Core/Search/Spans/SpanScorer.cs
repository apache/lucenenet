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
        protected internal Spans Spans;

        protected internal bool More = true;

        protected internal int Doc;
        protected internal float Freq_Renamed;
        protected internal int NumMatches;
        protected internal readonly Similarity.SimScorer DocScorer;

        protected internal SpanScorer(Spans spans, Weight weight, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.DocScorer = docScorer;
            this.Spans = spans;

            Doc = -1;
            More = spans.Next();
        }

        public override int NextDoc()
        {
            if (!SetFreqCurrentDoc())
            {
                Doc = NO_MORE_DOCS;
            }
            return Doc;
        }

        public override int Advance(int target)
        {
            if (!More)
            {
                return Doc = NO_MORE_DOCS;
            }
            if (Spans.Doc() < target) // setFreqCurrentDoc() leaves spans.doc() ahead
            {
                More = Spans.SkipTo(target);
            }
            if (!SetFreqCurrentDoc())
            {
                Doc = NO_MORE_DOCS;
            }
            return Doc;
        }

        protected internal virtual bool SetFreqCurrentDoc()
        {
            if (!More)
            {
                return false;
            }
            Doc = Spans.Doc();
            Freq_Renamed = 0.0f;
            NumMatches = 0;
            do
            {
                int matchLength = Spans.End() - Spans.Start();
                Freq_Renamed += DocScorer.ComputeSlopFactor(matchLength);
                NumMatches++;
                More = Spans.Next();
            } while (More && (Doc == Spans.Doc()));
            return true;
        }

        public override int DocID()
        {
            return Doc;
        }

        public override float Score()
        {
            return DocScorer.Score(Doc, Freq_Renamed);
        }

        public override int Freq
        {
            get { return NumMatches; }
        }

        /// <summary>
        /// Returns the intermediate "sloppy freq" adjusted for edit distance
        ///  @lucene.internal
        /// </summary>
        // only public so .payloads can see it.
        public virtual float SloppyFreq()
        {
            return Freq_Renamed;
        }

        public override long Cost()
        {
            return Spans.Cost();
        }
    }
}