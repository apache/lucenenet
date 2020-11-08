using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene40
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

    // javadocs
    using Directory = Lucene.Net.Store.Directory;
    using IBits = Lucene.Net.Util.IBits;
    using IMutableBits = Lucene.Net.Util.IMutableBits;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOContext = Lucene.Net.Store.IOContext;
    using SegmentCommitInfo = Lucene.Net.Index.SegmentCommitInfo;

    /// <summary>
    /// Lucene 4.0 Live Documents Format.
    /// <para/>
    /// <para>The .del file is optional, and only exists when a segment contains
    /// deletions.</para>
    /// <para>Although per-segment, this file is maintained exterior to compound segment
    /// files.</para>
    /// <para>Deletions (.del) --&gt; Format,Header,ByteCount,BitCount, Bits | DGaps (depending
    /// on Format)</para>
    /// <list type="bullet">
    ///   <item><description>Format,ByteSize,BitCount --&gt; Uint32 (<see cref="Store.DataOutput.WriteInt32(int)"/>) </description></item>
    ///   <item><description>Bits --&gt; &lt; Byte (<see cref="Store.DataOutput.WriteByte(byte)"/>) &gt; <sup>ByteCount</sup></description></item>
    ///   <item><description>DGaps --&gt; &lt;DGap,NonOnesByte&gt; <sup>NonzeroBytesCount</sup></description></item>
    ///   <item><description>DGap --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///   <item><description>NonOnesByte --&gt;  Byte(<see cref="Store.DataOutput.WriteByte(byte)"/>) </description></item>
    ///   <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    /// </list>
    /// <para>Format is 1: indicates cleared DGaps.</para>
    /// <para>ByteCount indicates the number of bytes in Bits. It is typically
    /// (SegSize/8)+1.</para>
    /// <para>BitCount indicates the number of bits that are currently set in Bits.</para>
    /// <para>Bits contains one bit for each document indexed. When the bit corresponding
    /// to a document number is cleared, that document is marked as deleted. Bit ordering
    /// is from least to most significant. Thus, if Bits contains two bytes, 0x00 and
    /// 0x02, then document 9 is marked as alive (not deleted).</para>
    /// <para>DGaps represents sparse bit-vectors more efficiently than Bits. It is made
    /// of DGaps on indexes of nonOnes bytes in Bits, and the nonOnes bytes themselves.
    /// The number of nonOnes bytes in Bits (NonOnesBytesCount) is not stored.</para>
    /// <para>For example, if there are 8000 bits and only bits 10,12,32 are cleared, DGaps
    /// would be used:</para>
    /// <para>(VInt) 1 , (byte) 20 , (VInt) 3 , (Byte) 1</para>
    /// </summary>
    public class Lucene40LiveDocsFormat : LiveDocsFormat
    {
        /// <summary>
        /// Extension of deletes </summary>
        internal const string DELETES_EXTENSION = "del";

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40LiveDocsFormat()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override IMutableBits NewLiveDocs(int size)
        {
            BitVector bitVector = new BitVector(size);
            bitVector.InvertAll();
            return bitVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override IMutableBits NewLiveDocs(IBits existing)
        {
            BitVector liveDocs = (BitVector)existing;
            return (BitVector)liveDocs.Clone();
        }

        public override IBits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context)
        {
            string filename = IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.DelGen);
            BitVector liveDocs = new BitVector(dir, filename, context);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(liveDocs.Count() == info.Info.DocCount - info.DelCount, "liveDocs.Count()={0} info.DocCount={1} info.DelCount={2}", liveDocs.Count(), info.Info.DocCount, info.DelCount);
                Debugging.Assert(liveDocs.Length == info.Info.DocCount);
            }
            return liveDocs;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void WriteLiveDocs(IMutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount, IOContext context)
        {
            string filename = IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.NextDelGen);
            BitVector liveDocs = (BitVector)bits;
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(liveDocs.Count() == info.Info.DocCount - info.DelCount - newDelCount);
                Debugging.Assert(liveDocs.Length == info.Info.DocCount);
            }
            liveDocs.Write(dir, filename, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Files(SegmentCommitInfo info, ICollection<string> files)
        {
            if (info.HasDeletions)
            {
                files.Add(IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.DelGen));
            }
        }
    }
}