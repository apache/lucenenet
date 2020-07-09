using J2N.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

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

    using BinaryDocValues = Index.BinaryDocValues;
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using BytesRef = Util.BytesRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using CorruptIndexException = Index.CorruptIndexException;
    using DocValues = Index.DocValues;
    using DocValuesType = Index.DocValuesType;
    using FieldInfo = Index.FieldInfo;
    using IBits = Util.IBits;
    using IndexFileNames = Index.IndexFileNames;
    using IndexInput = Store.IndexInput;
    using NumericDocValues = Index.NumericDocValues;
    using SegmentReadState = Index.SegmentReadState;
    using SortedDocValues = Index.SortedDocValues;
    using SortedSetDocValues = Index.SortedSetDocValues;
    using StringHelper = Util.StringHelper;

    public class SimpleTextDocValuesReader : DocValuesProducer // LUCENENET NOTE: Changed from internal to public because it is subclassed by a public class
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

        private readonly int maxDoc;
        private readonly IndexInput data;
        private readonly BytesRef scratch = new BytesRef();
        private readonly IDictionary<string, OneField> fields = new Dictionary<string, OneField>();

        // LUCENENET NOTE: Changed from public to internal because the class had to be made public, but is not for public use.
        internal SimpleTextDocValuesReader(SegmentReadState state, string ext)
        {
            data = state.Directory.OpenInput(
                    IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, ext), state.Context);
            maxDoc = state.SegmentInfo.DocCount;
            
            while (true)
            {
                ReadLine();
                if (scratch.Equals(SimpleTextDocValuesWriter.END))
                {
                    break;
                }
                Debug.Assert(StartsWith(SimpleTextDocValuesWriter.FIELD), scratch.Utf8ToString());
                var fieldName = StripPrefix(SimpleTextDocValuesWriter.FIELD);
                var field = new OneField();
                
                fields[fieldName] = field;

                ReadLine();
                Debug.Assert(StartsWith(SimpleTextDocValuesWriter.TYPE), scratch.Utf8ToString());

                var dvType =
                    (DocValuesType)
                        Enum.Parse(typeof (DocValuesType), StripPrefix(SimpleTextDocValuesWriter.TYPE));

                if (dvType == DocValuesType.NUMERIC)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MINVALUE),
                        "got " + scratch.Utf8ToString() + " field=" + fieldName + " ext=" + ext);
                    field.MinValue = Convert.ToInt64(StripPrefix(SimpleTextDocValuesWriter.MINVALUE), CultureInfo.InvariantCulture);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    field.DataStartFilePointer = data.GetFilePointer();
                    data.Seek(data.GetFilePointer() + (1 + field.Pattern.Length + 2)*maxDoc);
                }
                else if (dvType == DocValuesType.BINARY)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MAXLENGTH));
                    field.MaxLength = Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH), CultureInfo.InvariantCulture);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    field.DataStartFilePointer = data.GetFilePointer();
                    data.Seek(data.GetFilePointer() + (9 + field.Pattern.Length + field.MaxLength + 2)*maxDoc);
                }
                else if (dvType == DocValuesType.SORTED || dvType == DocValuesType.SORTED_SET)
                {
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.NUMVALUES));
                    field.NumValues = Convert.ToInt64(StripPrefix(SimpleTextDocValuesWriter.NUMVALUES), CultureInfo.InvariantCulture);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.MAXLENGTH));
                    field.MaxLength = Convert.ToInt32(StripPrefix(SimpleTextDocValuesWriter.MAXLENGTH), CultureInfo.InvariantCulture);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.PATTERN));
                    field.Pattern = StripPrefix(SimpleTextDocValuesWriter.PATTERN);
                    ReadLine();
                    Debug.Assert(StartsWith(SimpleTextDocValuesWriter.ORDPATTERN));
                    field.OrdPattern = StripPrefix(SimpleTextDocValuesWriter.ORDPATTERN);
                    field.DataStartFilePointer = data.GetFilePointer();
                    data.Seek(data.GetFilePointer() + (9 + field.Pattern.Length + field.MaxLength)*field.NumValues +
                              (1 + field.OrdPattern.Length)*maxDoc);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            // We should only be called from above if at least one
            // field has DVs:
            Debug.Assert(fields.Count > 0);
        }

        public override NumericDocValues GetNumeric(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];
            Debug.Assert(field != null);

            // SegmentCoreReaders already verifies this field is valid:
            Debug.Assert(field != null, "field=" + fieldInfo.Name + " fields=" + fields);

            var @in = (IndexInput)data.Clone();
            var scratch = new BytesRef();
            
            return new NumericDocValuesAnonymousInnerClassHelper(this, field, @in, scratch);
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
                try
                {
                    if (docId < 0 || docId >= _outerInstance.maxDoc)
                        throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.maxDoc - 1) +
                                                           "; got " + docId);

                    _input.Seek(_field.DataStartFilePointer + (1 + _field.Pattern.Length + 2) * docId);
                    SimpleTextUtil.ReadLine(_input, _scratch);


                    decimal bd;
                    try
                    {
                        // LUCNENENET: .NET doesn't have a way to specify a pattern with decimal, but all of the standard ones are built in.
                        bd = decimal.Parse(_scratch.Utf8ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException ex)
                    {
                        throw new CorruptIndexException("failed to parse long value (resource=" + _input + ")", ex);
                    }

                    SimpleTextUtil.ReadLine(_input, _scratch); // read the line telling us if its real or not
                    return (long)BigInteger.Add(new BigInteger(_field.MinValue), new BigInteger(bd));
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }
        }

        private IBits GetNumericDocsWithField(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];
            var input = (IndexInput)data.Clone();
            var scratch = new BytesRef();
            return new BitsAnonymousInnerClassHelper(this, field, input, scratch);
        }

        private class BitsAnonymousInnerClassHelper : IBits
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
                try
                {
                    _input.Seek(_field.DataStartFilePointer + (1 + _field.Pattern.Length + 2) * index);
                    SimpleTextUtil.ReadLine(_input, _scratch); // data
                    SimpleTextUtil.ReadLine(_input, _scratch); // 'T' or 'F'
                    return _scratch.Bytes[_scratch.Offset] == (byte)'T';
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }

            public int Length => _outerInstance.maxDoc;
        }

        public override BinaryDocValues GetBinary(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];
            Debug.Assert(field != null);
            var input = (IndexInput)data.Clone();
            var scratch = new BytesRef();

            return new BinaryDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
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
                try
                {
                    if (docId < 0 || docId >= _outerInstance.maxDoc)
                        throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.maxDoc - 1) +
                                                           "; got " + docId);

                    _input.Seek(_field.DataStartFilePointer + (9 + _field.Pattern.Length + _field.MaxLength + 2) * docId);
                    SimpleTextUtil.ReadLine(_input, _scratch);
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH));
                    int len;
                    try
                    {
                        // LUCNENENET: .NET doesn't have a way to specify a pattern with integer, but all of the standard ones are built in.
                        len = int.Parse(Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                            _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length), NumberStyles.Integer, CultureInfo.InvariantCulture);
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
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }
        }

        private IBits GetBinaryDocsWithField(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];
            var input = (IndexInput)data.Clone();
            var scratch = new BytesRef();

            return new BitsAnonymousInnerClassHelper2(this, field, input, scratch);
        }

        private class BitsAnonymousInnerClassHelper2 : IBits
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
                try
                {
                    _input.Seek(_field.DataStartFilePointer + (9 + _field.Pattern.Length + _field.MaxLength + 2) * index);
                    SimpleTextUtil.ReadLine(_input, _scratch);
                    Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextDocValuesWriter.LENGTH));
                    int len;
                    try
                    {
                        len = int.Parse(Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                            _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length), NumberStyles.Number, CultureInfo.InvariantCulture);
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
                    return _scratch.Bytes[_scratch.Offset] == (byte)'T';
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }

            public int Length => _outerInstance.maxDoc;
        }

        public override SortedDocValues GetSorted(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];

            // SegmentCoreReaders already verifies this field is valid:
            Debug.Assert(field != null);
            var input = (IndexInput)data.Clone();
            var scratch = new BytesRef();

            return new SortedDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
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
                if (docId < 0 || docId >= _outerInstance.maxDoc)
                {
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.maxDoc - 1) + "; got " +
                                                       docId);
                }

                try
                {
                    _input.Seek(_field.DataStartFilePointer + _field.NumValues * (9 + _field.Pattern.Length + _field.MaxLength) +
                             docId * (1 + _field.OrdPattern.Length));
                    SimpleTextUtil.ReadLine(_input, _scratch);
                    try
                    {
                        // LUCNENENET: .NET doesn't have a way to specify a pattern with integer, but all of the standard ones are built in.
                        return int.Parse(_scratch.Utf8ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture) - 1;
                    }
                    catch (Exception pe)
                    {
                        var e = new CorruptIndexException("failed to parse ord (resource=" + _input + ")", pe);
                        throw e;
                    }
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                try
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
                        // LUCNENENET: .NET doesn't have a way to specify a pattern with integer, but all of the standard ones are built in.
                        len = int.Parse(Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                            _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length), NumberStyles.Integer, CultureInfo.InvariantCulture);

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
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }

            public override int ValueCount => (int)_field.NumValues;
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo fieldInfo)
        {
            var field = fields[fieldInfo.Name];

            // SegmentCoreReaders already verifies this field is
            // valid:
            Debug.Assert(field != null);

            var input = (IndexInput) data.Clone();
            var scratch = new BytesRef();
            
            return new SortedSetDocValuesAnonymousInnerClassHelper(this, field, input, scratch);
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
                _currentOrds = EMPTY_STRINGS;
                _currentIndex = 0;
            }

            // LUCENENET specific - optimized empty array creation
            private static readonly string[] EMPTY_STRINGS =
