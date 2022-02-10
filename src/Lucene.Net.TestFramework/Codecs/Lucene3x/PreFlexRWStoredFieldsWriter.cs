using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs.Lucene3x
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

    /// <summary>
    /// @lucene.experimental </summary>
#pragma warning disable 612, 618
    internal sealed class PreFlexRWStoredFieldsWriter : StoredFieldsWriter
    {
        private readonly Directory directory;
        private readonly string segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput fieldsStream;
        private IndexOutput indexStream;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public PreFlexRWStoredFieldsWriter(Directory directory, string segment, IOContext context)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(directory != null);
            this.directory = directory;
            this.segment = segment;

            bool success = false;
            try
            {
                fieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION), context);
                indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION), context);

                fieldsStream.WriteInt32(Lucene3xStoredFieldsReader.FORMAT_CURRENT);
                indexStream.WriteInt32(Lucene3xStoredFieldsReader.FORMAT_CURRENT);

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

        // Writes the contents of buffer into the fields stream
        // and adds a new entry for this document into the index
        // stream.  this assumes the buffer was already written
        // in the correct fields format.
        public override void StartDocument(int numStoredFields)
        {
            indexStream.WriteInt64(fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            fieldsStream.WriteVInt32(numStoredFields);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Dispose(fieldsStream, indexStream);
                }
                finally
                {
                    fieldsStream = indexStream = null;
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
            IOUtils.DeleteFilesIgnoringExceptions(directory, 
                IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION), 
                IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            fieldsStream.WriteVInt32(info.Number);
            int bits = 0;
            BytesRef bytes;
            string @string;
            // TODO: maybe a field should serialize itself?
            // this way we don't bake into indexer all these
            // specific encodings for different fields?  and apps
            // can customize...

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
                        bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_INT;
                        break;
                    case NumericFieldType.INT64:
                        bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_LONG;
                        break;
                    case NumericFieldType.SINGLE:
                        bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_FLOAT;
                        break;
                    case NumericFieldType.DOUBLE:
                        bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_DOUBLE;
                        break;
                    default:
                        throw new ArgumentException("cannot store numeric type " + field.NumericType);
                }

                @string = null;
                bytes = null;
            }
            else
            {
                bytes = field.GetBinaryValue();
                if (bytes != null)
                {
                    bits |= Lucene3xStoredFieldsReader.FIELD_IS_BINARY;
                    @string = null;
                }
                else
                {
                    @string = field.GetStringValue();
                    if (@string is null)
                    {
                        throw new ArgumentException("field " + field.Name + " is stored but does not have binaryValue, stringValue nor numericValue");
                    }
                }
            }

            fieldsStream.WriteByte((byte)bits);

            if (bytes != null)
            {
                fieldsStream.WriteVInt32(bytes.Length);
                fieldsStream.WriteBytes(bytes.Bytes, bytes.Offset, bytes.Length);
            }
            else if (@string != null)
            {
                fieldsStream.WriteString(field.GetStringValue());
            }
            else
            {
                switch (field.NumericType)
                {
                    case NumericFieldType.BYTE:
                    case NumericFieldType.INT16:
                    case NumericFieldType.INT32:
                        fieldsStream.WriteInt32(field.GetInt32Value().Value);
                        break;
                    case NumericFieldType.INT64:
                        fieldsStream.WriteInt64(field.GetInt64Value().Value);
                        break;
                    case NumericFieldType.SINGLE:
                        fieldsStream.WriteInt32(J2N.BitConversion.SingleToInt32Bits(field.GetSingleValue().Value));
                        break;
                    case NumericFieldType.DOUBLE:
                        fieldsStream.WriteInt64(J2N.BitConversion.DoubleToInt64Bits(field.GetDoubleValue().Value));
                        break;
                    default:
                        if (Debugging.AssertsEnabled) Debugging.Assert(false);
                        break;
                }
            }
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (4 + ((long)numDocs) * 8 != indexStream.Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw RuntimeException.Create("fdx size mismatch: docCount is " + numDocs + " but fdx file size is " + indexStream.Position + " file=" + indexStream.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }
    }
#pragma warning restore 612, 618
}