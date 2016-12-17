using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene40
{
    using Bits = Lucene.Net.Util.Bits;

    // javadocs
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

    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOContext = Lucene.Net.Store.IOContext;
    using MutableBits = Lucene.Net.Util.MutableBits;
    using SegmentCommitInfo = Lucene.Net.Index.SegmentCommitInfo;

    /// <summary>
    /// Lucene 4.0 Live Documents Format.
    /// <p>
    /// <p>The .del file is optional, and only exists when a segment contains
    /// deletions.</p>
    /// <p>Although per-segment, this file is maintained exterior to compound segment
    /// files.</p>
    /// <p>Deletions (.del) --&gt; Format,Header,ByteCount,BitCount, Bits | DGaps (depending
    /// on Format)</p>
    /// <ul>
    ///   <li>Format,ByteSize,BitCount --&gt; <seealso cref="DataOutput#writeInt Uint32"/></li>
    ///   <li>Bits --&gt; &lt;<seealso cref="DataOutput#writeByte Byte"/>&gt; <sup>ByteCount</sup></li>
    ///   <li>DGaps --&gt; &lt;DGap,NonOnesByte&gt; <sup>NonzeroBytesCount</sup></li>
    ///   <li>DGap --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>NonOnesByte --&gt; <seealso cref="DataOutput#writeByte Byte"/></li>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    /// </ul>
    /// <p>Format is 1: indicates cleared DGaps.</p>
    /// <p>ByteCount indicates the number of bytes in Bits. It is typically
    /// (SegSize/8)+1.</p>
    /// <p>BitCount indicates the number of bits that are currently set in Bits.</p>
    /// <p>Bits contains one bit for each document indexed. When the bit corresponding
    /// to a document number is cleared, that document is marked as deleted. Bit ordering
    /// is from least to most significant. Thus, if Bits contains two bytes, 0x00 and
    /// 0x02, then document 9 is marked as alive (not deleted).</p>
    /// <p>DGaps represents sparse bit-vectors more efficiently than Bits. It is made
    /// of DGaps on indexes of nonOnes bytes in Bits, and the nonOnes bytes themselves.
    /// The number of nonOnes bytes in Bits (NonOnesBytesCount) is not stored.</p>
    /// <p>For example, if there are 8000 bits and only bits 10,12,32 are cleared, DGaps
    /// would be used:</p>
    /// <p>(VInt) 1 , (byte) 20 , (VInt) 3 , (Byte) 1</p>
    /// </summary>
    public class Lucene40LiveDocsFormat : LiveDocsFormat
    {
        /// <summary>
        /// Extension of deletes </summary>
        internal static readonly string DELETES_EXTENSION = "del";

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40LiveDocsFormat()
        {
        }

        public override MutableBits NewLiveDocs(int size)
        {
            BitVector bitVector = new BitVector(size);
            bitVector.InvertAll();
            return bitVector;
        }

        public override MutableBits NewLiveDocs(Bits existing)
        {
            BitVector liveDocs = (BitVector)existing;
            return (BitVector)liveDocs.Clone();
        }

        public override Bits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context)
        {
            string filename = IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.DelGen);
            BitVector liveDocs = new BitVector(dir, filename, context);
            Debug.Assert(liveDocs.Count() == info.Info.DocCount - info.DelCount, "liveDocs.count()=" + liveDocs.Count() + " info.docCount=" + info.Info.DocCount + " info.getDelCount()=" + info.DelCount);
            Debug.Assert(liveDocs.Length() == info.Info.DocCount);
            return liveDocs;
        }

        public override void WriteLiveDocs(MutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount, IOContext context)
        {
            string filename = IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.NextDelGen);
            BitVector liveDocs = (BitVector)bits;
            Debug.Assert(liveDocs.Count() == info.Info.DocCount - info.DelCount - newDelCount);
            Debug.Assert(liveDocs.Length() == info.Info.DocCount);
            liveDocs.Write(dir, filename, context);
        }

        public override void Files(SegmentCommitInfo info, ICollection<string> files)
        {
            if (info.HasDeletions())
            {
                files.Add(IndexFileNames.FileNameFromGeneration(info.Info.Name, DELETES_EXTENSION, info.DelGen));
            }
        }
    }
}