using Lucene.Net.Documents;
using System;
using System.Globalization;

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

    using BytesRef = Util.BytesRef;
    using Directory = Store.Directory;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IIndexableField = Index.IIndexableField;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// Writes plain-text stored fields.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextStoredFieldsWriter : StoredFieldsWriter
    {
        private int _numDocsWritten;
        private readonly Directory _directory;
        private readonly string _segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput _output;
#pragma warning restore CA2213 // Disposable fields should be disposed

        internal const string FIELDS_EXTENSION = "fld";

        internal static readonly BytesRef TYPE_STRING = new BytesRef("string");
        internal static readonly BytesRef TYPE_BINARY = new BytesRef("binary");
        internal static readonly BytesRef TYPE_INT = new BytesRef("int");
        internal static readonly BytesRef TYPE_LONG = new BytesRef("long");
        internal static readonly BytesRef TYPE_FLOAT = new BytesRef("float");
        internal static readonly BytesRef TYPE_DOUBLE = new BytesRef("double");

        internal static readonly BytesRef END = new BytesRef("END");
        internal static readonly BytesRef DOC = new BytesRef("doc ");
        internal static readonly BytesRef NUM = new BytesRef("  numfields ");
        internal static readonly BytesRef FIELD = new BytesRef("  field ");
        internal static readonly BytesRef NAME = new BytesRef("    name ");
        internal static readonly BytesRef TYPE = new BytesRef("    type ");
        internal static readonly BytesRef VALUE = new BytesRef("    value ");

        private readonly BytesRef _scratch = new BytesRef();

        public SimpleTextStoredFieldsWriter(Directory directory, string segment, IOContext context)
        {
            _directory = directory;
            _segment = segment;
            var success = false;
            try
            {
                _output = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Abort();
                }
            }
        }

        public override void StartDocument(int numStoredFields)
        {
            Write(DOC);
            Write(Convert.ToString(_numDocsWritten, CultureInfo.InvariantCulture));
            NewLine();

            Write(NUM);
            Write(Convert.ToString(numStoredFields, CultureInfo.InvariantCulture));
            NewLine();

            _numDocsWritten++;
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            Write(FIELD);
            Write(info.Number.ToString(CultureInfo.InvariantCulture));
            NewLine();

            Write(NAME);
            Write(field.Name);
            NewLine();

            Write(TYPE);

            // LUCENENET specific - To avoid boxing/unboxing, we don't
            // call GetNumericValue(). Instead, we check the field.NumericType and then
            // call the appropriate conversion method. 
            if (field.NumericType != NumericFieldType.NONE)
            {
                switch (field.NumericType)
                {
                    case NumericFieldType.BYTE:
                    case NumericFieldType.INT16:
                    case NumericFieldType.INT32:
                        Write(TYPE_INT);
                        NewLine();

                        Write(VALUE);
                        Write(field.GetStringValue(CultureInfo.InvariantCulture));
                        NewLine();
                        break;
                    case NumericFieldType.INT64:
                        Write(TYPE_LONG);
                        NewLine();

                        Write(VALUE);
                        Write(field.GetStringValue(CultureInfo.InvariantCulture));
                        NewLine();
                        break;
                    case NumericFieldType.SINGLE:
                        Write(TYPE_FLOAT);
                        NewLine();

                        Write(VALUE);
                        Write(field.GetStringValue(CultureInfo.InvariantCulture)); // LUCENENET: Use the "J" format that is the default round-trippable format
                        NewLine();
                        break;
                    case NumericFieldType.DOUBLE:
                        Write(TYPE_DOUBLE);
                        NewLine();

                        Write(VALUE);
                        Write(field.GetStringValue(CultureInfo.InvariantCulture)); // LUCENENET: Use the "J" format that is the default round-trippable format
                        NewLine();
                        break;
                    default:
                        throw new ArgumentException("cannot store numeric type " + field.NumericType);
                }
            }
            else
            {
                BytesRef bytes = field.GetBinaryValue();
                if (bytes != null)
                {
                    Write(TYPE_BINARY);
                    NewLine();

                    Write(VALUE);
                    Write(bytes);
                    NewLine();
                }
                else if (field.GetStringValue() is null)
                {
                    throw new ArgumentException("field " + field.Name +
                                                       " is stored but does not have binaryValue, stringValue nor numericValue");
                }
                else
                {
                    Write(TYPE_STRING);
                    NewLine();

                    Write(VALUE);
                    Write(field.GetStringValue());
                    NewLine();
                }
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored) when (ignored.IsThrowable())
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(_directory,
                    IndexFileNames.SegmentFileName(_segment, "", FIELDS_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (_numDocsWritten != numDocs)
            {
                throw RuntimeException.Create("mergeFields produced an invalid result: docCount is " + numDocs + " but only saw " +
                                    _numDocsWritten + " file=" + _output +
                                    "; now aborting this merge to prevent index corruption");
            }
            Write(END);
            NewLine();
            SimpleTextUtil.WriteChecksum(_output, _scratch);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            try
            {
                IOUtils.Dispose(_output);
            }
            finally
            {
                _output = null;
            }
        }

        private void Write(string s)
        {
            SimpleTextUtil.Write(_output, s, _scratch);
        }

        private void Write(BytesRef bytes)
        {
            SimpleTextUtil.Write(_output, bytes);
        }

        private void NewLine()
        {
            SimpleTextUtil.WriteNewline(_output);
        }
    }
}