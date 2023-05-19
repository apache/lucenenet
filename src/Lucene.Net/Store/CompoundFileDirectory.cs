using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Store
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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Class for accessing a compound stream.
    /// This class implements a directory, but is limited to only read operations.
    /// Directory methods that would normally modify data throw an exception.
    /// <para/>
    /// All files belonging to a segment have the same name with varying extensions.
    /// The extensions correspond to the different file formats used by the <see cref="Codecs.Codec"/>.
    /// When using the Compound File format these files are collapsed into a
    /// single <c>.cfs</c> file (except for the <see cref="Codecs.LiveDocsFormat"/>, with a
    /// corresponding <c>.cfe</c> file indexing its sub-files.
    /// <para/>
    /// Files:
    /// <list type="bullet">
    ///     <item><description><c>.cfs</c>: An optional "virtual" file consisting of all the other
    ///         index files for systems that frequently run out of file handles.</description></item>
    ///     <item><description><c>.cfe</c>: The "virtual" compound file's entry table holding all
    ///         entries in the corresponding .cfs file.</description></item>
    /// </list>
    /// <para>Description:</para>
    /// <list type="bullet">
    ///     <item><description>Compound (.cfs) --&gt; Header, FileData <sup>FileCount</sup></description></item>
    ///     <item><description>Compound Entry Table (.cfe) --&gt; Header, FileCount, &lt;FileName,
    ///         DataOffset, DataLength&gt; <sup>FileCount</sup>, Footer</description></item>
    ///     <item><description>Header --&gt; <see cref="CodecUtil.WriteHeader"/></description></item>
    ///     <item><description>FileCount --&gt; <see cref="DataOutput.WriteVInt32"/></description></item>
    ///     <item><description>DataOffset,DataLength --&gt; <see cref="DataOutput.WriteInt64"/></description></item>
    ///     <item><description>FileName --&gt; <see cref="DataOutput.WriteString"/></description></item>
    ///     <item><description>FileData --&gt; raw file data</description></item>
    ///     <item><description>Footer --&gt; <see cref="CodecUtil.WriteFooter"/></description></item>
    /// </list>
    /// <para>Notes:</para>
    /// <list type="bullet">
    ///   <item><description>FileCount indicates how many files are contained in this compound file.
    ///         The entry table that follows has that many entries.</description></item>
    ///   <item><description>Each directory entry contains a long pointer to the start of this file's data
    ///         section, the files length, and a <see cref="string"/> with that file's name.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class CompoundFileDirectory : BaseDirectory
    {
        /// <summary>
        /// Offset/Length for a slice inside of a compound file </summary>
        public sealed class FileEntry
        {
            internal long Offset { get; set; }
            internal long Length { get; set; }
        }

        private readonly Directory directory;
        private readonly string fileName;
        //private readonly int readBufferSize; // LUCENENET: Never read
        private readonly IDictionary<string, FileEntry> entries;
        private readonly bool openForWrite;
        private static readonly IDictionary<string, FileEntry> SENTINEL = Collections.EmptyMap<string, FileEntry>();
        private readonly CompoundFileWriter writer;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly IndexInputSlicer handle;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Create a new <see cref="CompoundFileDirectory"/>.
        /// </summary>
        public CompoundFileDirectory(Directory directory, string fileName, IOContext context, bool openForWrite)
        {
            this.directory = directory;
            this.fileName = fileName;
            //this.readBufferSize = BufferedIndexInput.GetBufferSize(context); // LUCENENET: Never read
            this.IsOpen = false;
            this.openForWrite = openForWrite;
            if (!openForWrite)
            {
                bool success = false;
                handle = directory.CreateSlicer(fileName, context);
                try
                {
                    this.entries = ReadEntries(handle, directory, fileName);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(handle);
                    }
                }
                this.IsOpen = true;
                writer = null;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!(directory is CompoundFileDirectory),"compound file inside of compound file: {0}", fileName);
                this.entries = SENTINEL;
                this.IsOpen = true;
                writer = new CompoundFileWriter(directory, fileName);
                handle = null;
            }
        }

        // LUCENENET NOTE: These MUST be sbyte because they can be negative
        private static readonly sbyte CODEC_MAGIC_BYTE1 = (sbyte)CodecUtil.CODEC_MAGIC.TripleShift(24);
        private static readonly sbyte CODEC_MAGIC_BYTE2 = (sbyte)CodecUtil.CODEC_MAGIC.TripleShift(16);
        private static readonly sbyte CODEC_MAGIC_BYTE3 = (sbyte)CodecUtil.CODEC_MAGIC.TripleShift(8);
        private static readonly sbyte CODEC_MAGIC_BYTE4 = (sbyte)CodecUtil.CODEC_MAGIC;

        /// <summary>
        /// Helper method that reads CFS entries from an input stream </summary>
        private static IDictionary<string, FileEntry> ReadEntries(IndexInputSlicer handle, Directory dir, string name)
        {
            Exception priorE = null; // LUCENENET: No need to cast to IOExcpetion
            IndexInput stream = null;
            ChecksumIndexInput entriesStream = null;
            // read the first VInt. If it is negative, it's the version number
            // otherwise it's the count (pre-3.1 indexes)
            try
            {
                IDictionary<string, FileEntry> mapping;
#pragma warning disable 612, 618
                stream = handle.OpenFullSlice();
#pragma warning restore 612, 618
                int firstInt = stream.ReadVInt32();
                // impossible for 3.0 to have 63 files in a .cfs, CFS writer was not visible
                // and separate norms/etc are outside of cfs.
                if (firstInt == CODEC_MAGIC_BYTE1)
                {
                    sbyte secondByte = (sbyte)stream.ReadByte();
                    sbyte thirdByte = (sbyte)stream.ReadByte();
                    sbyte fourthByte = (sbyte)stream.ReadByte();
                    if (secondByte != CODEC_MAGIC_BYTE2 || thirdByte != CODEC_MAGIC_BYTE3 || fourthByte != CODEC_MAGIC_BYTE4)
                    {
                        throw new CorruptIndexException("Illegal/impossible header for CFS file: " + secondByte + "," + thirdByte + "," + fourthByte);
                    }
                    int version = CodecUtil.CheckHeaderNoMagic(stream, CompoundFileWriter.DATA_CODEC, CompoundFileWriter.VERSION_START, CompoundFileWriter.VERSION_CURRENT);
                    string entriesFileName = IndexFileNames.SegmentFileName(
                                                    IndexFileNames.StripExtension(name), "", 
                                                    IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
                    entriesStream = dir.OpenChecksumInput(entriesFileName, IOContext.READ_ONCE);
                    CodecUtil.CheckHeader(entriesStream, CompoundFileWriter.ENTRY_CODEC, CompoundFileWriter.VERSION_START, CompoundFileWriter.VERSION_CURRENT);
                    int numEntries = entriesStream.ReadVInt32();
                    mapping = new Dictionary<string, FileEntry>(numEntries);
                    for (int i = 0; i < numEntries; i++)
                    {
                        FileEntry fileEntry = new FileEntry();
                        string id = entriesStream.ReadString();
                        FileEntry previous = mapping.Put(id, fileEntry);
                        if (previous != null)
                        {
                            throw new CorruptIndexException("Duplicate cfs entry id=" + id + " in CFS: " + entriesStream);
                        }
                        fileEntry.Offset = entriesStream.ReadInt64();
                        fileEntry.Length = entriesStream.ReadInt64();
                    }
                    if (version >= CompoundFileWriter.VERSION_CHECKSUM)
                    {
                        CodecUtil.CheckFooter(entriesStream);
                    }
                    else
                    {
#pragma warning disable 612, 618
                        CodecUtil.CheckEOF(entriesStream);
#pragma warning restore 612, 618
                    }
                }
                else
                {
                    // TODO remove once 3.x is not supported anymore
                    mapping = ReadLegacyEntries(stream, firstInt);
                }
                return mapping;
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                priorE = ioe;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(priorE, stream, entriesStream);
            }
            // this is needed until Java 7's real try-with-resources:
            throw AssertionError.Create("impossible to get here");
        }

        private static IDictionary<string, FileEntry> ReadLegacyEntries(IndexInput stream, int firstInt)
        {
            IDictionary<string, FileEntry> entries = new Dictionary<string, FileEntry>();
            int count;
            bool stripSegmentName;
            if (firstInt < CompoundFileWriter.FORMAT_PRE_VERSION)
            {
                if (firstInt < CompoundFileWriter.FORMAT_NO_SEGMENT_PREFIX)
                {
                    throw new CorruptIndexException("Incompatible format version: " 
                        + firstInt + " expected >= " + CompoundFileWriter.FORMAT_NO_SEGMENT_PREFIX + " (resource: " + stream + ")");
                }
                // It's a post-3.1 index, read the count.
                count = stream.ReadVInt32();
                stripSegmentName = false;
            }
            else
            {
                count = firstInt;
                stripSegmentName = true;
            }

            // read the directory and init files
            long streamLength = stream.Length;
            FileEntry entry = null;
            for (int i = 0; i < count; i++)
            {
                long offset = stream.ReadInt64();
                if (offset < 0 || offset > streamLength)
                {
                    throw new CorruptIndexException("Invalid CFS entry offset: " + offset + " (resource: " + stream + ")");
                }
                string id = stream.ReadString();

                if (stripSegmentName)
                {
                    // Fix the id to not include the segment names. this is relevant for
                    // pre-3.1 indexes.
                    id = IndexFileNames.StripSegmentName(id);
                }

                if (entry != null)
                {
                    // set length of the previous entry
                    entry.Length = offset - entry.Offset;
                }

                entry = new FileEntry();
                entry.Offset = offset;

                FileEntry previous = entries.Put(id, entry);
                if (previous != null)
                {
                    throw new CorruptIndexException("Duplicate cfs entry id=" + id + " in CFS: " + stream);
                }
            }

            // set the length of the final entry
            if (entry != null)
            {
                entry.Length = streamLength - entry.Offset;
            }

            return entries;
        }

        public Directory Directory => directory;

        public string Name => fileName;

        protected override void Dispose(bool disposing)
        {
            // allow double close - usually to be consistent with other closeables
            if (!CompareAndSetIsOpen(expect: true, update: false)) return; // already closed

            UninterruptableMonitor.Enter(this);
            try
            {
                if (disposing)
                {
                    // LUCENENET: Moved double-dispose logic outside of lock.
                    if (writer != null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(openForWrite);
                        writer.Dispose();
                    }
                    else
                    {
                        IOUtils.Dispose(handle);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                if (Debugging.AssertsEnabled) Debugging.Assert(!openForWrite);
                string id = IndexFileNames.StripSegmentName(name);
                if (!entries.TryGetValue(id, out FileEntry entry) || entry is null)
                {
                    throw new FileNotFoundException("No sub-file with id " + id +
                        " found (fileName=" + name + " files: " +
                        string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", entries.Keys) + ")");
                }
                return handle.OpenSlice(name, entry.Offset, entry.Length);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns an array of strings, one for each file in the directory. </summary>
        public override string[] ListAll()
        {
            EnsureOpen();
            string[] res;
            if (writer != null)
            {
                res = writer.ListAll();
            }
            else
            {
                res = entries.Keys.ToArray();
                // Add the segment name
                string seg = IndexFileNames.ParseSegmentName(fileName);
                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = seg + res[i];
                }
            }
            return res;
        }

        /// <summary>
        /// Returns true iff a file with the given name exists. </summary>
        [Obsolete("this method will be removed in 5.0")]
        public override bool FileExists(string name)
        {
            EnsureOpen();
            if (this.writer != null)
            {
                return writer.FileExists(name);
            }
            return entries.ContainsKey(IndexFileNames.StripSegmentName(name));
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="NotSupportedException"> always: not supported by CFS  </exception>
        public override void DeleteFile(string name)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="NotSupportedException"> always: not supported by CFS  </exception>
#pragma warning disable IDE0060, CA1822 // Remove unused parameter, Mark members as static
        public void RenameFile(string from, string to)
#pragma warning restore IDE0060, CA1822 // Remove unused parameter, Mark members as static
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Returns the length of a file in the directory. </summary>
        /// <exception cref="IOException"> if the file does not exist  </exception>
        public override long FileLength(string name)
        {
            EnsureOpen();
            if (this.writer != null)
            {
                return writer.FileLength(name);
            }
            FileEntry e = entries[IndexFileNames.StripSegmentName(name)];
            if (e is null)
            {
                throw new FileNotFoundException(name);
            }
            return e.Length;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            return writer.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="NotSupportedException"> always: not supported by CFS  </exception>
        public override Lock MakeLock(string name)
        {
            throw UnsupportedOperationException.Create();
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            if (Debugging.AssertsEnabled) Debugging.Assert(!openForWrite);
            string id = IndexFileNames.StripSegmentName(name);
            if (!entries.TryGetValue(id, out FileEntry entry) || entry is null)
            {
                throw new FileNotFoundException("No sub-file with id " + id + 
                    " found (fileName=" + name + " files: " + 
                    string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", entries.Keys) + ")");
            }
            return new IndexInputSlicerAnonymousClass(this, entry);
        }

        private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
        {
            private readonly CompoundFileDirectory outerInstance;

            private readonly FileEntry entry;

            public IndexInputSlicerAnonymousClass(CompoundFileDirectory outerInstance, FileEntry entry)
            {
                this.outerInstance = outerInstance;
                this.entry = entry;
            }

            protected override void Dispose(bool disposing)
            {
                // LUCENENET: Intentionally blank
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return outerInstance.handle.OpenSlice(sliceDescription, entry.Offset + offset, length);
            }

            [Obsolete("Only for reading CFS files from 3.x indexes.")]
            public override IndexInput OpenFullSlice()
            {
                return OpenSlice("full-slice", 0, entry.Length);
            }
        }

        public override string ToString()
        {
            return "CompoundFileDirectory(file=\"" + fileName + "\" in dir=" + directory + ")";
        }
    }
}