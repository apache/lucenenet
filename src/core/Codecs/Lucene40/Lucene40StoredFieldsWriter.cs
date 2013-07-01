using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene40
{
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
        // currently unused: internal const int FIELD_IS_NUMERIC_SHORT = 5 << _NUMERIC_BIT_SHIFT;
        // currently unused: internal const int FIELD_IS_NUMERIC_BYTE = 6 << _NUMERIC_BIT_SHIFT;

        internal const String CODEC_NAME_IDX = "Lucene40StoredFieldsIndex";
        internal const String CODEC_NAME_DAT = "Lucene40StoredFieldsData";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;
        internal static readonly long HEADER_LENGTH_IDX = CodecUtil.HeaderLength(CODEC_NAME_IDX);
        internal static readonly long HEADER_LENGTH_DAT = CodecUtil.HeaderLength(CODEC_NAME_DAT);

        /** Extension of stored fields file */
        public const string FIELDS_EXTENSION = "fdt";

        /** Extension of stored fields index file */
        public const string FIELDS_INDEX_EXTENSION = "fdx";

        private readonly Directory directory;
        private readonly String segment;
        private IndexOutput fieldsStream;
        private IndexOutput indexStream;

        public Lucene40StoredFieldsWriter(Directory directory, String segment, IOContext context)
        {
            //assert directory != null;
            this.directory = directory;
            this.segment = segment;

            bool success = false;
            try
            {
                fieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION), context);
                indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION), context);

                CodecUtil.WriteHeader(fieldsStream, CODEC_NAME_DAT, VERSION_CURRENT);
                CodecUtil.WriteHeader(indexStream, CODEC_NAME_IDX, VERSION_CURRENT);
                //assert HEADER_LENGTH_DAT == fieldsStream.getFilePointer();
                //assert HEADER_LENGTH_IDX == indexStream.getFilePointer();
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
            indexStream.WriteLong(fieldsStream.FilePointer);
            fieldsStream.WriteVInt(numStoredFields);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(fieldsStream, indexStream);
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
            catch { }
            IOUtils.DeleteFilesIgnoringExceptions(directory,
                IndexFileNames.SegmentFileName(segment, "", FIELDS_EXTENSION),
                IndexFileNames.SegmentFileName(segment, "", FIELDS_INDEX_EXTENSION));
        }

        public override void WriteField(FieldInfo info, IIndexableField field)
        {
            fieldsStream.WriteVInt(info.number);
            int bits = 0;
            BytesRef bytes;
            String str;
            // TODO: maybe a field should serialize itself?
            // this way we don't bake into indexer all these
            // specific encodings for different fields?  and apps
            // can customize...

            object number = field.NumericValue;
            if (number != null)
            {
                if (number is byte || number is sbyte || number is short || number is int)
                {
                    bits |= FIELD_IS_NUMERIC_INT;
                }
                else if (number is long)
                {
                    bits |= FIELD_IS_NUMERIC_LONG;
                }
                else if (number is float)
                {
                    bits |= FIELD_IS_NUMERIC_FLOAT;
                }
                else if (number is double)
                {
                    bits |= FIELD_IS_NUMERIC_DOUBLE;
                }
                else
                {
                    throw new ArgumentException("cannot store numeric type " + number.GetType());
                }
                str = null;
                bytes = null;
            }
            else
            {
                bytes = field.BinaryValue;
                if (bytes != null)
                {
                    bits |= FIELD_IS_BINARY;
                    str = null;
                }
                else
                {
                    str = field.StringValue;
                    if (str == null)
                    {
                        throw new ArgumentException("field " + field.Name + " is stored but does not have binaryValue, stringValue nor numericValue");
                    }
                }
            }

            fieldsStream.WriteByte((byte)bits);

            if (bytes != null)
            {
                fieldsStream.WriteVInt(bytes.length);
                fieldsStream.WriteBytes(bytes.bytes, bytes.offset, bytes.length);
            }
            else if (str != null)
            {
                fieldsStream.WriteString(field.StringValue);
            }
            else
            {
                if (number is byte || number is sbyte || number is short || number is int)
                {
                    fieldsStream.WriteInt((int)number);
                }
                else if (number is long)
                {
                    fieldsStream.WriteLong((long)number);
                }
                else if (number is float)
                {
                    fieldsStream.WriteInt(Number.FloatToIntBits((float)number));
                }
                else if (number is double)
                {
                    fieldsStream.WriteLong(BitConverter.DoubleToInt64Bits((double)number));
                }
                else
                {
                    throw new InvalidOperationException("Cannot get here");
                }
            }
        }

        public void AddRawDocuments(IndexInput stream, int[] lengths, int numDocs)
        {
            long position = fieldsStream.FilePointer;
            long start = position;
            for (int i = 0; i < numDocs; i++)
            {
                indexStream.WriteLong(position);
                position += lengths[i];
            }
            fieldsStream.CopyBytes(stream, position - start);
            //assert fieldsStream.getFilePointer() == position;
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (HEADER_LENGTH_IDX + ((long)numDocs) * 8 != indexStream.FilePointer)
                // This is most likely a bug in Sun JRE 1.6.0_04/_05;
                // we detect that the bug has struck, here, and
                // throw an exception to prevent the corruption from
                // entering the index.  See LUCENE-1282 for
                // details.
                throw new SystemException("fdx size mismatch: docCount is " + numDocs + " but fdx file size is " + indexStream.FilePointer + " file=" + indexStream.ToString() + "; now aborting this merge to prevent index corruption");
        }

        public override int Merge(MergeState mergeState)
        {
            int docCount = 0;
            // Used for bulk-reading raw bytes for stored fields
            int[] rawDocLengths = new int[MAX_RAW_MERGE_DOCS];
            int idx = 0;

            foreach (AtomicReader reader in mergeState.readers)
            {
                SegmentReader matchingSegmentReader = mergeState.matchingSegmentReaders[idx++];
                Lucene40StoredFieldsReader matchingFieldsReader = null;
                if (matchingSegmentReader != null)
                {
                    StoredFieldsReader fieldsReader = matchingSegmentReader.FieldsReader;
                    // we can only bulk-copy if the matching reader is also a Lucene40FieldsReader
                    if (fieldsReader != null && fieldsReader is Lucene40StoredFieldsReader)
                    {
                        matchingFieldsReader = (Lucene40StoredFieldsReader)fieldsReader;
                    }
                }

                if (reader.LiveDocs != null)
                {
                    docCount += CopyFieldsWithDeletions(mergeState,
                                                        reader, matchingFieldsReader, rawDocLengths);
                }
                else
                {
                    docCount += CopyFieldsNoDeletions(mergeState,
                                                      reader, matchingFieldsReader, rawDocLengths);
                }
            }
            Finish(mergeState.fieldInfos, docCount);
            return docCount;
        }

        private const int MAX_RAW_MERGE_DOCS = 4192;

        private int CopyFieldsWithDeletions(MergeState mergeState, AtomicReader reader,
                                      Lucene40StoredFieldsReader matchingFieldsReader, int[] rawDocLengths)
        {
            int docCount = 0;
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;
            //assert liveDocs != null;
            if (matchingFieldsReader != null)
            {
                // We can bulk-copy because the fieldInfos are "congruent"
                for (int j = 0; j < maxDoc; )
                {
                    if (!liveDocs[j])
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
                        if (j >= maxDoc) break;
                        if (!liveDocs[j])
                        {
                            j++;
                            break;
                        }
                    } while (numDocs < MAX_RAW_MERGE_DOCS);

                    IndexInput stream = matchingFieldsReader.RawDocs(rawDocLengths, start, numDocs);
                    AddRawDocuments(stream, rawDocLengths, numDocs);
                    docCount += numDocs;
                    mergeState.checkAbort.Work(300 * numDocs);
                }
            }
            else
            {
                for (int j = 0; j < maxDoc; j++)
                {
                    if (!liveDocs[j])
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
                    AddDocument(doc, mergeState.fieldInfos);
                    docCount++;
                    mergeState.checkAbort.Work(300);
                }
            }
            return docCount;
        }

        private int CopyFieldsNoDeletions(MergeState mergeState, AtomicReader reader,
                                   Lucene40StoredFieldsReader matchingFieldsReader, int[] rawDocLengths)
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
                    mergeState.checkAbort.Work(300 * len);
                }
            }
            else
            {
                for (; docCount < maxDoc; docCount++)
                {
                    // NOTE: it's very important to first assign to doc then pass it to
                    // fieldsWriter.addDocument; see LUCENE-1282
                    Document doc = reader.Document(docCount);
                    AddDocument(doc, mergeState.fieldInfos);
                    mergeState.checkAbort.Work(300);
                }
            }
            return docCount;
        }
    }
}
