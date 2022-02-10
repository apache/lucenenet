using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    /// <summary>
    /// @lucene.experimental
    /// </summary>
    [Obsolete("(4.0)")]
    internal class SegmentTermDocs
    {
        //protected SegmentReader parent;
        private readonly FieldInfos fieldInfos;

        private readonly TermInfosReader tis;
        protected IBits m_liveDocs;
        protected IndexInput m_freqStream;
        protected int m_count;
        protected internal int m_df;
        internal int doc = 0;
        internal int freq;

        private readonly int skipInterval; // LUCENENET: marked readonly
        private readonly int maxSkipLevels; // LUCENENET: marked readonly
        private Lucene3xSkipListReader skipListReader;

        private long freqBasePointer;
        private long proxBasePointer;

        private long skipPointer;
        private bool haveSkipped;

        protected bool m_currentFieldStoresPayloads;
        protected IndexOptions m_indexOptions;

        public SegmentTermDocs(IndexInput freqStream, TermInfosReader tis, FieldInfos fieldInfos)
        {
            this.m_freqStream = (IndexInput)freqStream.Clone();
            this.tis = tis;
            this.fieldInfos = fieldInfos;
            skipInterval = tis.SkipInterval;
            maxSkipLevels = tis.MaxSkipLevels;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Seek(Term term)
        {
            TermInfo ti = tis.Get(term);
            Seek(ti, term);
        }

        public virtual IBits LiveDocs
        {
            get => this.m_liveDocs; // LUCENENET specific - per MSDN, a property must always have a getter
            set => this.m_liveDocs = value;
        }

        public virtual void Seek(SegmentTermEnum segmentTermEnum)
        {
            TermInfo ti;
            Term term;

            // use comparison of fieldinfos to verify that termEnum belongs to the same segment as this SegmentTermDocs
            if (segmentTermEnum.fieldInfos == fieldInfos) // optimized case
            {
                term = segmentTermEnum.Term();
                ti = segmentTermEnum.TermInfo();
            } // punt case
            else
            {
                term = segmentTermEnum.Term();
                ti = tis.Get(term);
            }

            Seek(ti, term);
        }

        internal virtual void Seek(TermInfo ti, Term term)
        {
            m_count = 0;
            FieldInfo fi = fieldInfos.FieldInfo(term.Field);
            this.m_indexOptions = (fi != null) ? fi.IndexOptions : IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
            m_currentFieldStoresPayloads = (fi != null) && fi.HasPayloads;
            if (ti is null)
            {
                m_df = 0;
            }
            else
            {
                m_df = ti.DocFreq;
                doc = 0;
                freqBasePointer = ti.FreqPointer;
                proxBasePointer = ti.ProxPointer;
                skipPointer = freqBasePointer + ti.SkipOffset;
                m_freqStream.Seek(freqBasePointer);
                haveSkipped = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_freqStream.Dispose();
                if (skipListReader != null)
                {
                    skipListReader.Dispose();
                }
            }
        }

        public int Doc => doc;

        public int Freq => freq;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                int docCode = m_freqStream.ReadVInt32();

                if (m_indexOptions == IndexOptions.DOCS_ONLY)
                {
                    doc += docCode;
                }
                else
                {
                    doc += docCode.TripleShift(1); // shift off low bit
                    if ((docCode & 1) != 0) // if low bit is set
                    {
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = m_freqStream.ReadVInt32(); // else read freq
                        if (Debugging.AssertsEnabled) Debugging.Assert(freq != 1);
                    }
                }

                m_count++;

                if (m_liveDocs is null || m_liveDocs.Get(doc))
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
                    int docCode = m_freqStream.ReadVInt32();
                    doc += docCode.TripleShift(1); // shift off low bit
                    if ((docCode & 1) != 0) // if low bit is set
                    {
                        freq = 1; // freq is one
                    }
                    else
                    {
                        freq = m_freqStream.ReadVInt32(); // else read freq
                    }
                    m_count++;

                    if (m_liveDocs is null || m_liveDocs.Get(doc))
                    {
                        docs[i] = doc;
                        freqs[i] = freq;
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
                doc += m_freqStream.ReadVInt32();
                m_count++;

                if (m_liveDocs is null || m_liveDocs.Get(doc))
                {
                    docs[i] = doc;
                    // Hardware freq to 1 when term freqs were not
                    // stored in the index
                    freqs[i] = 1;
                    ++i;
                }
            }
            return i;
        }

        /// <summary>
        /// Overridden by <see cref="SegmentTermPositions"/> to skip in prox stream. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal virtual void SkipProx(long proxPointer, int payloadLength)
        {
        }

        /// <summary>
        /// Optimized implementation. </summary>
        public virtual bool SkipTo(int target)
        {
            // don't skip if the target is close (within skipInterval docs away)
            if ((target - skipInterval) >= doc && m_df >= skipInterval) // optimized case
            {
                if (skipListReader is null)
                {
                    skipListReader = new Lucene3xSkipListReader((IndexInput)m_freqStream.Clone(), maxSkipLevels, skipInterval); // lazily clone
                }

                if (!haveSkipped) // lazily initialize skip stream
                {
                    skipListReader.Init(skipPointer, freqBasePointer, proxBasePointer, m_df, m_currentFieldStoresPayloads);
                    haveSkipped = true;
                }

                int newCount = skipListReader.SkipTo(target);
                if (newCount > m_count)
                {
                    m_freqStream.Seek(skipListReader.FreqPointer);
                    SkipProx(skipListReader.ProxPointer, skipListReader.PayloadLength);

                    doc = skipListReader.Doc;
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
            } while (target > doc);
            return true;
        }
    }
}