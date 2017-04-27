using System;
using System.Diagnostics;

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

    using Lucene.Net.Support;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// @lucene.experimental </summary>
#pragma warning disable 612, 618
    internal sealed class PreFlexRWStoredFieldsWriter : StoredFieldsWriter
    {
        private readonly Directory Directory;
        private readonly string Segment;
        private IndexOutput FieldsStream;
        private IndexOutput IndexStream;

        public PreFlexRWStoredFieldsWriter(Directory directory, string segment, IOContext context)
        {
            Debug.Assert(directory != null);
            this.Directory = directory;
            this.Segment = segment;

            bool success = false;
            try
            {
                FieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION), context);
                IndexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION), context);

                FieldsStream.WriteInt32(Lucene3xStoredFieldsReader.FORMAT_CURRENT);
                IndexStream.WriteInt32(Lucene3xStoredFieldsReader.FORMAT_CURRENT);

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
            IndexStream.WriteInt64(FieldsStream.GetFilePointer());
            FieldsStream.WriteVInt32(numStoredFields);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(FieldsStream, IndexStream);
                }
                finally
                {
                    FieldsStream = IndexStream = null;
                }
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
#pragma warning disable 168
            catch (Exception ignored)
#pragma warning restore 168
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(Directory, IndexFileNames.SegmentFileName(Segment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION), IndexFileNames.SegmentFileName(Segment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            FieldsStream.WriteVInt32(info.Number);
            int bits = 0;
            BytesRef bytes;
            string @string;
            // TODO: maybe a field should serialize itself?
            // this way we don't bake into indexer all these
            // specific encodings for different fields?  and apps
            // can customize...

            object number = field.GetNumericValue();
            if (number != null)
            {
                if (number is sbyte? || number is short? || number is int?)
                {
                    bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_INT;
                }
                else if (number is long?)
                {
                    bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_LONG;
                }
                else if (number is float?)
                {
                    bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_FLOAT;
                }
                else if (number is double?)
                {
                    bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_DOUBLE;
                }
                else
                {
                    throw new System.ArgumentException("cannot store numeric type " + number.GetType());
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
                    if (@string == null)
                    {
                        throw new System.ArgumentException("field " + field.Name + " is stored but does not have binaryValue, stringValue nor numericValue");
                    }
                }
            }

            FieldsStream.WriteByte((byte)(sbyte)bits);

            if (bytes != null)
            {
                FieldsStream.WriteVInt32(bytes.Length);
                FieldsStream.WriteBytes(bytes.Bytes, bytes.Offset, bytes.Length);
            }
            else if (@string != null)
            {
                FieldsStream.WriteString(field.GetStringValue());
            }
            else
            {
                if (number is sbyte? || number is short? || number is int?)
                {
                    FieldsStream.WriteInt32((int)number);
                }
                else if (number is long?)
                {
                    FieldsStream.WriteInt64((long)number);
                }
                else if (number is float?)
                {
                    FieldsStream.WriteInt32(Number.SingleToInt32Bits((float)number));
                }
                else if (number is double?)
                {
                    FieldsStream.WriteInt64(BitConverter.DoubleToInt64Bits((double)number));
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (4 + ((long)numDocs) * 8 != IndexStream.GetFilePointer())
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw new Exception("fdx size mismatch: docCount is " + numDocs + " but fdx file size is " + IndexStream.GetFilePointer() + " file=" + IndexStream.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }
    }
#pragma warning restore 612, 618
}