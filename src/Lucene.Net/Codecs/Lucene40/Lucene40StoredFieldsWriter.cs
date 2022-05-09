using J2N;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene40
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IBits = Lucene.Net.Util.IBits;
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MergeState = Lucene.Net.Index.MergeState;
    using SegmentReader = Lucene.Net.Index.SegmentReader;

    /// <summary>
    /// Class responsible for writing stored document fields.
    /// <para/>
    /// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="Lucene40StoredFieldsFormat"/>
    public sealed class Lucene40StoredFieldsWriter : StoredFieldsWriter
    {
        // NOTE: bit 0 is free here!  You can steal it!
        internal const int FIELD_IS_BINARY = 1 << 1;

        // the old bit 1 << 2 was compressed, is now left out

        private const int _NUMERIC_BIT_SHIFT = 3;
        internal const int FIELD_IS_NUMERIC_MASK = 0x07 << _NUMERIC_BIT_SHIFT;

        internal const int FIELD_IS_NUMERIC_INT = 1 << _NUMERIC_BIT_SHIFT;
        internal const int FIELD_IS_NUMERIC_LONG = 2 << _NUMERIC_BIT_SHIFT;
        internal const int FIELD_IS_NUMERIC_FLOAT = 3 << _NUMERIC_BIT_SHIFT;
        internal const int FIELD_IS_NUMERIC_DOUBLE = 4 << _NUMERIC_BIT_SHIFT;

        // the next possible bits are: 1 << 6; 1 << 7
        // currently unused: static final int FIELD_IS_NUMERIC_SHORT = 5 << _NUMERIC_BIT_SHIFT;
        // currently unused: static final int FIELD_IS_NUMERIC_BYTE = 6 << _NUMERIC_BIT_SHIFT;

        internal const string CODEC_NAME_IDX = "Lucene40StoredFieldsIndex";
        internal const string CODEC_NAME_DAT = "Lucene40StoredFieldsData";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;
        internal static readonly long HEADER_LENGTH_IDX = CodecUtil.HeaderLength(CODEC_NAME_IDX);
        internal static readonly long HEADER_LENGTH_DAT = CodecUtil.HeaderLength(CODEC_NAME_DAT);

        /// <summary>
        /// Extension of stored fields file. </summary>
        public const string FIELDS_EXTENSION = "fdt";

        /// <summary>
        /// Extension of stored fields index file. </summary>
        public const string FIELDS_INDEX_EXTENSION = "fdx";

        private readonly Directory directory;
        private readonly string segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput fieldsStream;
        private IndexOutput indexStream;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Sole constructor. </summary>
        public Lucene40StoredFieldsWriter(Directory directory, string segment, IOContext context)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(directory != null);
            this.directory = directory;
            this.segment = segment;

            bool success = false;
            try
            {
                fieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION), context);

                CodecUtil.WriteHeader(fieldsStream, CODEC_NAME_DAT, VERSION_CURRENT);
                CodecUtil.WriteHeader(indexStream, CODEC_NAME_IDX, VERSION_CURRENT);
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(HEADER_LENGTH_DAT == fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    Debugging.Assert(HEADER_LENGTH_IDX == indexStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void StartDocument(int numStoredFields)
        {
            indexStream.WriteInt64(fieldsStream.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            fieldsStream.WriteVInt32(numStoredFields);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception ignored) when (ignored.IsThrowable())
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION));
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
                        bits |= FIELD_IS_NUMERIC_INT;
                        break;
                    case NumericFieldType.INT64:
                        bits |= FIELD_IS_NUMERIC_LONG;
                        break;
                    case NumericFieldType.SINGLE:
                        bits |= FIELD_IS_NUMERIC_FLOAT;
                        break;
                    case NumericFieldType.DOUBLE:
                        bits |= FIELD_IS_NUMERIC_DOUBLE;
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
                    bits |= FIELD_IS_BINARY;
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
                        fieldsStream.WriteInt32(BitConversion.SingleToInt32Bits(field.GetSingleValue().Value));
                        break;
                    case NumericFieldType.DOUBLE:
                        fieldsStream.WriteInt64(BitConversion.DoubleToInt64Bits(field.GetDoubleValue().Value));
                        break;
                    default:
                        throw AssertionError.Create("Cannot get here");
                }
            }
        }

        /// <summary>
        /// Bulk write a contiguous series of documents.  The
        /// <paramref name="lengths"/> array is the length (in bytes) of each raw
        /// document.  The <paramref name="stream"/> <see cref="IndexInput"/> is the
        /// fieldsStream from which we should bulk-copy all
        /// bytes.
        /// </summary>
        public void AddRawDocuments(IndexInput stream, int[] lengths, int numDocs)
        {
            long position = fieldsStream.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long start = position;
            for (int i = 0; i < numDocs; i++)
            {
                indexStream.WriteInt64(position);
                position += lengths[i];
            }
            fieldsStream.CopyBytes(stream, position - start);
            if (Debugging.AssertsEnabled) Debugging.Assert(fieldsStream.Position == position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (HEADER_LENGTH_IDX + ((long)numDocs) * 8 != indexStream.Position) // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            // this is most likely a bug in Sun JRE 1.6.0_04/_05;
            // we detect that the bug has struck, here, and
            // throw an exception to prevent the corruption from
            // entering the index.  See LUCENE-1282 for
            // details.
            {
                throw RuntimeException.Create("fdx size mismatch: docCount is " + numDocs + " but fdx file size is " + indexStream.Position + " file=" + indexStream.ToString() + "; now aborting this merge to prevent index corruption");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int Merge(MergeState mergeState)
        {
            int docCount = 0;
            // Used for bulk-reading raw bytes for stored fields
            int[] rawDocLengths = new int[MAX_RAW_MERGE_DOCS];
            int idx = 0;

            foreach (AtomicReader reader in mergeState.Readers)
            {
                SegmentReader matchingSegmentReader = mergeState.MatchingSegmentReaders[idx++];
                Lucene40StoredFieldsReader matchingFieldsReader = null;
                if (matchingSegmentReader != null)
                {
                    StoredFieldsReader fieldsReader = matchingSegmentReader.FieldsReader;
                    // we can only bulk-copy if the matching reader is also a Lucene40FieldsReader
                    if (fieldsReader != null && fieldsReader is Lucene40StoredFieldsReader lucene40StoredFieldsReader)
                    {
                        matchingFieldsReader = lucene40StoredFieldsReader;
                    }
                }

                if (reader.LiveDocs != null)
                {
                    docCount += CopyFieldsWithDeletions(mergeState, reader, matchingFieldsReader, rawDocLengths);
                }
                else
                {
                    docCount += CopyFieldsNoDeletions(mergeState, reader, matchingFieldsReader, rawDocLengths);
                }
            }
            Finish(mergeState.FieldInfos, docCount);
            return docCount;
        }

        /// <summary>
        /// Maximum number of contiguous documents to bulk-copy
        /// when merging stored fields.
        /// </summary>
        private const int MAX_RAW_MERGE_DOCS = 4192;

        private int CopyFieldsWithDeletions(MergeState mergeState, AtomicReader reader, Lucene40StoredFieldsReader matchingFieldsReader, int[] rawDocLengths)
        {
            int docCount = 0;
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;
            if (Debugging.AssertsEnabled) Debugging.Assert(liveDocs != null);
            if (matchingFieldsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                for (int j = 0; j < maxDoc; )
                {
                    if (!liveDocs.Get(j))
                    {
                        // skip deleted docs
                        ++j;
                        continue;
                    }
                    // We can optimize this case (doing a bulk byte copy) since the field
                    // numbers are identical
                    int start = j, numDocs = 0;
                    do
                    {
                        j++;
                        numDocs++;
                        if (j >= maxDoc)
                        {
                            break;
                        }
                        if (!liveDocs.Get(j))
                        {
                            j++;
                            break;
                        }
                    } while (numDocs < MAX_RAW_MERGE_DOCS);

                    IndexInput stream = matchingFieldsReader.RawDocs(rawDocLengths, start, numDocs);
                    AddRawDocuments(stream, rawDocLengths, numDocs);
                    docCount += numDocs;
                    mergeState.CheckAbort.Work(300 * numDocs);
                }
            }
            else
            {
                for (int j = 0; j < maxDoc; j++)
                {
                    if (!liveDocs.Get(j))
                    {
                        // skip deleted docs
                        continue;
                    }
                    // TODO: this could be more efficient using
                    // FieldVisitor instead of loading/writing entire
                    // doc; ie we just have to renumber the field number
                    // on the fly?
                    // NOTE: it's very important to first assign to doc then pass it to
                    // fieldsWriter.addDocument; see LUCENE-1282
                    Document doc = reader.Document(j);
                    AddDocument(doc, mergeState.FieldInfos);
                    docCount++;
                    mergeState.CheckAbort.Work(300);
                }
            }
            return docCount;
        }

        private int CopyFieldsNoDeletions(MergeState mergeState, AtomicReader reader, Lucene40StoredFieldsReader matchingFieldsReader, int[] rawDocLengths)
        {
            int maxDoc = reader.MaxDoc;
            int docCount = 0;
            if (matchingFieldsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                while (docCount < maxDoc)
                {
                    int len = Math.Min(MAX_RAW_MERGE_DOCS, maxDoc - docCount);
                    IndexInput stream = matchingFieldsReader.RawDocs(rawDocLengths, docCount, len);
                    AddRawDocuments(stream, rawDocLengths, len);
                    docCount += len;
                    mergeState.CheckAbort.Work(300 * len);
                }
            }
            else
            {
                for (; docCount < maxDoc; docCount++)
                {
                    // NOTE: it's very important to first assign to doc then pass it to
                    // fieldsWriter.addDocument; see LUCENE-1282
                    Document doc = reader.Document(docCount);
                    AddDocument(doc, mergeState.FieldInfos);
                    mergeState.CheckAbort.Work(300);
                }
            }
            return docCount;
        }
    }
}