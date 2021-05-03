using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

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

    /// <summary>
    /// This stores a monotonically increasing set of <see cref="Index.Term"/>, <see cref="TermInfo"/> pairs in a
    /// <see cref="Directory"/>.  A <see cref="TermInfo"/>s can be written once, in order.
    /// </summary>
#pragma warning disable 612, 618
    internal sealed class TermInfosWriter : IDisposable
    {
        /// <summary>
        /// The file format version, a negative number. </summary>
        public const int FORMAT = -3;

        // Changed strings to true utf8 with length-in-bytes not
        // length-in-chars
        public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

        // NOTE: always change this if you switch to a new format!
        public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

        private FieldInfos fieldInfos;
        private IndexOutput output;
        private readonly TermInfo lastTi = new TermInfo(); // LUCENENET: marked readonly
        private long size;

        // TODO: the default values for these two parameters should be settable from
        // IndexWriter.  However, once that's done, folks will start setting them to
        // ridiculous values and complaining that things don't work well, as with
        // mergeFactor.  So, let's wait until a number of folks find that alternate
        // values work better.  Note that both of these values are stored in the
        // segment, so that it's safe to change these w/o rebuilding all indexes.

        /// <summary>
        /// Expert: The fraction of terms in the "dictionary" which should be stored
        /// in RAM.  Smaller values use more memory, but make searching slightly
        /// faster, while larger values use less memory and make searching slightly
        /// slower.  Searching is typically not dominated by dictionary lookup, so
        /// tweaking this is rarely useful.
        /// </summary>
        internal int indexInterval = 128;

        /// <summary>
        /// Expert: The fraction of term entries stored in skip tables,
        /// used to accelerate skipping.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here.
        /// </summary>
        internal int skipInterval = 16;

        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal int maxSkipLevels = 10;

        private long lastIndexPointer;
        private bool isIndex;
        private readonly BytesRef lastTerm = new BytesRef();
        private int lastFieldNumber = -1;

        private TermInfosWriter other;

        internal TermInfosWriter(Directory directory, string segment, FieldInfos fis, int interval)
        {
            Initialize(directory, segment, fis, interval, false);
            bool success = false;
            try
            {
                other = new TermInfosWriter(directory, segment, fis, interval, true);
                other.other = this;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);

                    try
                    {
                        directory.DeleteFile(IndexFileNames.SegmentFileName(segment, "", (isIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
                    }
                    catch (Exception ignored) when (ignored.IsIOException())
                    {
                    }
                }
            }
        }

        private TermInfosWriter(Directory directory, string segment, FieldInfos fis, int interval, bool isIndex)
        {
            Initialize(directory, segment, fis, interval, isIndex);
        }

        private void Initialize(Directory directory, string segment, FieldInfos fis, int interval, bool isi)
        {
            indexInterval = interval;
            fieldInfos = fis;
            isIndex = isi;
            output = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", (isIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)), IOContext.DEFAULT);
            bool success = false;
            try
            {
                output.WriteInt32(FORMAT_CURRENT); // write format
                output.WriteInt64(0); // leave space for size
                output.WriteInt32(indexInterval); // write indexInterval
                output.WriteInt32(skipInterval); // write skipInterval
                output.WriteInt32(maxSkipLevels); // write maxSkipLevels
                if (Debugging.AssertsEnabled) Debugging.Assert(InitUTF16Results());
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);

                    try
                    {
                        directory.DeleteFile(IndexFileNames.SegmentFileName(segment, "", (isIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
                    }
                    catch (Exception ignored) when (ignored.IsIOException())
                    {
                    }
                }
            }
        }

        // Currently used only by assert statements
        internal CharsRef utf16Result1;

        internal CharsRef utf16Result2;
        private readonly BytesRef scratchBytes = new BytesRef();

        // Currently used only by assert statements
        private bool InitUTF16Results()
        {
            utf16Result1 = new CharsRef(10);
            utf16Result2 = new CharsRef(10);
            return true;
        }

        /// <summary>
        /// note: -1 is the empty field: "" !!!! </summary>
        internal static string FieldName(FieldInfos infos, int fieldNumber)
        {
            if (fieldNumber == -1)
            {
                return "";
            }
            else
            {
                return infos.FieldInfo(fieldNumber).Name;
            }
        }

        // Currently used only by assert statement
        private int CompareToLastTerm(int fieldNumber, BytesRef term)
        {
            if (lastFieldNumber != fieldNumber)
            {
                int cmp = FieldName(fieldInfos, lastFieldNumber).CompareToOrdinal(FieldName(fieldInfos, fieldNumber));
                // If there is a field named "" (empty string) then we
                // will get 0 on this comparison, yet, it's "OK".  But
                // it's not OK if two different field numbers map to
                // the same name.
                if (cmp != 0 || lastFieldNumber != -1)
                {
                    return cmp;
                }
            }

            scratchBytes.CopyBytes(term);
            if (Debugging.AssertsEnabled) Debugging.Assert(lastTerm.Offset == 0);
            UnicodeUtil.UTF8toUTF16(lastTerm.Bytes, 0, lastTerm.Length, utf16Result1);

            if (Debugging.AssertsEnabled) Debugging.Assert(scratchBytes.Offset == 0);
            UnicodeUtil.UTF8toUTF16(scratchBytes.Bytes, 0, scratchBytes.Length, utf16Result2);

            int len;
            if (utf16Result1.Length < utf16Result2.Length)
            {
                len = utf16Result1.Length;
            }
            else
            {
                len = utf16Result2.Length;
            }

            for (int i = 0; i < len; i++)
            {
                char ch1 = utf16Result1.Chars[i];
                char ch2 = utf16Result2.Chars[i];
                if (ch1 != ch2)
                {
                    return ch1 - ch2;
                }
            }
            if (utf16Result1.Length == 0 && lastFieldNumber == -1)
            {
                // If there is a field named "" (empty string) with a term text of "" (empty string) then we
                // will get 0 on this comparison, yet, it's "OK".
                return -1;
            }
            return utf16Result1.Length - utf16Result2.Length;
        }

        /// <summary>
        /// Adds a new &lt;&lt; <paramref name="fieldNumber"/>, termBytes&gt;, <see cref="TermInfo"/>&gt; pair to the set.
        /// Term must be lexicographically greater than all previous Terms added.
        /// <see cref="TermInfo"/> pointers must be positive and greater than all previous.
        /// </summary>
        public void Add(int fieldNumber, BytesRef term, TermInfo ti)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(CompareToLastTerm(fieldNumber, term) < 0 || (isIndex && term.Length == 0 && lastTerm.Length == 0),
                    "Terms are out of order: field={0} (number {1}) lastField={2} (number {3}) text={4} lastText={5}",
                    FieldName(fieldInfos, fieldNumber), fieldNumber, FieldName(fieldInfos, lastFieldNumber), lastFieldNumber,
                    // LUCENENET specific - use wrapper BytesRefFormatter struct to defer building the string unless string.Format() is called
                    new BytesRefFormatter(term, BytesRefFormat.UTF8), new BytesRefFormatter(lastTerm, BytesRefFormat.UTF8));

                Debugging.Assert(ti.FreqPointer >= lastTi.FreqPointer, "freqPointer out of order ({0} < {1})", ti.FreqPointer, lastTi.FreqPointer);
                Debugging.Assert(ti.ProxPointer >= lastTi.ProxPointer, "proxPointer out of order ({0} < {1})", ti.ProxPointer, lastTi.ProxPointer);
            }

            if (!isIndex && size % indexInterval == 0)
            {
                other.Add(lastFieldNumber, lastTerm, lastTi); // add an index term
            }
            WriteTerm(fieldNumber, term); // write term

            output.WriteVInt32(ti.DocFreq); // write doc freq
            output.WriteVInt64(ti.FreqPointer - lastTi.FreqPointer); // write pointers
            output.WriteVInt64(ti.ProxPointer - lastTi.ProxPointer);

            if (ti.DocFreq >= skipInterval)
            {
                output.WriteVInt32(ti.SkipOffset);
            }

            if (isIndex)
            {
                output.WriteVInt64(other.output.Position - lastIndexPointer); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                lastIndexPointer = other.output.Position; // write pointer // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }

            lastFieldNumber = fieldNumber;
            lastTi.Set(ti);
            size++;
        }

        private void WriteTerm(int fieldNumber, BytesRef term)
        {
            //System.out.println("  tiw.write field=" + fieldNumber + " term=" + term.utf8ToString());

            // TODO: UTF16toUTF8 could tell us this prefix
            // Compute prefix in common with last term:
            int start = 0;
            int limit = term.Length < lastTerm.Length ? term.Length : lastTerm.Length;
            while (start < limit)
            {
                if (term.Bytes[start + term.Offset] != lastTerm.Bytes[start + lastTerm.Offset])
                {
                    break;
                }
                start++;
            }

            int length = term.Length - start;
            output.WriteVInt32(start); // write shared prefix length
            output.WriteVInt32(length); // write delta length
            output.WriteBytes(term.Bytes, start + term.Offset, length); // write delta bytes
            output.WriteVInt32(fieldNumber); // write field num
            lastTerm.CopyBytes(term);
        }

        /// <summary>
        /// Called to complete TermInfos creation. </summary>
        public void Dispose()
        {
            try
            {
                output.Seek(4); // write size after format
                output.WriteInt64(size);
            }
            finally
            {
                try
                {
                    output.Dispose();
                }
                finally
                {
                    if (!isIndex)
                    {
                        other.Dispose();
                    }
                }
            }
        }
    }
#pragma warning restore 612, 618
}