using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using Directory = Store.Directory;
    using DocsAndPositionsEnum = Index.DocsAndPositionsEnum;
    using DocsEnum = Index.DocsEnum;
    using Fields = Index.Fields;
    using IBits = Util.IBits;
    using IndexFileNames = Index.IndexFileNames;
    using IndexInput = Store.IndexInput;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using SegmentInfo = Index.SegmentInfo;
    using StringHelper = Util.StringHelper;
    using Terms = Index.Terms;
    using TermsEnum = Index.TermsEnum;
    using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// Reads plain-text term vectors.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextTermVectorsReader : TermVectorsReader
    {
        private long[] _offsets; // docid -> offset in .vec file
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexInput _input;
#pragma warning restore CA2213 // Disposable fields should be disposed
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
                    catch (Exception t) when (t.IsThrowable())
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
                    _offsets[upto] = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    upto++;
                }
            }
            SimpleTextUtil.CheckFooter(input);
            if (Debugging.AssertsEnabled) Debugging.Assert(upto == _offsets.Length);
        }

        public override Fields Get(int doc)
        {
            // LUCENENET specific: Use StringComparer.Ordinal to get the same ordering as Java
            var fields = new JCG.SortedDictionary<string, SimpleTVTerms>(StringComparer.Ordinal);

            _input.Seek(_offsets[doc]);
            ReadLine();
            if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.NUMFIELDS));
            var numFields = ParseInt32At(SimpleTextTermVectorsWriter.NUMFIELDS.Length);
            if (numFields == 0)
            {
                return null; // no vectors for this doc
            }
            for (var i = 0; i < numFields; i++)
            {
                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELD));
                // skip fieldNumber:
                ParseInt32At(SimpleTextTermVectorsWriter.FIELD.Length);

                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDNAME));
                var fieldName = ReadString(SimpleTextTermVectorsWriter.FIELDNAME.Length, _scratch);

                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDPOSITIONS));
                var positions = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDPOSITIONS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDOFFSETS));
                var offsets = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDOFFSETS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDPAYLOADS));
                var payloads = Convert.ToBoolean(ReadString(SimpleTextTermVectorsWriter.FIELDPAYLOADS.Length, _scratch), CultureInfo.InvariantCulture);

                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.FIELDTERMCOUNT));
                var termCount = ParseInt32At(SimpleTextTermVectorsWriter.FIELDTERMCOUNT.Length);

                var terms = new SimpleTVTerms(offsets, positions, payloads);
                fields.Add(fieldName, terms);

                for (var j = 0; j < termCount; j++)
                {
                    ReadLine();
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.TERMTEXT));
                    var term = new BytesRef();
                    var termLength = _scratch.Length - SimpleTextTermVectorsWriter.TERMTEXT.Length;
                    term.Grow(termLength);
                    term.Length = termLength;
                    Arrays.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextTermVectorsWriter.TERMTEXT.Length, term.Bytes, term.Offset, termLength);

                    var postings = new SimpleTVPostings();
                    terms.terms.Add(term, postings);

                    ReadLine();
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.TERMFREQ));
                    postings.freq = ParseInt32At(SimpleTextTermVectorsWriter.TERMFREQ.Length);

                    if (!positions && !offsets) continue;

                    if (positions)
                    {
                        postings.positions = new int[postings.freq];
                        if (payloads)
                        {
                            postings.payloads = new BytesRef[postings.freq];
                        }
                    }

                    if (offsets)
                    {
                        postings.startOffsets = new int[postings.freq];
                        postings.endOffsets = new int[postings.freq];
                    }

                    for (var k = 0; k < postings.freq; k++)
                    {
                        if (positions)
                        {
                            ReadLine();
                            if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.POSITION));
                            postings.positions[k] = ParseInt32At(SimpleTextTermVectorsWriter.POSITION.Length);
                            if (payloads)
                            {
                                ReadLine();
                                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.PAYLOAD));
                                if (_scratch.Length - SimpleTextTermVectorsWriter.PAYLOAD.Length == 0)
                                {
                                    postings.payloads[k] = null;
                                }
                                else
                                {
                                    var payloadBytes = new byte[_scratch.Length - SimpleTextTermVectorsWriter.PAYLOAD.Length];
                                    Arrays.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextTermVectorsWriter.PAYLOAD.Length, payloadBytes, 0,
                                        payloadBytes.Length);
                                    postings.payloads[k] = new BytesRef(payloadBytes);
                                }
                            }
                        }

                        if (!offsets) continue;

                        ReadLine();
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.STARTOFFSET));
                        postings.startOffsets[k] = ParseInt32At(SimpleTextTermVectorsWriter.STARTOFFSET.Length);

                        ReadLine();
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextTermVectorsWriter.ENDOFFSET));
                        postings.endOffsets[k] = ParseInt32At(SimpleTextTermVectorsWriter.ENDOFFSET.Length);
                    }
                }
            }
            return new SimpleTVFields(fields);
        }

        public override object Clone()
        {
            if (_input is null)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this TermVectorsReader is disposed.");
            }
            return new SimpleTextTermVectorsReader(_offsets, (IndexInput)_input.Clone());
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            try
            {
                IOUtils.Dispose(_input);
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

        /// <summary>
        /// NOTE: This was parseIntAt() in Lucene.
        /// </summary>
        private int ParseInt32At(int offset)
        {
            UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + offset, _scratch.Length - offset, _scratchUtf16);
            return ArrayUtil.ParseInt32(_scratchUtf16.Chars, 0, _scratchUtf16.Length);
        }

        private string ReadString(int offset, BytesRef scratch)
        {
            UnicodeUtil.UTF8toUTF16(scratch.Bytes, scratch.Offset + offset, scratch.Length - offset, _scratchUtf16);
            return _scratchUtf16.ToString();
        }

        private class SimpleTVFields : Fields
        {
            private readonly IDictionary<string, SimpleTVTerms> _fields;

            internal SimpleTVFields(IDictionary<string, SimpleTVTerms> fields)
            {
                _fields = fields;
            }

            public override IEnumerator<string> GetEnumerator()
            {
                return _fields.Keys.GetEnumerator();
            }

            public override Terms GetTerms(string field)
            {
                _fields.TryGetValue(field, out SimpleTVTerms result);
                return result;
            }

            public override int Count => _fields.Count;
        }

        private class SimpleTVTerms : Terms
        {
            internal readonly JCG.SortedDictionary<BytesRef, SimpleTVPostings> terms;
            private readonly bool _hasOffsetsRenamed;
            private readonly bool _hasPositionsRenamed;
            private readonly bool _hasPayloadsRenamed;

            internal SimpleTVTerms(bool hasOffsets, bool hasPositions, bool hasPayloads)
            {
                _hasOffsetsRenamed = hasOffsets;
                _hasPositionsRenamed = hasPositions;
                _hasPayloadsRenamed = hasPayloads;
                terms = new JCG.SortedDictionary<BytesRef, SimpleTVPostings>();
            }

            public override TermsEnum GetEnumerator()
            {
                // TODO: reuse
                return new SimpleTVTermsEnum(terms);
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

            public override long Count => terms.Count;

            public override long SumTotalTermFreq => -1;

            public override long SumDocFreq => terms.Count;

            public override int DocCount => 1;

            public override bool HasFreqs => true;

            public override bool HasOffsets => _hasOffsetsRenamed;

            public override bool HasPositions => _hasPositionsRenamed;

            public override bool HasPayloads => _hasPayloadsRenamed;
        }

        private class SimpleTVPostings
        {
            internal int freq;
            internal int[] positions;
            internal int[] startOffsets; 
            internal int[] endOffsets;
            internal BytesRef[] payloads;
        }

        private class SimpleTVTermsEnum : TermsEnum
        {
            private readonly JCG.SortedDictionary<BytesRef, SimpleTVPostings> _terms;
            private IEnumerator<KeyValuePair<BytesRef, SimpleTVPostings>> _iterator;
            private KeyValuePair<BytesRef, SimpleTVPostings> _current;

            internal SimpleTVTermsEnum(JCG.SortedDictionary<BytesRef, SimpleTVPostings> terms)
            {
                _terms = terms;
                _iterator = terms.GetEnumerator();
            }

            public override SeekStatus SeekCeil(BytesRef text)
            {
                var newTerms = new JCG.SortedDictionary<BytesRef, SimpleTVPostings>(_terms.Comparer);
                foreach (var p in _terms)
                    if (p.Key.CompareTo(text) >= 0)
                        newTerms.Add(p.Key, p.Value);

                _iterator = newTerms.GetEnumerator();

                // LUCENENET specific: Since in .NET we don't have a HasNext() method, we need
                // to call MoveNext(). Since we need
                // to check the result anyway for the Equals() comparison, this makes sense here.
                if (!MoveNext())
                {
                    return SeekStatus.END;
                }
                else
                {
                    return _current.Key.Equals(text) ? SeekStatus.FOUND : SeekStatus.NOT_FOUND;
                }
            }

            public override void SeekExact(long ord)
            {
                throw UnsupportedOperationException.Create();
            }

            public override bool MoveNext()
            {
                if (_iterator.MoveNext())
                {
                    _current = _iterator.Current;
                    return _current.Key != null;
                }
                else
                {
                    return false;
                }
            }

            [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public override BytesRef Next()
            {
                if (MoveNext())
                    return _current.Key;
                return null;
            }

            public override BytesRef Term => _current.Key;

            public override long Ord => throw UnsupportedOperationException.Create();

            public override int DocFreq => 1;

            public override long TotalTermFreq => _current.Value.freq;

            public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
            {
                // TODO: reuse
                var e = new SimpleTVDocsEnum();
                e.Reset(liveDocs, (flags & DocsFlags.FREQS) == 0 ? 1 : _current.Value.freq);
                return e;
            }

            public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
            {
                var postings = _current.Value;
                if (postings.positions is null && postings.startOffsets is null)
                    return null;

                // TODO: reuse
                var e = new SimpleTVDocsAndPositionsEnum();
                e.Reset(liveDocs, postings.positions, postings.startOffsets, postings.endOffsets, postings.payloads);
                return e;
            }

            public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;
        }

        // note: these two enum classes are exactly like the Default impl...
        private class SimpleTVDocsEnum : DocsEnum
        {
            private bool _didNext;
            private int _doc = -1;
            private int _freqRenamed;
            private IBits _liveDocs;

            public override int Freq
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(_freqRenamed != -1);
                    return _freqRenamed;
                }
            }

            public override int DocID => _doc;

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

            public virtual void Reset(IBits liveDocs, int freq)
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

        private class SimpleTVDocsAndPositionsEnum : DocsAndPositionsEnum
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

                    if (Debugging.AssertsEnabled) Debugging.Assert(_startOffsets != null);
                    return _startOffsets.Length;
                }
            }

            public override int DocID => _doc;

            public override int NextDoc()
            {
                if (!_didNext && (_liveDocs is null || _liveDocs.Get(0)))
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

            public virtual void Reset(IBits liveDocs, int[] positions, int[] startOffsets, int[] endOffsets,
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

            public override BytesRef GetPayload()
            {
                return _payloads?[_nextPos - 1];
            }

            public override int NextPosition()
            {
                //if (Debugging.AssertsEnabled) Debugging.Assert((_positions != null && _nextPos < _positions.Length) ||
                //    _startOffsets != null && _nextPos < _startOffsets.Length);

                // LUCENENET: The above assertion was for control flow when testing. In Java, it would throw an AssertionError, which is
                // caught by the BaseTermVectorsFormatTestCase.assertEquals(RandomTokenStream tk, FieldType ft, Terms terms) method in the
                // part that is checking for an error after reading to the end of the enumerator.

                // In .NET it is more natural to throw an InvalidOperationException in this case, since we would potentially get an
                // IndexOutOfRangeException if we didn't, which doesn't really provide good feedback as to what the cause is.
                // This matches the behavior of Lucene 8.x. See #267.
                if (((_positions != null && _nextPos < _positions.Length) || _startOffsets != null && _nextPos < _startOffsets.Length) == false)
                    throw IllegalStateException.Create("Read past last position");

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
                    if (_startOffsets is null)
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
                    if (_endOffsets is null)
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

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
        }
    }
}