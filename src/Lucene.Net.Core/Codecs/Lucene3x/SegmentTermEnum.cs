using System;
using System.Diagnostics;
using FieldInfos = Lucene.Net.Index.FieldInfos;
using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;

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

    using IndexInput = Lucene.Net.Store.IndexInput;
    using Term = Lucene.Net.Index.Term;

    /// @deprecated (4.0) No longer used with flex indexing, except for
    /// reading old segments
    /// @lucene.experimental

    [Obsolete("(4.0) No longer used with flex indexing, except for reading old segments")]
    internal sealed class SegmentTermEnum : IDisposable
    {
        private IndexInput input;
        internal FieldInfos fieldInfos;
        internal long size;
        internal long position = -1;

        // Changed strings to true utf8 with length-in-bytes not
        // length-in-chars
        public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

        // NOTE: always change this if you switch to a new format!
        // whenever you add a new format, make it 1 smaller (negative version logic)!
        public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        // when removing support for old versions, leave the last supported version here
        public const int FORMAT_MINIMUM = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        private TermBuffer termBuffer = new TermBuffer();
        private TermBuffer prevBuffer = new TermBuffer();
        private TermBuffer scanBuffer = new TermBuffer(); // used for scanning

        internal TermInfo termInfo = new TermInfo();

        private int format;
        private bool isIndex = false;
        internal long indexPointer = 0;
        internal int indexInterval; // LUCENENET NOTE: Changed from public field to internal (class is internal anyway)
        internal int skipInterval;
        internal int newSuffixStart;
        internal int maxSkipLevels;
        private bool first = true;

        public SegmentTermEnum(IndexInput i, FieldInfos fis, bool isi)
        {
            input = i;
            fieldInfos = fis;
            isIndex = isi;
            maxSkipLevels = 1; // use single-level skip lists for formats > -3

            int firstInt = input.ReadInt();
            if (firstInt >= 0)
            {
                // original-format file, without explicit format version number
                format = 0;
                size = firstInt;

                // back-compatible settings
                indexInterval = 128;
                skipInterval = int.MaxValue; // switch off skipTo optimization
            }
            else
            {
                // we have a format version number
                format = firstInt;

                // check that it is a format we can understand
                if (format > FORMAT_MINIMUM)
                {
                    throw new IndexFormatTooOldException(input, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }
                if (format < FORMAT_CURRENT)
                {
                    throw new IndexFormatTooNewException(input, format, FORMAT_MINIMUM, FORMAT_CURRENT);
                }

                size = input.ReadLong(); // read the size

                indexInterval = input.ReadInt();
                skipInterval = input.ReadInt();
                maxSkipLevels = input.ReadInt();
                Debug.Assert(indexInterval > 0, "indexInterval=" + indexInterval + " is negative; must be > 0");
                Debug.Assert(skipInterval > 0, "skipInterval=" + skipInterval + " is negative; must be > 0");
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

            clone.input = (IndexInput)input.Clone();
            clone.termInfo = new TermInfo(termInfo);

            clone.termBuffer = (TermBuffer)termBuffer.Clone();
            clone.prevBuffer = (TermBuffer)prevBuffer.Clone();
            clone.scanBuffer = new TermBuffer();

            return clone;
        }

        internal void Seek(long pointer, long p, Term t, TermInfo ti)
        {
            input.Seek(pointer);
            position = p;
            termBuffer.Set(t);
            prevBuffer.Reset();
            //System.out.println("  ste doSeek prev=" + prevBuffer.toTerm() + " this=" + this);
            termInfo.Set(ti);
            first = p == -1;
        }

        /// <summary>
        /// Increments the enumeration to the next element.  True if one exists. </summary>
        public bool Next()
        {
            prevBuffer.Set(termBuffer);
            //System.out.println("  ste setPrev=" + prev() + " this=" + this);

            if (position++ >= size - 1)
            {
                termBuffer.Reset();
                //System.out.println("    EOF");
                return false;
            }

            termBuffer.Read(input, fieldInfos);
            newSuffixStart = termBuffer.newSuffixStart;

            termInfo.DocFreq = input.ReadVInt(); // read doc freq
            termInfo.FreqPointer += input.ReadVLong(); // read freq pointer
            termInfo.ProxPointer += input.ReadVLong(); // read prox pointer

            if (termInfo.DocFreq >= skipInterval)
            {
                termInfo.SkipOffset = input.ReadVInt();
            }

            if (isIndex)
            {
                indexPointer += input.ReadVLong(); // read index pointer
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
            scanBuffer.Set(term);
            int count = 0;
            if (first)
            {
                // Always force initial next() in case term is
                // Term("", "")
                Next();
                first = false;
                count++;
            }
            while (scanBuffer.CompareTo(termBuffer) > 0 && Next())
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
            return termBuffer.ToTerm();
        }

        /// <summary>
        /// Returns the previous Term enumerated. Initially null. </summary>
        internal Term Prev()
        {
            return prevBuffer.ToTerm();
        }

        /// <summary>
        /// Returns the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        internal TermInfo TermInfo()
        {
            return new TermInfo(termInfo);
        }

        /// <summary>
        /// Sets the argument to the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        internal void TermInfo(TermInfo ti)
        {
            ti.Set(termInfo);
        }

        /// <summary>
        /// Returns the docFreq from the current TermInfo in the enumeration.
        /// Initially invalid, valid after next() called for the first time.
        /// </summary>
        public int DocFreq
        {
            get { return termInfo.DocFreq; }
        }

        /* Returns the freqPointer from the current TermInfo in the enumeration.
          Initially invalid, valid after next() called for the first time.*/

        internal long FreqPointer
        {
            get { return termInfo.FreqPointer; }
        }

        /* Returns the proxPointer from the current TermInfo in the enumeration.
          Initially invalid, valid after next() called for the first time.*/

        internal long ProxPointer
        {
            get { return termInfo.ProxPointer; }
        }

        /// <summary>
        /// Closes the enumeration to further activity, freeing resources. </summary>
        public void Dispose()
        {
            input.Dispose();
        }
    }
}