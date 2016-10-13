using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Store
{
    using Lucene.Net.Support;
    using System;

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

    // javadocs
    using CodecUtil = Lucene.Net.Codecs.CodecUtil;

    // javadocs
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Class for accessing a compound stream.
    /// this class implements a directory, but is limited to only read operations.
    /// Directory methods that would normally modify data throw an exception.
    /// <p>
    /// All files belonging to a segment have the same name with varying extensions.
    /// The extensions correspond to the different file formats used by the <seealso cref="Codec"/>.
    /// When using the Compound File format these files are collapsed into a
    /// single <tt>.cfs</tt> file (except for the <seealso cref="LiveDocsFormat"/>, with a
    /// corresponding <tt>.cfe</tt> file indexing its sub-files.
    /// <p>
    /// Files:
    /// <ul>
    ///    <li><tt>.cfs</tt>: An optional "virtual" file consisting of all the other
    ///    index files for systems that frequently run out of file handles.
    ///    <li><tt>.cfe</tt>: The "virtual" compound file's entry table holding all
    ///    entries in the corresponding .cfs file.
    /// </ul>
    /// <p>Description:</p>
    /// <ul>
    ///   <li>Compound (.cfs) --&gt; Header, FileData <sup>FileCount</sup></li>
    ///   <li>Compound Entry Table (.cfe) --&gt; Header, FileCount, &lt;FileName,
    ///       DataOffset, DataLength&gt; <sup>FileCount</sup>, Footer</li>
    ///   <li>Header --&gt; <seealso cref="CodecUtil#writeHeader CodecHeader"/></li>
    ///   <li>FileCount --&gt; <seealso cref="DataOutput#writeVInt VInt"/></li>
    ///   <li>DataOffset,DataLength --&gt; <seealso cref="DataOutput#writeLong UInt64"/></li>
    ///   <li>FileName --&gt; <seealso cref="DataOutput#writeString String"/></li>
    ///   <li>FileData --&gt; raw file data</li>
    ///   <li>Footer --&gt; <seealso cref="CodecUtil#writeFooter CodecFooter"/></li>
    /// </ul>
    /// <p>Notes:</p>
    /// <ul>
    ///   <li>FileCount indicates how many files are contained in this compound file.
    ///       The entry table that follows has that many entries.
    ///   <li>Each directory entry contains a long pointer to the start of this file's data
    ///       section, the files length, and a String with that file's name.
    /// </ul>
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class CompoundFileDirectory : BaseDirectory
    {
        /// <summary>
        /// Offset/Length for a slice inside of a compound file </summary>
        public sealed class FileEntry
        {
            internal long Offset;
            internal long Length;
        }

        private readonly Directory Directory_Renamed;
        private readonly string FileName;
        private readonly int ReadBufferSize;
        private readonly IDictionary<string, FileEntry> Entries;
        private readonly bool OpenForWrite;
        private static readonly IDictionary<string, FileEntry> SENTINEL = CollectionsHelper.EmptyMap<string, FileEntry>();
        private readonly CompoundFileWriter Writer;
        private readonly IndexInputSlicer Handle;

        /// <summary>
        /// Create a new CompoundFileDirectory.
        /// </summary>
        public CompoundFileDirectory(Directory directory, string fileName, IOContext context, bool openForWrite)
        {
            this.Directory_Renamed = directory;
            this.FileName = fileName;
            this.ReadBufferSize = BufferedIndexInput.BufferSize(context);
            this.isOpen = false;
            this.OpenForWrite = openForWrite;
            if (!openForWrite)
            {
                bool success = false;
                Handle = directory.CreateSlicer(fileName, context);
                try
                {
                    this.Entries = ReadEntries(Handle, directory, fileName);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(Handle);
                    }
                }
                this.isOpen = true;
                Writer = null;
            }
            else
            {
                Debug.Assert(!(directory is CompoundFileDirectory), "compound file inside of compound file: " + fileName);
                this.Entries = SENTINEL;
                this.isOpen = true;
                Writer = new CompoundFileWriter(directory, fileName);
                Handle = null;
            }
        }

        // LUCENENET NOTE: These MUST be sbyte because they can be negative
        private static readonly sbyte CODEC_MAGIC_BYTE1 = (sbyte)Number.URShift(CodecUtil.CODEC_MAGIC, 24);
        private static readonly sbyte CODEC_MAGIC_BYTE2 = (sbyte)Number.URShift(CodecUtil.CODEC_MAGIC, 16);
        private static readonly sbyte CODEC_MAGIC_BYTE3 = (sbyte)Number.URShift(CodecUtil.CODEC_MAGIC, 8);
        private static readonly sbyte CODEC_MAGIC_BYTE4 = unchecked((sbyte)CodecUtil.CODEC_MAGIC);

        /// <summary>
        /// Helper method that reads CFS entries from an input stream </summary>
        private static IDictionary<string, FileEntry> ReadEntries(IndexInputSlicer handle, Directory dir, string name)
        {
            System.IO.IOException priorE = null;
            IndexInput stream = null;
            ChecksumIndexInput entriesStream = null;
            // read the first VInt. If it is negative, it's the version number
            // otherwise it's the count (pre-3.1 indexes)
            try
            {
                IDictionary<string, FileEntry> mapping;
                stream = handle.OpenFullSlice();
                int firstInt = stream.ReadVInt();
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
                    string entriesFileName = IndexFileNames.SegmentFileName(IndexFileNames.StripExtension(name), "", IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
                    entriesStream = dir.OpenChecksumInput(entriesFileName, IOContext.READONCE);
                    CodecUtil.CheckHeader(entriesStream, CompoundFileWriter.ENTRY_CODEC, CompoundFileWriter.VERSION_START, CompoundFileWriter.VERSION_CURRENT);
                    int numEntries = entriesStream.ReadVInt();
                    mapping = new Dictionary<string, FileEntry>(numEntries);
                    for (int i = 0; i < numEntries; i++)
                    {
                        FileEntry fileEntry = new FileEntry();
                        string id = entriesStream.ReadString();

                        //If the key was already present
                        if (mapping.ContainsKey(id))
                        {
                            throw new CorruptIndexException("Duplicate cfs entry id=" + id + " in CFS: " + entriesStream);
                        }
                        else
                        {
                            mapping[id] = fileEntry;
                        }
                        fileEntry.Offset = entriesStream.ReadLong();
                        fileEntry.Length = entriesStream.ReadLong();
                    }
                    if (version >= CompoundFileWriter.VERSION_CHECKSUM)
                    {
                        CodecUtil.CheckFooter(entriesStream);
                    }
                    else
                    {
                        CodecUtil.CheckEOF(entriesStream);
                    }
                }
                else
                {
                    // TODO remove once 3.x is not supported anymore
                    mapping = ReadLegacyEntries(stream, firstInt);
                }
                return mapping;
            }
            catch (System.IO.IOException ioe)
            {
                priorE = ioe;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(priorE, stream, entriesStream);
            }
            // this is needed until Java 7's real try-with-resources:
            throw new InvalidOperationException("impossible to get here");
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
                    throw new CorruptIndexException("Incompatible format version: " + firstInt + " expected >= " + CompoundFileWriter.FORMAT_NO_SEGMENT_PREFIX + " (resource: " + stream + ")");
                }
                // It's a post-3.1 index, read the count.
                count = stream.ReadVInt();
                stripSegmentName = false;
            }
            else
            {
                count = firstInt;
                stripSegmentName = true;
            }

            // read the directory and init files
            long streamLength = stream.Length();
            FileEntry entry = null;
            for (int i = 0; i < count; i++)
            {
                long offset = stream.ReadLong();
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

                FileEntry previous;
                if (entries.TryGetValue(id, out previous))
                {
                    throw new CorruptIndexException("Duplicate cfs entry id=" + id + " in CFS: " + stream);
                }
                else
                {
                    entries[id] = entry;
                }
            }

            // set the length of the final entry
            if (entry != null)
            {
                entry.Length = streamLength - entry.Offset;
            }

            return entries;
        }

        public Directory Directory
        {
            get
            {
                return Directory_Renamed;
            }
        }

        public string Name
        {
            get
            {
                return FileName;
            }
        }

        public override void Dispose()
        {
            lock (this)
            {
                if (!IsOpen)
                {
                    // allow double close - usually to be consistent with other closeables
                    return; // already closed
                }
                isOpen = false;
                if (Writer != null)
                {
                    Debug.Assert(OpenForWrite);
                    Writer.Dispose();
                }
                else
                {
                    IOUtils.Close(Handle);
                }
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                EnsureOpen();
                Debug.Assert(!OpenForWrite);
                string id = IndexFileNames.StripSegmentName(name);
                FileEntry entry;
                if (!Entries.TryGetValue(id, out entry))
                {
                    throw new Exception("No sub-file with id " + id + " found (fileName=" + name + " files: " + Arrays.ToString(Entries.Keys) + ")");
                }
                return Handle.OpenSlice(name, entry.Offset, entry.Length);
            }
        }

        /// <summary>
        /// Returns an array of strings, one for each file in the directory. </summary>
        public override string[] ListAll()
        {
            EnsureOpen();
            string[] res;
            if (Writer != null)
            {
                res = Writer.ListAll();
            }
            else
            {
                res = Entries.Keys.ToArray();
                // Add the segment name
                string seg = IndexFileNames.ParseSegmentName(FileName);
                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = seg + res[i];
                }
            }
            return res;
        }

        /// <summary>
        /// Returns true iff a file with the given name exists. </summary>
        [Obsolete]
        public override bool FileExists(string name)
        {
            EnsureOpen();
            if (this.Writer != null)
            {
                return Writer.FileExists(name);
            }
            return Entries.ContainsKey(IndexFileNames.StripSegmentName(name));
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="UnsupportedOperationException"> always: not supported by CFS  </exception>
        public override void DeleteFile(string name)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="UnsupportedOperationException"> always: not supported by CFS  </exception>
        public void RenameFile(string from, string to)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Returns the length of a file in the directory. </summary>
        /// <exception cref="System.IO.IOException"> if the file does not exist  </exception>
        public override long FileLength(string name)
        {
            EnsureOpen();
            if (this.Writer != null)
            {
                return Writer.FileLength(name);
            }
            FileEntry e = Entries[IndexFileNames.StripSegmentName(name)];
            if (e == null)
            {
                throw new Exception(name);
            }
            return e.Length;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            return Writer.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Not implemented </summary>
        /// <exception cref="UnsupportedOperationException"> always: not supported by CFS  </exception>
        public override Lock MakeLock(string name)
        {
            throw new System.NotSupportedException();
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            Debug.Assert(!OpenForWrite);
            string id = IndexFileNames.StripSegmentName(name);
            FileEntry entry = Entries[id];
            if (entry == null)
            {
                throw new Exception("No sub-file with id " + id + " found (fileName=" + name + " files: " + Entries.Keys + ")");
            }
            return new IndexInputSlicerAnonymousInnerClassHelper(this, entry);
        }

        private class IndexInputSlicerAnonymousInnerClassHelper : IndexInputSlicer
        {
            private readonly CompoundFileDirectory OuterInstance;

            private Lucene.Net.Store.CompoundFileDirectory.FileEntry Entry;

            public IndexInputSlicerAnonymousInnerClassHelper(CompoundFileDirectory outerInstance, Lucene.Net.Store.CompoundFileDirectory.FileEntry entry)
                : base(outerInstance)
            {
                this.OuterInstance = outerInstance;
                this.Entry = entry;
            }

            public override void Dispose(bool disposing)
            {
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return OuterInstance.Handle.OpenSlice(sliceDescription, Entry.Offset + offset, length);
            }

            [Obsolete]
            public override IndexInput OpenFullSlice()
            {
                return OpenSlice("full-slice", 0, Entry.Length);
            }
        }

        public override string ToString()
        {
            return "CompoundFileDirectory(file=\"" + FileName + "\" in dir=" + Directory_Renamed + ")";
        }
    }
}