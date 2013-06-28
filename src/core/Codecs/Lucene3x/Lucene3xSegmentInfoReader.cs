using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene3x
{
    [Obsolete]
    public class Lucene3xSegmentInfoReader : SegmentInfoReader
    {
        public static void ReadLegacyInfos(SegmentInfos infos, Directory directory, IndexInput input, int format)
        {
            infos.version = input.ReadLong(); // read version
            infos.counter = input.ReadInt(); // read counter
            Lucene3xSegmentInfoReader reader = new Lucene3xSegmentInfoReader();
            for (int i = input.ReadInt(); i > 0; i--)
            { // read segmentInfos
                SegmentInfoPerCommit siPerCommit = reader.ReadLegacySegmentInfo(directory, format, input);
                SegmentInfo si = siPerCommit.info;

                if (si.Version == null)
                {
                    // Could be a 3.0 - try to open the doc stores - if it fails, it's a
                    // 2.x segment, and an IndexFormatTooOldException will be thrown,
                    // which is what we want.
                    Directory dir = directory;
                    if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(si) != -1)
                    {
                        if (Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                        {
                            dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(
                                Lucene3xSegmentInfoFormat.GetDocStoreSegment(si), "",
                                Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), IOContext.READONCE, false);
                        }
                    }
                    else if (si.UseCompoundFile)
                    {
                        dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(
                            si.name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), IOContext.READONCE, false);
                    }

                    try
                    {
                        Lucene3xStoredFieldsReader.CheckCodeVersion(dir, Lucene3xSegmentInfoFormat.GetDocStoreSegment(si));
                    }
                    finally
                    {
                        // If we opened the directory, close it
                        if (dir != directory) dir.Dispose();
                    }

                    // Above call succeeded, so it's a 3.0 segment. Upgrade it so the next
                    // time the segment is read, its version won't be null and we won't
                    // need to open FieldsReader every time for each such segment.
                    si.Version = "3.0";
                }
                else if (si.Version.Equals("2.x"))
                {
                    // If it's a 3x index touched by 3.1+ code, then segments record their
                    // version, whether they are 2.x ones or not. We detect that and throw
                    // appropriate exception.
                    throw new IndexFormatTooOldException("segment " + si.name + " in resource " + input, si.Version);
                }
                infos.Add(siPerCommit);
            }

            infos.userData = input.ReadStringStringMap();
        }

        public override SegmentInfo Read(Directory directory, string segmentName, IOContext context)
        {
            // NOTE: this is NOT how 3.x is really written...
            String fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);

            bool success = false;

            IndexInput input = directory.OpenInput(fileName, context);

            try
            {
                SegmentInfo si = ReadUpgradedSegmentInfo(segmentName, directory, input);
                success = true;
                return si;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }

        private static void AddIfExists(Directory dir, ISet<String> files, String fileName)
        {
            if (dir.FileExists(fileName))
            {
                files.Add(fileName);
            }
        }

        private SegmentInfoPerCommit ReadLegacySegmentInfo(Directory dir, int format, IndexInput input)
        {
            // check that it is a format we can understand
            if (format > Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS)
            {
                throw new IndexFormatTooOldException(input, format,
                                                     Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
            }
            if (format < Lucene3xSegmentInfoFormat.FORMAT_3_1)
            {
                throw new IndexFormatTooNewException(input, format,
                                                     Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
            }
            String version;
            if (format <= Lucene3xSegmentInfoFormat.FORMAT_3_1)
            {
                version = input.ReadString();
            }
            else
            {
                version = null;
            }

            String name = input.ReadString();

            int docCount = input.ReadInt();
            long delGen = input.ReadLong();

            int docStoreOffset = input.ReadInt();
            IDictionary<String, String> attributes = new HashMap<String, String>();

            // parse the docstore stuff and shove it into attributes
            String docStoreSegment;
            bool docStoreIsCompoundFile;
            if (docStoreOffset != -1)
            {
                docStoreSegment = input.ReadString();
                docStoreIsCompoundFile = input.ReadByte() == SegmentInfo.YES;
                attributes[Lucene3xSegmentInfoFormat.DS_OFFSET_KEY] = docStoreOffset.ToString();
                attributes[Lucene3xSegmentInfoFormat.DS_NAME_KEY] = docStoreSegment;
                attributes[Lucene3xSegmentInfoFormat.DS_COMPOUND_KEY] = docStoreIsCompoundFile.ToString();
            }
            else
            {
                docStoreSegment = name;
                docStoreIsCompoundFile = false;
            }

            // pre-4.0 indexes write a byte if there is a single norms file
            byte b = input.ReadByte();

            //System.out.println("version=" + version + " name=" + name + " docCount=" + docCount + " delGen=" + delGen + " dso=" + docStoreOffset + " dss=" + docStoreSegment + " dssCFs=" + docStoreIsCompoundFile + " b=" + b + " format=" + format);

            //assert 1 == b : "expected 1 but was: "+ b + " format: " + format;
            int numNormGen = input.ReadInt();
            IDictionary<int, long> normGen;
            if (numNormGen == SegmentInfo.NO)
            {
                normGen = null;
            }
            else
            {
                normGen = new HashMap<int, long>();
                for (int j = 0; j < numNormGen; j++)
                {
                    normGen[j] = input.ReadLong();
                }
            }
            bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

            int delCount = input.ReadInt();
            //assert delCount <= docCount;

            bool hasProx = input.ReadByte() == 1;

            IDictionary<String, String> diagnostics = input.ReadStringStringMap();

            if (format <= Lucene3xSegmentInfoFormat.FORMAT_HAS_VECTORS)
            {
                // NOTE: unused
                int hasVectors = input.ReadByte();
            }

            // Replicate logic from 3.x's SegmentInfo.files():
            ISet<String> files = new HashSet<String>();
            if (isCompoundFile)
            {
                files.Add(IndexFileNames.SegmentFileName(name, "", IndexFileNames.COMPOUND_FILE_EXTENSION));
            }
            else
            {
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xFieldInfosReader.FIELD_INFOS_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.FREQ_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.PROX_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.TERMS_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xNormsProducer.NORMS_EXTENSION));
            }

            if (docStoreOffset != -1)
            {
                if (docStoreIsCompoundFile)
                {
                    files.Add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION));
                }
                else
                {
                    files.Add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
                    files.Add(IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
                    AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
                    AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
                    AddIfExists(dir, files, IndexFileNames.SegmentFileName(docStoreSegment, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
                }
            }
            else if (!isCompoundFile)
            {
                files.Add(IndexFileNames.SegmentFileName(name, "", Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
                files.Add(IndexFileNames.SegmentFileName(name, "", Lucene3xStoredFieldsReader.FIELDS_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION));
                AddIfExists(dir, files, IndexFileNames.SegmentFileName(name, "", Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION));
            }

            // parse the normgen stuff and shove it into attributes
            if (normGen != null)
            {
                attributes[Lucene3xSegmentInfoFormat.NORMGEN_KEY] = numNormGen.ToString();
                foreach (KeyValuePair<int, long> ent in normGen)
                {
                    long gen = ent.Value;
                    if (gen >= SegmentInfo.YES)
                    {
                        // Definitely a separate norm file, with generation:
                        files.Add(IndexFileNames.FileNameFromGeneration(name, "s" + ent.Key, gen));
                        attributes[Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + ent.Key] = gen.ToString();
                    }
                    else if (gen == SegmentInfo.NO)
                    {
                        // No separate norm
                    }
                    else
                    {
                        // We should have already hit indexformat too old exception
                        //assert false;
                    }
                }
            }

            SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile,
                                               null, diagnostics, attributes);
            info.Files = files;

            SegmentInfoPerCommit infoPerCommit = new SegmentInfoPerCommit(info, delCount, delGen);
            return infoPerCommit;
        }

        private SegmentInfo ReadUpgradedSegmentInfo(String name, Directory dir, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME,
                                         Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_START,
                                         Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
            String version = input.ReadString();

            int docCount = input.ReadInt();

            IDictionary<String, String> attributes = input.ReadStringStringMap();

            bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

            IDictionary<String, String> diagnostics = input.ReadStringStringMap();

            ISet<String> files = input.ReadStringSet();

            SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile,
                                               null, diagnostics, attributes);
            info.Files = files;
            return info;
        }
    }
}
