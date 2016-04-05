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
    using System.ComponentModel;
    using Support;

	using BinaryDocValues = Index.BinaryDocValues;
	using CorruptIndexException = Index.CorruptIndexException;
	using DocValues = Index.DocValues;
	using FieldInfo = Index.FieldInfo;
	using DocValuesType = Index.FieldInfo.DocValuesType_e;
	using IndexFileNames = Index.IndexFileNames;
	using NumericDocValues = Index.NumericDocValues;
	using SegmentReadState = Index.SegmentReadState;
	using SortedDocValues = Index.SortedDocValues;
	using SortedSetDocValues = Index.SortedSetDocValues;
	using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using IndexInput = Store.IndexInput;
	using Bits = Util.Bits;
	using BytesRef = Util.BytesRef;
	using StringHelper = Util.StringHelper;

    public class SimpleTextDocValuesReader : DocValuesProducer
    {
        internal class OneField
        {
            public long DataStartFilePointer { get; set; }
            public string Pattern { get; set; }
            public string OrdPattern { get; set; }
            public int MaxLength { get; set; }
            public bool FixedLength { get; set; }
            public long MinValue { get; set; }
            public long NumValues { get; set; }
        }

        internal readonly int MAX_DOC;
        internal readonly IndexInput DATA;
        internal readonly BytesRef SCRATCH = new BytesRef();
        internal readonly IDictionary<string, OneField> FIELDS = new Dictionary<string, OneField>();

        public SimpleTextDocValuesReader(SegmentReadState state, string ext)
        {
            DATA = state.Directory.OpenInput(
                    IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, ext), state.Context);
            MAX_DOC = state.SegmentInfo.DocCount;
            
            while (true)
            {
                ReadLine();
                if (SCRATCH.Equals(SimpleTextDocValuesWriter.END))
                {
                    break;
                }
                Debug.Assert(StartsWith(SimpleTextDocValuesWriter.FIELD), SCRATCH.Utf8ToString());
                var fieldName = StripPrefix(SimpleTextDocValuesWriter.FIELD);
                var field = new OneField();
                
                FIELDS[fieldName] = field;

                ReadLine();
                Debug.Assert(StartsWith(SimpleTextDocValuesWriter.TYPE), SCRATCH.Utf8ToString());

                var dvType =
                    (FieldInfo.DocValuesType_e)
                        Enum.Parse(typeof (FieldInfo.DocValuesType_e), StripPrefix(SimpleTextDocValuesWriter.TYPE));

                if (dvType == FieldInfo.DocValuesType_e.NUMERIC)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MINVALUE),
                        "got " + SCRATCH.Utf8ToString() + " field=" + fieldName + " ext=" + ext);
                    field.MinValue = Convert.ToInt64(StripPrefix(SimpleTextDocValuesWriter.MINVALUE));
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    field.DataStartFilePointer = DATA.FilePointer;
                    DATA.Seek(DATA.FilePointer + (1 + field.Pattern.Length + 2)*MAX_DOC);
                }
                else if (dvType == FieldInfo.DocValuesType_e.BINARY)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MAXLENGTH));
                    field.MaxLength = Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH));
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    field.DataStartFilePointer = DATA.FilePointer;
                    DATA.Seek(DATA.FilePointer + (9 + field.Pattern.Length + field.MaxLength + 2)*MAX_DOC);
                }
                else if (dvType == FieldInfo.DocValuesType_e.SORTED || dvType == FieldInfo.DocValuesType_e.SORTED_SET)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.NUMVALUES));
                    field.NumValues = Convert.ToInt64(StripPrefix(SimpleTextDocValuesWriter.NUMVALUES));
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MAXLENGTH));
                    field.MaxLength = Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH));
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.ORDPATTERN));
                    field.OrdPattern = StripPrefix(SimpleTextDocValuesWriter.ORDPATTERN);
                    field.DataStartFilePointer = DATA.FilePointer;
                    DATA.Seek(DATA.FilePointer + (9 + field.Pattern.Length + field.MaxLength)*field.NumValues +
                              (1 + field.OrdPattern.Length)*MAX_DOC);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            // We should only be called from above if at least one
            // field has DVs:
            Debug.Assert(FIELDS.Count > 0);
        }

        public override NumericDocValues GetNumeric(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];
            Debug.Assert(field != null);

            // SegmentCoreReaders already verifies this field is valid:
            Debug.Assert(field != null, "field=" + fieldInfo.Name + " fields=" + FIELDS);

            var @in = (IndexInput)DATA.Clone();
            var scratch = new BytesRef();
            
            return new NumericDocValuesAnonymousInnerClassHelper(this, field, @in, scratch);
        }

        public override BinaryDocValues GetBinary(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];
            Debug.Assert(field != null);
            var input = (IndexInput)DATA.Clone();
            var scratch = new BytesRef();

            return new BinaryDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
        }

        public override SortedDocValues GetSorted(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];

            // SegmentCoreReaders already verifies this field is valid:
            Debug.Assert(field != null);
            var input = (IndexInput)DATA.Clone();
            var scratch = new BytesRef();

            return new SortedDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];

            // SegmentCoreReaders already verifies this field is
            // valid:
            Debug.Assert(field != null);

            var input = (IndexInput) DATA.Clone();
            var scratch = new BytesRef();
            
            return new SortedSetDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
        }

        public override Bits GetDocsWithField(FieldInfo field)
        {
            switch (field.DocValuesType)
            {
                case FieldInfo.DocValuesType_e.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), MAX_DOC);
                case FieldInfo.DocValuesType_e.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), MAX_DOC);
                case FieldInfo.DocValuesType_e.BINARY:
                    return GetBinaryDocsWithField(field);
                case FieldInfo.DocValuesType_e.NUMERIC:
                    return GetNumericDocsWithField(field);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) return;

            DATA.Dispose();
        }

        private Bits GetNumericDocsWithField(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];
            var input = (IndexInput)DATA.Clone();
            var scratch = new BytesRef();
            return new BitsAnonymousInnerClassHelper(this, field, input, scratch);
        }

        private Bits GetBinaryDocsWithField(FieldInfo fieldInfo)
        {
            var field = FIELDS[fieldInfo.Name];
            var input = (IndexInput)DATA.Clone();
            var scratch = new BytesRef();

            return new BitsAnonymousInnerClassHelper2(this, field, input, scratch);
        }

        /// <summary> Used only in ctor: </summary>
        private void ReadLine()
        {
            SimpleTextUtil.ReadLine(DATA, SCRATCH);
        }

        /// <summary> Used only in ctor: </summary>
        private bool StartsWith(BytesRef prefix)
        {
            return StringHelper.StartsWith(SCRATCH, prefix);
        }

        /// <summary> Used only in ctor: </summary>
        private string StripPrefix(BytesRef prefix)
        {
            return SCRATCH.Bytes.SubList(SCRATCH.Offset + prefix.Length, SCRATCH.Length - prefix.Length).ToString();
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            var iScratch = new BytesRef();
            var clone = (IndexInput) DATA.Clone();
            clone.Seek(0);
            ChecksumIndexInput input = new BufferedChecksumIndexInput(clone);
            while (true)
            {
                SimpleTextUtil.ReadLine(input, iScratch);
                if (!iScratch.Equals(SimpleTextDocValuesWriter.END)) continue;

                SimpleTextUtil.CheckFooter(input);
                break;
            }
        }



        private class BitsAnonymousInnerClassHelper : Bits
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;

            public BitsAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance,
                OneField field, IndexInput @in, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = @in;
                _scratch = scratch;
            }

            public bool Get(int index)
            {
                _input.Seek(_field.DataStartFilePointer + (1 + _field.Pattern.Length + 2) * index);
                SimpleTextUtil.ReadLine(_input, _scratch); // data
                SimpleTextUtil.ReadLine(_input, _scratch); // 'T' or 'F'
                return _scratch.Bytes[_scratch.Offset] == (sbyte)'T';
            }

            public int Length()
            {
                return _outerInstance.MAX_DOC;
            }
        }

        private class BitsAnonymousInnerClassHelper2 : Bits
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;

            public BitsAnonymousInnerClassHelper2(SimpleTextDocValuesReader outerInstance, OneField field,
                IndexInput input, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = input;
                _scratch = scratch;
            }

            public bool Get(int index)
            {
                _input.Seek(_field.DataStartFilePointer + (9 + _field.Pattern.Length + _field.MaxLength + 2) * index);
                SimpleTextUtil.ReadLine(_input, _scratch);
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH));
                int len;
                try
                {
                    len = int.Parse(_scratch.Bytes.SubList(_scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                                _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length).ToString());
                }
                catch (FormatException ex)
                {
                    throw new CorruptIndexException("failed to parse int value (resource=" + _input + ")", ex);
                }

                // skip past bytes
                var bytes = new byte[len];
                _input.ReadBytes(bytes, 0, len);
                SimpleTextUtil.ReadLine(_input, _scratch); // newline
                SimpleTextUtil.ReadLine(_input, _scratch); // 'T' or 'F'
                return _scratch.Bytes[_scratch.Offset] == (sbyte)'T';
            }

            public int Length()
            {
                return _outerInstance.MAX_DOC;
            }
        }

        private class SortedDocValuesAnonymousInnerClassHelper : SortedDocValues
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;
            private readonly string _decoderFormat;
            private readonly string _ordDecoderFormat;

            public SortedDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance,
                OneField field, IndexInput input, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = input;
                _scratch = scratch;
                _decoderFormat = field.Pattern;
                _ordDecoderFormat = field.OrdPattern;
            }

            public override int GetOrd(int docId)
            {
                if (docId < 0 || docId >= _outerInstance.MAX_DOC)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.MAX_DOC - 1) + "; got " +
                                                       docId);
                }

                _input.Seek(_field.DataStartFilePointer + _field.NumValues * (9 + _field.Pattern.Length + _field.MaxLength) +
                         docId * (1 + _field.OrdPattern.Length));
                SimpleTextUtil.ReadLine(_input, _scratch);
                try
                {
                    return int.Parse(Decimal.Parse(_scratch.Utf8ToString()).ToString(_ordDecoderFormat)) - 1;
                }
                catch (Exception pe)
                {
                    var e = new CorruptIndexException("failed to parse ord (resource=" + _input + ")", pe);
                    throw e;
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                if (ord < 0 || ord >= _field.NumValues)
                {
                    throw new IndexOutOfRangeException("ord must be 0 .. " + (_field.NumValues - 1) + "; got " +
                                                              ord);
                }
                _input.Seek(_field.DataStartFilePointer + ord * (9 + _field.Pattern.Length + _field.MaxLength));
                SimpleTextUtil.ReadLine(_input, _scratch);
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH),
                    "got " + _scratch.Utf8ToString() + " in=" + _input);
                int len;
                try
                {
                    len =
                        int.Parse(
                            Decimal.Parse(_scratch.Bytes.SubList(
                                _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                                _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length).ToString()).ToString(_decoderFormat));
                }
                catch (Exception pe)
                {
                    var e = new CorruptIndexException("failed to parse int length (resource=" + _input + ")", pe);
                    throw e;
                }

                result.Bytes = new byte[len];
                result.Offset = 0;
                result.Length = len;
                _input.ReadBytes(result.Bytes, 0, len);
            }

            public override int ValueCount
            {
                get { return (int)_field.NumValues; }
            }
        }

        private class SortedSetDocValuesAnonymousInnerClassHelper : SortedSetDocValues
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;
            private readonly string _decoderFormat;

            public SortedSetDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance,
                OneField field, IndexInput input, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = input;
                _scratch = scratch;
                _decoderFormat = field.Pattern;
                _currentOrds = new string[0];
                _currentIndex = 0;
            }

            private string[] _currentOrds;
            private int _currentIndex;

            public override long NextOrd()
            {
                return _currentIndex == _currentOrds.Length ? NO_MORE_ORDS : Convert.ToInt64(_currentOrds[_currentIndex++]);
            }

            public override int Document
            {
                set
                {
                    if (value < 0 || value >= _outerInstance.MAX_DOC)
                        throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.MAX_DOC - 1) + "; got " +
                                                           value);


                    _input.Seek(_field.DataStartFilePointer + _field.NumValues * (9 + _field.Pattern.Length + _field.MaxLength) +
                             value * (1 + _field.OrdPattern.Length));
                    SimpleTextUtil.ReadLine(_input, _scratch);
                    var ordList = _scratch.Utf8ToString().Trim();
                    _currentOrds = ordList.Length == 0 ? new string[0] : ordList.Split(",", true);
                    _currentIndex = 0;
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                if (ord < 0 || ord >= _field.NumValues)
                {
                    throw new IndexOutOfRangeException("ord must be 0 .. " + (_field.NumValues - 1) + "; got " + ord);
                }

                _input.Seek(_field.DataStartFilePointer + ord * (9 + _field.Pattern.Length + _field.MaxLength));
                SimpleTextUtil.ReadLine(_input, _scratch);
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH),
                    "got " + _scratch.Utf8ToString() + " in=" + _input);
                int len;
                try
                {
                    len =
                        int.Parse(
                            Decimal.Parse(_scratch.Bytes.SubList(
                                _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                                _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length).ToString()).ToString(_decoderFormat));
                }
                catch (Exception pe)
                {
                    var e = new CorruptIndexException("failed to parse int length (resource=" + _input + ")", pe);
                    throw e;
                }

                result.Bytes = new byte[len];
                result.Offset = 0;
                result.Length = len;
                _input.ReadBytes(result.Bytes, 0, len);
            }

            public override long ValueCount
            {
                get { return _field.NumValues; }
            }
        }

        private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;

            public NumericDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance,
                OneField field, IndexInput input, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = input;
                _scratch = scratch;
            }

            public override long Get(int docId)
            {
                if (docId < 0 || docId >= _outerInstance.MAX_DOC)
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.MAX_DOC - 1) +
                                                       "; got " + docId);

                _input.Seek(_field.DataStartFilePointer + (1 + _field.Pattern.Length + 2) * docId);
                SimpleTextUtil.ReadLine(_input, _scratch);

                long bd;
                try
                {
                    bd = long.Parse(_scratch.Utf8ToString());
                }
                catch (FormatException ex)
                {
                    throw new CorruptIndexException("failed to parse long value (resource=" + _input + ")", ex);
                }

                SimpleTextUtil.ReadLine(_input, _scratch); // read the line telling us if its real or not
                return _field.MinValue + bd;
            }
        }

        private class BinaryDocValuesAnonymousInnerClassHelper : BinaryDocValues
        {
            private readonly SimpleTextDocValuesReader _outerInstance;

            private readonly OneField _field;
            private readonly IndexInput _input;
            private readonly BytesRef _scratch;

            public BinaryDocValuesAnonymousInnerClassHelper(SimpleTextDocValuesReader outerInstance, OneField field,
                IndexInput input, BytesRef scratch)
            {
                _outerInstance = outerInstance;
                _field = field;
                _input = input;
                _scratch = scratch;
            }

            public override void Get(int docId, BytesRef result)
            {
                if (docId < 0 || docId >= _outerInstance.MAX_DOC)
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.MAX_DOC - 1) +
                                                       "; got " + docId);

                _input.Seek(_field.DataStartFilePointer + (9 + _field.Pattern.Length + _field.MaxLength + 2) * docId);
                SimpleTextUtil.ReadLine(_input, _scratch);
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH));
                int len;
                try
                {
                    len = int.Parse(_scratch.Bytes.SubList(
                                _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                                _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length).ToString());
                }
                catch (FormatException ex)
                {
                    throw new CorruptIndexException("failed to parse int value (resource=" + _input + ")", ex);
                }

                result.Bytes = new byte[len];
                result.Offset = 0;
                result.Length = len;
                _input.ReadBytes(result.Bytes, 0, len);
            }
        }

    }

}