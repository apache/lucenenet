using System;
using System.Diagnostics;
using IBits = Lucene.Net.Util.IBits;

namespace Lucene.Net.Codecs.Lucene3x
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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using Term = Lucene.Net.Index.Term;

    /// @deprecated (4.0)
    ///  @lucene.experimental
    [Obsolete("(4.0)")]
    internal class SegmentTermDocs
    {
        //protected SegmentReader parent;
        private readonly FieldInfos FieldInfos;

        private readonly TermInfosReader Tis;
        protected IBits m_liveDocs;
        protected IndexInput m_freqStream;
        protected int m_count;
        protected internal int m_df;
        internal int Doc_Renamed = 0;
        internal int Freq_Renamed;

        private int SkipInterval;
        private int MaxSkipLevels;
        private Lucene3xSkipListReader SkipListReader;

        private long FreqBasePointer;
        private long ProxBasePointer;

        private long SkipPointer;
        private bool HaveSkipped;

        protected bool m_currentFieldStoresPayloads;
        protected IndexOptions? m_indexOptions;

        public SegmentTermDocs(IndexInput freqStream, TermInfosReader tis, FieldInfos fieldInfos)
        {
            this.m_freqStream = (IndexInput)freqStream.Clone();
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

        public virtual IBits LiveDocs
        {
            get
            {
                return this.m_liveDocs; // LUCENENET specific - per MSDN, a property must always have a getter
            }
            set
            {
                this.m_liveDocs = value;
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
            m_count = 0;
            FieldInfo fi = FieldInfos.FieldInfo(term.Field);
            this.m_indexOptions = (fi != null) ? fi.IndexOptions : IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            m_currentFieldStoresPayloads = (fi != null) && fi.HasPayloads;
            if (ti == null)
            {
                m_df = 0;
            }
            else
            {
                m_df = ti.DocFreq;
                Doc_Renamed = 0;
                FreqBasePointer = ti.FreqPointer;
                ProxBasePointer = ti.ProxPointer;
                SkipPointer = FreqBasePointer + ti.SkipOffset;
                m_freqStream.Seek(FreqBasePointer);
                HaveSkipped = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_freqStream.Dispose();
                if (SkipListReader != null)
                {
                    SkipListReader.Dispose();
                }
            }
        }

        public int Doc
        {
            get { return Doc_Renamed; }
        }

        public int Freq
        {
            get { return Freq_Renamed; }
        }

        protected internal virtual void SkippingDoc()
        {
        }

        public virtual bool Next()
        {
            while (true)
            {
                if (m_count == m_df)
                {
                    return false;
                }
                int docCode = m_freqStream.ReadVInt();

                if (m_indexOptions == IndexOptions.DOCS_ONLY)
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
                        Freq_Renamed = m_freqStream.ReadVInt(); // else read freq
                        Debug.Assert(Freq_Renamed != 1);
                    }
                }

                m_count++;

                if (m_liveDocs == null || m_liveDocs.Get(Doc_Renamed))
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
            if (m_indexOptions == IndexOptions.DOCS_ONLY)
            {
                return ReadNoTf(docs, freqs, length);
            }
            else
            {
                int i = 0;
                while (i < length && m_count < m_df)
                {
                    // manually inlined call to next() for speed
                    int docCode = m_freqStream.ReadVInt();
                    Doc_Renamed += (int)((uint)docCode >> 1); // shift off low bit
                    if ((docCode & 1) != 0) // if low bit is set
                    {
                        Freq_Renamed = 1; // freq is one
                    }
                    else
                    {
                        Freq_Renamed = m_freqStream.ReadVInt(); // else read freq
                    }
                    m_count++;

                    if (m_liveDocs == null || m_liveDocs.Get(Doc_Renamed))
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
            while (i < length && m_count < m_df)
            {
                // manually inlined call to next() for speed
                Doc_Renamed += m_freqStream.ReadVInt();
                m_count++;

                if (m_liveDocs == null || m_liveDocs.Get(Doc_Renamed))
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
            if ((target - SkipInterval) >= Doc_Renamed && m_df >= SkipInterval) // optimized case
            {
                if (SkipListReader == null)
                {
                    SkipListReader = new Lucene3xSkipListReader((IndexInput)m_freqStream.Clone(), MaxSkipLevels, SkipInterval); // lazily clone
                }

                if (!HaveSkipped) // lazily initialize skip stream
                {
                    SkipListReader.Init(SkipPointer, FreqBasePointer, ProxBasePointer, m_df, m_currentFieldStoresPayloads);
                    HaveSkipped = true;
                }

                int newCount = SkipListReader.SkipTo(target);
                if (newCount > m_count)
                {
                    m_freqStream.Seek(SkipListReader.FreqPointer);
                    SkipProx(SkipListReader.ProxPointer, SkipListReader.PayloadLength);

                    Doc_Renamed = SkipListReader.Doc;
                    m_count = newCount;
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