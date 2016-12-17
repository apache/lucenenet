using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using Bits = Lucene.Net.Util.Bits;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// @deprecated (4.0)
    ///  @lucene.experimental
    [Obsolete("(4.0)")]
    internal class SegmentTermDocs
    {
        //protected SegmentReader parent;
        private readonly FieldInfos FieldInfos;

        private readonly TermInfosReader Tis;
        protected internal Bits LiveDocs_Renamed;
        protected internal IndexInput FreqStream;
        protected internal int Count;
        protected internal int Df;
        internal int Doc_Renamed = 0;
        internal int Freq_Renamed;

        private int SkipInterval;
        private int MaxSkipLevels;
        private Lucene3xSkipListReader SkipListReader;

        private long FreqBasePointer;
        private long ProxBasePointer;

        private long SkipPointer;
        private bool HaveSkipped;

        protected internal bool CurrentFieldStoresPayloads;
        protected internal FieldInfo.IndexOptions? IndexOptions;

        public SegmentTermDocs(IndexInput freqStream, TermInfosReader tis, FieldInfos fieldInfos)
        {
            this.FreqStream = (IndexInput)freqStream.Clone();
            this.Tis = tis;
            this.FieldInfos = fieldInfos;
            SkipInterval = tis.SkipInterval;
            MaxSkipLevels = tis.MaxSkipLevels;
        }

        public virtual void Seek(Term term)
        {
            TermInfo ti = Tis.Get(term);
            Seek(ti, term);
        }

        public virtual Bits LiveDocs
        {
            get
            {
                return this.LiveDocs_Renamed; // LUCENENET specific - per MSDN, a property must always have a getter
            }
            set
            {
                this.LiveDocs_Renamed = value;
            }
        }

        public virtual void Seek(SegmentTermEnum segmentTermEnum)
        {
            TermInfo ti;
            Term term;

            // use comparison of fieldinfos to verify that termEnum belongs to the same segment as this SegmentTermDocs
            if (segmentTermEnum.FieldInfos == FieldInfos) // optimized case
            {
                term = segmentTermEnum.Term();
                ti = segmentTermEnum.TermInfo();
            } // punt case
            else
            {
                term = segmentTermEnum.Term();
                ti = Tis.Get(term);
            }

            Seek(ti, term);
        }

        internal virtual void Seek(TermInfo ti, Term term)
        {
            Count = 0;
            FieldInfo fi = FieldInfos.FieldInfo(term.Field);
            this.IndexOptions = (fi != null) ? fi.FieldIndexOptions : FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            CurrentFieldStoresPayloads = (fi != null) && fi.HasPayloads();
            if (ti == null)
            {
                Df = 0;
            }
            else
            {
                Df = ti.DocFreq;
                Doc_Renamed = 0;
                FreqBasePointer = ti.FreqPointer;
                ProxBasePointer = ti.ProxPointer;
                SkipPointer = FreqBasePointer + ti.SkipOffset;
                FreqStream.Seek(FreqBasePointer);
                HaveSkipped = false;
            }
        }

        public virtual void Close() // LUCENENET TODO: Make into Dispose() (maybe protected override)
        {
            FreqStream.Dispose();
            if (SkipListReader != null)
            {
                SkipListReader.Dispose();
            }
        }

        public int Doc() // LUCENENET TODO: make property
        {
            return Doc_Renamed;
        }

        public int Freq() // LUCENENET TODO: make property
        {
            return Freq_Renamed;
        }

        protected internal virtual void SkippingDoc()
        {
        }

        public virtual bool Next()
        {
            while (true)
            {
                if (Count == Df)
                {
                    return false;
                }
                int docCode = FreqStream.ReadVInt();

                if (IndexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
                {
                    Doc_Renamed += docCode;
                }
                else
                {
                    Doc_Renamed += (int)((uint)docCode >> 1); // shift off low bit
                    if ((docCode & 1) != 0) // if low bit is set
                    {
                        Freq_Renamed = 1; // freq is one
                    }
                    else
                    {
                        Freq_Renamed = FreqStream.ReadVInt(); // else read freq
                        Debug.Assert(Freq_Renamed != 1);
                    }
                }

                Count++;

                if (LiveDocs_Renamed == null || LiveDocs_Renamed.Get(Doc_Renamed))
                {
                    break;
                }
                SkippingDoc();
            }
            return true;
        }

        /// <summary>
        /// Optimized implementation. </summary>
        public virtual int Read(int[] docs, int[] freqs)
        {
            int length = docs.Length;
            if (IndexOptions == FieldInfo.IndexOptions.DOCS_ONLY)
            {
                return ReadNoTf(docs, freqs, length);
            }
            else
            {
                int i = 0;
                while (i < length && Count < Df)
                {
                    // manually inlined call to next() for speed
                    int docCode = FreqStream.ReadVInt();
                    Doc_Renamed += (int)((uint)docCode >> 1); // shift off low bit
                    if ((docCode & 1) != 0) // if low bit is set
                    {
                        Freq_Renamed = 1; // freq is one
                    }
                    else
                    {
                        Freq_Renamed = FreqStream.ReadVInt(); // else read freq
                    }
                    Count++;

                    if (LiveDocs_Renamed == null || LiveDocs_Renamed.Get(Doc_Renamed))
                    {
                        docs[i] = Doc_Renamed;
                        freqs[i] = Freq_Renamed;
                        ++i;
                    }
                }
                return i;
            }
        }

        private int ReadNoTf(int[] docs, int[] freqs, int length)
        {
            int i = 0;
            while (i < length && Count < Df)
            {
                // manually inlined call to next() for speed
                Doc_Renamed += FreqStream.ReadVInt();
                Count++;

                if (LiveDocs_Renamed == null || LiveDocs_Renamed.Get(Doc_Renamed))
                {
                    docs[i] = Doc_Renamed;
                    // Hardware freq to 1 when term freqs were not
                    // stored in the index
                    freqs[i] = 1;
                    ++i;
                }
            }
            return i;
        }

        /// <summary>
        /// Overridden by SegmentTermPositions to skip in prox stream. </summary>
        protected internal virtual void SkipProx(long proxPointer, int payloadLength)
        {
        }

        /// <summary>
        /// Optimized implementation. </summary>
        public virtual bool SkipTo(int target)
        {
            // don't skip if the target is close (within skipInterval docs away)
            if ((target - SkipInterval) >= Doc_Renamed && Df >= SkipInterval) // optimized case
            {
                if (SkipListReader == null)
                {
                    SkipListReader = new Lucene3xSkipListReader((IndexInput)FreqStream.Clone(), MaxSkipLevels, SkipInterval); // lazily clone
                }

                if (!HaveSkipped) // lazily initialize skip stream
                {
                    SkipListReader.Init(SkipPointer, FreqBasePointer, ProxBasePointer, Df, CurrentFieldStoresPayloads);
                    HaveSkipped = true;
                }

                int newCount = SkipListReader.SkipTo(target);
                if (newCount > Count)
                {
                    FreqStream.Seek(SkipListReader.FreqPointer);
                    SkipProx(SkipListReader.ProxPointer, SkipListReader.PayloadLength);

                    Doc_Renamed = SkipListReader.Doc;
                    Count = newCount;
                }
            }

            // done skipping, now just scan
            do
            {
                if (!Next())
                {
                    return false;
                }
            } while (target > Doc_Renamed);
            return true;
        }
    }
}