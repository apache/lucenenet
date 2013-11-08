/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
using ChecksumIndexOutput = Lucene.Net.Store.ChecksumIndexOutput;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using NoSuchDirectoryException = Lucene.Net.Store.NoSuchDirectoryException;

namespace Lucene.Net.Index
{

    /// <summary> A collection of segmentInfo objects with methods for operating on
    /// those segments in relation to the file system.
    /// 
    /// <p/><b>NOTE:</b> This API is new and still experimental
    /// (subject to change suddenly in the next release)<p/>
    /// </summary>
    [Serializable]
    public sealed class SegmentInfos : List<SegmentInfoPerCommit>, ICloneable
    {
        public const int VERSION_40 = 0;

        public const int FORMAT_SEGMENTS_GEN_CURRENT = -2;

        public int counter = 0; // used to name new segments
        /// <summary> counts how often the index has been changed by adding or deleting docs.
        /// starting with the current time in milliseconds forces to create unique version numbers.
        /// </summary>
        internal long version;

        private long generation = 0; // generation of the "segments_N" for the next commit
        private long lastGeneration = 0; // generation of the "segments_N" file we last successfully read
        // or wrote; this is normally the same as generation except if
        // there was an IOException that had interrupted a commit

        internal IDictionary<string, string> userData = new HashMap<string, string>(); // Opaque Map<String, String> that user can specify during IndexWriter.commit

        /// <summary> If non-null, information about loading segments_N files</summary>
        /// <seealso cref="SetInfoStream">
        /// </seealso>
        private static StreamWriter infoStream;

        public SegmentInfos()
        {
        }

        public SegmentInfoPerCommit Info(int i)
        {
            return (SegmentInfoPerCommit)this[i];
        }

        /// <summary> Get the generation (N) of the current segments_N file
        /// from a list of files.
        /// 
        /// </summary>
        /// <param name="files">-- array of file names to check
        /// </param>
        public static long GetLastCommitGeneration(String[] files)
        {
            if (files == null)
            {
                return -1;
            }
            long max = -1;
            foreach (String file in files)
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

        /// <summary> Get the generation (N) of the current segments_N file
        /// in the directory.
        /// 
        /// </summary>
        /// <param name="directory">-- directory to search for the latest segments_N file
        /// </param>
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

        /// <summary> Get the filename of the current segments_N file
        /// from a list of files.
        /// 
        /// </summary>
        /// <param name="files">-- array of file names to check
        /// </param>

        public static string GetLastCommitSegmentsFileName(string[] files)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(files));
        }

        /// <summary> Get the filename of the current segments_N file
        /// in the directory.
        /// 
        /// </summary>
        /// <param name="directory">-- directory to search for the latest segments_N file
        /// </param>
        public static string GetLastCommitSegmentsFileName(Directory directory)
        {
            return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", GetLastCommitGeneration(directory));
        }

