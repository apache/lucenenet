using System.Diagnostics;

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

    using Lucene.Net.Index;
    using Lucene.Net.Support;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    internal sealed class ExactPhraseScorer : Scorer
    {
        private readonly int EndMinus1; // LUCENENET TODO: rename (private)

        private const int CHUNK = 4096;

        private int Gen; // LUCENENET TODO: rename (private)
        private readonly int[] Counts = new int[CHUNK]; // LUCENENET TODO: rename (private)
        private readonly int[] Gens = new int[CHUNK]; // LUCENENET TODO: rename (private)

        internal bool NoDocs; // LUCENENET TODO: rename (private)
        private readonly long Cost_Renamed; // LUCENENET TODO: rename (private)

        private sealed class ChunkState
        {
            internal readonly DocsAndPositionsEnum PosEnum; // LUCENENET TODO: Make property
            internal readonly int Offset; // LUCENENET TODO: Make property
            internal readonly bool UseAdvance; // LUCENENET TODO: Make property
            internal int PosUpto; // LUCENENET TODO: Make property
            internal int PosLimit; // LUCENENET TODO: Make property
            internal int Pos; // LUCENENET TODO: Make property
            internal int LastPos; // LUCENENET TODO: Make property

            public ChunkState(DocsAndPositionsEnum posEnum, int offset, bool useAdvance)
            {
                this.PosEnum = posEnum;
                this.Offset = offset;
                this.UseAdvance = useAdvance;
            }
        }

        private readonly ChunkState[] ChunkStates; // LUCENENET TODO: rename (private)

        private int DocID_Renamed = -1; // LUCENENET TODO: rename (private)
        private int Freq_Renamed; // LUCENENET TODO: rename (private)

        private readonly Similarity.SimScorer DocScorer; // LUCENENET TODO: rename (private)

        internal ExactPhraseScorer(Weight weight, PhraseQuery.PostingsAndFreq[] postings, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.DocScorer = docScorer;

            ChunkStates = new ChunkState[postings.Length];

            EndMinus1 = postings.Length - 1;

            // min(cost)
            Cost_Renamed = postings[0].Postings.Cost();

            for (int i = 0; i < postings.Length; i++)
            {
                // Coarse optimization: advance(target) is fairly
                // costly, so, if the relative freq of the 2nd
                // rarest term is not that much (> 1/5th) rarer than
                // the first term, then we just use .nextDoc() when
                // ANDing.  this buys ~15% gain for phrases where
                // freq of rarest 2 terms is close:
                bool useAdvance = postings[i].DocFreq > 5 * postings[0].DocFreq;
                ChunkStates[i] = new ChunkState(postings[i].Postings, -postings[i].Position, useAdvance);
                if (i > 0 && postings[i].Postings.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                {
                    NoDocs = true;
                    return;
                }
            }
        }

        public override int NextDoc()
        {
            while (true)
            {
                // first (rarest) term
                int doc = ChunkStates[0].PosEnum.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    DocID_Renamed = doc;
                    return doc;
                }

                // not-first terms
                int i = 1;
                while (i < ChunkStates.Length)
                {
                    ChunkState cs = ChunkStates[i];
                    int doc2 = cs.PosEnum.DocID();
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

                if (i == ChunkStates.Length)
                {
                    // this doc has all the terms -- now test whether
                    // phrase occurs
                    DocID_Renamed = doc;

                    Freq_Renamed = PhraseFreq();
                    if (Freq_Renamed != 0)
                    {
                        return DocID_Renamed;
                    }
                }
            }
        }

        public override int Advance(int target)
        {
            // first term
            int doc = ChunkStates[0].PosEnum.Advance(target);
            if (doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                DocID_Renamed = DocIdSetIterator.NO_MORE_DOCS;
                return doc;
            }

            while (true)
            {
                // not-first terms
                int i = 1;
                while (i < ChunkStates.Length)
                {
                    int doc2 = ChunkStates[i].PosEnum.DocID();
                    if (doc2 < doc)
                    {
                        doc2 = ChunkStates[i].PosEnum.Advance(doc);
                    }
                    if (doc2 > doc)
                    {
                        break;
                    }
                    i++;
                }

                if (i == ChunkStates.Length)
                {
                    // this doc has all the terms -- now test whether
                    // phrase occurs
                    DocID_Renamed = doc;
                    Freq_Renamed = PhraseFreq();
                    if (Freq_Renamed != 0)
                    {
                        return DocID_Renamed;
                    }
                }

                doc = ChunkStates[0].PosEnum.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    DocID_Renamed = doc;
                    return doc;
                }
            }
        }

        public override string ToString()
        {
            return "ExactPhraseScorer(" + weight + ")";
        }

        public override int Freq
        {
            get { return Freq_Renamed; }
        }

        public override int DocID()
        {
            return DocID_Renamed;
        }

        public override float Score()
        {
            return DocScorer.Score(DocID_Renamed, Freq_Renamed);
        }

        private int PhraseFreq()
        {
            Freq_Renamed = 0; // LUCENENET TODO: rename (private)

            // init chunks
            for (int i = 0; i < ChunkStates.Length; i++)
            {
                ChunkState cs = ChunkStates[i];
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
                Gen++;

                if (Gen == 0)
                {
                    // wraparound
                    Arrays.Fill(Gens, 0);
                    Gen++;
                }

                // first term
                {
                    ChunkState cs = ChunkStates[0];
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            Counts[posIndex] = 1;
                            Debug.Assert(Gens[posIndex] != Gen);
                            Gens[posIndex] = Gen;
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
                for (int t = 1; t < EndMinus1; t++)
                {
                    ChunkState cs = ChunkStates[t];
                    any = false;
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            if (posIndex >= 0 && Gens[posIndex] == Gen && Counts[posIndex] == t)
                            {
                                // viable
                                Counts[posIndex]++;
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
                    ChunkState cs = ChunkStates[EndMinus1];
                    while (cs.Pos < chunkEnd)
                    {
                        if (cs.Pos > cs.LastPos)
                        {
                            cs.LastPos = cs.Pos;
                            int posIndex = cs.Pos - chunkStart;
                            if (posIndex >= 0 && Gens[posIndex] == Gen && Counts[posIndex] == EndMinus1)
                            {
                                Freq_Renamed++;
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

            return Freq_Renamed;
        }

        public override long Cost()
        {
            return Cost_Renamed;
        }
    }
}