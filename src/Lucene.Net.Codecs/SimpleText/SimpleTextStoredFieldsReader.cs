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
    using System.Globalization;
    using System.Text;
    using Support;

	using FieldInfo = Index.FieldInfo;
	using FieldInfos = Index.FieldInfos;
	using IndexFileNames = Index.IndexFileNames;
	using SegmentInfo = Index.SegmentInfo;
	using StoredFieldVisitor = Index.StoredFieldVisitor;
	using AlreadyClosedException = Store.AlreadyClosedException;
	using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
	using ChecksumIndexInput = Store.ChecksumIndexInput;
	using Directory = Store.Directory;
	using IOContext = Store.IOContext;
	using IndexInput = Store.IndexInput;
	using ArrayUtil = Util.ArrayUtil;
	using BytesRef = Util.BytesRef;
	using CharsRef = Util.CharsRef;
	using IOUtils = Util.IOUtils;
	using StringHelper = Util.StringHelper;
	using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// reads plaintext stored fields
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextStoredFieldsReader : StoredFieldsReader
    {
        private long[] _offsets; // docid -> offset in .fld file
        private IndexInput _input;
        private readonly BytesRef _scratch = new BytesRef();
        private readonly CharsRef _scratchUtf16 = new CharsRef();
        private readonly FieldInfos _fieldInfos;

        public SimpleTextStoredFieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
        {
            _fieldInfos = fn;
            var success = false;
            try
            {
                _input =
                    directory.OpenInput(
                        IndexFileNames.SegmentFileName(si.Name, "", SimpleTextStoredFieldsWriter.FIELDS_EXTENSION),
                        context);
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
                    catch
                    {
                        // ensure we throw our original exception
                    }
                }
            }
            ReadIndex(si.DocCount);
        }

        /// <remarks>Used by clone</remarks>
        internal SimpleTextStoredFieldsReader(long[] offsets, IndexInput input, FieldInfos fieldInfos)
        {
            _offsets = offsets;
            _input = input;
            _fieldInfos = fieldInfos;
        }

        /// <remarks>
        /// we don't actually write a .fdx-like index, instead we read the 
        /// stored fields file in entirety up-front and save the offsets 
        /// so we can seek to the documents later.
        /// </remarks>
        private void ReadIndex(int size)
        {
            ChecksumIndexInput input = new BufferedChecksumIndexInput(_input);
            _offsets = new long[size];
            var upto = 0;
            while (!_scratch.Equals(SimpleTextStoredFieldsWriter.END))
            {
                SimpleTextUtil.ReadLine(input, _scratch);
                if (StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.DOC))
                {
                    _offsets[upto] = input.FilePointer;
                    upto++;
                }
            }
            SimpleTextUtil.CheckFooter(input);
            Debug.Assert(upto == _offsets.Length);
        }

        public override void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            _input.Seek(_offsets[n]);
            ReadLine();
            Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.NUM));
            var numFields = ParseIntAt(SimpleTextStoredFieldsWriter.NUM.Length);

            for (var i = 0; i < numFields; i++)
            {
                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.FIELD));
                int fieldNumber = ParseIntAt(SimpleTextStoredFieldsWriter.FIELD.Length);
                FieldInfo fieldInfo = _fieldInfos.FieldInfo(fieldNumber);
                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.NAME));
                ReadLine();
                Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.TYPE));

                BytesRef type;
                if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_STRING, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_STRING;
                }
                else if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_BINARY, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_BINARY;
                }
                else if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_INT, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_INT;
                }
                else if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_LONG, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_LONG;
                }
                else if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_FLOAT, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_FLOAT;
                }
                else if (EqualsAt(SimpleTextStoredFieldsWriter.TYPE_DOUBLE, _scratch, SimpleTextStoredFieldsWriter.TYPE.Length))
                {
                    type = SimpleTextStoredFieldsWriter.TYPE_DOUBLE;
                }
                else
                {
                    throw new Exception("unknown field type");
                }

                switch (visitor.NeedsField(fieldInfo))
                {
                    case StoredFieldVisitor.Status.YES:
                        ReadField(type, fieldInfo, visitor);
                        break;
                    case StoredFieldVisitor.Status.NO:
                        ReadLine();
                        Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.VALUE));
                        break;
                    case StoredFieldVisitor.Status.STOP:
                        return;
                }
            }
        }

        private void ReadField(BytesRef type, FieldInfo fieldInfo, StoredFieldVisitor visitor)
        {
            ReadLine();
            Debug.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.VALUE));
            if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_STRING))
            {
                visitor.StringField(fieldInfo,
                    Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length,
                        _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_BINARY))
            {
                var copy = new byte[_scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length];
                Array.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, copy, 0, copy.Length);
                visitor.BinaryField(fieldInfo, copy);
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_INT))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.Int32Field(fieldInfo, Convert.ToInt32(_scratchUtf16.ToString(), CultureInfo.InvariantCulture));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_LONG))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.Int64Field(fieldInfo, Convert.ToInt64(_scratchUtf16.ToString(), CultureInfo.InvariantCulture));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_FLOAT))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.SingleField(fieldInfo, Convert.ToSingle(_scratchUtf16.ToString(), CultureInfo.InvariantCulture));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_DOUBLE))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.DoubleField(fieldInfo, Convert.ToDouble(_scratchUtf16.ToString(), CultureInfo.InvariantCulture));
            }
        }

        public override object Clone()
        {
            if (_input == null)
            {
                throw new AlreadyClosedException("this FieldsReader is closed");
            }
            return new SimpleTextStoredFieldsReader(_offsets, (IndexInput) _input.Clone(), _fieldInfos);
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

        private bool EqualsAt(BytesRef a, BytesRef b, int bOffset)
        {
            return a.Length == b.Length - bOffset &&
                   ArrayUtil.Equals(a.Bytes, a.Offset, b.Bytes, b.Offset + bOffset, b.Length - bOffset);
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