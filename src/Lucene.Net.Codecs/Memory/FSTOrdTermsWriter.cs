using Lucene.Net.Support;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Codecs.Memory
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

    using BytesRef = Util.BytesRef;
    using FieldInfo = Index.FieldInfo;
    using FST = FST;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOptions = Index.IndexOptions;
    using IndexOutput = Store.IndexOutput;
    using Int32sRef = Util.Int32sRef;
    using IOUtils = Util.IOUtils;
    using PositiveInt32Outputs = Util.Fst.PositiveInt32Outputs;
    using RAMOutputStream = Store.RAMOutputStream;
    using SegmentWriteState = Index.SegmentWriteState;
    using Util = Util.Fst.Util;

    /// <summary>
    /// FST-based term dict, using ord as FST output.
    /// <para/>
    /// The FST holds the mapping between &lt;term, ord&gt;, and 
    /// term's metadata is delta encoded into a single byte block.
    /// <para/>
    /// Typically the byte block consists of four parts:
    /// <list type="number">
    ///     <item><description>term statistics: docFreq, totalTermFreq;</description></item>
    ///     <item><description>monotonic long[], e.g. the pointer to the postings list for that term;</description></item>
    ///     <item><description>generic byte[], e.g. other information customized by postings base.</description></item>
    ///     <item><description>single-level skip list to speed up metadata decoding by ord.</description></item>
    /// </list>
    /// <para/>
    /// <para>
    /// Files:
    /// <list type="bullet">
    ///  <item><description><c>.tix</c>: <a href="#Termindex">Term Index</a></description></item>
    ///  <item><description><c>.tbk</c>: <a href="#Termblock">Term Block</a></description></item>
    /// </list>
    /// </para>
    /// 
    /// <a name="Termindex" id="Termindex"></a>
    /// <h3>Term Index</h3>
    /// <para>
    ///  The .tix contains a list of FSTs, one for each field.
    ///  The FST maps a term to its corresponding order in current field.
    /// </para>
    /// 
    /// <list type="bullet">
    ///  <item><description>TermIndex(.tix) --&gt; Header, TermFST<sup>NumFields</sup>, Footer</description></item>
    ///  <item><description>TermFST --&gt; <see cref="FST{T}"/></description></item>
    ///  <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///  <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/>) </description></item>
    /// </list>
    /// 
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///  <item><description>
    ///  Since terms are already sorted before writing to <a href="#Termblock">Term Block</a>, 
    ///  their ords can directly used to seek term metadata from term block.
    ///  </description></item>
    /// </list>
    /// 
    /// <a name="Termblock" id="Termblock"></a>
    /// <h3>Term Block</h3>
    /// <para>
    /// The .tbk contains all the statistics and metadata for terms, along with field summary (e.g. 
    /// per-field data like number of documents in current field). For each field, there are four blocks:
    /// <list type="bullet">
    ///   <item><description>statistics bytes block: contains term statistics; </description></item>
    ///   <item><description>metadata longs block: delta-encodes monotonic part of metadata; </description></item>
    ///   <item><description>metadata bytes block: encodes other parts of metadata; </description></item>
    ///   <item><description>skip block: contains skip data, to speed up metadata seeking and decoding</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>File Format:</para>
    /// <list type="bullet">
    ///  <item><description>TermBlock(.tbk) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</description></item>
    ///  <item><description>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?, SumDocFreq,
    ///                                         DocCount, LongsSize, DataBlock &gt; <sup>NumFields</sup>, Footer</description></item>
    /// 
    ///  <item><description>DataBlock --&gt; StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength, 
    ///                       SkipBlock, StatsBlock, MetaLongsBlock, MetaBytesBlock </description></item>
    ///  <item><description>SkipBlock --&gt; &lt; StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta, 
    ///                            MetaLongsSkipDelta<sup>LongsSize</sup> &gt;<sup>NumTerms</sup></description></item>
    ///  <item><description>StatsBlock --&gt; &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) ? &gt; <sup>NumTerms</sup></description></item>
    ///  <item><description>MetaLongsBlock --&gt; &lt; LongDelta<sup>LongsSize</sup>, BytesSize &gt; <sup>NumTerms</sup></description></item>
    ///  <item><description>MetaBytesBlock --&gt; Byte <sup>MetaBytesBlockLength</sup></description></item>
    ///  <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///  <item><description>DirOffset --&gt; Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///  <item><description>NumFields, FieldNumber, DocCount, DocFreq, LongsSize, 
    ///        FieldNumber, DocCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///  <item><description>NumTerms, SumTotalTermFreq, SumDocFreq, StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength,
    ///        StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta, MetaLongsSkipStart, TotalTermFreq, 
    ///        LongDelta,--&gt; VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    ///  <item><description>Footer --&gt; CodecFooter (<see cref="CodecUtil.WriteFooter(IndexOutput)"/>) </description></item>
    /// </list>
    /// <para>Notes: </para>
    /// <list type="bullet">
    ///  <item><description>
    ///   The format of PostingsHeader and MetaBytes are customized by the specific postings implementation:
    ///   they contain arbitrary per-file data (such as parameters or versioning information), and per-term data 
    ///   (non-monotonic ones like pulsed postings data).
    ///  </description></item>
    ///  <item><description>
    ///   During initialization the reader will load all the blocks into memory. SkipBlock will be decoded, so that during seek
    ///   term dict can lookup file pointers directly. StatsFPDelta, MetaLongsSkipFPDelta, etc. are file offset
    ///   for every SkipInterval's term. MetaLongsSkipDelta is the difference from previous one, which indicates
    ///   the value of preceding metadata longs for every SkipInterval's term.
    ///  </description></item>
    ///  <item><description>
    ///   DocFreq is the count of documents which contain the term. TotalTermFreq is the total number of occurrences of the term. 
    ///   Usually these two values are the same for long tail terms, therefore one bit is stole from DocFreq to check this case,
    ///   so that encoding of TotalTermFreq may be omitted.
    ///  </description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public class FSTOrdTermsWriter : FieldsConsumer
    {
        internal const string TERMS_INDEX_EXTENSION = "tix";
        internal const string TERMS_BLOCK_EXTENSION = "tbk";
        internal const string TERMS_CODEC_NAME = "FST_ORD_TERMS_DICT";
        public const int TERMS_VERSION_START = 0;
        public const int TERMS_VERSION_CHECKSUM = 1;
        public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;
        public const int SKIP_INTERVAL = 8;

        private readonly PostingsWriterBase postingsWriter;
        //private readonly FieldInfos fieldInfos; // LUCENENET: Never read
        private readonly IList<FieldMetaData> _fields = new JCG.List<FieldMetaData>();
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput blockOut = null;
        private readonly IndexOutput indexOut = null; // LUCENENET: marked readonly
#pragma warning restore CA2213 // Disposable fields should be disposed

        public FSTOrdTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter)
        {
            var termsIndexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_INDEX_EXTENSION);
            var termsBlockFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_BLOCK_EXTENSION);

            this.postingsWriter = postingsWriter;
            //fieldInfos = state.FieldInfos; // LUCENENET: Never read

            var success = false;
            try
            {
                indexOut = state.Directory.CreateOutput(termsIndexFileName, state.Context);
                blockOut = state.Directory.CreateOutput(termsBlockFileName, state.Context);
                WriteHeader(indexOut);
                WriteHeader(blockOut);
                this.postingsWriter.Init(blockOut);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(indexOut, blockOut);
                }
            }
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            return new TermsWriter(this, field);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (blockOut is null) return;

                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                try
                {
                    var blockDirStart = blockOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                    // write field summary
                    blockOut.WriteVInt32(_fields.Count);
                    foreach (var field in _fields)
                    {
                        blockOut.WriteVInt32(field.FieldInfo.Number);
                        blockOut.WriteVInt64(field.NumTerms);
                        if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                        {
                            blockOut.WriteVInt64(field.SumTotalTermFreq);
                        }
                        blockOut.WriteVInt64(field.SumDocFreq);
                        blockOut.WriteVInt32(field.DocCount);
                        blockOut.WriteVInt32(field.Int64sSize);
                        blockOut.WriteVInt64(field.StatsOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        blockOut.WriteVInt64(field.MetaInt64sOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        blockOut.WriteVInt64(field.MetaBytesOut.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                        field.SkipOut.WriteTo(blockOut);
                        field.StatsOut.WriteTo(blockOut);
                        field.MetaInt64sOut.WriteTo(blockOut);
                        field.MetaBytesOut.WriteTo(blockOut);
                        field.Dict.Save(indexOut);
                    }
                    WriteTrailer(blockOut, blockDirStart);
                    CodecUtil.WriteFooter(indexOut);
                    CodecUtil.WriteFooter(blockOut);
                }
                catch (Exception ioe2) when (ioe2.IsIOException())
                {
                    ioe = ioe2;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ioe, blockOut, indexOut, postingsWriter);
                    blockOut = null;
                }
            }
        }

        private static void WriteHeader(IndexOutput @out) // LUCENENET: CA1822: Mark members as static
        {
            CodecUtil.WriteHeader(@out, TERMS_CODEC_NAME, TERMS_VERSION_CURRENT);
        }

        private static void WriteTrailer(IndexOutput output, long dirStart) // LUCENENET: CA1822: Mark members as static
        {
            output.WriteInt64(dirStart);
        }

        private class FieldMetaData
        {
            public FieldInfo FieldInfo { get; set; }
            public long NumTerms { get; set; }
            public long SumTotalTermFreq { get; set; }
            public long SumDocFreq { get; set; }
            public int DocCount { get; set; }
            /// <summary>
            /// NOTE: This was longsSize (field) in Lucene.
            /// </summary>
            public int Int64sSize { get; set; }
            public FST<Int64> Dict { get; set; }

            // TODO: block encode each part 

            // vint encode next skip point (fully decoded when reading)
            public RAMOutputStream SkipOut { get; set; }
            // vint encode df, (ttf-df)
            public RAMOutputStream StatsOut { get; set; }
            // vint encode monotonic long[] and length for corresponding byte[]
            /// <summary>
            /// NOTE: This was metaLongsOut (field) in Lucene.
            /// </summary>
            public RAMOutputStream MetaInt64sOut { get; set; }
            // generic byte[]
            public RAMOutputStream MetaBytesOut { get; set; }
        }

        internal sealed class TermsWriter : TermsConsumer
        {
            private readonly FSTOrdTermsWriter _outerInstance;

            private readonly Builder<Int64> _builder;
            private readonly PositiveInt32Outputs _outputs;
            private readonly FieldInfo _fieldInfo;
            private readonly int _longsSize;
            private long _numTerms;

            private readonly Int32sRef _scratchTerm = new Int32sRef();
            private readonly RAMOutputStream _statsOut = new RAMOutputStream();
            private readonly RAMOutputStream _metaLongsOut = new RAMOutputStream();
            private readonly RAMOutputStream _metaBytesOut = new RAMOutputStream();
            private readonly RAMOutputStream _skipOut = new RAMOutputStream();

            private long _lastBlockStatsFp;
            private long _lastBlockMetaLongsFp;
            private long _lastBlockMetaBytesFp;
            private readonly long[] _lastBlockLongs;

            private readonly long[] _lastLongs;
            private long _lastMetaBytesFp;

            internal TermsWriter(FSTOrdTermsWriter outerInstance, FieldInfo fieldInfo)
            {
                _outerInstance = outerInstance;
                _numTerms = 0;
                _fieldInfo = fieldInfo;
                _longsSize = outerInstance.postingsWriter.SetField(fieldInfo);
                _outputs = PositiveInt32Outputs.Singleton;
                _builder = new Builder<Int64>(FST.INPUT_TYPE.BYTE1, _outputs);

                _lastBlockStatsFp = 0;
                _lastBlockMetaLongsFp = 0;
                _lastBlockMetaBytesFp = 0;
                _lastBlockLongs = new long[_longsSize];

                _lastLongs = new long[_longsSize];
                _lastMetaBytesFp = 0;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                _outerInstance.postingsWriter.StartTerm();
                return _outerInstance.postingsWriter;
            }


            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (_numTerms > 0 && _numTerms%SKIP_INTERVAL == 0)
                {
                    BufferSkip();
                }
                // write term meta data into fst
                var longs = new long[_longsSize];

                long delta = stats.TotalTermFreq - stats.DocFreq;
                if (stats.TotalTermFreq > 0)
                {
                    if (delta == 0)
                    {
                        _statsOut.WriteVInt32(stats.DocFreq << 1 | 1);
                    }
                    else
                    {
                        _statsOut.WriteVInt32(stats.DocFreq << 1 | 0);
                        _statsOut.WriteVInt64(stats.TotalTermFreq - stats.DocFreq);
                    }
                }
                else
                {
                    _statsOut.WriteVInt32(stats.DocFreq);
                }
                var state = _outerInstance.postingsWriter.NewTermState();
                state.DocFreq = stats.DocFreq;
                state.TotalTermFreq = stats.TotalTermFreq;
                _outerInstance.postingsWriter.FinishTerm(state);
                _outerInstance.postingsWriter.EncodeTerm(longs, _metaBytesOut, _fieldInfo, state, true);
                for (var i = 0; i < _longsSize; i++)
                {
                    _metaLongsOut.WriteVInt64(longs[i] - _lastLongs[i]);
                    _lastLongs[i] = longs[i];
                }
                _metaLongsOut.WriteVInt64(_metaBytesOut.Position - _lastMetaBytesFp); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                _builder.Add(Util.ToInt32sRef(text, _scratchTerm), _numTerms);
                _numTerms++;

                _lastMetaBytesFp = _metaBytesOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (_numTerms <= 0) return;

                var metadata = new FieldMetaData
                {
                    FieldInfo = _fieldInfo,
                    NumTerms = _numTerms,
                    SumTotalTermFreq = sumTotalTermFreq,
                    SumDocFreq = sumDocFreq,
                    DocCount = docCount,
                    Int64sSize = _longsSize,
                    SkipOut = _skipOut,
                    StatsOut = _statsOut,
                    MetaInt64sOut = _metaLongsOut,
                    MetaBytesOut = _metaBytesOut,
                    Dict = _builder.Finish()
                };
                _outerInstance._fields.Add(metadata);
            }

            private void BufferSkip()
            {
                _skipOut.WriteVInt64(_statsOut.Position - _lastBlockStatsFp); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                _skipOut.WriteVInt64(_metaLongsOut.Position - _lastBlockMetaLongsFp); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                _skipOut.WriteVInt64(_metaBytesOut.Position - _lastBlockMetaBytesFp); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                for (var i = 0; i < _longsSize; i++)
                {
                    _skipOut.WriteVInt64(_lastLongs[i] - _lastBlockLongs[i]);
                }
                _lastBlockStatsFp = _statsOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                _lastBlockMetaLongsFp = _metaLongsOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                _lastBlockMetaBytesFp = _metaBytesOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                Arrays.Copy(_lastLongs, 0, _lastBlockLongs, 0, _longsSize);
            }
        }
    }
}