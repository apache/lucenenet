using System;

namespace Lucene.Net.Search
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

    /// <summary>
    /// Randomize collection order. Don't forget to call <seealso cref="#flush()"/> when
    ///  collection is finished to collect buffered documents.
    /// </summary>
    internal sealed class RandomOrderCollector : Collector
    {
        internal readonly Random Random;
        internal readonly Collector @in;
        internal Scorer Scorer_Renamed;
        internal FakeScorer fakeScorer;

        internal int Buffered;
        internal readonly int BufferSize;
        internal readonly int[] DocIDs;
        internal readonly float[] Scores;
        internal readonly int[] Freqs;

        internal RandomOrderCollector(Random random, Collector @in)
        {
            if (!@in.AcceptsDocsOutOfOrder())
            {
                throw new System.ArgumentException();
            }
            this.@in = @in;
            this.Random = random;
            BufferSize = 1 + random.Next(100);
            DocIDs = new int[BufferSize];
            Scores = new float[BufferSize];
            Freqs = new int[BufferSize];
            Buffered = 0;
        }

        public override void SetScorer(Scorer scorer)
        {
            this.Scorer_Renamed = scorer;
            fakeScorer = new FakeScorer();
            @in.SetScorer(fakeScorer);
        }

        private void Shuffle()
        {
            for (int i = Buffered - 1; i > 0; --i)
            {
                int other = Random.Next(i + 1);

                int tmpDoc = DocIDs[i];
                DocIDs[i] = DocIDs[other];
                DocIDs[other] = tmpDoc;

                float tmpScore = Scores[i];
                Scores[i] = Scores[other];
                Scores[other] = tmpScore;

                int tmpFreq = Freqs[i];
                Freqs[i] = Freqs[other];
                Freqs[other] = tmpFreq;
            }
        }

        public void Flush()
        {
            Shuffle();
            for (int i = 0; i < Buffered; ++i)
            {
                fakeScorer.doc = DocIDs[i];
                fakeScorer.freq = Freqs[i];
                fakeScorer.score = Scores[i];
                @in.Collect(fakeScorer.DocID());
            }
            Buffered = 0;
        }

        public override void Collect(int doc)
        {
            DocIDs[Buffered] = doc;
            Scores[Buffered] = Scorer_Renamed.Score();
            try
            {
                Freqs[Buffered] = Scorer_Renamed.Freq;
            }
            catch (System.NotSupportedException)
            {
                Freqs[Buffered] = -1;
            }
            if (++Buffered == BufferSize)
            {
                Flush();
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return @in.AcceptsDocsOutOfOrder();
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            throw new System.NotSupportedException();
        }
    }
}