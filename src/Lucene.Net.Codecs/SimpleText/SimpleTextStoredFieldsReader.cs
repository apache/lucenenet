using J2N.Globalization;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Globalization;
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

    using ArrayUtil = Util.ArrayUtil;
    using BufferedChecksumIndexInput = Store.BufferedChecksumIndexInput;
    using BytesRef = Util.BytesRef;
    using CharsRef = Util.CharsRef;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using Directory = Store.Directory;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using IndexInput = Store.IndexInput;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using SegmentInfo = Index.SegmentInfo;
    using StoredFieldVisitor = Index.StoredFieldVisitor;
    using StringHelper = Util.StringHelper;
    using UnicodeUtil = Util.UnicodeUtil;

    /// <summary>
    /// Reads plain text stored fields.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextStoredFieldsReader : StoredFieldsReader
    {
        private long[] _offsets; // docid -> offset in .fld file
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexInput _input;
#pragma warning restore CA2213 // Disposable fields should be disposed
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
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // ensure we throw our original exception
                    }
                }
            }
            ReadIndex(si.DocCount);
        }

        /// <remarks>Used by clone.</remarks>
        internal SimpleTextStoredFieldsReader(long[] offsets, IndexInput input, FieldInfos fieldInfos)
        {
            _offsets = offsets;
            _input = input;
            _fieldInfos = fieldInfos;
        }

        /// <remarks>
        /// We don't actually write a .fdx-like index, instead we read the 
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
                    _offsets[upto] = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    upto++;
                }
            }
            SimpleTextUtil.CheckFooter(input);
            if (Debugging.AssertsEnabled) Debugging.Assert(upto == _offsets.Length);
        }

        public override void VisitDocument(int n, StoredFieldVisitor visitor)
        {
            _input.Seek(_offsets[n]);
            ReadLine();
            if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.NUM));
            var numFields = ParseInt32At(SimpleTextStoredFieldsWriter.NUM.Length);

            for (var i = 0; i < numFields; i++)
            {
                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.FIELD));
                int fieldNumber = ParseInt32At(SimpleTextStoredFieldsWriter.FIELD.Length);
                FieldInfo fieldInfo = _fieldInfos.FieldInfo(fieldNumber);
                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.NAME));
                ReadLine();
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.TYPE));

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
                    throw RuntimeException.Create("unknown field type");
                }

                switch (visitor.NeedsField(fieldInfo))
                {
                    case StoredFieldVisitor.Status.YES:
                        ReadField(type, fieldInfo, visitor);
                        break;
                    case StoredFieldVisitor.Status.NO:
                        ReadLine();
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.VALUE));
                        break;
                    case StoredFieldVisitor.Status.STOP:
                        return;
                }
            }
        }

        private void ReadField(BytesRef type, FieldInfo fieldInfo, StoredFieldVisitor visitor)
        {
            ReadLine();
            if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(_scratch, SimpleTextStoredFieldsWriter.VALUE));
            if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_STRING))
            {
                visitor.StringField(fieldInfo,
                    Encoding.UTF8.GetString(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length,
                        _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_BINARY))
            {
                var copy = new byte[_scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length];
                Arrays.Copy(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, copy, 0, copy.Length);
                visitor.BinaryField(fieldInfo, copy);
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_INT))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.Int32Field(fieldInfo, J2N.Numerics.Int32.Parse(_scratchUtf16.ToString(), NumberFormatInfo.InvariantInfo));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_LONG))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.Int64Field(fieldInfo, J2N.Numerics.Int64.Parse(_scratchUtf16.ToString(), NumberFormatInfo.InvariantInfo));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_FLOAT))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.SingleField(fieldInfo, J2N.Numerics.Single.Parse(_scratchUtf16.ToString(), NumberStyle.Float, NumberFormatInfo.InvariantInfo));
            }
            else if (Equals(type, SimpleTextStoredFieldsWriter.TYPE_DOUBLE))
            {
                UnicodeUtil.UTF8toUTF16(_scratch.Bytes, _scratch.Offset + SimpleTextStoredFieldsWriter.VALUE.Length, _scratch.Length - SimpleTextStoredFieldsWriter.VALUE.Length,
                    _scratchUtf16);
                visitor.DoubleField(fieldInfo, J2N.Numerics.Double.Parse(_scratchUtf16.ToString(), NumberStyle.Float, NumberFormatInfo.InvariantInfo));
            }
        }

        public override object Clone()
        {
            if (_input is null)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this FieldsReader is disposed.");
            }
            return new SimpleTextStoredFieldsReader(_offsets, (IndexInput) _input.Clone(), _fieldInfos);
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

        private static bool EqualsAt(BytesRef a, BytesRef b, int bOffset) // LUCENENET: CA1822: Mark members as static
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