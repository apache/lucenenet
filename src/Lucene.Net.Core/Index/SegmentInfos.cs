using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Codecs;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.IO;
    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using Lucene3xSegmentInfoReader = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoReader;
    using NoSuchDirectoryException = Lucene.Net.Store.NoSuchDirectoryException;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// A collection of segmentInfo objects with methods for operating on
    /// those segments in relation to the file system.
    /// <p>
    /// The active segments in the index are stored in the segment info file,
    /// <tt>segments_N</tt>. There may be one or more <tt>segments_N</tt> files in the
    /// index; however, the one with the largest generation is the active one (when
    /// older segments_N files are present it's because they temporarily cannot be
    /// deleted, or, a writer is in the process of committing, or a custom
    /// <seealso cref="Lucene.Net.Index.IndexDeletionPolicy IndexDeletionPolicy"/>
    /// is in use). this file lists each segment by name and has details about the
    /// codec and generation of deletes.
    /// </p>
    /// <p>There is also a file <tt>segments.gen</tt>. this file contains
    /// the current generation (the <tt>_N</tt> in <tt>segments_N</tt>) of the index.
    /// this is used only as a fallback in case the current generation cannot be
    /// accurately determined by directory listing alone (as is the case for some NFS
    /// clients with time-based directory cache expiration). this file simply contains
    /// an <seealso cref="DataOutput#writeInt Int32"/> version header
    /// (<seealso cref="#FORMAT_SEGMENTS_GEN_CURRENT"/>), followed by the
    /// generation recorded as <seealso cref="DataOutput#writeLong Int64"/>, written twice.</p>
    /// <p>
    /// Files:
    /// <ul>
    ///   <li><tt>segments.gen</tt>: GenHeader, Generation, Generation, Footer
    ///   <li><tt>segments_N</tt>: Header, Version, NameCounter, SegCount,
    ///    &lt;SegName, SegCodec, DelGen, DeletionCount, FieldInfosGen, UpdatesFiles&gt;<sup>SegCount</sup>,
    ///    CommitUserData, Footer
    /// </ul>
    /// </p>
    /// Data types:
    /// <p>
    /// <ul>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>GenHeader, NameCounter, SegCount, DeletionCount --&gt; <seealso cref="DataOutput#writeInt Int32"/></li>
    ///   <li>Generation, Version, DelGen, Checksum, FieldInfosGen --&gt; <seealso cref="DataOutput#writeLong Int64"/></li>
    ///   <li>SegName, SegCodec --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>CommitUserData --&gt; <seealso cref="DataOutput#writeStringStringMap Map&lt;String,String&gt;"/></li>
    ///   <li>UpdatesFiles --&gt; <seealso cref="DataOutput#writeStringSet(Set) Set&lt;String&gt;"/></li>
    ///   <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    /// </ul>
    /// </p>
    /// Field Descriptions:
    /// <p>
    /// <ul>
    ///   <li>Version counts how often the index has been changed by adding or deleting
    ///       documents.</li>
    ///   <li>NameCounter is used to generate names for new segment files.</li>
    ///   <li>SegName is the name of the segment, and is used as the file name prefix for
    ///       all of the files that compose the segment's index.</li>
    ///   <li>DelGen is the generation count of the deletes file. If this is -1,
    ///       there are no deletes. Anything above zero means there are deletes
    ///       stored by <seealso cref="LiveDocsFormat"/>.</li>
    ///   <li>DeletionCount records the number of deleted documents in this segment.</li>
    ///   <li>SegCodec is the <seealso cref="Codec#getName() name"/> of the Codec that encoded
    ///       this segment.</li>
    ///   <li>CommitUserData stores an optional user-supplied opaque
    ///       Map&lt;String,String&gt; that was passed to
    ///       <seealso cref="IndexWriter#setCommitData(java.util.Map)"/>.</li>
    ///   <li>FieldInfosGen is the generation count of the fieldInfos file. If this is -1,
    ///       there are no updates to the fieldInfos in that segment. Anything above zero
    ///       means there are updates to fieldInfos stored by <seealso cref="FieldInfosFormat"/>.</li>
    ///   <li>UpdatesFiles stores the list of files that were updated in that segment.</li>
    /// </ul>
    /// </p>
    ///
    /// @lucene.experimental
    /// </summary>
    
    public sealed class SegmentInfos /*: List<SegmentCommitInfo>*/
    {
        /// <summary>
        /// The file format version for the segments_N codec header, up to 4.5. </summary>
        public const int VERSION_40 = 0;

        /// <summary>
        /// The file format version for the segments_N codec header, since 4.6+. </summary>
        public const int VERSION_46 = 1;

        /// <summary>
        /// The file format version for the segments_N codec header, since 4.8+ </summary>
        public const int VERSION_48 = 2;

        // Used for the segments.gen file only!
        // Whenever you add a new format, make it 1 smaller (negative version logic)!
        private const int FORMAT_SEGMENTS_GEN_47 = -2;

        private const int FORMAT_SEGMENTS_GEN_CHECKSUM = -3;
        private const int FORMAT_SEGMENTS_GEN_START = FORMAT_SEGMENTS_GEN_47;

        /// <summary>
        /// Current format of segments.gen </summary>
        public const int FORMAT_SEGMENTS_GEN_CURRENT = FORMAT_SEGMENTS_GEN_CHECKSUM;

        /// <summary>
        /// Used to name new segments. </summary>
        public int Counter;

        private long _generation; // generation of the "segments_N" for the next commit
        private long _lastGeneration; // generation of the "segments_N" file we last successfully read
        // or wrote; this is normally the same as generation except if
        // there was an IOException that had interrupted a commit

        /// <summary>
        /// Opaque Map&lt;String, String&gt; that user can specify during IndexWriter.commit </summary>
        private IDictionary<string, string> _userData = CollectionsHelper.EmptyMap<string, string>();

        private List<SegmentCommitInfo> segments = new List<SegmentCommitInfo>();

        /// <summary>
        /// If non-null, information about loading segments_N files </summary>
        /// will be printed here.  <seealso cref= #setInfoStream. </seealso>
        private static StreamWriter infoStream = null;

        /// <summary>
        /// Sole constructor. Typically you call this and then
        ///  use {@link #read(Directory) or
        ///  #read(Directory,String)} to populate each {@link
        ///  SegmentCommitInfo}.  Alternatively, you can add/remove your
        ///  own <seealso cref="SegmentCommitInfo"/>s.
        /// </summary>
        public SegmentInfos()
        {
        }

        /// <summary>
        /// Returns <seealso cref="SegmentCommitInfo"/> at the provided
        ///  index.
        /// </summary>
        public SegmentCommitInfo Info(int i)
        {
            return segments[i];
        }

        /// <summary>
        /// Get the generation of the most recent commit to the
        /// list of index files (N in the segments_N file).
        /// </summary>
        /// <param name="files"> -- array of file names to check </param>
        public static long GetLastCommitGeneration(string[] files)
        {
            if (files == null)
            {
                return -1;
            }
            long max = -1;
            foreach (var file in files)
            {
                if (file.StartsWith(IndexFileNames.SEGMENTS) && !file.Equals(IndexFileNames.SEGMENTS_GEN))
                {
                    long gen = GenerationFromSegmentsFileName(file);
                    if (gen > max)
                    {
                        max = gen;
                    }
                }
            }
            return max;
        }

        /// <summary>
        /// Get the generation of the most recent commit to the
        /// index in this directory (N in the segments_N file).
        /// </summary>
        /// <param name="directory"> -- directory to search for the latest segments_N file </param>
        public static long GetLastCommitGeneration(Directory directory)
        {
            try
            {
                return GetLastCommitGeneration(directory.ListAll());
            }
            catch (NoSuchDirectoryException)
            {
                return -1;
            }
        }

        /// <summary>
        /// Get the filename of the segments_N file for the most
        /// recent commit in the list of index files.
        /// </summary>
        /// <param name="files"> -- array of file names to check </param>

        public static string GetLastCommitSegmentsFileName(string[] files)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(files));
        }

        /// <summary>
        /// Get the filename of the segments_N file for the most
        /// recent commit to the index in this Directory.
        /// </summary>
        /// <param name="directory"> -- directory to search for the latest segments_N file </param>
        public static string GetLastCommitSegmentsFileName(Directory directory)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(directory));
        }

        /// <summary>
        /// Get the segments_N filename in use by this segment infos.
        /// </summary>
        public string SegmentsFileName
        {
            get
            {
                return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", _lastGeneration);
            }
        }

        /// <summary>
        /// Parse the generation off the segments file name and
        /// return it.
        /// </summary>
        public static long GenerationFromSegmentsFileName(string fileName)
        {
            if (fileName.Equals(IndexFileNames.SEGMENTS))
            {
                return 0;
            }
            else if (fileName.StartsWith(IndexFileNames.SEGMENTS))
            {
                return Convert.ToInt64(fileName.Substring(1 + IndexFileNames.SEGMENTS.Length));
            }
            else
            {
                throw new System.ArgumentException("fileName \"" + fileName + "\" is not a segments file");
            }
        }

        /// <summary>
        /// A utility for writing the <seealso cref="IndexFileNames#SEGMENTS_GEN"/> file to a
        /// <seealso cref="Directory"/>.
        ///
        /// <p>
        /// <b>NOTE:</b> this is an internal utility which is kept public so that it's
        /// accessible by code from other packages. You should avoid calling this
        /// method unless you're absolutely sure what you're doing!
        ///
        /// @lucene.internal
        /// </summary>
        public static void WriteSegmentsGen(Directory dir, long generation)
        {
            try
            {
                IndexOutput genOutput = dir.CreateOutput(IndexFileNames.SEGMENTS_GEN, IOContext.READONCE);
                try
                {
                    genOutput.WriteInt(FORMAT_SEGMENTS_GEN_CURRENT);
                    genOutput.WriteLong(generation);
                    genOutput.WriteLong(generation);
                    CodecUtil.WriteFooter(genOutput);
                }
                finally
                {
                    genOutput.Dispose();
                    dir.Sync(Collections.Singleton(IndexFileNames.SEGMENTS_GEN));
                }
            }
            catch (Exception)
            {
                // It's OK if we fail to write this file since it's
                // used only as one of the retry fallbacks.
                try
                {
                    dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                }
                catch (Exception)
                {
                    // Ignore; this file is only used in a retry
                    // fallback on init.
                }
            }
        }

        /// <summary>
        /// Get the next segments_N filename that will be written.
        /// </summary>
        public string NextSegmentFileName
        {
            get
            {
                long nextGeneration;

                if (_generation == -1)
                {
                    nextGeneration = 1;
                }
                else
                {
                    nextGeneration = _generation + 1;
                }
                return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", nextGeneration);
            }
        }

        /// <summary>
        /// Read a particular segmentFileName.  Note that this may
        /// throw an IOException if a commit is in process.
        /// </summary>
        /// <param name="directory"> -- directory containing the segments file </param>
        /// <param name="segmentFileName"> -- segment file to load </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public void Read(Directory directory, string segmentFileName)
        {
            var success = false;

            // Clear any previous segments:
            this.Clear();

            _generation = GenerationFromSegmentsFileName(segmentFileName);

            _lastGeneration = _generation;

            var input = directory.OpenChecksumInput(segmentFileName, IOContext.READ);
            try
            {
                int format = input.ReadInt();
                int actualFormat;
                if (format == CodecUtil.CODEC_MAGIC)
                {
                    // 4.0+
                    actualFormat = CodecUtil.CheckHeaderNoMagic(input, "segments", VERSION_40, VERSION_48);
                    Version = input.ReadLong();
                    Counter = input.ReadInt();
                    int numSegments = input.ReadInt();
                    if (numSegments < 0)
                    {
                        throw new CorruptIndexException("invalid segment count: " + numSegments + " (resource: " + input + ")");
                    }
                    for (var seg = 0; seg < numSegments; seg++)
                    {
                        var segName = input.ReadString();
                        var codec = Codec.ForName(input.ReadString());
                        //System.out.println("SIS.read seg=" + seg + " codec=" + codec);
                        var info = codec.SegmentInfoFormat().SegmentInfoReader.Read(directory, segName, IOContext.READ);
                        info.Codec = codec;
                        long delGen = input.ReadLong();
                        int delCount = input.ReadInt();
                        if (delCount < 0 || delCount > info.DocCount)
                        {
                            throw new CorruptIndexException("invalid deletion count: " + delCount + " vs docCount=" + info.DocCount + " (resource: " + input + ")");
                        }
                        long fieldInfosGen = -1;
                        if (actualFormat >= VERSION_46)
                        {
                            fieldInfosGen = input.ReadLong();
                        }
                        var siPerCommit = new SegmentCommitInfo(info, delCount, delGen, fieldInfosGen);
                        if (actualFormat >= VERSION_46)
                        {
                            int numGensUpdatesFiles = input.ReadInt();
                            IDictionary<long, ISet<string>> genUpdatesFiles;
                            if (numGensUpdatesFiles == 0)
                            {
                                genUpdatesFiles = CollectionsHelper.EmptyMap<long, ISet<string>>();
                            }
                            else
                            {
                                genUpdatesFiles = new Dictionary<long, ISet<string>>(numGensUpdatesFiles);
                                for (int i = 0; i < numGensUpdatesFiles; i++)
                                {
                                    genUpdatesFiles[input.ReadLong()] = input.ReadStringSet();
                                }
                            }
                            siPerCommit.GenUpdatesFiles = genUpdatesFiles;
                        }
                        Add(siPerCommit);
                    }
                    _userData = input.ReadStringStringMap();
                }
                else
                {
                    actualFormat = -1;
                    Lucene3xSegmentInfoReader.ReadLegacyInfos(this, directory, input, format);
                    Codec codec = Codec.ForName("Lucene3x");
                    foreach (SegmentCommitInfo info in segments)
                    {
                        info.Info.Codec = codec;
                    }
                }

                if (actualFormat >= VERSION_48)
                {
                    CodecUtil.CheckFooter(input);
                }
                else
                {
                    long checksumNow = input.Checksum;
                    long checksumThen = input.ReadLong();
                    if (checksumNow != checksumThen)
                    {
                        throw new CorruptIndexException("checksum mismatch in segments file (resource: " + input + ")");
                    }
                    CodecUtil.CheckEOF(input);
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    // Clear any segment infos we had loaded so we
                    // have a clean slate on retry:
                    this.Clear();
                    IOUtils.CloseWhileHandlingException(input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }

        /// <summary>
        /// Find the latest commit ({@code segments_N file}) and
        ///  load all <seealso cref="SegmentCommitInfo"/>s.
        /// </summary>
        public void Read(Directory directory)
        {
            _generation = _lastGeneration = -1;

            new FindSegmentsFileAnonymousInnerClassHelper(this, directory).Run();
        }

        private class FindSegmentsFileAnonymousInnerClassHelper : FindSegmentsFile
        {
            private readonly SegmentInfos OuterInstance;

            private new readonly Directory Directory;

            public FindSegmentsFileAnonymousInnerClassHelper(SegmentInfos outerInstance, Directory directory)
                : base(directory)
            {
                this.OuterInstance = outerInstance;
                this.Directory = directory;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                OuterInstance.Read(Directory, segmentFileName);
                return null;
            }
        }

        // Only non-null after prepareCommit has been called and
        // before finishCommit is called
        internal IndexOutput PendingSegnOutput;

        private const string SEGMENT_INFO_UPGRADE_CODEC = "SegmentInfo3xUpgrade";
        private const int SEGMENT_INFO_UPGRADE_VERSION = 0;

        private void Write(Directory directory)
        {
            string segmentsFileName = NextSegmentFileName;

            // Always advance the generation on write:
            if (_generation == -1)
            {
                _generation = 1;
            }
            else
            {
                _generation++;
            }

            IndexOutput segnOutput = null;
            bool success = false;

            var upgradedSIFiles = new HashSet<string>();

            try
            {
                segnOutput = directory.CreateOutput(segmentsFileName, IOContext.DEFAULT);
                CodecUtil.WriteHeader(segnOutput, "segments", VERSION_48);
                segnOutput.WriteLong(Version);
                segnOutput.WriteInt(Counter); // write counter
                segnOutput.WriteInt(Size()); // write infos
                foreach (SegmentCommitInfo siPerCommit in segments)
                {
                    SegmentInfo si = siPerCommit.Info;
                    segnOutput.WriteString(si.Name);
                    segnOutput.WriteString(si.Codec.Name);
                    segnOutput.WriteLong(siPerCommit.DelGen);
                    int delCount = siPerCommit.DelCount;
                    if (delCount < 0 || delCount > si.DocCount)
                    {
                        throw new InvalidOperationException("cannot write segment: invalid docCount segment=" + si.Name + " docCount=" + si.DocCount + " delCount=" + delCount);
                    }
                    segnOutput.WriteInt(delCount);
                    segnOutput.WriteLong(siPerCommit.FieldInfosGen);
                    IDictionary<long, ISet<string>> genUpdatesFiles = siPerCommit.UpdatesFiles;
                    segnOutput.WriteInt(genUpdatesFiles.Count);
                    foreach (KeyValuePair<long, ISet<string>> e in genUpdatesFiles)
                    {
                        segnOutput.WriteLong(e.Key);
                        segnOutput.WriteStringSet(e.Value);
                    }
                    Debug.Assert(si.Dir == directory);

                    // If this segment is pre-4.x, perform a one-time
                    // "ugprade" to write the .si file for it:
                    string version = si.Version;
                    if (version == null || StringHelper.VersionComparator.Compare(version, "4.0") < 0)
                    {
                        if (!SegmentWasUpgraded(directory, si))
                        {
                            string markerFileName = IndexFileNames.SegmentFileName(si.Name, "upgraded", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
                            si.AddFile(markerFileName);

                            string segmentFileName = Write3xInfo(directory, si, IOContext.DEFAULT);
                            upgradedSIFiles.Add(segmentFileName);
                            directory.Sync(/*Collections.singletonList(*/new[] { segmentFileName }/*)*/);

                            // Write separate marker file indicating upgrade
                            // is completed.  this way, if there is a JVM
                            // kill/crash, OS crash, power loss, etc. while
                            // writing the upgraded file, the marker file
                            // will be missing:
                            IndexOutput @out = directory.CreateOutput(markerFileName, IOContext.DEFAULT);
                            try
                            {
                                CodecUtil.WriteHeader(@out, SEGMENT_INFO_UPGRADE_CODEC, SEGMENT_INFO_UPGRADE_VERSION);
                            }
                            finally
                            {
                                @out.Dispose();
                            }
                            upgradedSIFiles.Add(markerFileName);
                            directory.Sync(/*Collections.SingletonList(*/new[] { markerFileName }/*)*/);
                        }
                    }
                }
                segnOutput.WriteStringStringMap(_userData);
                PendingSegnOutput = segnOutput;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    // We hit an exception above; try to close the file
                    // but suppress any exception:
                    IOUtils.CloseWhileHandlingException(segnOutput);

                    foreach (string fileName in upgradedSIFiles)
                    {
                        try
                        {
                            directory.DeleteFile(fileName);
                        }
                        catch (Exception)
                        {
                            // Suppress so we keep throwing the original exception
                        }
                    }

                    try
                    {
                        // Try not to leave a truncated segments_N file in
                        // the index:
                        directory.DeleteFile(segmentsFileName);
                    }
                    catch (Exception)
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }
        }

        private static bool SegmentWasUpgraded(Directory directory, SegmentInfo si)
        {
            // Check marker file:
            string markerFileName = IndexFileNames.SegmentFileName(si.Name, "upgraded", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
            IndexInput @in = null;
            try
            {
                @in = directory.OpenInput(markerFileName, IOContext.READONCE);
                if (CodecUtil.CheckHeader(@in, SEGMENT_INFO_UPGRADE_CODEC, SEGMENT_INFO_UPGRADE_VERSION, SEGMENT_INFO_UPGRADE_VERSION) == 0)
                {
                    return true;
                }
            }
            catch (IOException)
            {
                // Ignore: if something is wrong w/ the marker file,
                // we will just upgrade again
            }
            finally
            {
                if (@in != null)
                {
                    IOUtils.CloseWhileHandlingException(@in);
                }
            }
            return false;
        }

        [Obsolete]
        public static string Write3xInfo(Directory dir, SegmentInfo si, IOContext context)
        {
            // NOTE: this is NOT how 3.x is really written...
            string fileName = IndexFileNames.SegmentFileName(si.Name, "", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
            si.AddFile(fileName);

            //System.out.println("UPGRADE write " + fileName);
            bool success = false;
            IndexOutput output = dir.CreateOutput(fileName, context);
            try
            {
                // we are about to write this SI in 3.x format, dropping all codec information, etc.
                // so it had better be a 3.x segment or you will get very confusing errors later.
                if ((si.Codec is Lucene3xCodec) == false)
                {
                    throw new InvalidOperationException("cannot write 3x SegmentInfo unless codec is Lucene3x (got: " + si.Codec + ")");
                }

                CodecUtil.WriteHeader(output, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
                // Write the Lucene version that created this segment, since 3.1
                output.WriteString(si.Version);
                output.WriteInt(si.DocCount);

                output.WriteStringStringMap(si.Attributes());

                output.WriteByte((byte)(sbyte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo.NO));
                output.WriteStringStringMap(si.Diagnostics);
                output.WriteStringSet(si.Files);

                output.Dispose();

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(output);
                    try
                    {
                        si.Dir.DeleteFile(fileName);
                    }
                    catch (Exception)
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }

            return fileName;
        }

        /// <summary>
        /// Returns a copy of this instance, also copying each
        /// SegmentInfo.
        /// </summary>

        public object Clone()
        {
            var sis = (SegmentInfos)base.MemberwiseClone();
            // deep clone, first recreate all collections:
            sis.segments = new List<SegmentCommitInfo>(Size());
            foreach (SegmentCommitInfo info in segments)
            {
                Debug.Assert(info.Info.Codec != null);
                // dont directly access segments, use add method!!!
                sis.Add((SegmentCommitInfo)(info.Clone()));
            }
            sis._userData = new Dictionary<string, string>(_userData);
            return sis;
        }

        public List<SegmentCommitInfo> Segments
        {
            get { return segments; }
        }


        /// <summary>
        /// Counts how often the index has been changed.
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        /// Returns current generation. </summary>
        public long Generation
        {
            get
            {
                return _generation;
            }
        }

        /// <summary>
        /// Returns last succesfully read or written generation. </summary>
        public long LastGeneration
        {
            get
            {
                return _lastGeneration;
            }
        }

        /// <summary>
        /// If non-null, information about retries when loading
        /// the segments file will be printed to this.
        /// </summary>
        public static StreamWriter InfoStream
        {
            set
            {
                SegmentInfos.infoStream = value;
            }
            get
            {
                return infoStream;
            }
        }

        /* Advanced configuration of retry logic in loading
           segments_N file */
        private static int DefaultGenLookaheadCount_Renamed = 10;

        /// <summary>
        /// Advanced: set how many times to try incrementing the
        /// gen when loading the segments file.  this only runs if
        /// the primary (listing directory) and secondary (opening
        /// segments.gen file) methods fail to find the segments
        /// file.
        ///
        /// @lucene.experimental
        /// </summary>
        public static int DefaultGenLookaheadCount
        {
            set
            {
                DefaultGenLookaheadCount_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the {@code defaultGenLookaheadCount}.
        /// </summary>
        /// <seealso cref= #setDefaultGenLookaheadCount
        ///
        /// @lucene.experimental </seealso>
        public static int DefaultGenLookahedCount
        {
            get
            {
                return DefaultGenLookaheadCount_Renamed;
            }
        }

        /// <summary>
        /// Prints the given message to the infoStream. Note, this method does not
        /// check for null infoStream. It assumes this check has been performed by the
        /// caller, which is recommended to avoid the (usually) expensive message
        /// creation.
        /// </summary>
        private static void Message(string message)
        {
            infoStream.WriteLine("SIS [" + Thread.CurrentThread.Name + "]: " + message);
        }

        /// <summary>
        /// Utility class for executing code that needs to do
        /// something with the current segments file.  this is
        /// necessary with lock-less commits because from the time
        /// you locate the current segments file name, until you
        /// actually open it, read its contents, or check modified
        /// time, etc., it could have been deleted due to a writer
        /// commit finishing.
        /// </summary>
        public abstract class FindSegmentsFile
        {
            internal readonly Directory Directory;

            /// <summary>
            /// Sole constructor. </summary>
            protected FindSegmentsFile(Directory directory)
            {
                this.Directory = directory;
            }

            /// <summary>
            /// Locate the most recent {@code segments} file and
            ///  run <seealso cref="#doBody"/> on it.
            /// </summary>
            public virtual object Run()
            {
                return Run(null);
            }

            /// <summary>
            /// Run <seealso cref="#doBody"/> on the provided commit. </summary>
            public virtual object Run(IndexCommit commit)
            {
                if (commit != null)
                {
                    if (Directory != commit.Directory)
                    {
                        throw new IOException("the specified commit does not match the specified Directory");
                    }
                    return DoBody(commit.SegmentsFileName);
                }

                string segmentFileName = null;
                long lastGen = -1;
                long gen = 0;
                int genLookaheadCount = 0;
                IOException exc = null;
                int retryCount = 0;

                bool useFirstMethod = true;

                // Loop until we succeed in calling doBody() without
                // hitting an IOException.  An IOException most likely
                // means a commit was in process and has finished, in
                // the time it took us to load the now-old infos files
                // (and segments files).  It's also possible it's a
                // true error (corrupt index).  To distinguish these,
                // on each retry we must see "forward progress" on
                // which generation we are trying to load.  If we
                // don't, then the original error is real and we throw
                // it.

                // We have three methods for determining the current
                // generation.  We try the first two in parallel (when
                // useFirstMethod is true), and fall back to the third
                // when necessary.

                while (true)
                {
                    if (useFirstMethod)
                    {
                        // List the directory and use the highest
                        // segments_N file.  this method works well as long
                        // as there is no stale caching on the directory
                        // contents (NOTE: NFS clients often have such stale
                        // caching):
                        string[] files = null;

                        long genA = -1;

                        files = Directory.ListAll();

                        if (files != null)
                        {
                            genA = GetLastCommitGeneration(files);
                        }

                        if (infoStream != null)
                        {
                            Message("directory listing genA=" + genA);
                        }

                        // Also open segments.gen and read its
                        // contents.  Then we take the larger of the two
                        // gens.  this way, if either approach is hitting
                        // a stale cache (NFS) we have a better chance of
                        // getting the right generation.
                        long genB = -1;
                        ChecksumIndexInput genInput = null;
                        try
                        {
                            genInput = Directory.OpenChecksumInput(IndexFileNames.SEGMENTS_GEN, IOContext.READONCE);
                        }
                        catch (IOException e)
                        {
                            if (infoStream != null)
                            {
                                Message("segments.gen open: IOException " + e);
                            }
                        }

                        if (genInput != null)
                        {
                            try
                            {
                                int version = genInput.ReadInt();
                                if (version == FORMAT_SEGMENTS_GEN_47 || version == FORMAT_SEGMENTS_GEN_CHECKSUM)
                                {
                                    long gen0 = genInput.ReadLong();
                                    long gen1 = genInput.ReadLong();
                                    if (infoStream != null)
                                    {
                                        Message("fallback check: " + gen0 + "; " + gen1);
                                    }
                                    if (version == FORMAT_SEGMENTS_GEN_CHECKSUM)
                                    {
                                        CodecUtil.CheckFooter(genInput);
                                    }
                                    else
                                    {
                                        CodecUtil.CheckEOF(genInput);
                                    }
                                    if (gen0 == gen1)
                                    {
                                        // The file is consistent.
                                        genB = gen0;
                                    }
                                }
                                else
                                {
                                    throw new IndexFormatTooNewException(genInput, version, FORMAT_SEGMENTS_GEN_START, FORMAT_SEGMENTS_GEN_CURRENT);
                                }
                            }
                            catch (Exception err2)
                            {
                                // rethrow any format exception
                                if (err2 is CorruptIndexException)
                                {
                                    throw;
                                }
                            }
                            finally
                            {
                                genInput.Dispose();
                            }
                        }

                        if (infoStream != null)
                        {
                            Message(IndexFileNames.SEGMENTS_GEN + " check: genB=" + genB);
                        }

                        // Pick the larger of the two gen's:
                        gen = Math.Max(genA, genB);

                        if (gen == -1)
                        {
                            // Neither approach found a generation
                            throw new IndexNotFoundException("no segments* file found in " + Directory + ": files: " + Arrays.ToString(files));
                        }
                    }

                    if (useFirstMethod && lastGen == gen && retryCount >= 2)
                    {
                        // Give up on first method -- this is 3rd cycle on
                        // listing directory and checking gen file to
                        // attempt to locate the segments file.
                        useFirstMethod = false;
                    }

                    // Second method: since both directory cache and
                    // file contents cache seem to be stale, just
                    // advance the generation.
                    if (!useFirstMethod)
                    {
                        if (genLookaheadCount < DefaultGenLookaheadCount_Renamed)
                        {
                            gen++;
                            genLookaheadCount++;
                            if (infoStream != null)
                            {
                                Message("look ahead increment gen to " + gen);
                            }
                        }
                        else
                        {
                            // All attempts have failed -- throw first exc:
                            throw exc;
                        }
                    }
                    else if (lastGen == gen)
                    {
                        // this means we're about to try the same
                        // segments_N last tried.
                        retryCount++;
                    }
                    else
                    {
                        // Segment file has advanced since our last loop
                        // (we made "progress"), so reset retryCount:
                        retryCount = 0;
                    }

                    lastGen = gen;

                    segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen);

                    try
                    {
                        object v = DoBody(segmentFileName);
                        if (infoStream != null)
                        {
                            Message("success on " + segmentFileName);
                        }
                        return v;
                    }
                    catch (IOException err)
                    {
                        // Save the original root cause:
                        if (exc == null)
                        {
                            exc = err;
                        }

                        if (infoStream != null)
                        {
                            Message("primary Exception on '" + segmentFileName + "': " + err + "'; will retry: retryCount=" + retryCount + "; gen = " + gen);
                        }

                        if (gen > 1 && useFirstMethod && retryCount == 1)
                        {
                            // this is our second time trying this same segments
                            // file (because retryCount is 1), and, there is
                            // possibly a segments_(N-1) (because gen > 1).
                            // So, check if the segments_(N-1) exists and
                            // try it if so:
                            string prevSegmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", gen - 1);

                            bool prevExists;

                            try
                            {
                                Directory.OpenInput(prevSegmentFileName, IOContext.DEFAULT).Dispose();
                                prevExists = true;
                            }
                            catch (IOException ioe)
                            {
                                prevExists = false;
                            }

                            if (prevExists)
                            {
                                if (infoStream != null)
                                {
                                    Message("fallback to prior segment file '" + prevSegmentFileName + "'");
                                }
                                try
                                {
                                    object v = DoBody(prevSegmentFileName);
                                    if (infoStream != null)
                                    {
                                        Message("success on fallback " + prevSegmentFileName);
                                    }
                                    return v;
                                }
                                catch (IOException err2)
                                {
                                    if (infoStream != null)
                                    {
                                        Message("secondary Exception on '" + prevSegmentFileName + "': " + err2 + "'; will retry");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Subclass must implement this.  The assumption is an
            /// IOException will be thrown if something goes wrong
            /// during the processing that could have been caused by
            /// a writer committing.
            /// </summary>
            protected internal abstract object DoBody(string segmentFileName);
        }

        // Carry over generation numbers from another SegmentInfos
        internal void UpdateGeneration(SegmentInfos other)
        {
            _lastGeneration = other._lastGeneration;
            _generation = other._generation;
        }

        internal void RollbackCommit(Directory dir)
        {
            if (PendingSegnOutput != null)
            {
                // Suppress so we keep throwing the original exception
                // in our caller
                IOUtils.CloseWhileHandlingException(PendingSegnOutput);
                PendingSegnOutput = null;

                // Must carefully compute fileName from "generation"
                // since lastGeneration isn't incremented:
                string segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", _generation);
                // Suppress so we keep throwing the original exception
                // in our caller
                IOUtils.DeleteFilesIgnoringExceptions(dir, segmentFileName);
            }
        }

        /// <summary>
        /// Call this to start a commit.  this writes the new
        ///  segments file, but writes an invalid checksum at the
        ///  end, so that it is not visible to readers.  Once this
        ///  is called you must call <seealso cref="#finishCommit"/> to complete
        ///  the commit or <seealso cref="#rollbackCommit"/> to abort it.
        ///  <p>
        ///  Note: <seealso cref="#changed()"/> should be called prior to this
        ///  method if changes have been made to this <seealso cref="SegmentInfos"/> instance
        ///  </p>
        ///
        /// </summary>
        internal void PrepareCommit(Directory dir)
        {
            if (PendingSegnOutput != null)
            {
                throw new InvalidOperationException("prepareCommit was already called");
            }
            Write(dir);
        }

        /// <summary>
        /// Returns all file names referenced by SegmentInfo
        ///  instances matching the provided Directory (ie files
        ///  associated with any "external" segments are skipped).
        ///  The returned collection is recomputed on each
        ///  invocation.
        /// </summary>
        public ICollection<string> Files(Directory dir, bool includeSegmentsFile)
        {
            var files = new HashSet<string>();
            if (includeSegmentsFile)
            {
                string segmentFileName = SegmentsFileName;
                if (segmentFileName != null)
                {
                    files.Add(segmentFileName);
                }
            }
            var size = Size();
            for (int i = 0; i < size; i++)
            {
                var info = Info(i);
                Debug.Assert(info.Info.Dir == dir);
                if (info.Info.Dir == dir)
                {
                    files.UnionWith(info.Files());
                }
            }

            return files;
        }

        internal void FinishCommit(Directory dir)
        {
            if (PendingSegnOutput == null)
            {
                throw new InvalidOperationException("prepareCommit was not called");
            }
            bool success = false;
            try
            {
                CodecUtil.WriteFooter(PendingSegnOutput);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    // Closes pendingSegnOutput & deletes partial segments_N:
                    RollbackCommit(dir);
                }
                else
                {
                    success = false;
                    try
                    {
                        PendingSegnOutput.Dispose();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            // Closes pendingSegnOutput & deletes partial segments_N:
                            RollbackCommit(dir);
                        }
                        else
                        {
                            PendingSegnOutput = null;
                        }
                    }
                }
            }

            // NOTE: if we crash here, we have left a segments_N
            // file in the directory in a possibly corrupt state (if
            // some bytes made it to stable storage and others
            // didn't).  But, the segments_N file includes checksum
            // at the end, which should catch this case.  So when a
            // reader tries to read it, it will throw a
            // CorruptIndexException, which should cause the retry
            // logic in SegmentInfos to kick in and load the last
            // good (previous) segments_N-1 file.

            var fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", _generation);
            success = false;
            try
            {
                dir.Sync(Collections.Singleton(fileName));
                success = true;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        dir.DeleteFile(fileName);
                    }
                    catch (Exception)
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }

            _lastGeneration = _generation;
            WriteSegmentsGen(dir, _generation);
        }

        /// <summary>
        /// Writes & syncs to the Directory dir, taking care to
        ///  remove the segments file on exception
        ///  <p>
        ///  Note: <seealso cref="#changed()"/> should be called prior to this
        ///  method if changes have been made to this <seealso cref="SegmentInfos"/> instance
        ///  </p>
        ///
        /// </summary>
        internal void Commit(Directory dir)
        {
            PrepareCommit(dir);
            FinishCommit(dir);
        }

        /// <summary>
        /// Returns readable description of this segment. </summary>
        public string ToString(Directory directory)
        {
            var buffer = new StringBuilder();
            buffer.Append(SegmentsFileName).Append(": ");
            int count = Size();
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    buffer.Append(' ');
                }
                SegmentCommitInfo info = Info(i);
                buffer.Append(info.ToString(directory, 0));
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Return {@code userData} saved with this commit.
        /// </summary>
        /// <seealso cref= IndexWriter#commit() </seealso>
        public IDictionary<string, string> UserData
        {
            get
            {
                return _userData;
            }
            set
            {
                if (value == null)
                {
                    _userData = CollectionsHelper.EmptyMap<string, string>();
                }
                else
                {
                    _userData = value;
                }
            }
        }

        /// <summary>
        /// Replaces all segments in this instance, but keeps
        ///  generation, version, counter so that future commits
        ///  remain write once.
        /// </summary>
        internal void Replace(SegmentInfos other)
        {
            RollbackSegmentInfos(other.AsList());
            _lastGeneration = other._lastGeneration;
        }

        /// <summary>
        /// Returns sum of all segment's docCounts.  Note that
        ///  this does not include deletions
        /// </summary>
        public int TotalDocCount()
        {
            return segments.Sum(info => info.Info.DocCount);
        }

        /// <summary>
        /// Call this before committing if changes have been made to the
        ///  segments.
        /// </summary>
        public void Changed()
        {
            Version++;
        }

        /// <summary>
        /// applies all changes caused by committing a merge to this SegmentInfos </summary>
        internal void ApplyMergeChanges(MergePolicy.OneMerge merge, bool dropSegment)
        {
            var mergedAway = new HashSet<SegmentCommitInfo>(merge.Segments);
            bool inserted = false;
            int newSegIdx = 0;
            for (int segIdx = 0, cnt = segments.Count; segIdx < cnt; segIdx++)
            {
                Debug.Assert(segIdx >= newSegIdx);
                SegmentCommitInfo info = segments[segIdx];
                if (mergedAway.Contains(info))
                {
                    if (!inserted && !dropSegment)
                    {
                        segments[segIdx] = merge.info;
                        inserted = true;
                        newSegIdx++;
                    }
                }
                else
                {
                    segments[newSegIdx] = info;
                    newSegIdx++;
                }
            }

            // the rest of the segments in list are duplicates, so don't remove from map, only list!
            segments.SubList(newSegIdx, segments.Count).Clear();

            // Either we found place to insert segment, or, we did
            // not, but only because all segments we merged becamee
            // deleted while we are merging, in which case it should
            // be the case that the new segment is also all deleted,
            // we insert it at the beginning if it should not be dropped:
            if (!inserted && !dropSegment)
            {
                segments.Insert(0, merge.info);
            }
        }

        internal IList<SegmentCommitInfo> CreateBackupSegmentInfos()
        {
            var list = new List<SegmentCommitInfo>(Size());
            foreach (var info in segments)
            {
                Debug.Assert(info.Info.Codec != null);
                list.Add((SegmentCommitInfo)(info.Clone()));
            }
            return list;
        }

        internal void RollbackSegmentInfos(IList<SegmentCommitInfo> infos)
        {
            this.Clear();
            this.AddAll(infos);
        }

        /// <summary>
        /// Returns all contained segments as an <b>unmodifiable</b> <seealso cref="List"/> view. </summary>
        public IList<SegmentCommitInfo> AsList()
        {
            return Collections.UnmodifiableList<SegmentCommitInfo>(segments);
        }

        /// <summary>
        /// Returns number of <seealso cref="SegmentCommitInfo"/>s. </summary>
        public int Size()
        {
            return segments.Count;
        }

        /// <summary>
        /// Appends the provided <seealso cref="SegmentCommitInfo"/>. </summary>
        public void Add(SegmentCommitInfo si)
        {
            segments.Add(si);
        }

        /// <summary>
        /// Appends the provided <seealso cref="SegmentCommitInfo"/>s. </summary>
        public void AddAll(IEnumerable<SegmentCommitInfo> sis)
        {
            foreach (var si in sis)
            {
                this.Add(si);
            }
        }

        /// <summary>
        /// Clear all <seealso cref="SegmentCommitInfo"/>s. </summary>
        public void Clear()
        {
            segments.Clear();
        }

        /// <summary>
        /// Remove the provided <seealso cref="SegmentCommitInfo"/>.
        ///
        /// <p><b>WARNING</b>: O(N) cost
        /// </summary>
        public void Remove(SegmentCommitInfo si)
        {
            segments.Remove(si);
        }

        /// <summary>
        /// Remove the <seealso cref="SegmentCommitInfo"/> at the
        /// provided index.
        ///
        /// <p><b>WARNING</b>: O(N) cost
        /// </summary>
        internal void Remove(int index)
        {
            segments.RemoveAt(index);
        }

        /// <summary>
        /// Return true if the provided {@link
        ///  SegmentCommitInfo} is contained.
        ///
        /// <p><b>WARNING</b>: O(N) cost
        /// </summary>
        internal bool Contains(SegmentCommitInfo si)
        {
            return segments.Contains(si);
        }

        /// <summary>
        /// Returns index of the provided {@link
        ///  SegmentCommitInfo}.
        ///
        /// <p><b>WARNING</b>: O(N) cost
        /// </summary>
        internal int IndexOf(SegmentCommitInfo si)
        {
            return segments.IndexOf(si);
        }
    }
}