        /// <summary> Get the segments_N filename in use by this segment infos.</summary>
        public string SegmentsFileName
        {
            get { return IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", lastGeneration); }
        }

        /// <summary> Parse the generation off the segments file name and
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
                return Number.Parse(fileName.Substring(1 + IndexFileNames.SEGMENTS.Length), Character.MAX_RADIX);
            }
            else
            {
                throw new ArgumentException("fileName \"" + fileName + "\" is not a segments file");
            }
        }


        /// <summary> Get the next segments_N filename that will be written.</summary>
        public string NextSegmentFileName
        {
            get
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
        }

        /// <summary> Read a particular segmentFileName.  Note that this may
        /// throw an IOException if a commit is in process.
        /// 
        /// </summary>
        /// <param name="directory">-- directory containing the segments file
        /// </param>
        /// <param name="segmentFileName">-- segment file to load
        /// </param>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        public void Read(Directory directory, string segmentFileName)
        {
            bool success = false;

            // Clear any previous segments:
            Clear();

            generation = GenerationFromSegmentsFileName(segmentFileName);

            lastGeneration = generation;

            var input = new ChecksumIndexInput(directory.OpenInput(segmentFileName, IOContext.READ));

            try
            {
                int format = input.ReadInt();
                if (format == CodecUtil.CODEC_MAGIC)
                {
                    // 4.0+
                    CodecUtil.CheckHeaderNoMagic(input, "segments", VERSION_40, VERSION_40);
                    version = input.ReadLong();
                    counter = input.ReadInt();
                    int numSegments = input.ReadInt();
                    if (numSegments < 0)
                    {
                        throw new CorruptIndexException("invalid segment count: " + numSegments + " (resource: " + input + ")");
                    }
                    for (int seg = 0; seg < numSegments; seg++)
                    {
                        String segName = input.ReadString();
                        Codec codec = Codec.ForName(input.ReadString());
                        //System.out.println("SIS.read seg=" + seg + " codec=" + codec);
                        SegmentInfo info = codec.SegmentInfoFormat.SegmentInfoReader.Read(directory, segName, IOContext.READ);
                        info.Codec = codec;
                        long delGen = input.ReadLong();
                        int delCount = input.ReadInt();
                        if (delCount < 0 || delCount > info.DocCount)
                        {
                            throw new CorruptIndexException("invalid deletion count: " + delCount + " (resource: " + input + ")");
                        }
                        Add(new SegmentInfoPerCommit(info, delCount, delGen));
                    }
                    userData = input.ReadStringStringMap();
                }
                else
                {
                    Lucene3xSegmentInfoReader.ReadLegacyInfos(this, directory, input, format);
                    Codec codec = Codec.ForName("Lucene3x");
                    foreach (SegmentInfoPerCommit info in this)
                    {
                        info.info.Codec = codec;
                    }
                }

                long checksumNow = input.Checksum;
                long checksumThen = input.ReadLong();
                if (checksumNow != checksumThen)
                {
                    throw new CorruptIndexException("checksum mismatch in segments file (resource: " + input + ")");
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
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
                else
                {
                    input.Dispose();
                }
            }
        }

        private sealed class AnonymousClassFindSegmentsFile : FindSegmentsFile
        {
            private SegmentInfos enclosingInstance;

            internal AnonymousClassFindSegmentsFile(SegmentInfos enclosingInstance, Directory Param1)
                : base(Param1)
            {
                this.enclosingInstance = enclosingInstance;
            }


            protected override object DoBody(string segmentFileName)
            {
                enclosingInstance.Read(directory, segmentFileName);
                return null;
            }
        }

        /// <summary> This version of read uses the retry logic (for lock-less
        /// commits) to find the right segments file to load.
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        public void Read(Directory directory)
        {

            generation = lastGeneration = -1;

            new AnonymousClassFindSegmentsFile(this, directory).Run();
        }

        // Only non-null after prepareCommit has been called and
        // before finishCommit is called
        internal ChecksumIndexOutput pendingSegnOutput;

        private const string SEGMENT_INFO_UPGRADE_CODEC = "SegmentInfo3xUpgrade";
        private const int SEGMENT_INFO_UPGRADE_VERSION = 0;

        private void Write(Directory directory)
        {
            String segmentsFileName = NextSegmentFileName;

            // Always advance the generation on write:
            if (generation == -1)
            {
                generation = 1;
            }
            else
            {
                generation++;
            }

            ChecksumIndexOutput segnOutput = null;
            bool success = false;

            ISet<String> upgradedSIFiles = new HashSet<String>();

            try
            {
                segnOutput = new ChecksumIndexOutput(directory.CreateOutput(segmentsFileName, IOContext.DEFAULT));
                CodecUtil.WriteHeader(segnOutput, "segments", VERSION_40);
                segnOutput.WriteLong(version);
                segnOutput.WriteInt(counter); // write counter
                segnOutput.WriteInt(Count); // write infos
                foreach (SegmentInfoPerCommit siPerCommit in this)
                {
                    SegmentInfo si = siPerCommit.info;
                    segnOutput.WriteString(si.name);
                    segnOutput.WriteString(si.Codec.Name);
                    segnOutput.WriteLong(siPerCommit.DelGen);
                    segnOutput.WriteInt(siPerCommit.DelCount);
                    //assert si.dir == directory;

                    //assert siPerCommit.getDelCount() <= si.getDocCount();

                    // If this segment is pre-4.x, perform a one-time
                    // "ugprade" to write the .si file for it:
                    String version2 = si.Version;
                    if (version2 == null || StringHelper.VersionComparator.Compare(version2, "4.0") < 0)
                    {

                        if (!SegmentWasUpgraded(directory, si))
                        {

                            String markerFileName = IndexFileNames.SegmentFileName(si.name, "upgraded", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
                            si.AddFile(markerFileName);

                            String segmentFileName = Write3xInfo(directory, si, IOContext.DEFAULT);
                            upgradedSIFiles.Add(segmentFileName);
                            directory.Sync(new[] { segmentFileName });

                            // Write separate marker file indicating upgrade
                            // is completed.  This way, if there is a JVM
                            // kill/crash, OS crash, power loss, etc. while
                            // writing the upgraded file, the marker file
                            // will be missing:
                            si.AddFile(markerFileName);
                            IndexOutput output = directory.CreateOutput(markerFileName, IOContext.DEFAULT);
                            try
                            {
                                CodecUtil.WriteHeader(output, SEGMENT_INFO_UPGRADE_CODEC, SEGMENT_INFO_UPGRADE_VERSION);
                            }
                            finally
                            {
                                output.Dispose();
                            }
                            upgradedSIFiles.Add(markerFileName);
                            directory.Sync(new[] { markerFileName });
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
                    IOUtils.CloseWhileHandlingException((IDisposable)segnOutput);

                    foreach (String fileName in upgradedSIFiles)
                    {
                        try
                        {
                            directory.DeleteFile(fileName);
                        }
                        catch (Exception t)
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
                    catch (Exception t)
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }
        }

        private static bool SegmentWasUpgraded(Directory directory, SegmentInfo si)
        {
            // Check marker file:
            String markerFileName = IndexFileNames.SegmentFileName(si.name, "upgraded", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
            IndexInput input = null;
            try
            {
                input = directory.OpenInput(markerFileName, IOContext.READONCE);
                if (CodecUtil.CheckHeader(input, SEGMENT_INFO_UPGRADE_CODEC, SEGMENT_INFO_UPGRADE_VERSION, SEGMENT_INFO_UPGRADE_VERSION) == 0)
                {
                    return true;
                }
            }
            catch (IOException ioe)
            {
                // Ignore: if something is wrong w/ the marker file,
                // we will just upgrade again
            }
            finally
            {
                if (input != null)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
            return false;
        }

        [Obsolete]
        public static string Write3xInfo(Directory dir, SegmentInfo si, IOContext context)
        {

            // NOTE: this is NOT how 3.x is really written...
            String fileName = IndexFileNames.SegmentFileName(si.name, "", Lucene3xSegmentInfoFormat.UPGRADED_SI_EXTENSION);
            si.AddFile(fileName);

            //System.out.println("UPGRADE write " + fileName);
            bool success = false;
            IndexOutput output = dir.CreateOutput(fileName, context);
            try
            {
                // we are about to write this SI in 3.x format, dropping all codec information, etc.
                // so it had better be a 3.x segment or you will get very confusing errors later.
                //assert si.getCodec() instanceof Lucene3xCodec : "broken test, trying to mix preflex with other codecs";
                CodecUtil.WriteHeader(output, Lucene3xSegmentInfoFormat.UPGRADED_SI_CODEC_NAME,
                                              Lucene3xSegmentInfoFormat.UPGRADED_SI_VERSION_CURRENT);
                // Write the Lucene version that created this segment, since 3.1
                output.WriteString(si.Version);
                output.WriteInt(si.DocCount);

                output.WriteStringStringMap(si.Attributes);

                output.WriteByte((byte)(si.UseCompoundFile ? SegmentInfo.YES : SegmentInfo.NO));
                output.WriteStringStringMap(si.Diagnostics);
                output.WriteStringSet(si.Files);

                output.Dispose();

                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)output);
                    try
                    {
                        si.dir.DeleteFile(fileName);
                    }
                    catch (Exception t)
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }

            return fileName;
        }

        /// <summary> Returns a copy of this instance, also copying each
        /// SegmentInfo.
        /// </summary>
        public object Clone()
        {
            SegmentInfos sis = new SegmentInfos();
            sis.counter = this.counter;
            sis.version = this.version;
            sis.generation = this.generation;
            sis.lastGeneration = this.lastGeneration;
            sis.pendingSegnOutput = this.pendingSegnOutput;
            for (int i = 0; i < this.Count; i++)
            {
                sis.Add((SegmentInfoPerCommit)this[i].Clone());
            }
            sis.userData = new HashMap<string, string>(userData);
            return sis;
        }

        /// <summary> version number when this SegmentInfos was generated.</summary>
        public long Version
        {
            get { return version; }
        }

        public long Generation
        {
            get { return generation; }
        }

        public long LastGeneration
        {
            get { return lastGeneration; }
        }

        private static int defaultGenLookaheadCount = 10;

        /// <summary> Advanced: set how many times to try incrementing the
        /// gen when loading the segments file.  This only runs if
        /// the primary (listing directory) and secondary (opening
        /// segments.gen file) methods fail to find the segments
        /// file.
        /// </summary>
        public static int DefaultGenLookaheadCount
        {
            set { defaultGenLookaheadCount = value; }
            get { return defaultGenLookaheadCount; }
        }

        /// <seealso cref="SetInfoStream">
        /// </seealso>
        public static StreamWriter InfoStream
        {
            get { return infoStream; }
            set
            {
                infoStream = value;
            }
        }

        private static void Message(string message)
        {
            if (infoStream != null)
            {
                infoStream.WriteLine("SIS [" + ThreadClass.Current().Name + "]: " + message);
            }
        }

        /// <summary> Utility class for executing code that needs to do
        /// something with the current segments file.  This is
        /// necessary with lock-less commits because from the time
        /// you locate the current segments file name, until you
        /// actually open it, read its contents, or check modified
        /// time, etc., it could have been deleted due to a writer
        /// commit finishing.
        /// </summary>
        public abstract class FindSegmentsFile
        {

            internal Directory directory;

            protected FindSegmentsFile(Directory directory)
            {
                this.directory = directory;
            }

            public object Run()
            {
                return Run(null);
            }

            public object Run(IndexCommit commit)
            {
                if (commit != null)
                {
                    if (directory != commit.Directory)
                        throw new IOException("the specified commit does not match the specified Directory");
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
                // generation.  We try the first two in parallel, and
                // fall back to the third when necessary.

                while (true)
                {

                    if (useFirstMethod)
                    {

                        // Method 1: list the directory and use the highest
                        // segments_N file.  This method works well as long
                        // as there is no stale caching on the directory
                        // contents (NOTE: NFS clients often have such stale
                        // caching):
                        string[] files = null;

                        long genA = -1;

                        files = directory.ListAll();

                        if (files != null)
                            genA = GetLastCommitGeneration(files);

                        Message("directory listing genA=" + genA);

                        // Method 2: open segments.gen and read its
                        // contents.  Then we take the larger of the two
                        // gens.  This way, if either approach is hitting
                        // a stale cache (NFS) we have a better chance of
                        // getting the right generation.
                        long genB = -1;
                        IndexInput genInput = null;
                        try
                        {
                            genInput = directory.OpenInput(IndexFileNames.SEGMENTS_GEN, IOContext.READONCE);
                        }
                        catch (FileNotFoundException e)
                        {
                            if (infoStream != null)
                            {
                                Message("segments.gen open: FileNotFoundException " + e);
                            }
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
                                if (version == FORMAT_SEGMENTS_GEN_CURRENT)
                                {
                                    long gen0 = genInput.ReadLong();
                                    long gen1 = genInput.ReadLong();
                                    if (infoStream != null)
                                    {
                                        Message("fallback check: " + gen0 + "; " + gen1);
                                    }
                                    if (gen0 == gen1)
                                    {
                                        // The file is consistent.
                                        genB = gen0;
                                    }
                                }
                                else
                                {
                                    throw new IndexFormatTooNewException(genInput, version, FORMAT_SEGMENTS_GEN_CURRENT, FORMAT_SEGMENTS_GEN_CURRENT);
                                }
                            }
                            catch (IOException err2)
                            {
                                // rethrow any format exception
                                if (err2 is CorruptIndexException) throw err2;
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
                            throw exc;
                        }
                    }
                    else if (lastGen == gen)
                    {
                        // This means we're about to try the same
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

                    segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS,
                                                                            "",
                                                                            gen);

                    try
                    {
                        Object v = DoBody(segmentFileName);
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

                            // This is our second time trying this same segments
                            // file (because retryCount is 1), and, there is
                            // possibly a segments_(N-1) (because gen > 1).
                            // So, check if the segments_(N-1) exists and
                            // try it if so:
                            String prevSegmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS,
                                                                                               "",
                                                                                               gen - 1);

                            bool prevExists;
                            prevExists = directory.FileExists(prevSegmentFileName);

                            if (prevExists)
                            {
                                if (infoStream != null)
                                {
                                    Message("fallback to prior segment file '" + prevSegmentFileName + "'");
                                }
                                try
                                {
                                    Object v = DoBody(prevSegmentFileName);
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

            /// <summary> Subclass must implement this.  The assumption is an
            /// IOException will be thrown if something goes wrong
            /// during the processing that could have been caused by
            /// a writer committing.
            /// </summary>
            protected abstract object DoBody(string segmentFileName);
        }

        /// <summary> Returns a new SegmentInfos containg the SegmentInfo
        /// instances in the specified range first (inclusive) to
        /// last (exclusive), so total number of segments returned
        /// is last-first.
        /// </summary>
        public SegmentInfos Range(int first, int last)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.AddRange(this.GetRange(first, last - first));
            return infos;
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
                IOUtils.CloseWhileHandlingException((IDisposable)pendingSegnOutput);
                pendingSegnOutput = null;

                // Must carefully compute fileName from "generation"
                // since lastGeneration isn't incremented:
                string segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS,
                                                                                      "",
                                                                                     generation);
                // Suppress so we keep throwing the original exception
                // in our caller
                IOUtils.DeleteFilesIgnoringExceptions(dir, segmentFileName);
            }
        }

        /// <summary>Call this to start a commit.  This writes the new
        /// segments file, but writes an invalid checksum at the
        /// end, so that it is not visible to readers.  Once this
        /// is called you must call <see cref="FinishCommit" /> to complete
        /// the commit or <see cref="RollbackCommit" /> to abort it. 
        /// </summary>
        internal void PrepareCommit(Directory dir)
        {
            if (pendingSegnOutput != null)
                throw new SystemException("prepareCommit was already called");

            Write(dir);
        }

        /// <summary>Returns all file names referenced by SegmentInfo
        /// instances matching the provided Directory (ie files
        /// associated with any "external" segments are skipped).
        /// The returned collection is recomputed on each
        /// invocation.  
        /// </summary>
        public ICollection<string> Files(Directory dir, bool includeSegmentsFile)
        {
            HashSet<string> files = new HashSet<string>();
            if (includeSegmentsFile)
            {
                string segmentFileName = SegmentsFileName;
                if (segmentFileName != null)
                {
                    /*
                     * TODO: if lastGen == -1 we get might get null here it seems wrong to
                     * add null to the files set
                     */
                    files.Add(segmentFileName);
                }
            }
            int size = Count;
            for (int i = 0; i < size; i++)
            {
                SegmentInfoPerCommit info = Info(i);
                //assert info.info.dir == dir;
                if (info.info.dir == dir)
                {
                    files.UnionWith(info.Files);
                }
            }
            return files;
        }

        internal void FinishCommit(Directory dir)
        {
            if (pendingSegnOutput == null)
                throw new System.SystemException("prepareCommit was not called");
            bool success = false;
            try
            {
                pendingSegnOutput.FinishCommit();
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

            string fileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", generation);
            success = false;
            try
            {
                dir.Sync(new[] { fileName });
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
                    catch
                    {
                        // Suppress so we keep throwing the original exception
                    }
                }
            }

            lastGeneration = generation;

            try
            {
                IndexOutput genOutput = dir.CreateOutput(IndexFileNames.SEGMENTS_GEN, IOContext.READONCE);
                try
                {
                    genOutput.WriteInt(FORMAT_SEGMENTS_GEN_CURRENT);
                    genOutput.WriteLong(generation);
                    genOutput.WriteLong(generation);
                }
                finally
                {
                    genOutput.Dispose();
                    dir.Sync(new[] { IndexFileNames.SEGMENTS_GEN });
                }
            }
            catch (System.Exception)
            {
                // It's OK if we fail to write this file since it's
                // used only as one of the retry fallbacks.
                try
                {
                    dir.DeleteFile(IndexFileNames.SEGMENTS_GEN);
                }
                catch
                {
                    // Ignore; this file is only used in a retry
                    // fallback on init.
                }
            }
        }

        /// <summary>Writes &amp; syncs to the Directory dir, taking care to
        /// remove the segments file on exception 
        /// </summary>
        public void Commit(Directory dir)
        {
            PrepareCommit(dir);
            FinishCommit(dir);
        }

        public string ToString(Directory directory)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append(SegmentsFileName).Append(": ");
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    buffer.Append(' ');
                }
                SegmentInfoPerCommit info = Info(i);
                buffer.Append(info.ToString(directory, 0));
            }
            return buffer.ToString();
        }

        public IDictionary<string, string> UserData
        {
            get { return userData; }
            internal set
            {
                userData = value ?? new HashMap<string, string>();
            }
        }

        /// <summary>Replaces all segments in this instance, but keeps
        /// generation, version, counter so that future commits
        /// remain write once.
        /// </summary>
        internal void Replace(SegmentInfos other)
        {
            RollbackSegmentInfos(other);
            lastGeneration = other.lastGeneration;
        }

        public int TotalDocCount
        {
            get
            {
                int count = 0;
                foreach (SegmentInfoPerCommit info in this)
                {
                    count += info.info.DocCount;
                }
                return count;
            }
        }

        public void Changed()
        {
            version++;
        }

        internal void ApplyMergeChanges(MergePolicy.OneMerge merge, bool dropSegment)
        {
            ISet<SegmentInfoPerCommit> mergedAway = new HashSet<SegmentInfoPerCommit>(merge.segments);
            bool inserted = false;
            int newSegIdx = 0;
            for (int segIdx = 0, cnt = this.Count; segIdx < cnt; segIdx++)
            {
                //assert segIdx >= newSegIdx;
                SegmentInfoPerCommit info = this[segIdx];
                if (mergedAway.Contains(info))
                {
                    if (!inserted && !dropSegment)
                    {
                        this[segIdx] = merge.info;
                        inserted = true;
                        newSegIdx++;
                    }
                }
                else
                {
                    this[newSegIdx] = info;
                    newSegIdx++;
                }
            }

            // the rest of the segments in list are duplicates, so don't remove from map, only list!
            this.SubList(newSegIdx, this.Count).Clear();
            
            // Either we found place to insert segment, or, we did
            // not, but only because all segments we merged becamee
            // deleted while we are merging, in which case it should
            // be the case that the new segment is also all deleted,
            // we insert it at the beginning if it should not be dropped:
            if (!inserted && !dropSegment)
            {
                this.Insert(0, merge.info);
            }
        }

        internal IList<SegmentInfoPerCommit> CreateBackupSegmentInfos()
        {
            IList<SegmentInfoPerCommit> list = new List<SegmentInfoPerCommit>(Count);
            foreach (SegmentInfoPerCommit info in this)
            {
                //assert info.info.getCodec() != null;
                list.Add((SegmentInfoPerCommit)info.Clone());
            }
            return list;
        }

        internal void RollbackSegmentInfos(IList<SegmentInfoPerCommit> infos)
        {
            this.Clear();
            this.AddRange(infos);
        }

        // .NET Port: no need for iterator() method here or any of the other list methods as we : List<T>
        
        /// <summary>
        /// Simple brute force implementation.
        /// If size is equal, compare items one by one.
        /// </summary>
        /// <param name="obj">SegmentInfos object to check equality for</param>
        /// <returns>true if lists are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var objToCompare = obj as SegmentInfos;
            if (objToCompare == null) return false;

            if (this.Count != objToCompare.Count) return false;

            for (int idx = 0; idx < this.Count; idx++)
            {
                if (!this[idx].Equals(objToCompare[idx])) return false;
            }

            return true;
        }

        /// <summary>
        /// Calculate hash code of SegmentInfos
        /// </summary>
        /// <returns>hash code as in java version of ArrayList</returns>
        public override int GetHashCode()
        {
            int h = 1;
            for (int i = 0; i < this.Count; i++)
            {
                SegmentInfoPerCommit si = (this[i] as SegmentInfoPerCommit);
                h = 31 * h + (si == null ? 0 : si.GetHashCode());
            }

            return h;
        }
    }
}