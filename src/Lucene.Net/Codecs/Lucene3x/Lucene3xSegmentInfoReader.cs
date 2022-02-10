using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using JCG = J2N.Collections.Generic;
using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
using Directory = Lucene.Net.Store.Directory;
using System.Runtime.CompilerServices;

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

    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexFormatTooNewException = Lucene.Net.Index.IndexFormatTooNewException;
    using IndexFormatTooOldException = Lucene.Net.Index.IndexFormatTooOldException;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentCommitInfo = Lucene.Net.Index.SegmentCommitInfo;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;

    /// <summary>
    /// Lucene 3x implementation of <see cref="SegmentInfoReader"/>.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("Only for reading existing 3.x indexes")]
    public class Lucene3xSegmentInfoReader : SegmentInfoReader
    {
        public static void ReadLegacyInfos(SegmentInfos infos, Directory directory, IndexInput input, int format)
        {
            infos.Version = input.ReadInt64(); // read version
            infos.Counter = input.ReadInt32(); // read counter
            Lucene3xSegmentInfoReader reader = new Lucene3xSegmentInfoReader();
            for (int i = input.ReadInt32(); i > 0; i--) // read segmentInfos
            {
                SegmentCommitInfo siPerCommit = reader.ReadLegacySegmentInfo(directory, format, input);
                SegmentInfo si = siPerCommit.Info;

                if (si.Version is null)
                {
                    // Could be a 3.0 - try to open the doc stores - if it fails, it's a
                    // 2.x segment, and an IndexFormatTooOldException will be thrown,
                    // which is what we want.
                    Directory dir = directory;
                    if (Lucene3xSegmentInfoFormat.GetDocStoreOffset(si) != -1)
                    {
                        if (Lucene3xSegmentInfoFormat.GetDocStoreIsCompoundFile(si))
                        {
                            dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(Lucene3xSegmentInfoFormat.GetDocStoreSegment(si), "", Lucene3xCodec.COMPOUND_FILE_STORE_EXTENSION), IOContext.READ_ONCE, false);
                        }
                    }
                    else if (si.UseCompoundFile)
                    {
                        dir = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.Name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), IOContext.READ_ONCE, false);
                    }

                    try
                    {
                        Lucene3xStoredFieldsReader.CheckCodeVersion(dir, Lucene3xSegmentInfoFormat.GetDocStoreSegment(si));
                    }
                    finally
                    {
                        // If we opened the directory, close it
                        if (dir != directory)
                        {
                            dir.Dispose();
                        }
                    }

                    // Above call succeeded, so it's a 3.0 segment. Upgrade it so the next
                    // time the segment is read, its version won't be null and we won't
                    // need to open FieldsReader every time for each such segment.
                    si.Version = "3.0";
                }
                else if (si.Version.Equals("2.x", StringComparison.Ordinal))
                {
                    // If it's a 3x index touched by 3.1+ code, then segments record their
                    // version, whether they are 2.x ones or not. We detect that and throw
                    // appropriate exception.
                    throw new IndexFormatTooOldException("segment " + si.Name + " in resource " + input, si.Version);
                }
                infos.Add(siPerCommit);
            }

            infos.UserData = input.ReadStringStringMap();
        }

        public override SegmentInfo Read(Directory directory, string segmentName, IOContext context)
        {
            // NOTE: this is NOT how 3.x is really written...
            string fileName = IndexFileNames.SegmentFileName(segmentName, "", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);

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
                    IOUtils.DisposeWhileHandlingException(input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddIfExists(Directory dir, ISet<string> files, string fileName)
        {
            if (dir.FileExists(fileName))
            {
                files.Add(fileName);
            }
        }

        /// <summary>
        /// Reads from legacy 3.x segments_N. </summary>
        private SegmentCommitInfo ReadLegacySegmentInfo(Directory dir, int format, IndexInput input)
        {
            // check that it is a format we can understand
            if (format > Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS)
            {
                throw new IndexFormatTooOldException(input, format, Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
            }
            if (format < Lucene3xSegmentInfoFormat.FORMAT_3_1)
            {
                throw new IndexFormatTooNewException(input, format, Lucene3xSegmentInfoFormat.FORMAT_DIAGNOSTICS, Lucene3xSegmentInfoFormat.FORMAT_3_1);
            }
            string version;
            if (format <= Lucene3xSegmentInfoFormat.FORMAT_3_1)
            {
                version = input.ReadString();
            }
            else
            {
                version = null;
            }

            string name = input.ReadString();

            int docCount = input.ReadInt32();
            long delGen = input.ReadInt64();

            int docStoreOffset = input.ReadInt32();
            IDictionary<string, string> attributes = new Dictionary<string, string>();

            // parse the docstore stuff and shove it into attributes
            string docStoreSegment;
            bool docStoreIsCompoundFile;
            if (docStoreOffset != -1)
            {
                docStoreSegment = input.ReadString();
                docStoreIsCompoundFile = input.ReadByte() == SegmentInfo.YES;
                attributes[Lucene3xSegmentInfoFormat.DS_OFFSET_KEY] = Convert.ToString(docStoreOffset, CultureInfo.InvariantCulture);
                attributes[Lucene3xSegmentInfoFormat.DS_NAME_KEY] = docStoreSegment;
                attributes[Lucene3xSegmentInfoFormat.DS_COMPOUND_KEY] = Convert.ToString(docStoreIsCompoundFile, CultureInfo.InvariantCulture);
            }
            else
            {
                docStoreSegment = name;
                docStoreIsCompoundFile = false;
            }

            // pre-4.0 indexes write a byte if there is a single norms file
            byte b = input.ReadByte();

            //System.out.println("version=" + version + " name=" + name + " docCount=" + docCount + " delGen=" + delGen + " dso=" + docStoreOffset + " dss=" + docStoreSegment + " dssCFs=" + docStoreIsCompoundFile + " b=" + b + " format=" + format);

            if (Debugging.AssertsEnabled) Debugging.Assert(1 == b,"expected 1 but was: {0} format: {1}", b, format);
            int numNormGen = input.ReadInt32();
            IDictionary<int, long> normGen;
            if (numNormGen == SegmentInfo.NO)
            {
                normGen = null;
            }
            else
            {
                normGen = new Dictionary<int, long>();
                for (int j = 0; j < numNormGen; j++)
                {
                    normGen[j] = input.ReadInt64();
                }
            }
            bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

            int delCount = input.ReadInt32();
            if (Debugging.AssertsEnabled) Debugging.Assert(delCount <= docCount);

            //bool hasProx = input.ReadByte() == 1;
            input.ReadByte(); // LUCENENET: IDE0059: Remove unnecessary value assignment

            IDictionary<string, string> diagnostics = input.ReadStringStringMap();

            if (format <= Lucene3xSegmentInfoFormat.FORMAT_HAS_VECTORS)
            {
                // NOTE: unused
                //int hasVectors = input.ReadByte();
                input.ReadByte(); // LUCENENET: IDE0059: Remove unnecessary value assignment
            }

            // Replicate logic from 3.x's SegmentInfo.files():
            ISet<string> files = new JCG.HashSet<string>();
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
                attributes[Lucene3xSegmentInfoFormat.NORMGEN_KEY] = Convert.ToString(numNormGen, CultureInfo.InvariantCulture);
                foreach (KeyValuePair<int, long> ent in normGen)
                {
                    long gen = ent.Value;
                    if (gen >= SegmentInfo.YES)
                    {
                        // Definitely a separate norm file, with generation:
                        files.Add(IndexFileNames.FileNameFromGeneration(name, "s" + ent.Key, gen));
                        attributes[Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + ent.Key] = Convert.ToString(gen, CultureInfo.InvariantCulture);
                    }
                    else if (gen == SegmentInfo.NO)
                    {
                        // No separate norm
                    }
                    else
                    {
                        // We should have already hit indexformat too old exception
                        if (Debugging.AssertsEnabled) Debugging.Assert(false);
                    }
                }
            }

            SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile, null, diagnostics, attributes.AsReadOnly());
            info.SetFiles(files);

            SegmentCommitInfo infoPerCommit = new SegmentCommitInfo(info, delCount, delGen, -1);
            return infoPerCommit;
        }

        private SegmentInfo ReadUpgradedSegmentInfo(string name, Directory dir, IndexInput input)
        {
            CodecUtil.CheckHeader(input, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_START, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
            string version = input.ReadString();

            int docCount = input.ReadInt32();

            IDictionary<string, string> attributes = input.ReadStringStringMap();

            bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;

            IDictionary<string, string> diagnostics = input.ReadStringStringMap();

            ISet<string> files = input.ReadStringSet();

            SegmentInfo info = new SegmentInfo(dir, version, name, docCount, isCompoundFile, null, diagnostics, attributes.AsReadOnly());
            info.SetFiles(files);
            return info;
        }
    }
}