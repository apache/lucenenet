using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    public class Lucene3xCodec : Codec
    {
        public Lucene3xCodec()
            : base("Lucene3x")
        {
        }

        private readonly PostingsFormat postingsFormat = new Lucene3xPostingsFormat();

        private readonly StoredFieldsFormat fieldsFormat = new Lucene3xStoredFieldsFormat();

        private readonly TermVectorsFormat vectorsFormat = new Lucene3xTermVectorsFormat();

        private readonly FieldInfosFormat fieldInfosFormat = new Lucene3xFieldInfosFormat();

        private readonly SegmentInfoFormat infosFormat = new Lucene3xSegmentInfoFormat();

        private readonly Lucene3xNormsFormat normsFormat = new Lucene3xNormsFormat();

        /** Extension of compound file for doc store files*/
        internal const String COMPOUND_FILE_STORE_EXTENSION = "cfx";

        // TODO: this should really be a different impl
        private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

        // 3.x doesn't support docvalues
        private readonly DocValuesFormat docValuesFormat = new AnonymousLucene3xDocValuesFormat();

        private sealed class AnonymousLucene3xDocValuesFormat : DocValuesFormat
        {
            public AnonymousLucene3xDocValuesFormat()
                : base("Lucene3x")
            {
            }

            public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
            {
                throw new NotSupportedException();
            }

            public override DocValuesProducer FieldsProducer(SegmentReadState state)
            {
                return null; // we have no docvalues, ever
            }
        }

        public override PostingsFormat PostingsFormat
        {
            get { return postingsFormat; }
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return docValuesFormat }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return fieldsFormat; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return vectorsFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return fieldInfosFormat; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return infosFormat; }
        }

        public override NormsFormat NormsFormat
        {
            get { return normsFormat; }
        }

        public override LiveDocsFormat LiveDocsFormat
        {
            get { return liveDocsFormat; }
        }

        public static ISet<String> GetDocStoreFiles(SegmentInfo info)
        {
            if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(info) != -1)
            {
                String dsName = Lucene3xSegmentInfoFormat.GetDocStoreSegment(info);
                ISet<String> files = new HashSet<String>();
                if (Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(info))
                {
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", COMPOUND_FILE_STORE_EXTENSION));
                }
                else
                {
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(dsName, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
                }
                return files;
            }
            else
            {
                return null;
            }
        }
    }
}
