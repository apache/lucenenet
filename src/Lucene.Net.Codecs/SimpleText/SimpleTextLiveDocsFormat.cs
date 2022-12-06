using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using BitSet = Lucene.Net.Util.OpenBitSet;

namespace Lucene.Net.Codecs.SimpleText
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

    using ArrayUtil = Util.ArrayUtil;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using Directory = Store.Directory;
    using IBits = Util.IBits;
    using IMutableBits = Util.IMutableBits;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using SegmentCommitInfo = Index.SegmentCommitInfo;
    using StringHelper = Util.StringHelper;
    using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// Reads/writes plain text live docs.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextLiveDocsFormat : LiveDocsFormat
    {
        internal const string LIVEDOCS_EXTENSION = "liv";

        internal static readonly BytesRef SIZE = new BytesRef("size ");
        internal static readonly BytesRef DOC = new BytesRef("  doc ");
        internal static readonly BytesRef END = new BytesRef("END");

        public override IMutableBits NewLiveDocs(int size)
        {
            return new SimpleTextMutableBits(size);
        }

        public override IMutableBits NewLiveDocs(IBits existing)
        {
            var bits = (SimpleTextBits) existing;
            return new SimpleTextMutableBits((BitSet)bits.bits.Clone(), bits.Length);
        }

        public override IBits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(info.HasDeletions);
            var scratch = new BytesRef();
            var scratchUtf16 = new CharsRef();

            var fileName = IndexFileNames.FileNameFromGeneration(info.Info.Name, LIVEDOCS_EXTENSION, info.DelGen);
            ChecksumIndexInput input = null;
            var success = false;

            try
            {
                input = dir.OpenChecksumInput(fileName, context);

                SimpleTextUtil.ReadLine(input, scratch);
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SIZE));
                var size = ParseInt32At(scratch, SIZE.Length, scratchUtf16);

                var bits = new BitSet(size);

                SimpleTextUtil.ReadLine(input, scratch);
                while (!scratch.Equals(END))
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, DOC));
                    var docid = ParseInt32At(scratch, DOC.Length, scratchUtf16);
                    bits.Set(docid);
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
                    IOUtils.Dispose(input);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
            }
        }

        /// <summary>
        /// NOTE: This was parseIntAt() in Lucene.
        /// </summary>
        private static int ParseInt32At(BytesRef bytes, int offset, CharsRef scratch) // LUCENENET: CA1822: Mark members as static
        {
            UnicodeUtil.UTF8toUTF16(bytes.Bytes, bytes.Offset + offset, bytes.Length - offset, scratch);
            return ArrayUtil.ParseInt32(scratch.Chars, 0, scratch.Length);
        }

        public override void WriteLiveDocs(IMutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount,
            IOContext context)
        {
            var set = ((SimpleTextBits) bits).bits;
            var size = bits.Length;
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
                    IOUtils.Dispose(output);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(output);
                }
            }
        }

        public override void Files(SegmentCommitInfo info, ICollection<string> files)
        {
            if (info.HasDeletions)
            {
                files.Add(IndexFileNames.FileNameFromGeneration(info.Info.Name, LIVEDOCS_EXTENSION, info.DelGen));
            }
        }

        // read-only
        internal class SimpleTextBits : IBits
        {
            internal readonly BitSet bits;
            private readonly int size;

            internal SimpleTextBits(BitSet bits, int size)
            {
                this.bits = bits;
                this.size = size;
            }

            public virtual bool Get(int index)
            {
                return bits.Get(index);
            }

            public virtual int Length => size;
        }

        // read-write
        internal class SimpleTextMutableBits : SimpleTextBits, IMutableBits
        {

            internal SimpleTextMutableBits(int size) 
                : this(new BitSet(size), size)
            {
                bits.Set(0, size);
            }

            internal SimpleTextMutableBits(BitSet bits, int size) 
                : base(bits, size)
            {
            }

            public virtual void Clear(int bit)
            {
                bits.Clear(bit);
            }
        }
    }
}