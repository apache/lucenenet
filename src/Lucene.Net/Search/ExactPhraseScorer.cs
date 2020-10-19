using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;

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

    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    internal sealed class ExactPhraseScorer : Scorer
    {
        private readonly int endMinus1;

        private const int CHUNK = 4096;

        private int gen;
        private readonly int[] counts = new int[CHUNK];
        private readonly int[] gens = new int[CHUNK];

        internal bool noDocs;
        private readonly long cost;

        private sealed class ChunkState
        {
            internal DocsAndPositionsEnum PosEnum { get; private set; }
            internal int Offset { get; private set; }
            internal bool UseAdvance { get; private set; }
            internal int PosUpto { get; set; }
            internal int PosLimit { get; set; }
            internal int Pos { get; set; }
            internal int LastPos { get; set; }

            public ChunkState(DocsAndPositionsEnum posEnum, int offset, bool useAdvance)
            {
                this.PosEnum = posEnum;
                this.Offset = offset;
                this.UseAdvance = useAdvance;
            }
        }

        private readonly ChunkState[] chunkStates;

        private int docID = -1;
        private int freq;

        private readonly Similarity.SimScorer docScorer;

        internal ExactPhraseScorer(Weight weight, PhraseQuery.PostingsAndFreq[] postings, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.docScorer = docScorer;

            chunkStates = new ChunkState[postings.Length];

            endMinus1 = postings.Length - 1;

            // min(cost)
            cost = postings[0].postings.GetCost();

            for (int i = 0; i < postings.Length; i++)
            {
                // Coarse optimization: advance(target) is fairly
                // costly, so, if the relative freq of the 2nd
                // rarest term is not that much (> 1/5th) rarer than
                // the first term, then we just use .nextDoc() when
                // ANDing.  this buys ~15% gain for phrases where
                // freq of rarest 2 terms is close:
                bool useAdvance = postings[i].docFreq > 5 * postings[0].docFreq;
                chunkStates[i] = new ChunkState(postings[i].postings, -postings[i].position, useAdvance);
                if (i > 0 && postings[i].postings.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                {
                    noDocs = true;
                    return;
                }
            }
        }

        public override int NextDoc()
        {
            while (true)
            {
                // first (rarest) term
                int doc = chunkStates[0].PosEnum.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    docID = doc;
                    return doc;
                }

                // not-first terms
                int i = 1;
                while (i < chunkStates.Length)
                {
                    ChunkState cs = chunkStates[i];
                    int doc2 = cs.PosEnum.DocID;
                    if (cs.UseAdvance)
                    {
                        if (doc2 < doc)
                        {
                            doc2 = cs.PosEnum.Advance(doc);
                        }
                    }
                    else
                    {
                        int iter = 0;
                        while (doc2 < doc)
                        {
                            // safety net -- fallback to .advance if we've
                            // done too many .nextDocs
                            if (++iter == 50)
                            {
                                doc2 = cs.PosEnum.Advance(doc);
                                break;
                            }
                            else
                            {
                                doc2 = cs.PosEnum.NextDoc();
                            }
                        }
                    }
                    if (doc2 > doc)
                    {
                        break;
                    }
                    i++;
                }

                if (i == chunkStates.Length)
                {
                    // this doc has all the terms -- now test whether
                    // phrase occurs
                    docID = doc;

                    freq = PhraseFreq();
                    if (freq != 0)
                    {
                        return docID;
                    }
                }
            }
        }

        public override int Advance(int target)
        {
            // first term
            int doc = chunkStates[0].PosEnum.Advance(target);
            if (doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                docID = DocIdSetIterator.NO_MORE_DOCS;
                return doc;
            }

            while (true)
            {
                // not-first terms
                int i = 1;
                while (i < chunkStates.Length)
                {
                    int doc2 = chunkStates[i].PosEnum.DocID;
                    if (doc2 < doc)
                    {
                        doc2 = chunkStates[i].PosEnum.Advance(doc);
                    }
                    if (doc2 > doc)
                    {
                        break;
                    }
                    i++;
                }

                if (i == chunkStates.Length)
                {
                    // this doc has all the terms -- now test whether
                    // phrase occurs
                    docID = doc;
                    freq = PhraseFreq();
                    if (freq != 0)
                    {
                        return docID;
                    }
                }

                doc = chunkStates[0].PosEnum.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    docID = doc;
                    return doc;
                }
            }
        }

        public override string ToString()
        {
            return "ExactPhraseScorer(" + m_weight + ")";
        }

        public override int Freq => freq;

        public override int DocID => docID;

        public override float GetScore()
        {
            return docScorer.Score(docID, freq);
        }

        private int PhraseFreq()
        {
            freq = 0;

            // init chunks
            for (int i = 0; i < chunkStates.Length; i++)
            {
                ChunkState cs = chunkStates[i];
                cs.PosLimit = cs.PosEnum.Freq;
                cs.Pos = cs.Offset + cs.PosEnum.NextPosition();
                cs.PosUpto = 1;
                cs.LastPos = -1;
            }

            int chunkStart = 0;
            int chunkEnd = CHUNK;

            // process chunk by chunk
            bool end = false;

            // TODO: we could fold in chunkStart into offset and
            // save one subtract per pos incr

            while (!end)
            {
                gen++;

                if (gen == 0)
                {
                    // wraparound
                    Arrays.Fill(gens, 0);
                    gen++;
                }

                // first term
                {
                    ChunkState cs = chunkStates[0];
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            counts[posIndex] = 1;
                            if (Debugging.AssertsEnabled) Debugging.Assert(gens[posIndex] != gen);
                            gens[posIndex] = gen;
                        }

                        if (cs.PosUpto == cs.PosLimit)
                        {
                            end = true;
                            break;
                        }
                        cs.PosUpto++;
                        cs.Pos = cs.Offset + cs.PosEnum.NextPosition();
                    }
                }

                // middle terms
                bool any = true;
                for (int t = 1; t < endMinus1; t++)
                {
                    ChunkState cs = chunkStates[t];
                    any = false;
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            if (posIndex >= 0 && gens[posIndex] == gen && counts[posIndex] == t)
                            {
                                // viable
                                counts[posIndex]++;
                                any = true;
                            }
                        }

                        if (cs.PosUpto == cs.PosLimit)
                        {
                            end = true;
                            break;
                        }
                        cs.PosUpto++;
                        cs.Pos = cs.Offset + cs.PosEnum.NextPosition();
                    }

                    if (!any)
                    {
                        break;
                    }
                }

                if (!any)
                {
                    // petered out for this chunk
                    chunkStart += CHUNK;
                    chunkEnd += CHUNK;
                    continue;
                }

                // last term

                {
                    ChunkState cs = chunkStates[endMinus1];
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            if (posIndex >= 0 && gens[posIndex] == gen && counts[posIndex] == endMinus1)
                            {
                                freq++;
                            }
                        }

                        if (cs.PosUpto == cs.PosLimit)
                        {
                            end = true;
                            break;
                        }
                        cs.PosUpto++;
                        cs.Pos = cs.Offset + cs.PosEnum.NextPosition();
                    }
                }

                chunkStart += CHUNK;
                chunkEnd += CHUNK;
            }

            return freq;
        }

        public override long GetCost()
        {
            return cost;
        }
    }
}