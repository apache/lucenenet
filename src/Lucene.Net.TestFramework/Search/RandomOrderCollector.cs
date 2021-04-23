using Lucene.Net.Index;
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

    /// <summary>
    /// Randomize collection order. Don't forget to call <see cref="Flush()"/> when
    /// collection is finished to collect buffered documents.
    /// </summary>
    internal sealed class RandomOrderCollector : ICollector
    {
        internal readonly Random random;
        internal readonly ICollector @in;
        internal Scorer scorer;
        internal FakeScorer fakeScorer;

        internal int buffered;
        internal readonly int bufferSize;
        internal readonly int[] docIDs;
        internal readonly float[] scores;
        internal readonly int[] freqs;

        internal RandomOrderCollector(Random random, ICollector @in)
        {
            if (!@in.AcceptsDocsOutOfOrder)
            {
                throw new ArgumentException();
            }
            this.@in = @in;
            this.random = random;
            bufferSize = 1 + random.Next(100);
            docIDs = new int[bufferSize];
            scores = new float[bufferSize];
            freqs = new int[bufferSize];
            buffered = 0;
        }

        public void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
            fakeScorer = new FakeScorer();
            @in.SetScorer(fakeScorer);
        }

        private void Shuffle()
        {
            for (int i = buffered - 1; i > 0; --i)
            {
                int other = random.Next(i + 1);

                int tmpDoc = docIDs[i];
                docIDs[i] = docIDs[other];
                docIDs[other] = tmpDoc;

                float tmpScore = scores[i];
                scores[i] = scores[other];
                scores[other] = tmpScore;

                int tmpFreq = freqs[i];
                freqs[i] = freqs[other];
                freqs[other] = tmpFreq;
            }
        }

        public void Flush()
        {
            Shuffle();
            for (int i = 0; i < buffered; ++i)
            {
                fakeScorer.doc = docIDs[i];
                fakeScorer.freq = freqs[i];
                fakeScorer.score = scores[i];
                @in.Collect(fakeScorer.DocID);
            }
            buffered = 0;
        }

        public void Collect(int doc)
        {
            docIDs[buffered] = doc;
            scores[buffered] = scorer.GetScore();
            try
            {
                freqs[buffered] = scorer.Freq;
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                freqs[buffered] = -1;
            }
            if (++buffered == bufferSize)
            {
                Flush();
            }
        }

        public bool AcceptsDocsOutOfOrder
            => @in.AcceptsDocsOutOfOrder;

        public void SetNextReader(AtomicReaderContext context)
            => throw UnsupportedOperationException.Create();
    }
}