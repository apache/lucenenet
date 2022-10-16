using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    using FST = Util.Fst.FST;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOptions = Index.IndexOptions;
    using IndexOutput = Store.IndexOutput;
    using Int32sRef = Util.Int32sRef;
    using IOUtils = Util.IOUtils;
    using RAMOutputStream = Store.RAMOutputStream;
    using SegmentWriteState = Index.SegmentWriteState;
    using Util = Util.Fst.Util;

    /// <summary>
    /// FST-based term dict, using metadata as FST output.
    /// <para/>
    /// The FST directly holds the mapping between &lt;term, metadata&gt;.
    /// <para/>
    /// Term metadata consists of three parts:
    /// <list type="number">
    ///     <item><description>term statistics: docFreq, totalTermFreq;</description></item>
    ///     <item><description>monotonic long[], e.g. the pointer to the postings list for that term;</description></item>
    ///     <item><description>generic byte[], e.g. other information need by postings reader.</description></item>
    /// </list>
    /// <para>
    /// File:
    /// <list type="bullet">
    ///   <item><description><c>.tst</c>: <a href="#Termdictionary">Term Dictionary</a></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 
    /// <a name="Termdictionary" id="Termdictionary"></a>
    /// <h3>Term Dictionary</h3>
    /// </para>
    /// <para>
    ///  The .tst contains a list of FSTs, one for each field.
    ///  The FST maps a term to its corresponding statistics (e.g. docfreq) 
    ///  and metadata (e.g. information for postings list reader like file pointer
    ///  to postings list).
    /// </para>
    /// <para>
    /// Typically the metadata is separated into two parts:
    /// <list type="bullet">
    ///   <item><description>
    ///    Monotonical long array: Some metadata will always be ascending in order
    ///    with the corresponding term. This part is used by FST to share outputs between arcs.
    ///   </description></item>
    ///   <item><description>
    ///    Generic byte array: Used to store non-monotonic metadata.
    ///   </description></item>
    /// </list>
    /// </para>
    /// 
    /// File format:
    /// <list type="bullet">
    ///  <item><description>TermsDict(.tst) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</description></item>
    ///  <item><description>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?, 
    ///                                      SumDocFreq, DocCount, LongsSize, TermFST &gt;<sup>NumFields</sup></description></item>
    ///  <item><description>TermFST TermData</description></item>
    ///  <item><description>TermData --&gt; Flag, BytesSize?, LongDelta<sup>LongsSize</sup>?, Byte<sup>BytesSize</sup>?, 
    ///                      &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) &gt; ? </description></item>
    ///  <item><description>Header --&gt; CodecHeader (<see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/>) </description></item>
    ///  <item><description>DirOffset --&gt; Uint64 (<see cref="Store.DataOutput.WriteInt64(long)"/>) </description></item>
    ///  <item><description>DocFreq, LongsSize, BytesSize, NumFields,
    ///        FieldNumber, DocCount --&gt; VInt (<see cref="Store.DataOutput.WriteVInt32(int)"/>) </description></item>
    ///  <item><description>TotalTermFreq, NumTerms, SumTotalTermFreq, SumDocFreq, LongDelta --&gt; 
    ///        VLong (<see cref="Store.DataOutput.WriteVInt64(long)"/>) </description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///  <item><description>
    ///   The format of PostingsHeader and generic meta bytes are customized by the specific postings implementation:
    ///   they contain arbitrary per-file data (such as parameters or versioning information), and per-term data
    ///   (non-monotonic ones like pulsed postings data).
    ///  </description></item>
    ///  <item><description>
    ///   The format of TermData is determined by FST, typically monotonic metadata will be dense around shallow arcs,
    ///   while in deeper arcs only generic bytes and term statistics exist.
    ///  </description></item>
    ///  <item><description>
    ///   The byte Flag is used to indicate which part of metadata exists on current arc. Specially the monotonic part
    ///   is omitted when it is an array of 0s.
    ///  </description></item>
    ///  <item><description>
    ///   Since LongsSize is per-field fixed, it is only written once in field summary.
    ///  </description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class FSTTermsWriter : FieldsConsumer
    {
        internal const string TERMS_EXTENSION = "tmp";
        internal const string TERMS_CODEC_NAME = "FST_TERMS_DICT";
        public const int TERMS_VERSION_START = 0;
        public const int TERMS_VERSION_CHECKSUM = 1;
        public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;

        private readonly PostingsWriterBase _postingsWriter;
        //private readonly FieldInfos _fieldInfos; // LUCENENET: Never read
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput _output;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly IList<FieldMetaData> _fields = new JCG.List<FieldMetaData>();

        public FSTTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter)
        {
            var termsFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
                TERMS_EXTENSION);

            _postingsWriter = postingsWriter;
            //_fieldInfos = state.FieldInfos; // LUCENENET: Never read
            _output = state.Directory.CreateOutput(termsFileName, state.Context);

            var success = false;
            try
            {
                WriteHeader(_output);
                _postingsWriter.Init(_output);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(_output);
                }
            }
        }

        private static void WriteHeader(IndexOutput output) // LUCENENET: CA1822: Mark members as static
        {
            CodecUtil.WriteHeader(output, TERMS_CODEC_NAME, TERMS_VERSION_CURRENT);
        }

        private static void WriteTrailer(IndexOutput output, long dirStart) // LUCENENET: CA1822: Mark members as static
        {
            output.WriteInt64(dirStart);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            return new TermsWriter(this, field);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_output is null) return;

                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                try
                {
                    // write field summary
                    var dirStart = _output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                    _output.WriteVInt32(_fields.Count);
                    foreach (var field in _fields)
                    {
                        _output.WriteVInt32(field.FieldInfo.Number);
                        _output.WriteVInt64(field.NumTerms);
                        if (field.FieldInfo.IndexOptions != IndexOptions.DOCS_ONLY)
                        {
                            _output.WriteVInt64(field.SumTotalTermFreq);
                        }
                        _output.WriteVInt64(field.SumDocFreq);
                        _output.WriteVInt32(field.DocCount);
                        _output.WriteVInt32(field.Int64sSize);
                        field.Dict.Save(_output);
                    }
                    WriteTrailer(_output, dirStart);
                    CodecUtil.WriteFooter(_output);
                }
                catch (Exception ioe2) when (ioe2.IsIOException())
                {
                    ioe = ioe2;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ioe, _output, _postingsWriter);
                    _output = null;
                }
            }
        }

        private class FieldMetaData
        {
            public FieldInfo FieldInfo { get; private set; }
            public long NumTerms { get; private set; }
            public long SumTotalTermFreq { get; private set; }
            public long SumDocFreq { get; private set; }
            public int DocCount { get; private set; }
            /// <summary>
            /// NOTE: This was longsSize (field) in Lucene.
            /// </summary>
            public int Int64sSize { get; private set; }
            public FST<FSTTermOutputs.TermData> Dict { get; private set; }

            public FieldMetaData(FieldInfo fieldInfo, long numTerms, long sumTotalTermFreq, long sumDocFreq,
                int docCount, int longsSize, FST<FSTTermOutputs.TermData> fst)
            {
                FieldInfo = fieldInfo;
                NumTerms = numTerms;
                SumTotalTermFreq = sumTotalTermFreq;
                SumDocFreq = sumDocFreq;
                DocCount = docCount;
                Int64sSize = longsSize;
                Dict = fst;
            }
        }

        internal sealed class TermsWriter : TermsConsumer
        {
            private readonly FSTTermsWriter _outerInstance;

            private readonly Builder<FSTTermOutputs.TermData> _builder;
            private readonly FSTTermOutputs _outputs;
            private readonly FieldInfo _fieldInfo;
            private readonly int _longsSize;
            private long _numTerms;

            private readonly Int32sRef _scratchTerm = new Int32sRef();
            //private readonly RAMOutputStream _statsWriter = new RAMOutputStream(); // LUCENENET: Never read
            private readonly RAMOutputStream _metaWriter = new RAMOutputStream();

            internal TermsWriter(FSTTermsWriter outerInstance, FieldInfo fieldInfo)
            {
                _outerInstance = outerInstance;
                _numTerms = 0;
                _fieldInfo = fieldInfo;
                _longsSize = outerInstance._postingsWriter.SetField(fieldInfo);
                _outputs = new FSTTermOutputs(fieldInfo, _longsSize);
                _builder = new Builder<FSTTermOutputs.TermData>(FST.INPUT_TYPE.BYTE1, _outputs);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                _outerInstance._postingsWriter.StartTerm();
                return _outerInstance._postingsWriter;
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                // write term meta data into fst

                var state = _outerInstance._postingsWriter.NewTermState();

                var meta = new FSTTermOutputs.TermData
                {
                    longs = new long[_longsSize],
                    bytes = null,
                    docFreq = state.DocFreq = stats.DocFreq,
                    totalTermFreq = state.TotalTermFreq = stats.TotalTermFreq
                };
                _outerInstance._postingsWriter.FinishTerm(state);
                _outerInstance._postingsWriter.EncodeTerm(meta.longs, _metaWriter, _fieldInfo, state, true);
                var bytesSize = (int) _metaWriter.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                if (bytesSize > 0)
                {
                    meta.bytes = new byte[bytesSize];
                    _metaWriter.WriteTo(meta.bytes, 0);
                    _metaWriter.Reset();
                }
                _builder.Add(Util.ToInt32sRef(text, _scratchTerm), meta);
                _numTerms++;
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                // save FST dict
                if (_numTerms <= 0) return;

                var fst = _builder.Finish();
                _outerInstance._fields.Add(new FieldMetaData(_fieldInfo, _numTerms, sumTotalTermFreq, sumDocFreq,
                    docCount, _longsSize, fst));
            }
        }
    }
}