using J2N;
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;
using Long = J2N.Numerics.Int64;

namespace Lucene.Net.Index
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

    using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
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
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// A collection of segmentInfo objects with methods for operating on
    /// those segments in relation to the file system.
    /// <para>
    /// The active segments in the index are stored in the segment info file,
    /// <c>segments_N</c>. There may be one or more <c>segments_N</c> files in the
    /// index; however, the one with the largest generation is the active one (when
    /// older segments_N files are present it's because they temporarily cannot be
    /// deleted, or, a writer is in the process of committing, or a custom
    /// <see cref="Lucene.Net.Index.IndexDeletionPolicy"/>
    /// is in use). This file lists each segment by name and has details about the
    /// codec and generation of deletes.
    /// </para>
    /// <para>There is also a file <c>segments.gen</c>. this file contains
    /// the current generation (the <c>_N</c> in <c>segments_N</c>) of the index.
    /// This is used only as a fallback in case the current generation cannot be
    /// accurately determined by directory listing alone (as is the case for some NFS
    /// clients with time-based directory cache expiration). This file simply contains
    /// an <see cref="Store.DataOutput.WriteInt32(int)"/> version header
    /// (<see cref="FORMAT_SEGMENTS_GEN_CURRENT"/>), followed by the
    /// generation recorded as <see cref="Store.DataOutput.WriteInt64(long)"/>, written twice.</para>
    /// <para>
    /// Files:
    /// <list type="bullet">
    ///   <item><description><c>segments.gen</c>: GenHeader, Generation, Generation, Footer</description></item>
    ///   <item><description><c>segments_N</c>: Header, Version, NameCounter, SegCount,
    ///    &lt;SegName, SegCodec, DelGen, DeletionCount, FieldInfosGen, UpdatesFiles&gt;<sup>SegCount</sup>,
    ///    CommitUserData, Footer</description></item>
    /// </list>
    /// </para>
    /// Data types:
    /// <para>
    /// <list type="bullet">
    ///   <item><description>Header --&gt; <see cref="CodecUtil.WriteHeader(Store.DataOutput, string, int)"/></description></item>
    ///   <item><description>GenHeader, NameCounter, SegCount, DeletionCount --&gt; <see cref="Store.DataOutput.WriteInt32(int)"/></description></item>
    ///   <item><description>Generation, Version, DelGen, Checksum, FieldInfosGen --&gt; <see cref="Store.DataOutput.WriteInt64(long)"/></description></item>
    ///   <item><description>SegName, SegCodec --&gt; <see cref="Store.DataOutput.WriteString(string)"/></description></item>
    ///   <item><description>CommitUserData --&gt; <see cref="Store.DataOutput.WriteStringStringMap(IDictionary{string, string})"/></description></item>
    ///   <item><description>UpdatesFiles --&gt; <see cref="Store.DataOutput.WriteStringSet(ISet{string})"/></description></item>
    ///   <item><description>Footer --&gt; <see cref="CodecUtil.WriteFooter(IndexOutput)"/></description></item>
    /// </list>
    /// </para>
    /// Field Descriptions:
    /// <para>
    /// <list type="bullet">
    ///   <item><description>Version counts how often the index has been changed by adding or deleting
    ///       documents.</description></item>
    ///   <item><description>NameCounter is used to generate names for new segment files.</description></item>
    ///   <item><description>SegName is the name of the segment, and is used as the file name prefix for
    ///       all of the files that compose the segment's index.</description></item>
    ///   <item><description>DelGen is the generation count of the deletes file. If this is -1,
    ///       there are no deletes. Anything above zero means there are deletes
    ///       stored by <see cref="Codecs.LiveDocsFormat"/>.</description></item>
    ///   <item><description>DeletionCount records the number of deleted documents in this segment.</description></item>
    ///   <item><description>SegCodec is the <see cref="Codec.Name"/> of the <see cref="Codec"/> that encoded
    ///       this segment.</description></item>
    ///   <item><description>CommitUserData stores an optional user-supplied opaque
    ///       <see cref="T:IDictionary{string, string}"/> that was passed to
    ///       <see cref="IndexWriter.SetCommitData(IDictionary{string, string})"/>.</description></item>
    ///   <item><description>FieldInfosGen is the generation count of the fieldInfos file. If this is -1,
    ///       there are no updates to the fieldInfos in that segment. Anything above zero
    ///       means there are updates to fieldInfos stored by <see cref="Codecs.FieldInfosFormat"/>.</description></item>
    ///   <item><description>UpdatesFiles stores the list of files that were updated in that segment.</description></item>
    /// </list>
    /// </para>
    ///
    /// @lucene.experimental
    /// </summary>

    public sealed class SegmentInfos : IEnumerable<SegmentCommitInfo> // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// The file format version for the segments_N codec header, up to 4.5. </summary>
        public static readonly int VERSION_40 = 0;

        /// <summary>
        /// The file format version for the segments_N codec header, since 4.6+. </summary>
        public static readonly int VERSION_46 = 1;

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
        public static readonly int FORMAT_SEGMENTS_GEN_CURRENT = FORMAT_SEGMENTS_GEN_CHECKSUM;

        /// <summary>
        /// Setting this to true will generate the same file names that were used in 4.8.0-beta00001 through 4.8.0-beta00015.
        /// When writing more than 10 segments, these segment names were incompatible with prior versions of Lucene.NET and incompatible with Lucene 4.8.0.
        /// <para/>
        /// This is only for reading codecs from the affected 4.8.0 beta versions, it is not recommended to use this setting for general use.
        /// <para/>
        /// This must be set prior to opening an index at application startup. When setting it at other times the behavior is undefined.
        /// <para/>
        /// Note that this property can also be set using the "useLegacySegmentNames" system property to "true" (such as setting the environment variable "lucene:useLegacySegmentNames").
        /// System properties can also be injected by supplying a <see cref="Configuration.IConfigurationFactory"/> at application startup
        /// through <see cref="Configuration.ConfigurationSettings.SetConfigurationFactory(Configuration.IConfigurationFactory)"/>.
        /// </summary>
        public static bool UseLegacySegmentNames
        {
            get => useLegacySegmentNames;
            set => useLegacySegmentNames = value;
        }
        internal static bool useLegacySegmentNames = Util.SystemProperties.GetPropertyAsBoolean("useLegacySegmentNames", defaultValue: false);

        /// <summary>
        /// Optimized version of <see cref="J2N.IntegralNumberExtensions.ToString(long, int)"/> with a radix of 36, that
        /// simply does a switch case for the first 100 numbers, which takes only 5% of the time as calculating it.
        /// We fall back to calling the method after 100 segments.
        /// <para/>
        /// This also implements the switch for <see cref="UseLegacySegmentNames"/> so it doesn't have to be dealt with externally.
        /// </summary>
        internal static string SegmentNumberToString(long segment, bool allowLegacyNames = true)
        {
            switch (segment)
            {
                case 0: return "0";
                case 1: return "1";
                case 2: return "2";
                case 3: return "3";
                case 4: return "4";
                case 5: return "5";
                case 6: return "6";
                case 7: return "7";
                case 8: return "8";
                case 9: return "9";
            }

            if (!allowLegacyNames || !useLegacySegmentNames)
            {
                return segment switch
                {
                    10 => "a",
                    11 => "b",
                    12 => "c",
                    13 => "d",
                    14 => "e",
                    15 => "f",
                    16 => "g",
                    17 => "h",
                    18 => "i",
                    19 => "j",
                    20 => "k",
                    21 => "l",
                    22 => "m",
                    23 => "n",
                    24 => "o",
                    25 => "p",
                    26 => "q",
                    27 => "r",
                    28 => "s",
                    29 => "t",
                    30 => "u",
                    31 => "v",
                    32 => "w",
                    33 => "x",
                    34 => "y",
                    35 => "z",
                    36 => "10",
                    37 => "11",
                    38 => "12",
                    39 => "13",
                    40 => "14",
                    41 => "15",
                    42 => "16",
                    43 => "17",
                    44 => "18",
                    45 => "19",
                    46 => "1a",
                    47 => "1b",
                    48 => "1c",
                    49 => "1d",
                    50 => "1e",
                    51 => "1f",
                    52 => "1g",
                    53 => "1h",
                    54 => "1i",
                    55 => "1j",
                    56 => "1k",
                    57 => "1l",
                    58 => "1m",
                    59 => "1n",
                    60 => "1o",
                    61 => "1p",
                    62 => "1q",
                    63 => "1r",
                    64 => "1s",
                    65 => "1t",
                    66 => "1u",
                    67 => "1v",
                    68 => "1w",
                    69 => "1x",
                    70 => "1y",
                    71 => "1z",
                    72 => "20",
                    73 => "21",
                    74 => "22",
                    75 => "23",
                    76 => "24",
                    77 => "25",
                    78 => "26",
                    79 => "27",
                    80 => "28",
                    81 => "29",
                    82 => "2a",
                    83 => "2b",
                    84 => "2c",
                    85 => "2d",
                    86 => "2e",
                    87 => "2f",
                    88 => "2g",
                    89 => "2h",
                    90 => "2i",
                    91 => "2j",
                    92 => "2k",
                    93 => "2l",
                    94 => "2m",
                    95 => "2n",
                    96 => "2o",
                    97 => "2p",
                    98 => "2q",
                    99 => "2r",
                    _ => segment.ToString(Character.MaxRadix),
                };
            }

            // This is wrong! Unfortunately, this is how the segment names were generated in
            // beta 1 thru 15, so we end up here if the switch is enabled to read them.
            // We should actually be using a radix of 36 rather than 10 (which was done correctly in Lucene.NET 3.0.3).
            return segment.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Used to name new segments. </summary>
        public int Counter { get; set; }

        // LUCENENET specific: Version made into property (see below)

        private long generation; // generation of the "segments_N" for the next commit
        private long lastGeneration; // generation of the "segments_N" file we last successfully read
        // or wrote; this is normally the same as generation except if
        // there was an IOException that had interrupted a commit

        /// <summary>
        /// Opaque <see cref="T:IDictionary{string, string}"/> that user can specify during <see cref="IndexWriter.Commit()"/> </summary>
        private IDictionary<string, string> userData = Collections.EmptyMap<string, string>();

        private JCG.List<SegmentCommitInfo> segments = new JCG.List<SegmentCommitInfo>();

        /// <summary>
        /// If non-null, information about loading segments_N files 
        /// will be printed here.</summary> 
        /// <seealso cref="InfoStream"/>
        private static TextWriter infoStream = null;

        /// <summary>
        /// Sole constructor. Typically you call this and then
        /// use <see cref="Read(Directory)"/> or
        /// <see cref="Read(Directory, string)"/> to populate each
        /// <see cref="SegmentCommitInfo"/>.  Alternatively, you can add/remove your
        /// own <see cref="SegmentCommitInfo"/>s.
        /// </summary>
        public SegmentInfos()
        {
        }

        [Obsolete("Use indexer instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public SegmentCommitInfo Info(int i)
        {
            return segments[i];
        }

        /// <summary>
        /// Returns <see cref="SegmentCommitInfo"/> at the provided
        /// index.
        /// <para/>
        /// This was info(int) in Lucene.
        /// </summary>
        public SegmentCommitInfo this[int index] => segments[index];

        /// <summary>
        /// Get the generation of the most recent commit to the
        /// list of index files (N in the segments_N file).
        /// </summary>
        /// <param name="files"> array of file names to check </param>
        public static long GetLastCommitGeneration(string[] files)
        {
            if (files is null)
            {
                return -1;
            }
            long max = -1;
            foreach (var file in files)
            {
                if (file.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal) && !file.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal))
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
        /// <param name="directory"> directory to search for the latest segments_N file </param>
        public static long GetLastCommitGeneration(Directory directory)
        {
            try
            {
                return GetLastCommitGeneration(directory.ListAll());
            }
            catch (Exception nsde) when (nsde.IsNoSuchDirectoryException())
            {
                return -1;
            }
        }

        /// <summary>
        /// Get the filename of the segments_N file for the most
        /// recent commit in the list of index files.
        /// </summary>
        /// <param name="files"> array of file names to check </param>

        public static string GetLastCommitSegmentsFileName(string[] files)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(files));
        }

        /// <summary>
        /// Get the filename of the segments_N file for the most
        /// recent commit to the index in this Directory.
        /// </summary>
        /// <param name="directory"> directory to search for the latest segments_N file </param>
        public static string GetLastCommitSegmentsFileName(Directory directory)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(directory));
        }

        /// <summary>
        /// Get the segments_N filename in use by this segment infos.
        /// </summary>
        public string GetSegmentsFileName()
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", lastGeneration);
        }

        /// <summary>
        /// Parse the generation off the segments file name and
        /// return it.
        /// </summary>
        public static long GenerationFromSegmentsFileName(string fileName)
        {
            if (fileName.Equals(IndexFileNames.SEGMENTS, StringComparison.Ordinal))
            {
                return 0;
            }
            else if (fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal))
            {
                // LUCENENET: Optimized parse so we don't allocate a substring
                return Long.Parse(fileName, 1 + IndexFileNames.SEGMENTS.Length, fileName.Length - (1 + IndexFileNames.SEGMENTS.Length), Character.MaxRadix);
            }
            else
            {
                throw new ArgumentException("fileName \"" + fileName + "\" is not a segments file");
            }
        }

        /// <summary>
        /// A utility for writing the <see cref="IndexFileNames.SEGMENTS_GEN"/> file to a
        /// <see cref="Directory"/>.
        /// <para/>
        /// <b>NOTE:</b> this is an internal utility which is kept public so that it's
        /// accessible by code from other packages. You should avoid calling this
        /// method unless you're absolutely sure what you're doing!
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static void WriteSegmentsGen(Directory dir, long generation)
        {
            try
            {
                IndexOutput genOutput = dir.CreateOutput(IndexFileNames.SEGMENTS_GEN, IOContext.READ_ONCE);
                try
                {
                    genOutput.WriteInt32(FORMAT_SEGMENTS_GEN_CURRENT);
                    genOutput.WriteInt64(generation);
                    genOutput.WriteInt64(generation);
                    CodecUtil.WriteFooter(genOutput);
                }
                finally
                {
                    genOutput.Dispose();
                    dir.Sync(new JCG.HashSet<string> { IndexFileNames.SEGMENTS_GEN });
                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                // It's OK if we fail to write this file since it's
                // used only as one of the retry fallbacks.
                try
                {
                    dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                }
                catch (Exception t2) when (t2.IsThrowable())
                {
                    // Ignore; this file is only used in a retry
                    // fallback on init.
                }
            }
        }

        /// <summary>
        /// Get the next segments_N filename that will be written.
        /// </summary>
        public string GetNextSegmentFileName()
        {
            long nextGeneration;

            if (generation == -1)
            {
                nextGeneration = 1;
            }
            else
            {
                nextGeneration = generation + 1;
            }
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", nextGeneration);
        }

        /// <summary>
        /// Read a particular <paramref name="segmentFileName"/>.  Note that this may
        /// throw an <see cref="IOException"/> if a commit is in process.
        /// </summary>
        /// <param name="directory"> directory containing the segments file </param>
        /// <param name="segmentFileName"> segment file to load </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Read(Directory directory, string segmentFileName)
        {
            var success = false;

            // Clear any previous segments:
            this.Clear();

            generation = GenerationFromSegmentsFileName(segmentFileName);

            lastGeneration = generation;

            var input = directory.OpenChecksumInput(segmentFileName, IOContext.READ);
            try
            {
                int format = input.ReadInt32();
                int actualFormat;
                if (format == CodecUtil.CODEC_MAGIC)
                {
                    // 4.0+
                    actualFormat = CodecUtil.CheckHeaderNoMagic(input, "segments", VERSION_40, VERSION_48);
                    Version = input.ReadInt64();
                    Counter = input.ReadInt32();
                    int numSegments = input.ReadInt32();
                    if (numSegments < 0)
                    {
                        throw new CorruptIndexException("invalid segment count: " + numSegments + " (resource: " + input + ")");
                    }
                    for (var seg = 0; seg < numSegments; seg++)
                    {
                        var segName = input.ReadString();
                        var codec = Codec.ForName(input.ReadString());
                        //System.out.println("SIS.read seg=" + seg + " codec=" + codec);
                        var info = codec.SegmentInfoFormat.SegmentInfoReader.Read(directory, segName, IOContext.READ);
                        info.Codec = codec;
                        long delGen = input.ReadInt64();
                        int delCount = input.ReadInt32();
                        if (delCount < 0 || delCount > info.DocCount)
                        {
                            throw new CorruptIndexException("invalid deletion count: " + delCount + " vs docCount=" + info.DocCount + " (resource: " + input + ")");
                        }
                        long fieldInfosGen = -1;
                        if (actualFormat >= VERSION_46)
                        {
                            fieldInfosGen = input.ReadInt64();
                        }
                        var siPerCommit = new SegmentCommitInfo(info, delCount, delGen, fieldInfosGen);
                        if (actualFormat >= VERSION_46)
                        {
                            int numGensUpdatesFiles = input.ReadInt32();
                            IDictionary<long, ISet<string>> genUpdatesFiles;
                            if (numGensUpdatesFiles == 0)
                            {
                                genUpdatesFiles = Collections.EmptyMap<long, ISet<string>>();
                            }
                            else
                            {
                                genUpdatesFiles = new Dictionary<long, ISet<string>>(numGensUpdatesFiles);
                                for (int i = 0; i < numGensUpdatesFiles; i++)
                                {
                                    genUpdatesFiles[input.ReadInt64()] = input.ReadStringSet();
                                }
                            }
                            siPerCommit.SetGenUpdatesFiles(genUpdatesFiles);
                        }
                        Add(siPerCommit);
                    }
                    userData = input.ReadStringStringMap();
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
                    long checksumThen = input.ReadInt64();
                    if (checksumNow != checksumThen)
                    {
                        throw new CorruptIndexException("checksum mismatch in segments file (resource: " + input + ")");
                    }
#pragma warning disable 612, 618
                    CodecUtil.CheckEOF(input);
#pragma warning restore 612, 618
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
                    IOUtils.DisposeWhileHandlingException(input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }

        /// <summary>
        /// Find the latest commit (<c>segments_N file</c>) and
        /// load all <see cref="SegmentCommitInfo"/>s.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Read(Directory directory)
        {
            generation = lastGeneration = -1;

            new FindSegmentsFileAnonymousClass(this, directory).Run();
        }

        private sealed class FindSegmentsFileAnonymousClass : FindSegmentsFile
        {
            private readonly SegmentInfos outerInstance;

            public FindSegmentsFileAnonymousClass(SegmentInfos outerInstance, Directory directory)
                : base(directory)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override object DoBody(string segmentFileName)
            {
                outerInstance.Read(directory, segmentFileName);
                return null;
            }
        }

        // Only non-null after prepareCommit has been called and
        // before finishCommit is called
        internal IndexOutput pendingSegnOutput;

        private const string SEGMENT_INFO_UPGRADE_CODEC = "SegmentInfo3xUpgrade";
        private const int SEGMENT_INFO_UPGRADE_VERSION = 0;

        private void Write(Directory directory)
        {
            string segmentsFileName = GetNextSegmentFileName();

            // Always advance the generation on write:
            if (generation == -1)
            {
                generation = 1;
            }
            else
            {
                generation++;
            }

            IndexOutput segnOutput = null;
            bool success = false;

            var upgradedSIFiles = new JCG.HashSet<string>();

            try
            {
                segnOutput = directory.CreateOutput(segmentsFileName, IOContext.DEFAULT);
                CodecUtil.WriteHeader(segnOutput, "segments", VERSION_48);
                segnOutput.WriteInt64(Version);
                segnOutput.WriteInt32(Counter); // write counter
                segnOutput.WriteInt32(Count); // write infos
                foreach (SegmentCommitInfo siPerCommit in segments)
                {
                    SegmentInfo si = siPerCommit.Info;
                    segnOutput.WriteString(si.Name);
                    segnOutput.WriteString(si.Codec.Name);
                    segnOutput.WriteInt64(siPerCommit.DelGen);
                    int delCount = siPerCommit.DelCount;
                    if (delCount < 0 || delCount > si.DocCount)
                    {
                        throw IllegalStateException.Create("cannot write segment: invalid docCount segment=" + si.Name + " docCount=" + si.DocCount + " delCount=" + delCount);
                    }
                    segnOutput.WriteInt32(delCount);
                    segnOutput.WriteInt64(siPerCommit.FieldInfosGen);
                    IDictionary<long, ISet<string>> genUpdatesFiles = siPerCommit.UpdatesFiles;
                    segnOutput.WriteInt32(genUpdatesFiles.Count);
                    foreach (KeyValuePair<long, ISet<string>> e in genUpdatesFiles)
                    {
                        segnOutput.WriteInt64(e.Key);
                        segnOutput.WriteStringSet(e.Value);
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(si.Dir == directory);

                    // If this segment is pre-4.x, perform a one-time
                    // "ugprade" to write the .si file for it:
                    string version = si.Version;
                    if (version is null || StringHelper.VersionComparer.Compare(version, "4.0") < 0)
                    {
                        if (!SegmentWasUpgraded(directory, si))
                        {
                            string markerFileName = IndexFileNames.SegmentFileName(si.Name, "upgraded", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
                            si.AddFile(markerFileName);

#pragma warning disable 612, 618
                            string segmentFileName = Write3xInfo(directory, si, IOContext.DEFAULT);
#pragma warning restore 612, 618
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
                segnOutput.WriteStringStringMap(userData);
                pendingSegnOutput = segnOutput;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    // We hit an exception above; try to close the file
                    // but suppress any exception:
                    IOUtils.DisposeWhileHandlingException(segnOutput);

                    foreach (string fileName in upgradedSIFiles)
                    {
                        try
                        {
                            directory.DeleteFile(fileName);
                        }
                        catch (Exception t) when (t.IsThrowable())
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
                    catch (Exception t) when (t.IsThrowable())
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
                @in = directory.OpenInput(markerFileName, IOContext.READ_ONCE);
                if (CodecUtil.CheckHeader(@in, SEGMENT_INFO_UPGRADE_CODEC, SEGMENT_INFO_UPGRADE_VERSION, SEGMENT_INFO_UPGRADE_VERSION) == 0)
                {
                    return true;
                }
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // Ignore: if something is wrong w/ the marker file,
                // we will just upgrade again
            }
            finally
            {
                if (@in != null)
                {
                    IOUtils.DisposeWhileHandlingException(@in);
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
                    throw IllegalStateException.Create("cannot write 3x SegmentInfo unless codec is Lucene3x (got: " + si.Codec + ")");
                }

                CodecUtil.WriteHeader(output, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME, Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
                // Write the Lucene version that created this segment, since 3.1
                output.WriteString(si.Version);
                output.WriteInt32(si.DocCount);

                output.WriteStringStringMap(si.Attributes);

                output.WriteByte((byte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo.NO));
                output.WriteStringStringMap(si.Diagnostics);
                output.WriteStringSet(si.GetFiles());

                output.Dispose();

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);
                    try
                    {
                        si.Dir.DeleteFile(fileName);
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }

            return fileName;
        }

        /// <summary>
        /// Returns a copy of this instance, also copying each
        /// <see cref="SegmentInfo"/>.
        /// </summary>
        public object Clone()
        {
            var sis = (SegmentInfos)base.MemberwiseClone();
            // deep clone, first recreate all collections:
            sis.segments = new JCG.List<SegmentCommitInfo>(Count);
            foreach (SegmentCommitInfo info in segments)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(info.Info.Codec != null);
                // dont directly access segments, use add method!!!
                sis.Add((SegmentCommitInfo)(info.Clone()));
            }
            sis.userData = new Dictionary<string, string>(userData);
            return sis;
        }

        // LUCENENET specific property for accessing segments private field
        public IList<SegmentCommitInfo> Segments => segments;


        /// <summary>
        /// Version number when this <see cref="SegmentInfos"/> was generated.
        /// </summary>
        public long Version { get; internal set; }

        /// <summary>
        /// Returns current generation. </summary>
        public long Generation => generation;

        /// <summary>
        /// Returns last succesfully read or written generation. </summary>
        public long LastGeneration => lastGeneration;

        /// <summary>
        /// If non-null, information about retries when loading
        /// the segments file will be printed to this.
        /// </summary>
        public static TextWriter InfoStream 
        {
            set =>
                // LUCENENET specific - use a SafeTextWriterWrapper to ensure that if the TextWriter
                // is disposed by the caller (using block) we don't get any exceptions if we keep using it.
                infoStream = value is null
                    ? null
                    : (value is SafeTextWriterWrapper ? value : new SafeTextWriterWrapper(value));
            get => infoStream;
        }

        /// <summary>
        /// Advanced configuration of retry logic in loading
        /// segments_N file
        /// </summary>
        private static int defaultGenLookaheadCount = 10;

        /// <summary>
        /// Gets or Sets the <see cref="defaultGenLookaheadCount"/>.
        /// <para/>
        /// Advanced: set how many times to try incrementing the
        /// gen when loading the segments file.  this only runs if
        /// the primary (listing directory) and secondary (opening
        /// segments.gen file) methods fail to find the segments
        /// file.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static int DefaultGenLookaheadCount // LUCENENET specific: corrected spelling issue with the getter
        {
            get => defaultGenLookaheadCount;
            set => defaultGenLookaheadCount = value;
        }

        /// <summary>
        /// Prints the given message to the <see cref="InfoStream"/>. Note, this method does not
        /// check for <c>null</c> <see cref="InfoStream"/>. It assumes this check has been performed by the
        /// caller, which is recommended to avoid the (usually) expensive message
        /// creation.
        /// </summary>
        private static void Message(string message)
        {
            infoStream.WriteLine("SIS [" + Thread.CurrentThread.Name + "]: " + message);
        }

        /// <summary>
        /// Utility class for executing code that needs to do
        /// something with the current segments file.  This is
        /// necessary with lock-less commits because from the time
        /// you locate the current segments file name, until you
        /// actually open it, read its contents, or check modified
        /// time, etc., it could have been deleted due to a writer
        /// commit finishing.
        /// </summary>
        public abstract class FindSegmentsFile
        {
            internal readonly Directory directory;

            /// <summary>
            /// Sole constructor. </summary>
            protected FindSegmentsFile(Directory directory) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            {
                this.directory = directory;
            }

            /// <summary>
            /// Locate the most recent <c>segments</c> file and
            /// run <see cref="DoBody(string)"/> on it.
            /// </summary>
            public virtual object Run()
            {
                return Run(null);
            }

            /// <summary>
            /// Run <see cref="DoBody(string)"/> on the provided commit. </summary>
            public virtual object Run(IndexCommit commit)
            {
                if (commit != null)
                {
                    if (directory != commit.Directory)
                    {
                        throw new IOException("the specified commit does not match the specified Directory");
                    }
                    return DoBody(commit.SegmentsFileName);
                }

                string segmentFileName/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
                long lastGen = -1;
                long gen = 0;
                int genLookaheadCount = 0;
                Exception exc = null; // LUCENENET: No need to cast to IOExcpetion
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
                        string[] files = directory.ListAll(); // LUCENENET: IDE0059: Remove unnecessary value assignment

                        long genA = -1;

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
                            genInput = directory.OpenChecksumInput(IndexFileNames.SEGMENTS_GEN, IOContext.READ_ONCE);
                        }
                        catch (Exception e) when (e.IsIOException())
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
                                int version = genInput.ReadInt32();
                                if (version == FORMAT_SEGMENTS_GEN_47 || version == FORMAT_SEGMENTS_GEN_CHECKSUM)
                                {
                                    long gen0 = genInput.ReadInt64();
                                    long gen1 = genInput.ReadInt64();
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
#pragma warning disable 612, 618
                                        CodecUtil.CheckEOF(genInput);
#pragma warning restore 612, 618
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
                            catch (Exception err2) when (err2.IsIOException())
                            {
                                // rethrow any format exception
                                if (err2 is CorruptIndexException)
                                {
                                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
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
                            throw new IndexNotFoundException("no segments* file found in " + directory + ": files: " + Arrays.ToString(files));
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
                        if (genLookaheadCount < defaultGenLookaheadCount)
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
                            ExceptionDispatchInfo.Capture(exc).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
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
                    catch (Exception err) when (err.IsIOException())
                    {
                        // Save the original root cause:
                        if (exc is null)
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
                                directory.OpenInput(prevSegmentFileName, IOContext.DEFAULT).Dispose();
                                prevExists = true;
                            }
                            catch (Exception ioe) when (ioe.IsIOException())
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
                                catch (Exception err2) when (err2.IsIOException())
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
            /// <see cref="IOException"/> will be thrown if something goes wrong
            /// during the processing that could have been caused by
            /// a writer committing.
            /// </summary>
            protected internal abstract object DoBody(string segmentFileName);
        }

        // Carry over generation numbers from another SegmentInfos
        internal void UpdateGeneration(SegmentInfos other)
        {
            lastGeneration = other.lastGeneration;
            generation = other.generation;
        }

        internal void RollbackCommit(Directory dir)
        {
            if (pendingSegnOutput != null)
            {
                // Suppress so we keep throwing the original exception
                // in our caller
                IOUtils.DisposeWhileHandlingException(pendingSegnOutput);
                pendingSegnOutput = null;

                // Must carefully compute fileName from "generation"
                // since lastGeneration isn't incremented:
                string segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", generation);
                // Suppress so we keep throwing the original exception
                // in our caller
                IOUtils.DeleteFilesIgnoringExceptions(dir, segmentFileName);
            }
        }

        /// <summary>
        /// Call this to start a commit.  This writes the new
        /// segments file, but writes an invalid checksum at the
        /// end, so that it is not visible to readers.  Once this
        /// is called you must call <see cref="FinishCommit(Directory)"/> to complete
        /// the commit or <see cref="RollbackCommit(Directory)"/> to abort it.
        /// <para>
        /// Note: <see cref="Changed()"/> should be called prior to this
        /// method if changes have been made to this <see cref="SegmentInfos"/> instance
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void PrepareCommit(Directory dir)
        {
            if (pendingSegnOutput != null)
            {
                throw IllegalStateException.Create("prepareCommit was already called");
            }
            Write(dir);
        }

        /// <summary>
        /// Returns all file names referenced by <see cref="SegmentInfo"/>
        /// instances matching the provided <see cref="Directory"/> (ie files
        /// associated with any "external" segments are skipped).
        /// The returned collection is recomputed on each
        /// invocation.
        /// </summary>
        public ICollection<string> GetFiles(Directory dir, bool includeSegmentsFile)
        {
            var files = new JCG.HashSet<string>();
            if (includeSegmentsFile)
            {
                string segmentFileName = GetSegmentsFileName();
                if (segmentFileName != null)
                {
                    files.Add(segmentFileName);
                }
            }
            var size = Count;
            for (int i = 0; i < size; i++)
            {
                var info = this[i];
                if (Debugging.AssertsEnabled) Debugging.Assert(info.Info.Dir == dir);
                if (info.Info.Dir == dir)
                {
                    files.UnionWith(info.GetFiles());
                }
            }

            return files;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void FinishCommit(Directory dir)
        {
            if (pendingSegnOutput is null)
            {
                throw IllegalStateException.Create("prepareCommit was not called");
            }
            bool success = false;
            try
            {
                CodecUtil.WriteFooter(pendingSegnOutput);
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
                        pendingSegnOutput.Dispose();
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
                            pendingSegnOutput = null;
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

            var fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", generation);
            success = false;
            try
            {
                dir.Sync(new JCG.HashSet<string> { fileName });
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

            lastGeneration = generation;
            WriteSegmentsGen(dir, generation);
        }

        /// <summary>
        /// Writes &amp; syncs to the Directory dir, taking care to
        /// remove the segments file on exception
        /// <para>
        /// Note: <see cref="Changed()"/> should be called prior to this
        /// method if changes have been made to this <see cref="SegmentInfos"/> instance
        /// </para>
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
            buffer.Append(GetSegmentsFileName()).Append(": ");
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    buffer.Append(' ');
                }
                SegmentCommitInfo info = this[i];
                buffer.Append(info.ToString(directory, 0));
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Gets <see cref="userData"/> saved with this commit.
        /// </summary>
        /// <seealso cref="IndexWriter.Commit()"/>
        public IDictionary<string, string> UserData
        {
            get => userData;
            internal set
            {
                if (value is null)
                {
                    userData = Collections.EmptyMap<string, string>();
                }
                else
                {
                    userData = value;
                }
            }
        }

        /// <summary>
        /// Replaces all segments in this instance, but keeps
        /// generation, version, counter so that future commits
        /// remain write once.
        /// </summary>
        internal void Replace(SegmentInfos other)
        {
            RollbackSegmentInfos(other.AsList());
            lastGeneration = other.lastGeneration;
        }

        /// <summary>
        /// Returns sum of all segment's docCounts.  Note that
        /// this does not include deletions
        /// </summary>
        public int TotalDocCount
        {
            get
            {
                int count = 0;
                foreach (SegmentCommitInfo info in this)
                {
                    count += info.Info.DocCount;
                }
                return count;
            }
        }

        /// <summary>
        /// Call this before committing if changes have been made to the
        /// segments.
        /// </summary>
        public void Changed()
        {
            Version++;
        }

        /// <summary>
        /// applies all changes caused by committing a merge to this <see cref="SegmentInfos"/> </summary>
        internal void ApplyMergeChanges(MergePolicy.OneMerge merge, bool dropSegment)
        {
            var mergedAway = new JCG.HashSet<SegmentCommitInfo>(merge.Segments);
            bool inserted = false;
            int newSegIdx = 0;
            for (int segIdx = 0, cnt = segments.Count; segIdx < cnt; segIdx++)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(segIdx >= newSegIdx);
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
            segments.RemoveRange(newSegIdx, segments.Count - newSegIdx); // LUCENENET: Converted end index to length

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
            var list = new JCG.List<SegmentCommitInfo>(Count);
            foreach (var info in segments)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(info.Info.Codec != null);
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
        /// Returns an <b>unmodifiable</b> <see cref="T:IEnumerator{SegmentCommitInfo}"/> of contained segments in order.
        /// </summary>
        public IEnumerator<SegmentCommitInfo> GetEnumerator()
        {
            return AsList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns all contained segments as an <b>unmodifiable</b> <see cref="T:IList{SegmentCommitInfo}"/> view. </summary>
        public IList<SegmentCommitInfo> AsList()
        {
            return segments.AsReadOnly();
        }

        /// <summary>
        /// Returns number of <see cref="SegmentCommitInfo"/>s.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count => segments.Count;

        /// <summary>
        /// Appends the provided <see cref="SegmentCommitInfo"/>. </summary>
        public void Add(SegmentCommitInfo si)
        {
            segments.Add(si);
        }

        /// <summary>
        /// Appends the provided <see cref="SegmentCommitInfo"/>s. </summary>
        public void AddAll(IEnumerable<SegmentCommitInfo> sis)
        {
            foreach (var si in sis)
            {
                this.Add(si);
            }
        }

        /// <summary>
        /// Clear all <see cref="SegmentCommitInfo"/>s. </summary>
        public void Clear()
        {
            segments.Clear();
        }

        /// <summary>
        /// Remove the provided <see cref="SegmentCommitInfo"/>.
        ///
        /// <para/><b>WARNING</b>: O(N) cost
        /// </summary>
        public void Remove(SegmentCommitInfo si)
        {
            segments.Remove(si);
        }

        /// <summary>
        /// Remove the <see cref="SegmentCommitInfo"/> at the
        /// provided index.
        ///
        /// <para/><b>WARNING</b>: O(N) cost
        /// </summary>
        internal void Remove(int index)
        {
            segments.RemoveAt(index);
        }

        /// <summary>
        /// Return true if the provided 
        /// <see cref="SegmentCommitInfo"/> is contained.
        ///
        /// <para/><b>WARNING</b>: O(N) cost
        /// </summary>
        internal bool Contains(SegmentCommitInfo si)
        {
            return segments.Contains(si);
        }

        /// <summary>
        /// Returns index of the provided
        /// <see cref="SegmentCommitInfo"/>.
        ///
        /// <para/><b>WARNING</b>: O(N) cost
        /// </summary>
        internal int IndexOf(SegmentCommitInfo si)
        {
            return segments.IndexOf(si);
        }
    }
}