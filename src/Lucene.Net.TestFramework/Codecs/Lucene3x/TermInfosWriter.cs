using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Codecs.Lucene3x
{
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharsRef = Lucene.Net.Util.CharsRef;
    using Directory = Lucene.Net.Store.Directory;

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

    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// this stores a monotonically increasing set of <Term, TermInfo> pairs in a
    ///  Directory.  A TermInfos can be written once, in order.
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

        private FieldInfos FieldInfos;
        private IndexOutput Output;
        private TermInfo LastTi = new TermInfo();
        private long Size;

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
        internal int IndexInterval = 128;

        /// <summary>
        /// Expert: The fraction of term entries stored in skip tables,
        /// used to accelerate skipping.  Larger values result in
        /// smaller indexes, greater acceleration, but fewer accelerable cases, while
        /// smaller values result in bigger indexes, less acceleration and more
        /// accelerable cases. More detailed experiments would be useful here.
        /// </summary>
        internal int SkipInterval = 16;

        /// <summary>
        /// Expert: The maximum number of skip levels. Smaller values result in
        /// slightly smaller indexes, but slower skipping in big posting lists.
        /// </summary>
        internal int MaxSkipLevels = 10;

        private long LastIndexPointer;
        private bool IsIndex;
        private readonly BytesRef LastTerm = new BytesRef();
        private int LastFieldNumber = -1;

        private TermInfosWriter Other;

        internal TermInfosWriter(Directory directory, string segment, FieldInfos fis, int interval)
        {
            Initialize(directory, segment, fis, interval, false);
            bool success = false;
            try
            {
                Other = new TermInfosWriter(directory, segment, fis, interval, true);
                Other.Other = this;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(Output);

                    try
                    {
                        directory.DeleteFile(IndexFileNames.SegmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
                    }
#pragma warning disable 168
                    catch (IOException ignored)
#pragma warning restore 168
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
            IndexInterval = interval;
            FieldInfos = fis;
            IsIndex = isi;
            Output = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)), IOContext.DEFAULT);
            bool success = false;
            try
            {
                Output.WriteInt32(FORMAT_CURRENT); // write format
                Output.WriteInt64(0); // leave space for size
                Output.WriteInt32(IndexInterval); // write indexInterval
                Output.WriteInt32(SkipInterval); // write skipInterval
                Output.WriteInt32(MaxSkipLevels); // write maxSkipLevels
                Debug.Assert(InitUTF16Results());
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(Output);

                    try
                    {
                        directory.DeleteFile(IndexFileNames.SegmentFileName(segment, "", (IsIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION)));
                    }
#pragma warning disable 168
                    catch (IOException ignored)
#pragma warning restore 168
                    {
                    }
                }
            }
        }

        // Currently used only by assert statements
        internal CharsRef Utf16Result1;

        internal CharsRef Utf16Result2;
        private readonly BytesRef ScratchBytes = new BytesRef();

        // Currently used only by assert statements
        private bool InitUTF16Results()
        {
            Utf16Result1 = new CharsRef(10);
            Utf16Result2 = new CharsRef(10);
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
            if (LastFieldNumber != fieldNumber)
            {
                int cmp = FieldName(FieldInfos, LastFieldNumber).CompareToOrdinal(FieldName(FieldInfos, fieldNumber));
                // If there is a field named "" (empty string) then we
                // will get 0 on this comparison, yet, it's "OK".  But
                // it's not OK if two different field numbers map to
                // the same name.
                if (cmp != 0 || LastFieldNumber != -1)
                {
                    return cmp;
                }
            }

            ScratchBytes.CopyBytes(term);
            Debug.Assert(LastTerm.Offset == 0);
            UnicodeUtil.UTF8toUTF16(LastTerm.Bytes, 0, LastTerm.Length, Utf16Result1);

            Debug.Assert(ScratchBytes.Offset == 0);
            UnicodeUtil.UTF8toUTF16(ScratchBytes.Bytes, 0, ScratchBytes.Length, Utf16Result2);

            int len;
            if (Utf16Result1.Length < Utf16Result2.Length)
            {
                len = Utf16Result1.Length;
            }
            else
            {
                len = Utf16Result2.Length;
            }

            for (int i = 0; i < len; i++)
            {
                char ch1 = Utf16Result1.Chars[i];
                char ch2 = Utf16Result2.Chars[i];
                if (ch1 != ch2)
                {
                    return ch1 - ch2;
                }
            }
            if (Utf16Result1.Length == 0 && LastFieldNumber == -1)
            {
                // If there is a field named "" (empty string) with a term text of "" (empty string) then we
                // will get 0 on this comparison, yet, it's "OK".
                return -1;
            }
            return Utf16Result1.Length - Utf16Result2.Length;
        }

        /// <summary>
        /// Adds a new <<fieldNumber, termBytes>, TermInfo> pair to the set.
        ///  Term must be lexicographically greater than all previous Terms added.
        ///  TermInfo pointers must be positive and greater than all previous.
        /// </summary>
        public void Add(int fieldNumber, BytesRef term, TermInfo ti)
        {
            Debug.Assert(CompareToLastTerm(fieldNumber, term) < 0 || (IsIndex && term.Length == 0 && LastTerm.Length == 0), "Terms are out of order: field=" + FieldName(FieldInfos, fieldNumber) + " (number " + fieldNumber + ")" + " lastField=" + FieldName(FieldInfos, LastFieldNumber) + " (number " + LastFieldNumber + ")" + " text=" + term.Utf8ToString() + " lastText=" + LastTerm.Utf8ToString());

            Debug.Assert(ti.FreqPointer >= LastTi.FreqPointer, "freqPointer out of order (" + ti.FreqPointer + " < " + LastTi.FreqPointer + ")");
            Debug.Assert(ti.ProxPointer >= LastTi.ProxPointer, "proxPointer out of order (" + ti.ProxPointer + " < " + LastTi.ProxPointer + ")");

            if (!IsIndex && Size % IndexInterval == 0)
            {
                Other.Add(LastFieldNumber, LastTerm, LastTi); // add an index term
            }
            WriteTerm(fieldNumber, term); // write term

            Output.WriteVInt32(ti.DocFreq); // write doc freq
            Output.WriteVInt64(ti.FreqPointer - LastTi.FreqPointer); // write pointers
            Output.WriteVInt64(ti.ProxPointer - LastTi.ProxPointer);

            if (ti.DocFreq >= SkipInterval)
            {
                Output.WriteVInt32(ti.SkipOffset);
            }

            if (IsIndex)
            {
                Output.WriteVInt64(Other.Output.GetFilePointer() - LastIndexPointer);
                LastIndexPointer = Other.Output.GetFilePointer(); // write pointer
            }

            LastFieldNumber = fieldNumber;
            LastTi.Set(ti);
            Size++;
        }

        private void WriteTerm(int fieldNumber, BytesRef term)
        {
            //System.out.println("  tiw.write field=" + fieldNumber + " term=" + term.utf8ToString());

            // TODO: UTF16toUTF8 could tell us this prefix
            // Compute prefix in common with last term:
            int start = 0;
            int limit = term.Length < LastTerm.Length ? term.Length : LastTerm.Length;
            while (start < limit)
            {
                if (term.Bytes[start + term.Offset] != LastTerm.Bytes[start + LastTerm.Offset])
                {
                    break;
                }
                start++;
            }

            int length = term.Length - start;
            Output.WriteVInt32(start); // write shared prefix length
            Output.WriteVInt32(length); // write delta length
            Output.WriteBytes(term.Bytes, start + term.Offset, length); // write delta bytes
            Output.WriteVInt32(fieldNumber); // write field num
            LastTerm.CopyBytes(term);
        }

        /// <summary>
        /// Called to complete TermInfos creation. </summary>
        public void Dispose()
        {
            try
            {
                Output.Seek(4); // write size after format
                Output.WriteInt64(Size);
            }
            finally
            {
                try
                {
                    Output.Dispose();
                }
                finally
                {
                    if (!IsIndex)
                    {
                        Other.Dispose();
                    }
                }
            }
        }
    }
#pragma warning restore 612, 618
}