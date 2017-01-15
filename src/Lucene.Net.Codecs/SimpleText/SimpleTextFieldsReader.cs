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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Support;
    using Util.Fst;
    
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using FieldInfo = Index.FieldInfo;
    using IndexOptions = Index.IndexOptions;
    using FieldInfos = Index.FieldInfos;
    using SegmentReadState = Index.SegmentReadState;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using IndexInput = Store.IndexInput;
    using ArrayUtil = Util.ArrayUtil;
    using IBits = Util.IBits;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using FixedBitSet = Util.FixedBitSet;
    using IOUtils = Util.IOUtils;
    using IntsRef = Util.IntsRef;
    using StringHelper = Util.StringHelper;
    using UnicodeUtil = Util.UnicodeUtil;
    using BytesRefFSTEnum = Util.Fst.BytesRefFSTEnum<Util.Fst.PairOutputs<long,long>.Pair>;
    using FST = Util.Fst.FST;
    using PairOutputs = Util.Fst.PairOutputs<long,long>;
    using PositiveIntOutputs = Util.Fst.PositiveIntOutputs;
    using Util = Util.Fst.Util;

    internal class SimpleTextFieldsReader : FieldsProducer
    {
        private readonly SortedDictionary<string, long?> _fields;
        private readonly IndexInput _input;
        private readonly FieldInfos _fieldInfos;
        private readonly int _maxDoc;
        private readonly IDictionary<string, SimpleTextTerms> _termsCache = new Dictionary<string, SimpleTextTerms>();

        public SimpleTextFieldsReader(SegmentReadState state)
        {
            _maxDoc = state.SegmentInfo.DocCount;
            _fieldInfos = state.FieldInfos;
            _input =
                state.Directory.OpenInput(
                    SimpleTextPostingsFormat.GetPostingsFileName(state.SegmentInfo.Name, state.SegmentSuffix),
                    state.Context);
            bool success = false;
            try
            {
                _fields = ReadFields((IndexInput)_input.Clone());
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException();
                }
            }
        }

        private SortedDictionary<string, long?> ReadFields(IndexInput @in)
        {
            ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
            var scratch = new BytesRef(10);
            var fields = new SortedDictionary<string, long?>();

            while (true)
            {
                SimpleTextUtil.ReadLine(input, scratch);
                if (scratch.Equals(SimpleTextFieldsWriter.END))
                {
                    SimpleTextUtil.CheckFooter(input);
                    return fields;
                }
                
                if (StringHelper.StartsWith(scratch, SimpleTextFieldsWriter.FIELD))
                {
                    var fieldName = Encoding.UTF8.GetString(scratch.Bytes, scratch.Offset + SimpleTextFieldsWriter.FIELD.Length,
                        scratch.Length - SimpleTextFieldsWriter.FIELD.Length);
                    fields[fieldName] = input.FilePointer;
                }
            }
        }

        private class SimpleTextTermsEnum : TermsEnum
        {
            private readonly SimpleTextFieldsReader _outerInstance;

            private readonly IndexOptions _indexOptions;
            private int _docFreq;
            private long _totalTermFreq;
            private long _docsStart;
            
            private readonly BytesRefFSTEnum<PairOutputs<long?, PairOutputs<long?,long?>.Pair>.Pair> _fstEnum;

            public SimpleTextTermsEnum(SimpleTextFieldsReader outerInstance,
                FST<PairOutputs<long?, PairOutputs<long?,long?>.Pair>.Pair> fst, IndexOptions indexOptions)
            {
                _outerInstance = outerInstance;
                _indexOptions = indexOptions;
                _fstEnum = new BytesRefFSTEnum<PairOutputs<long?, PairOutputs<long?,long?>.Pair>.Pair>(fst);
            }

            public override bool SeekExact(BytesRef text)
            {

                var result = _fstEnum.SeekExact(text);
                
                if (result == null) return false;
                
                var pair1 = result.Output;
                var pair2 = pair1.Output2;
                _docsStart = pair1.Output1.Value;
                _docFreq = (int) pair2.Output1;
                _totalTermFreq = pair2.Output2.Value;
                return true;
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                var result = _fstEnum.SeekCeil(text);
                if (result == null)
                    return SeekStatus.END;

                var pair1 = result.Output;
                var pair2 = pair1.Output2;
                _docsStart = pair1.Output1.Value;
                _docFreq = (int) pair2.Output1;
                _totalTermFreq = pair2.Output2.Value;

                return result.Input.Equals(text) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;

            }

            public override BytesRef Next()
            {
                var result = _fstEnum.Next();

                if (result == null) return null;

                var pair1 = result.Output;
                var pair2 = pair1.Output2;
                _docsStart = pair1.Output1.Value;
                _docFreq = (int)pair2.Output1;
                _totalTermFreq = pair2.Output2.Value;
                return result.Input;
            }

            public override BytesRef Term
            {
                get { return _fstEnum.Current.Input; }
            }

            public override long Ord
            {
                get { throw new NotSupportedException(); }
            }

            public override void SeekExact(long ord)
            {
                throw new NotSupportedException();
            }

            public override int DocFreq
            {
                get { return _docFreq; }
            }

            public override long TotalTermFreq
            {
                get { return _indexOptions == IndexOptions.DOCS_ONLY ? -1 : _totalTermFreq; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                SimpleTextDocsEnum docsEnum;
                if (reuse != null && reuse is SimpleTextDocsEnum && ((SimpleTextDocsEnum) reuse).CanReuse(_outerInstance._input))
                {
                    docsEnum = (SimpleTextDocsEnum) reuse;
                }
                else
                {
                    docsEnum = new SimpleTextDocsEnum(_outerInstance);
                }
                return docsEnum.Reset(_docsStart, liveDocs, _indexOptions == IndexOptions.DOCS_ONLY,
                    _docFreq);
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {

                if (_indexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) < 0)
                {
                    // Positions were not indexed
                    return null;
                }

                SimpleTextDocsAndPositionsEnum docsAndPositionsEnum;
                if (reuse != null && reuse is SimpleTextDocsAndPositionsEnum && ((SimpleTextDocsAndPositionsEnum) reuse).CanReuse(_outerInstance._input))
                {
                    docsAndPositionsEnum = (SimpleTextDocsAndPositionsEnum) reuse;
                }
                else
                {
                    docsAndPositionsEnum = new SimpleTextDocsAndPositionsEnum(_outerInstance);
                }
                return docsAndPositionsEnum.Reset(_docsStart, liveDocs, _indexOptions, _docFreq);
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }
        }

        private sealed class SimpleTextDocsEnum : DocsEnum
        {
            private readonly IndexInput _inStart;
            private readonly IndexInput _in;
            private bool _omitTf;
            private int _docId = -1;
            private int _tf;
            private IBits _liveDocs;
            private readonly BytesRef _scratch = new BytesRef(10);
            private readonly CharsRef _scratchUtf16 = new CharsRef(10);
            private int _cost;

            public SimpleTextDocsEnum(SimpleTextFieldsReader outerInstance)
            {
                _inStart = outerInstance._input;
                _in = (IndexInput) _inStart.Clone();
            }

            public bool CanReuse(IndexInput @in)
            {
                return @in == _inStart;
            }

            public SimpleTextDocsEnum Reset(long fp, IBits liveDocs, bool omitTf, int docFreq)
            {
                _liveDocs = liveDocs;
                _in.Seek(fp);
                _omitTf = omitTf;
                _docId = -1;
                _tf = 1;
                _cost = docFreq;
                return this;
            }

            public override int DocID
            {
                get { return _docId; }
            }

            public override int Freq
            {
                get { return _tf; }
            }

            public override int NextDoc()
            {
                if (_docId == NO_MORE_DOCS)
                {
                    return _docId;
                }
                bool first = true;
                int termFreq = 0;
                while (true)
                {
                    long lineStart = _in.FilePointer;
                    SimpleTextUtil.ReadLine(_in, _scratch);
                    if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.DOC))
                    {
                        if (!first && (_liveDocs == null || _liveDocs.Get(_docId)))
                        {
                            _in.Seek(lineStart);
                            if (!_omitTf)
                            {
                                _tf = termFreq;
                            }
                            return _docId;
                        }
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.DOC.Length, _scratch.Length - SimpleTextFieldsWriter.DOC.Length,
                            _scratchUtf16);
                        _docId = ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                        termFreq = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.FREQ.Length,
                            _scratch.Length - SimpleTextFieldsWriter.FREQ.Length, _scratchUtf16);
                        termFreq = ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.POS))
                    {
                        // skip termFreq++;
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        Debug.Assert(
                            StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.TERM) || StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FIELD) ||
                            StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.END), "scratch=" + _scratch.Utf8ToString());
                        if (!first && (_liveDocs == null || _liveDocs.Get(_docId)))
                        {
                            _in.Seek(lineStart);
                            if (!_omitTf)
                            {
                                _tf = termFreq;
                            }
                            return _docId;
                        }
                        return _docId = NO_MORE_DOCS;
                    }
                }
            }

            public override int Advance(int target)
            {
                // Naive -- better to index skip data
                return SlowAdvance(target);
            }

            public override long GetCost()
            {
                return _cost;
            }
        }

        private sealed class SimpleTextDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private readonly IndexInput _inStart;
            private readonly IndexInput _in;
            private int _docId = -1;
            private int _tf;
            private IBits _liveDocs;
            private readonly BytesRef _scratch = new BytesRef(10);
            private readonly BytesRef _scratch2 = new BytesRef(10);
            private readonly CharsRef _scratchUtf16 = new CharsRef(10);
            private readonly CharsRef _scratchUtf162 = new CharsRef(10);
            private BytesRef _payload;
            private long _nextDocStart;
            private bool _readOffsets;
            private bool _readPositions;
            private int _startOffset;
            private int _endOffset;
            private int _cost;

            public SimpleTextDocsAndPositionsEnum(SimpleTextFieldsReader outerInstance)
            {
                _inStart = outerInstance._input;
                _in = (IndexInput) _inStart.Clone();
            }

            public bool CanReuse(IndexInput @in)
            {
                return @in == _inStart;
            }

            public SimpleTextDocsAndPositionsEnum Reset(long fp, IBits liveDocs, IndexOptions indexOptions, int docFreq)
            {
                _liveDocs = liveDocs;
                _nextDocStart = fp;
                _docId = -1;
                _readPositions = indexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                _readOffsets = indexOptions.CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;

                if (!_readOffsets)
                {
                    _startOffset = -1;
                    _endOffset = -1;
                }
                _cost = docFreq;
                return this;
            }

            public override int DocID
            {
                get { return _docId; }
            }

            public override int Freq
            {
                get { return _tf; }
            }

            public override int NextDoc()
            {
                bool first = true;
                _in.Seek(_nextDocStart);
                long posStart = 0;
                while (true)
                {
                    long lineStart = _in.FilePointer;
                    SimpleTextUtil.ReadLine(_in, _scratch);
                    //System.out.println("NEXT DOC: " + scratch.utf8ToString());
                    if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.DOC))
                    {
                        if (!first && (_liveDocs == null || _liveDocs.Get(_docId)))
                        {
                            _nextDocStart = lineStart;
                            _in.Seek(posStart);
                            return _docId;
                        }
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.DOC.Length, _scratch.Length - SimpleTextFieldsWriter.DOC.Length,
                            _scratchUtf16);
                        _docId = ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                        _tf = 0;
                        first = false;
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.FREQ.Length,
                            _scratch.Length - SimpleTextFieldsWriter.FREQ.Length, _scratchUtf16);
                        _tf = ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                        posStart = _in.FilePointer;
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.POS))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.START_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.END_OFFSET))
                    {
                        // skip
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.PAYLOAD))
                    {
                        // skip
                    }
                    else
                    {
                        Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.TERM) || StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FIELD) ||
                                     StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.END));

                        if (!first && (_liveDocs == null || _liveDocs.Get(_docId)))
                        {
                            _nextDocStart = lineStart;
                            _in.Seek(posStart);
                            return _docId;
                        }
                        return _docId = NO_MORE_DOCS;
                    }
                }
            }

            public override int Advance(int target)
            {
                // Naive -- better to index skip data
                return SlowAdvance(target);
            }

            public override int NextPosition()
            {
                int pos;
                if (_readPositions)
                {
                    SimpleTextUtil.ReadLine(_in, _scratch);
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.POS), "got line=" + _scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.POS.Length, _scratch.Length - SimpleTextFieldsWriter.POS.Length,
                        _scratchUtf162);
                    pos = ArrayUtil.ParseInt(_scratchUtf162.Chars, 0, _scratchUtf162.Length);
                }
                else
                {
                    pos = -1;
                }

                if (_readOffsets)
                {
                    SimpleTextUtil.ReadLine(_in, _scratch);
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.START_OFFSET), "got line=" + _scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.START_OFFSET.Length,
                        _scratch.Length - SimpleTextFieldsWriter.START_OFFSET.Length, _scratchUtf162);
                    _startOffset = ArrayUtil.ParseInt(_scratchUtf162.Chars, 0, _scratchUtf162.Length);
                    SimpleTextUtil.ReadLine(_in, _scratch);
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.END_OFFSET), "got line=" + _scratch.Utf8ToString());
                    UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.END_OFFSET.Length,
                        _scratch.Length - SimpleTextFieldsWriter.END_OFFSET.Length, _scratchUtf162);
                    _endOffset = ArrayUtil.ParseInt(_scratchUtf162.Chars, 0, _scratchUtf162.Length);
                }

                long fp = _in.FilePointer;
                SimpleTextUtil.ReadLine(_in, _scratch);
                if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.PAYLOAD))
                {
                    int len = _scratch.Length - SimpleTextFieldsWriter.PAYLOAD.Length;
                    if (_scratch2.Bytes.Length < len)
                    {
                        _scratch2.Grow(len);
                    }
                    Array.Copy(_scratch.Bytes, SimpleTextFieldsWriter.PAYLOAD.Length, _scratch2.Bytes, 0, len);
                    _scratch2.Length = len;
                    _payload = _scratch2;
                }
                else
                {
                    _payload = null;
                    _in.Seek(fp);
                }
                return pos;
            }

            public override int StartOffset
            {
                get { return _startOffset; }
            }

            public override int EndOffset
            {
                get { return _endOffset; }
            }

            public override BytesRef Payload
            {
                get { return _payload; }
            }

            public override long GetCost()
            {
                return _cost;
            }
        }

        internal class TermData
        {
            public long DocsStart { get; set; }
            public int DocFreq { get; set; }

            public TermData(long docsStart, int docFreq)
            {
                DocsStart = docsStart;
                DocFreq = docFreq;
            }
        }

        private sealed class SimpleTextTerms : Terms
        {
            private readonly SimpleTextFieldsReader _outerInstance;

            private readonly long _termsStart;
            private readonly FieldInfo _fieldInfo;
            private readonly int _maxDoc;
            private long _sumTotalTermFreq;
            private long _sumDocFreq;
            private int _docCount;
            private FST<PairOutputs<long?, PairOutputs<long?,long?>.Pair>.Pair> _fst;
            private int _termCount;
            private readonly BytesRef _scratch = new BytesRef(10);
            private readonly CharsRef _scratchUtf16 = new CharsRef(10);

            public SimpleTextTerms(SimpleTextFieldsReader outerInstance, string field, long termsStart, int maxDoc)
            {
                _outerInstance = outerInstance;
                _maxDoc = maxDoc;
                _termsStart = termsStart;
                _fieldInfo = outerInstance._fieldInfos.FieldInfo(field);
                LoadTerms();
            }

            private void LoadTerms()
            {
                var posIntOutputs = PositiveIntOutputs.Singleton;
                var outputsInner = new PairOutputs<long?, long?>(posIntOutputs, posIntOutputs);
                var outputs = new PairOutputs<long?, PairOutputs<long?,long?>.Pair>(posIntOutputs, outputsInner);
                
                // honestly, wtf kind of generic mess is this.
                var b = new Builder<PairOutputs<long?, PairOutputs<long?,long?>.Pair>.Pair>(FST.INPUT_TYPE.BYTE1, outputs);
                var input = (IndexInput) _outerInstance._input.Clone();
                input.Seek(_termsStart);

                var lastTerm = new BytesRef(10);
                long lastDocsStart = -1;
                int docFreq = 0;
                long totalTermFreq = 0;
                var visitedDocs = new FixedBitSet(_maxDoc);

                var scratchIntsRef = new IntsRef();
                while (true)
                {
                    SimpleTextUtil.ReadLine(input, _scratch);
                    if (_scratch.Equals(SimpleTextFieldsWriter.END) || StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FIELD))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToIntsRef(lastTerm, scratchIntsRef),
                                outputs.NewPair(lastDocsStart, outputsInner.NewPair(docFreq, totalTermFreq)));
                            _sumTotalTermFreq += totalTermFreq;
                        }
                        break;
                    }
                    
                    if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.DOC))
                    {
                        docFreq++;
                        _sumDocFreq++;
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.DOC.Length, _scratch.Length - SimpleTextFieldsWriter.DOC.Length,
                            _scratchUtf16);
                        int docId = ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                        visitedDocs.Set(docId);
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.FREQ))
                    {
                        UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextFieldsWriter.FREQ.Length,
                            _scratch.Length - SimpleTextFieldsWriter.FREQ.Length, _scratchUtf16);
                        totalTermFreq += ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
                    }
                    else if (StringHelper.StartsWith(_scratch, SimpleTextFieldsWriter.TERM))
                    {
                        if (lastDocsStart != -1)
                        {
                            b.Add(Util.ToIntsRef(lastTerm, scratchIntsRef),
                                outputs.NewPair(lastDocsStart, outputsInner.NewPair(docFreq, totalTermFreq)));
                        }
                        lastDocsStart = input.FilePointer;
                        int len = _scratch.Length - SimpleTextFieldsWriter.TERM.Length;
                        if (len > lastTerm.Length)
                        {
                            lastTerm.Grow(len);
                        }
                        Array.Copy(_scratch.Bytes, SimpleTextFieldsWriter.TERM.Length, lastTerm.Bytes, 0, len);
                        lastTerm.Length = len;
                        docFreq = 0;
                        _sumTotalTermFreq += totalTermFreq;
                        totalTermFreq = 0;
                        _termCount++;
                    }
                }
                _docCount = visitedDocs.Cardinality();
                _fst = b.Finish();
            
            }

            /// <summary>Returns approximate RAM bytes used</summary>
            public long RamBytesUsed()
            {
                return (_fst != null) ? _fst.SizeInBytes() : 0;
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                return (_fst != null && _fieldInfo.IndexOptions.HasValue) ? new SimpleTextTermsEnum(_outerInstance, _fst, _fieldInfo.IndexOptions.Value) : TermsEnum.EMPTY;
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override long Count
            {
                get { return _termCount; }
            }

            public override long SumTotalTermFreq
            {
                get { return _fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY ? - 1 : _sumTotalTermFreq; }
            }

            public override long SumDocFreq
            {
                get { return _sumDocFreq; }
            }

            public override int DocCount
            {
                get { return _docCount; }
            }

            public override bool HasFreqs
            {
                get { return _fieldInfo.IndexOptions.GetValueOrDefault().CompareTo(IndexOptions.DOCS_AND_FREQS) >= 0; }
            }

            public override bool HasOffsets
            {
                get
                {
                    return _fieldInfo.IndexOptions.GetValueOrDefault().CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                }
            }

            public override bool HasPositions
            {
                get { return _fieldInfo.IndexOptions.GetValueOrDefault().CompareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0; }
            }

            public override bool HasPayloads
            {
                get { return _fieldInfo.HasPayloads; }
            }
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _fields.Keys.GetEnumerator();
        }

        public override Terms Terms(string field)
        {
            lock (this)
            {
                SimpleTextTerms terms;
                if (_termsCache.TryGetValue(field, out terms))
                {
                    return terms;
                }

                long? fp;
                if (!_fields.TryGetValue(field, out fp) || !fp.HasValue)
                {
                    return null;
                }

                terms = new SimpleTextTerms(this, field, fp.Value, _maxDoc);
                _termsCache[field] = (SimpleTextTerms) terms;
                return terms;
            }
        }

        public override int Count
        {
            get { return -1; }
        }

        public override void Dispose()
        {
            _input.Dispose();
        }

        public override long RamBytesUsed()
        {
            return _termsCache.Values.Sum(simpleTextTerms => (simpleTextTerms != null) ? simpleTextTerms.RamBytesUsed() : 0);
        }

        public override void CheckIntegrity()
        {
        }
    }

}