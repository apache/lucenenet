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
    using System.Globalization;

	using FieldInfo = Index.FieldInfo;
	using FieldInfos = Index.FieldInfos;
	using IndexFileNames = Index.IndexFileNames;
	using IIndexableField = Index.IIndexableField;
	using Directory = Store.Directory;
	using IOContext = Store.IOContext;
	using IndexOutput = Store.IndexOutput;
	using BytesRef = Util.BytesRef;
	using IOUtils = Util.IOUtils;

    /// <summary>
    /// Writes plain-text stored fields.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextStoredFieldsWriter : StoredFieldsWriter
    {
        private int _numDocsWritten;
        private readonly Directory _directory;
        private readonly string _segment;
        private IndexOutput _output;

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

            var n = field.GetNumericValue();

            if (n != null)
            {
                if (n is sbyte? || n is short? || n is int?)
                {
                    Write(TYPE_INT);
                    NewLine();

                    Write(VALUE);
                    Write(((int)n).ToString(CultureInfo.InvariantCulture));
                    NewLine();
                }
                else if (n is long?)
                {
                    Write(TYPE_LONG);
                    NewLine();

                    Write(VALUE);
                    Write(((long)n).ToString(CultureInfo.InvariantCulture));
                    NewLine();
                }
                else if (n is float?)
                {
                    Write(TYPE_FLOAT);
                    NewLine();

                    Write(VALUE);
                    // LUCENENET: Need to specify the "R" for round-trip: http://stackoverflow.com/a/611564/181087
                    Write(((float)n).ToString("R", CultureInfo.InvariantCulture));
                    NewLine();
                }
                else if (n is double?)
                {
                    Write(TYPE_DOUBLE);
                    NewLine();

                    Write(VALUE);
                    // LUCENENET: Need to specify the "R" for round-trip: http://stackoverflow.com/a/611564/181087
                    Write(((double)n).ToString("R", CultureInfo.InvariantCulture));
                    NewLine();
                }
                else
                {
                    throw new ArgumentException("cannot store numeric type " + n.GetType());
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
                else if (field.GetStringValue() == null)
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

        public override sealed void Abort()
	    {
	        try
	        {
	            Dispose();
	        }
	        finally
	        {
	            IOUtils.DeleteFilesIgnoringExceptions(_directory,
	                IndexFileNames.SegmentFileName(_segment, "", FIELDS_EXTENSION));
	        }
	    }

	    public override void Finish(FieldInfos fis, int numDocs)
	    {
	        if (_numDocsWritten != numDocs)
	        {
	            throw new Exception("mergeFields produced an invalid result: docCount is " + numDocs + " but only saw " +
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
	            IOUtils.Close(_output);
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