#if FEATURE_ARRAYEMPTY
                Array.Empty<string>();
#else
                new string[0];
#endif

            private string[] _currentOrds;
            private int _currentIndex;

            public override long NextOrd()
            {
                return _currentIndex == _currentOrds.Length ? NO_MORE_ORDS : Convert.ToInt64(_currentOrds[_currentIndex++], CultureInfo.InvariantCulture);
            }

            public override void SetDocument(int docID)
            {
                if (docID < 0 || docID >= _outerInstance.maxDoc)
                    throw new IndexOutOfRangeException("docID must be 0 .. " + (_outerInstance.maxDoc - 1) + "; got " +
                                                        docID);

                try
                {
                    _input.Seek(_field.DataStartFilePointer + _field.NumValues * (9 + _field.Pattern.Length + _field.MaxLength) +
                                docID * (1 + _field.OrdPattern.Length));
                    SimpleTextUtil.ReadLine(_input, _scratch);
                    var ordList = _scratch.Utf8ToString().Trim();
                    _currentOrds = ordList.Length == 0 ? EMPTY_STRINGS : ordList.Split(',').TrimEnd();
                    _currentIndex = 0;
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }

            public override void LookupOrd(long ord, BytesRef result)
            {
                try
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
                        // LUCNENENET: .NET doesn't have a way to specify a pattern with integer, but all of the standard ones are built in.
                        len = int.Parse(Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextDocValuesWriter.LENGTH.Length,
                            _scratch.Length - SimpleTextDocValuesWriter.LENGTH.Length), NumberStyles.Integer, CultureInfo.InvariantCulture);
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
                catch (IOException ioe)
                {
                    throw new Exception(ioe.ToString(), ioe);
                }
            }

            public override long ValueCount => _field.NumValues;
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            switch (field.DocValuesType)
            {
                case DocValuesType.SORTED_SET:
                    return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
                case DocValuesType.SORTED:
                    return DocValues.DocsWithValue(GetSorted(field), maxDoc);
                case DocValuesType.BINARY:
                    return GetBinaryDocsWithField(field);
                case DocValuesType.NUMERIC:
                    return GetNumericDocsWithField(field);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            data.Dispose();
        }

        /// <summary> Used only in ctor: </summary>
        private void ReadLine()
        {
            SimpleTextUtil.ReadLine(data, scratch);
        }

        /// <summary> Used only in ctor: </summary>
        private bool StartsWith(BytesRef prefix)
        {
            return StringHelper.StartsWith(scratch, prefix);
        }

        /// <summary> Used only in ctor: </summary>
        private string StripPrefix(BytesRef prefix)
        {
            return Encoding.UTF8.GetString(scratch.Bytes, scratch.Offset + prefix.Length, scratch.Length - prefix.Length);
        }

        public override long RamBytesUsed()
        {
            return 0;
        }

        public override void CheckIntegrity()
        {
            var iScratch = new BytesRef();
            var clone = (IndexInput) data.Clone();
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
    }
}