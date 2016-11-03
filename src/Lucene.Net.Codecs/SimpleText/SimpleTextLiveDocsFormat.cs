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

namespace Lucene.Net.Codecs.SimpleText
{

    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using Support;

	using IndexFileNames = Index.IndexFileNames;
	using SegmentCommitInfo = Index.SegmentCommitInfo;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using Directory = Store.Directory;
	using IOContext = Store.IOContext;
	using IndexOutput = Store.IndexOutput;
	using ArrayUtil = Util.ArrayUtil;
	using Bits = Util.Bits;
	using BytesRef = Util.BytesRef;
	using CharsRef = Util.CharsRef;
	using IOUtils = Util.IOUtils;
	using MutableBits = Util.MutableBits;
	using StringHelper = Util.StringHelper;
	using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// reads/writes plaintext live docs
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextLiveDocsFormat : LiveDocsFormat
    {

        internal const string LIVEDOCS_EXTENSION = "liv";

        internal static readonly BytesRef SIZE = new BytesRef("size ");
        internal static readonly BytesRef DOC = new BytesRef("  doc ");
        internal static readonly BytesRef END = new BytesRef("END");

        public override MutableBits NewLiveDocs(int size)
        {
            return new SimpleTextMutableBits(size);
        }

        public override MutableBits NewLiveDocs(Bits existing)
        {
            var bits = (SimpleTextBits) existing;
            return new SimpleTextMutableBits(new BitArray(bits.BITS), bits.SIZE);
        }

        public override Bits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context)
        {
            Debug.Assert(info.HasDeletions());
            var scratch = new BytesRef();
            var scratchUtf16 = new CharsRef();

            var fileName = IndexFileNames.FileNameFromGeneration(info.Info.Name, LIVEDOCS_EXTENSION, info.DelGen);
            ChecksumIndexInput input = null;
            var success = false;

            try
            {
                input = dir.OpenChecksumInput(fileName, context);

                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SIZE));
                var size = ParseIntAt(scratch, SIZE.Length, scratchUtf16);

                var bits = new BitArray(size);

                SimpleTextUtil.ReadLine(input, scratch);
                while (!scratch.Equals(END))
                {
                    Debug.Assert(StringHelper.StartsWith(scratch, DOC));
                    var docid = ParseIntAt(scratch, DOC.Length, scratchUtf16);
                    bits.SafeSet(docid, true);
                    SimpleTextUtil.ReadLine(input, scratch);
                }

                SimpleTextUtil.CheckFooter(input);

                success = true;
                return new SimpleTextBits(bits, size);
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
            }
        }

        private static int ParseIntAt(BytesRef bytes, int offset, CharsRef scratch)
        {
            UnicodeUtil.UTF8toUTF16(bytes.Bytes, bytes.Offset + offset, bytes.Length - offset, scratch);
            return ArrayUtil.ParseInt(scratch.Chars, 0, scratch.Length);
        }

        public override void WriteLiveDocs(MutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount,
            IOContext context)
        {
            var set = ((SimpleTextBits) bits).BITS;
            var size = bits.Length();
            var scratch = new BytesRef();

            var fileName = IndexFileNames.FileNameFromGeneration(info.Info.Name, LIVEDOCS_EXTENSION, info.NextDelGen);
            IndexOutput output = null;
            var success = false;
            try
            {
                output = dir.CreateOutput(fileName, context);
                SimpleTextUtil.Write(output, SIZE);
                SimpleTextUtil.Write(output, Convert.ToString(size, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(output);

                for (int i = set.NextSetBit(0); i >= 0; i = set.NextSetBit(i + 1))
                {
                    SimpleTextUtil.Write(output, DOC);
                    SimpleTextUtil.Write(output, Convert.ToString(i, CultureInfo.InvariantCulture), scratch);
                    SimpleTextUtil.WriteNewline(output);
                }

                SimpleTextUtil.Write(output, END);
                SimpleTextUtil.WriteNewline(output);
                SimpleTextUtil.WriteChecksum(output, scratch);
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(output);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(output);
                }
            }
        }

        public override void Files(SegmentCommitInfo info, ICollection<string> files)
        {
            if (info.HasDeletions())
            {
                files.Add(IndexFileNames.FileNameFromGeneration(info.Info.Name, LIVEDOCS_EXTENSION, info.DelGen));
            }
        }

        // read-only
        internal class SimpleTextBits : Bits
        {
            internal readonly BitArray BITS;
            internal readonly int SIZE;

            internal SimpleTextBits(BitArray bits, int size)
            {
                BITS = bits;
                SIZE = size;
            }

            public bool Get(int index)
            {
                return BITS.SafeGet(index);
            }

            public int Length()
            {
                return SIZE;
            }
        }

        // read-write
        internal class SimpleTextMutableBits : SimpleTextBits, MutableBits
        {

            internal SimpleTextMutableBits(int size) : this(new BitArray(size), size)
            {
                BITS.Set(0, size);
            }

            internal SimpleTextMutableBits(BitArray bits, int size) : base(bits, size)
            {
            }

            public void Clear(int bit)
            {
                BITS.SafeSet(bit, false);
            }
        }
    }

}