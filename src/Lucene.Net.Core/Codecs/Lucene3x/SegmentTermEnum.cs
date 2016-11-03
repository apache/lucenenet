using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
    using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;

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

    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// @deprecated (4.0) No longer used with flex indexing, except for
    /// reading old segments
    /// @lucene.experimental

    [Obsolete]
    public sealed class SegmentTermEnum : IDisposable
    {
        private IndexInput Input;
        internal FieldInfos FieldInfos;
        internal long Size;
        internal long Position = -1;

        // Changed strings to true utf8 with length-in-bytes not
        // length-in-chars
        public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

        // NOTE: always change this if you switch to a new format!
        // whenever you add a new format, make it 1 smaller (negative version logic)!
        public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        // when removing support for old versions, leave the last supported version here
        public const int FORMAT_MINIMUM = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        private TermBuffer TermBuffer = new TermBuffer();
        private TermBuffer PrevBuffer = new TermBuffer();
        private TermBuffer ScanBuffer = new TermBuffer(); // used for scanning

        internal TermInfo TermInfo_Renamed = new TermInfo();

        private int Format;
        private bool IsIndex = false;
        internal long IndexPointer = 0;
        public int IndexInterval;
        internal int SkipInterval;
        internal int NewSuffixStart;
        internal int MaxSkipLevels;
        private bool First = true;

        public SegmentTermEnum(IndexInput i, FieldInfos fis, bool isi)
        {
            Input = i;
            FieldInfos = fis;
            IsIndex = isi;
            MaxSkipLevels = 1; // use single-level skip lists for formats > -3

            int firstInt = Input.ReadInt();
            if (firstInt >= 0)
            {
                // original-format file, without explicit format version number
                Format = 0;
                Size = firstInt;

                // back-compatible settings
                IndexInterval = 128;
                SkipInterval = int.MaxValue; // switch off skipTo optimization
            }
            else
            {
                // we have a format version number
                Format = firstInt;

                // check that it is a format we can understand
                if (Format > FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(Input, Format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
                if (Format < FORMAT_CURRENT)
                {
                    throw new IndexFormatTooNewException(Input, Format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }

                Size = Input.ReadLong(); // read the size

                IndexInterval = Input.ReadInt();
                SkipInterval = Input.ReadInt();
                MaxSkipLevels = Input.ReadInt();
                Debug.Assert(IndexInterval > 0, "indexInterval=" + IndexInterval + " is negative; must be > 0");
                Debug.Assert(SkipInterval > 0, "skipInterval=" + SkipInterval + " is negative; must be > 0");
            }
        }

        public object Clone()
        {
            SegmentTermEnum clone = null;
            try
            {
                clone = (SegmentTermEnum)base.MemberwiseClone();
            }
            catch (InvalidOperationException e)
            {
            }

            clone.Input = (IndexInput)Input.Clone();
            clone.TermInfo_Renamed = new TermInfo(TermInfo_Renamed);

            clone.TermBuffer = (TermBuffer)TermBuffer.Clone();
            clone.PrevBuffer = (TermBuffer)PrevBuffer.Clone();
            clone.ScanBuffer = new TermBuffer();

            return clone;
        }

        internal void Seek(long pointer, long p, Term t, TermInfo ti)
        {
            Input.Seek(pointer);
            Position = p;
            TermBuffer.Set(t);
            PrevBuffer.Reset();
            //System.out.println("  ste doSeek prev=" + prevBuffer.toTerm() + " this=" + this);
            TermInfo_Renamed.Set(ti);
            First = p == -1;
        }

        /// <summary>
        /// Increments the enumeration to the next element.  True if one exists. </summary>
        public bool Next()
        {
            PrevBuffer.Set(TermBuffer);
            //System.out.println("  ste setPrev=" + prev() + " this=" + this);

            if (Position++ >= Size - 1)
            {
                TermBuffer.Reset();
                //System.out.println("    EOF");
                return false;
            }

            TermBuffer.Read(Input, FieldInfos);
            NewSuffixStart = TermBuffer.NewSuffixStart;

            TermInfo_Renamed.DocFreq = Input.ReadVInt(); // read doc freq
            TermInfo_Renamed.FreqPointer += Input.ReadVLong(); // read freq pointer
            TermInfo_Renamed.ProxPointer += Input.ReadVLong(); // read prox pointer

            if (TermInfo_Renamed.DocFreq >= SkipInterval)
            {
                TermInfo_Renamed.SkipOffset = Input.ReadVInt();
            }

            if (IsIndex)
            {
                IndexPointer += Input.ReadVLong(); // read index pointer
            }

            //System.out.println("  ste ret term=" + term());
            return true;
        }

        /* Optimized scan, without allocating new terms.
         *  Return number of invocations to next().
         *
         * NOTE: LUCENE-3183: if you pass Term("", "") here then this
         * will incorrectly return before positioning the enum,
         * and position will be -1; caller must detect this. */

        internal int ScanTo(Term term)
        {
            ScanBuffer.Set(term);
            int count = 0;
            if (First)
            {
                // Always force initial next() in case term is
                // Term("", "")
                Next();
                First = false;
                count++;
            }
            while (ScanBuffer.CompareTo(TermBuffer) > 0 && Next())
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Returns the current Term in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        public Term Term()
        {
            return TermBuffer.ToTerm();
        }

        /// <summary>
        /// Returns the previous Term enumerated. Initially null. </summary>
        internal Term Prev()
        {
            return PrevBuffer.ToTerm();
        }

        /// <summary>
        /// Returns the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        internal TermInfo TermInfo()
        {
            return new TermInfo(TermInfo_Renamed);
        }

        /// <summary>
        /// Sets the argument to the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        internal void TermInfo(TermInfo ti)
        {
            ti.Set(TermInfo_Renamed);
        }

        /// <summary>
        /// Returns the docFreq from the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        public int DocFreq()
        {
            return TermInfo_Renamed.DocFreq;
        }

        /* Returns the freqPointer from the current TermInfo in the enumeration.
          Initially invalid, valid after next() called for the first time.*/

        internal long FreqPointer()
        {
            return TermInfo_Renamed.FreqPointer;
        }

        /* Returns the proxPointer from the current TermInfo in the enumeration.
          Initially invalid, valid after next() called for the first time.*/

        internal long ProxPointer()
        {
            return TermInfo_Renamed.ProxPointer;
        }

        /// <summary>
        /// Closes the enumeration to further activity, freeing resources. </summary>
        public void Dispose()
        {
            Input.Dispose();
        }
    }
}