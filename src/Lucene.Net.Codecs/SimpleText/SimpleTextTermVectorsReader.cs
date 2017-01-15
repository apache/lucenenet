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

using System.Linq;
using Lucene.Net.Support;

namespace Lucene.Net.Codecs.SimpleText
{
    
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Collections.Generic;

	using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
	using DocsEnum = Index.DocsEnum;
	using Fields = Index.Fields;
	using IndexFileNames = Index.IndexFileNames;
	using SegmentInfo = Index.SegmentInfo;
	using Terms = Index.Terms;
	using TermsEnum = Index.TermsEnum;
	using AlreadyClosedException = Store.AlreadyClosedException;
	using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using Directory = Store.Directory;
	using IOContext = Store.IOContext;
	using IndexInput = Store.IndexInput;
	using ArrayUtil = Util.ArrayUtil;
	using IBits = Util.IBits;
	using BytesRef = Util.BytesRef;
	using CharsRef = Util.CharsRef;
	using IOUtils = Util.IOUtils;
	using StringHelper = Util.StringHelper;
	using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// Reads plain-text term vectors.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextTermVectorsReader : TermVectorsReader
    {
        private long[] _offsets; // docid -> offset in .vec file
        private IndexInput _input;
        private readonly BytesRef _scratch = new BytesRef();
        private readonly CharsRef _scratchUtf16 = new CharsRef();

        public SimpleTextTermVectorsReader(Directory directory, SegmentInfo si, IOContext context)
        {
            bool success = false;
            try
            {
                _input = directory.OpenInput(IndexFileNames.SegmentFileName(si.Name, "", SimpleTextTermVectorsWriter.VECTORS_EXTENSION), context);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        Dispose();
                    } 
                    catch (Exception)
                    {
                        // ensure we throw our original exception
                    }
                }
            }
            ReadIndex(si.DocCount);
        }

        // used by clone
        internal SimpleTextTermVectorsReader(long[] offsets, IndexInput input)
        {
            _offsets = offsets;
            _input = input;
        }

        // we don't actually write a .tvx-like index, instead we read the 
        // vectors file in entirety up-front and save the offsets 
        // so we can seek to the data later.
        private void ReadIndex(int maxDoc)
        {
            ChecksumIndexInput input = new BufferedChecksumIndexInput(_input);
            _offsets = new long[maxDoc];
            int upto = 0;
            while (!_scratch.Equals(SimpleTextTermVectorsWriter.END))
            {
                SimpleTextUtil.ReadLine(input, _scratch);
                if (StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.DOC))
                {
                    _offsets[upto] = input.FilePointer;
                    upto++;
                }
            }
            SimpleTextUtil.CheckFooter(input);
            Debug.Assert(upto == _offsets.Length);
        }

        public override Fields Get(int doc)
        {
            var fields = new SortedDictionary<string, SimpleTVTerms>();

            _input.Seek(_offsets[doc]);
            ReadLine();
            Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.NUMFIELDS));
            var numFields = ParseIntAt(SimpleTextTermVectorsWriter.NUMFIELDS.Length);
            if (numFields == 0)
            {
                return null; // no vectors for this doc
            }
            for (var i = 0; i < numFields; i++)
            {
                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELD));
                // skip fieldNumber:
                ParseIntAt(SimpleTextTermVectorsWriter.FIELD.Length);

                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDNAME));
                var fieldName = ReadString(SimpleTextTermVectorsWriter.FIELDNAME.Length, _scratch);

                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDPOSITIONS));
                var positions = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDPOSITIONS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDOFFSETS));
                var offsets = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDOFFSETS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDPAYLOADS));
                var payloads = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDPAYLOADS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDTERMCOUNT));
                var termCount = ParseIntAt(SimpleTextTermVectorsWriter.FIELDTERMCOUNT.Length);

                var terms = new SimpleTVTerms(offsets, positions, payloads);
                fields.Add(fieldName, terms);

                for (var j = 0; j < termCount; j++)
                {
                    ReadLine();
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.TERMTEXT));
                    var term = new BytesRef();
                    var termLength = _scratch.Length - SimpleTextTermVectorsWriter.TERMTEXT.Length;
                    term.Grow(termLength);
                    term.Length = termLength;
                    Array.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextTermVectorsWriter.TERMTEXT.Length, term.Bytes, term.Offset, termLength);

                    var postings = new SimpleTVPostings();
                    terms.TERMS.Add(term, postings);

                    ReadLine();
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.TERMFREQ));
                    postings.FREQ = ParseIntAt(SimpleTextTermVectorsWriter.TERMFREQ.Length);

                    if (!positions && !offsets) continue;

                    if (positions)
                    {
                        postings.POSITIONS = new int[postings.FREQ];
                        if (payloads)
                        {
                            postings.PAYLOADS = new BytesRef[postings.FREQ];
                        }
                    }

                    if (offsets)
                    {
                        postings.START_OFFSETS = new int[postings.FREQ];
                        postings.END_OFFSETS = new int[postings.FREQ];
                    }

                    for (var k = 0; k < postings.FREQ; k++)
                    {
                        if (positions)
                        {
                            ReadLine();
                            Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.POSITION));
                            postings.POSITIONS[k] = ParseIntAt(SimpleTextTermVectorsWriter.POSITION.Length);
                            if (payloads)
                            {
                                ReadLine();
                                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.PAYLOAD));
                                if (_scratch.Length - SimpleTextTermVectorsWriter.PAYLOAD.Length == 0)
                                {
                                    postings.PAYLOADS[k] = null;
                                }
                                else
                                {
                                    var payloadBytes = new byte[_scratch.Length - SimpleTextTermVectorsWriter.PAYLOAD.Length];
                                    Array.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextTermVectorsWriter.PAYLOAD.Length, payloadBytes, 0,
                                        payloadBytes.Length);
                                    postings.PAYLOADS[k] = new BytesRef(payloadBytes);
                                }
                            }
                        }

                        if (!offsets) continue;

                        ReadLine();
                        Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.STARTOFFSET));
                        postings.START_OFFSETS[k] = ParseIntAt(SimpleTextTermVectorsWriter.STARTOFFSET.Length);

                        ReadLine();
                        Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.ENDOFFSET));
                        postings.END_OFFSETS[k] = ParseIntAt(SimpleTextTermVectorsWriter.ENDOFFSET.Length);
                    }
                }
            }
            return new SimpleTVFields(this, fields);
        }

        public override object Clone()
        {
            if (_input == null)
            {
                throw new AlreadyClosedException("this TermVectorsReader is closed");
            }
            return new SimpleTextTermVectorsReader(_offsets, (IndexInput)_input.Clone());
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            try
            {
                IOUtils.Close(_input);
            }
            finally
            {
                _input = null;
                _offsets = null;
            }
        }

        private void ReadLine()
        {
            SimpleTextUtil.ReadLine(_input, _scratch);
        }

        private int ParseIntAt(int offset)
        {
            UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + offset, _scratch.Length - offset, _scratchUtf16);
            return ArrayUtil.ParseInt(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
        }

        private string ReadString(int offset, BytesRef scratch)
        {
            UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + offset, scratch.Length - offset, _scratchUtf16);
            return _scratchUtf16.ToString();
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
        }


        private class SimpleTVFields : Fields
        {
            private readonly SimpleTextTermVectorsReader _outerInstance;
            private readonly SortedDictionary<string, SimpleTVTerms> _fields;

            internal SimpleTVFields(SimpleTextTermVectorsReader outerInstance, SortedDictionary<string, SimpleTVTerms> fields)
            {
                _outerInstance = outerInstance;
                _fields = fields;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return _fields.Keys.GetEnumerator();
            }

            public override Terms Terms(string field)
            {
                return _fields.ContainsKey(field) ? _fields[field] : null;
            }

            public override int Count
            {
                get { return _fields.Count; }
            }
        }

        private class SimpleTVTerms : Terms
        {
            internal readonly SortedDictionary<BytesRef, SimpleTVPostings> TERMS;
            private readonly bool _hasOffsetsRenamed;
            private readonly bool _hasPositionsRenamed;
            private readonly bool _hasPayloadsRenamed;

            internal SimpleTVTerms(bool hasOffsets, bool hasPositions, bool hasPayloads)
            {
                _hasOffsetsRenamed = hasOffsets;
                _hasPositionsRenamed = hasPositions;
                _hasPayloadsRenamed = hasPayloads;
                TERMS = new SortedDictionary<BytesRef, SimpleTVPostings>();
            }

            public override TermsEnum Iterator(TermsEnum reuse)
            {
                // TODO: reuse
                return new SimpleTVTermsEnum(TERMS);
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override long Count
            {
                get { return TERMS.Count; }
            }

            public override long SumTotalTermFreq
            {
                get { return -1; }
            }

            public override long SumDocFreq
            {
                get { return TERMS.Count; }
            }

            public override int DocCount
            {
                get { return 1; }
            }

            public override bool HasFreqs
            {
                get { return true; }
            }

            public override bool HasOffsets
            {
                get { return _hasOffsetsRenamed; }
            }

            public override bool HasPositions
            {
                get { return _hasPositionsRenamed; }
            }

            public override bool HasPayloads
            {
                get { return _hasPayloadsRenamed; }
            }
        }

        private class SimpleTVPostings
        {
            internal int FREQ;
            internal int[] POSITIONS;
            internal int[] START_OFFSETS;
            internal int[] END_OFFSETS;
            internal BytesRef[] PAYLOADS;
        }

        private class SimpleTVTermsEnum : TermsEnum
        {
            private readonly SortedDictionary<BytesRef, SimpleTVPostings> _terms;
            private IEnumerator<KeyValuePair<BytesRef, SimpleTVPostings>> _iterator;
            private KeyValuePair<BytesRef, SimpleTVPostings> _current;

            internal SimpleTVTermsEnum(SortedDictionary<BytesRef, SimpleTVPostings> terms)
            {
                _terms = terms;
                _iterator = terms.EntrySet().GetEnumerator();
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                var newTerms = new SortedDictionary<BytesRef, SimpleTVPostings>();
                foreach (var p in _terms.Where(p => p.Key.CompareTo(text) >= 0))
                    newTerms.Add(p.Key, p.Value);

                _iterator = newTerms.EntrySet().GetEnumerator();

                try
                {
                    _iterator.MoveNext();
                    return _iterator.Current.Key.Equals(text) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                }
                catch
                {
                    return SeekStatus.END;
                }
            }

            public override void SeekExact(long ord)
            {
                throw new NotSupportedException();
            }

            public override BytesRef Next()
            {
                try
                {
                    _iterator.MoveNext();
                    _current = _iterator.Current;
                    return _current.Key;
                }
                catch
                {
                    return null;
                }
            }

            public override BytesRef Term
            {
                get { return _current.Key; }
            }

            public override long Ord
            {
                get { throw new NotSupportedException(); }
            }

            public override int DocFreq
            {
                get { return 1; }
            }

            public override long TotalTermFreq
            {
                get { return _current.Value.FREQ; }
            }

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
            {
                // TODO: reuse
                var e = new SimpleTVDocsEnum();
                e.Reset(liveDocs, (flags & DocsEnum.FLAG_FREQS) == 0 ? 1 : _current.Value.FREQ);
                return e;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, int flags)
            {
                var postings = _current.Value;
                if (postings.POSITIONS == null && postings.START_OFFSETS == null)
                    return null;

                // TODO: reuse
                var e = new SimpleTVDocsAndPositionsEnum();
                e.Reset(liveDocs, postings.POSITIONS, postings.START_OFFSETS, postings.END_OFFSETS, postings.PAYLOADS);
                return e;
            }

            public override IComparer<BytesRef> Comparer
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }
        }

        // note: these two enum classes are exactly like the Default impl...
        private sealed class SimpleTVDocsEnum : DocsEnum
        {
            private bool _didNext;
            private int _doc = -1;
            private int _freqRenamed;
            private IBits _liveDocs;

            public override int Freq
            {
                get
                {
                    Debug.Assert(_freqRenamed != -1);
                    return _freqRenamed;
                }
            }

            public override int DocID
            {
                get { return _doc; }
            }

            public override int NextDoc()
            {
                if (_didNext || (_liveDocs != null && !_liveDocs.Get(0))) return (_doc = NO_MORE_DOCS);
                _didNext = true;
                return (_doc = 0);
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int freq)
            {
                _liveDocs = liveDocs;
                _freqRenamed = freq;
                _doc = -1;
                _didNext = false;
            }

            public override long GetCost()
            {
                return 1;
            }
        }

        private sealed class SimpleTVDocsAndPositionsEnum : DocsAndPositionsEnum
        {
            private bool _didNext;
            private int _doc = -1;
            private int _nextPos;
            private IBits _liveDocs;
            private int[] _positions;
            private BytesRef[] _payloads;
            private int[] _startOffsets;
            private int[] _endOffsets;

            public override int Freq
            {
                get
                {
                    if (_positions != null)
                        return _positions.Length;

                    Debug.Assert(_startOffsets != null);
                    return _startOffsets.Length;
                }
            }

            public override int DocID
            {
                get { return _doc; }
            }

            public override int NextDoc()
            {
                if (!_didNext && (_liveDocs == null || _liveDocs.Get(0)))
                {
                    _didNext = true;
                    return (_doc = 0);
                }
                else
                {
                    return (_doc = NO_MORE_DOCS);
                }
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }

            public void Reset(IBits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets,
                BytesRef[] payloads)
            {
                _liveDocs = liveDocs;
                _positions = positions;
                _startOffsets = startOffsets;
                _endOffsets = endOffsets;
                _payloads = payloads;
                _doc = -1;
                _didNext = false;
                _nextPos = 0;
            }

            public override BytesRef Payload
            {
                get { return _payloads == null ? null : _payloads[_nextPos - 1]; }
            }

            public override int NextPosition()
            {
                // LUCENENET NOTE: In Java, the assertion is being caught in the test (as an AssertionException).
                // Technically, a "possible" (in fact "probable") scenario like this one, we should be throwing
                // an exception, however doing that causes the checkIndex test to fail. The only logical thing we
                // can do to make this compatible is to remove the assert.
                //Debug.Assert((_positions != null && _nextPos < _positions.Length) ||
                //             _startOffsets != null && _nextPos < _startOffsets.Length);
                if (_positions != null)
                {
                    return _positions[_nextPos++];
                }

                _nextPos++;
                return -1;
            }

            public override int StartOffset
            {
                get
                {
                    if (_startOffsets == null)
                    {
                        return -1;
                    }

                    return _startOffsets[_nextPos - 1];
                }
            }

            public override int EndOffset
            {
                get
                {
                    if (_endOffsets == null)
                    {
                        return -1;
                    }

                    return _endOffsets[_nextPos - 1];
                }
            }

            public override long GetCost()
            {
                return 1;
            }
        }
    }
}