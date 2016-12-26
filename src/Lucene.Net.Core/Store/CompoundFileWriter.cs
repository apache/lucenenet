using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


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
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Combines multiple files into a single compound file.
    /// </summary>
    /// <seealso cref= CompoundFileDirectory
    /// @lucene.internal </seealso>
    internal sealed class CompoundFileWriter : IDisposable
    {
        private sealed class FileEntry
        {
            /// <summary>
            /// source file </summary>
            internal string File; // LUCENENET TODO: make property

            internal long Length; // LUCENENET TODO: make property

            /// <summary>
            /// temporary holder for the start of this file's data section </summary>
            internal long Offset; // LUCENENET TODO: make property

            /// <summary>
            /// the directory which contains the file. </summary>
            internal Directory Dir; // LUCENENET TODO: make property
        }

        // Before versioning started.
        internal const int FORMAT_PRE_VERSION = 0;

        // Segment name is not written in the file names.
        internal const int FORMAT_NO_SEGMENT_PREFIX = -1;

        // versioning for the .cfs file
        internal const string DATA_CODEC = "CompoundFileWriterData";

        internal const int VERSION_START = 0;
        internal const int VERSION_CHECKSUM = 1;
        internal const int VERSION_CURRENT = VERSION_CHECKSUM;

        // versioning for the .cfe file
        internal const string ENTRY_CODEC = "CompoundFileWriterEntries";

        private readonly Directory Directory_Renamed;
        private readonly IDictionary<string, FileEntry> Entries = new Dictionary<string, FileEntry>();
        private readonly ISet<string> SeenIDs = new HashSet<string>();

        // all entries that are written to a sep. file but not yet moved into CFS
        private readonly LinkedList<FileEntry> PendingEntries = new LinkedList<FileEntry>();

        private bool Closed = false;
        private IndexOutput DataOut;
        private readonly AtomicBoolean OutputTaken = new AtomicBoolean(false);
        internal readonly string EntryTableName;
        internal readonly string DataFileName;

        /// <summary>
        /// Create the compound stream in the specified file. The file name is the
        /// entire name (no extensions are added).
        /// </summary>
        /// <exception cref="NullPointerException">
        ///           if <code>dir</code> or <code>name</code> is null </exception>
        internal CompoundFileWriter(Directory dir, string name)
        {
            if (dir == null)
            {
                throw new System.NullReferenceException("directory cannot be null");
            }
            if (name == null)
            {
                throw new System.NullReferenceException("name cannot be null");
            }
            Directory_Renamed = dir;
            EntryTableName = IndexFileNames.SegmentFileName(IndexFileNames.StripExtension(name), "", IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
            DataFileName = name;
        }

        private IndexOutput Output // LUCENENET TODO: Change to GetOutput() (throws exceptions)
        {
            get
            {
                lock (this)
                {
                    if (DataOut == null)
                    {
                        bool success = false;
                        try
                        {
                            DataOut = Directory_Renamed.CreateOutput(DataFileName, IOContext.DEFAULT);
                            CodecUtil.WriteHeader(DataOut, DATA_CODEC, VERSION_CURRENT);
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                            {
                                IOUtils.CloseWhileHandlingException(DataOut);
                            }
                        }
                    }
                    return DataOut;
                }
            }
        }

        /// <summary>
        /// Returns the directory of the compound file. </summary>
        internal Directory Directory
        {
            get
            {
                return Directory_Renamed;
            }
        }

        /// <summary>
        /// Returns the name of the compound file. </summary>
        internal string Name
        {
            get
            {
                return DataFileName;
            }
        }

        /// <summary>
        /// Closes all resources and writes the entry table
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///           if close() had been called before or if no file has been added to
        ///           this object </exception>
        public void Dispose()
        {
            if (Closed)
            {
                return;
            }
            System.IO.IOException priorException = null;
            IndexOutput entryTableOut = null;
            // TODO this code should clean up after itself
            // (remove partial .cfs/.cfe)
            try
            {
                if (PendingEntries.Count > 0 || OutputTaken.Get())
                {
                    throw new InvalidOperationException("CFS has pending open files");
                }
                Closed = true;
                // open the compound stream
                GetOutput();
                Debug.Assert(DataOut != null);
                CodecUtil.WriteFooter(DataOut);
            }
            catch (System.IO.IOException e)
            {
                priorException = e;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(priorException, DataOut);
            }
            try
            {
                entryTableOut = Directory_Renamed.CreateOutput(EntryTableName, IOContext.DEFAULT);
                WriteEntryTable(Entries.Values, entryTableOut);
            }
            catch (System.IO.IOException e)
            {
                priorException = e;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(priorException, entryTableOut);
            }
        }

        private void EnsureOpen()
        {
            if (Closed)
            {
                throw new AlreadyClosedException("CFS Directory is already closed");
            }
        }

        /// <summary>
        /// Copy the contents of the file with specified extension into the provided
        /// output stream.
        /// </summary>
        private long CopyFileEntry(IndexOutput dataOut, FileEntry fileEntry)
        {
            IndexInput @is = fileEntry.Dir.OpenInput(fileEntry.File, IOContext.READONCE);
            bool success = false;
            try
            {
                long startPtr = dataOut.FilePointer;
                long length = fileEntry.Length;
                dataOut.CopyBytes(@is, length);
                // Verify that the output length diff is equal to original file
                long endPtr = dataOut.FilePointer;
                long diff = endPtr - startPtr;
                if (diff != length)
                {
                    throw new System.IO.IOException("Difference in the output file offsets " + diff + " does not match the original file length " + length);
                }
                fileEntry.Offset = startPtr;
                success = true;
                return length;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(@is);
                    // copy successful - delete file
                    fileEntry.Dir.DeleteFile(fileEntry.File);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(@is);
                }
            }
        }

        private void WriteEntryTable(ICollection<FileEntry> entries, IndexOutput entryOut)
        {
            CodecUtil.WriteHeader(entryOut, ENTRY_CODEC, VERSION_CURRENT);
            entryOut.WriteVInt(entries.Count);
            foreach (FileEntry fe in entries)
            {
                entryOut.WriteString(IndexFileNames.StripSegmentName(fe.File));
                entryOut.WriteLong(fe.Offset);
                entryOut.WriteLong(fe.Length);
            }
            CodecUtil.WriteFooter(entryOut);
        }

        internal IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            bool success = false;
            bool outputLocked = false;
            try
            {
                Debug.Assert(name != null, "name must not be null");
                if (Entries.ContainsKey(name))
                {
                    throw new System.ArgumentException("File " + name + " already exists");
                }
                FileEntry entry = new FileEntry();
                entry.File = name;
                Entries[name] = entry;
                string id = IndexFileNames.StripSegmentName(name);
                Debug.Assert(!SeenIDs.Contains(id), "file=\"" + name + "\" maps to id=\"" + id + "\", which was already written");
                SeenIDs.Add(id);
                DirectCFSIndexOutput @out;

                if ((outputLocked = OutputTaken.CompareAndSet(false, true)))
                {
                    @out = new DirectCFSIndexOutput(this, Output, entry, false);
                }
                else
                {
                    entry.Dir = this.Directory_Renamed;
                    @out = new DirectCFSIndexOutput(this, Directory_Renamed.CreateOutput(name, context), entry, true);
                }
                success = true;
                return @out;
            }
            finally
            {
                if (!success)
                {
                    Entries.Remove(name);
                    if (outputLocked) // release the output lock if not successful
                    {
                        Debug.Assert(OutputTaken.Get());
                        ReleaseOutputLock();
                    }
                }
            }
        }

        private IndexOutput GetOutput() // LUCENENET TODO: Move to where Output property is now and delete unnecessary Output property
        {
            lock (this)
            {
                if (DataOut == null)
                {
                    bool success = false;
                    try
                    {
                        DataOut = Directory.CreateOutput(DataFileName, IOContext.DEFAULT);
                        CodecUtil.WriteHeader(DataOut, DATA_CODEC, VERSION_CURRENT);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            IOUtils.CloseWhileHandlingException((IDisposable)DataOut);
                        }
                    }
                }
                return DataOut;
            }
        }

        internal void ReleaseOutputLock()
        {
            OutputTaken.CompareAndSet(true, false);
        }

        private void PrunePendingEntries()
        {
            // claim the output and copy all pending files in
            if (OutputTaken.CompareAndSet(false, true))
            {
                try
                {
                    while (PendingEntries.Count > 0)
                    {
                        FileEntry entry = PendingEntries.First();
                        PendingEntries.RemoveFirst(); ;
                        CopyFileEntry(Output, entry);
                        Entries[entry.File] = entry;
                    }
                }
                finally
                {
                    bool compareAndSet = OutputTaken.CompareAndSet(true, false);
                    Debug.Assert(compareAndSet);
                }
            }
        }

        internal long FileLength(string name)
        {
            FileEntry fileEntry = Entries[name];
            if (fileEntry == null)
            {
                throw new Exception(name + " does not exist");
            }
            return fileEntry.Length;
        }

        internal bool FileExists(string name)
        {
            return Entries.ContainsKey(name);
        }

        internal string[] ListAll()
        {
            return Entries.Keys.ToArray();
        }

        private sealed class DirectCFSIndexOutput : IndexOutput
        {
            private readonly CompoundFileWriter OuterInstance;

            private readonly IndexOutput @delegate;
            private readonly long Offset;
            private bool Closed;
            private FileEntry Entry;
            private long WrittenBytes;
            private readonly bool IsSeparate;

            internal DirectCFSIndexOutput(CompoundFileWriter outerInstance, IndexOutput @delegate, FileEntry entry, bool isSeparate)
                : base()
            {
                this.OuterInstance = outerInstance;
                this.@delegate = @delegate;
                this.Entry = entry;
                entry.Offset = Offset = @delegate.FilePointer;
                this.IsSeparate = isSeparate;
            }

            public override void Flush()
            {
                @delegate.Flush();
            }

            public override void Dispose()
            {
                if (!Closed)
                {
                    Closed = true;
                    Entry.Length = WrittenBytes;
                    if (IsSeparate)
                    {
                        @delegate.Dispose();
                        // we are a separate file - push into the pending entries
                        OuterInstance.PendingEntries.AddLast(Entry);
                    }
                    else
                    {
                        // we have been written into the CFS directly - release the lock
                        OuterInstance.ReleaseOutputLock();
                    }
                    // now prune all pending entries and push them into the CFS
                    OuterInstance.PrunePendingEntries();
                }
            }

            public override long FilePointer
            {
                get
                {
                    return @delegate.FilePointer - Offset;
                }
            }

            public override void Seek(long pos)
            {
                Debug.Assert(!Closed);
                @delegate.Seek(Offset + pos);
            }

            public override long Length
            {
                get
                {
                    Debug.Assert(!Closed);
                    return @delegate.Length - Offset;
                }
            }

            public override void WriteByte(byte b)
            {
                Debug.Assert(!Closed);
                WrittenBytes++;
                @delegate.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                Debug.Assert(!Closed);
                WrittenBytes += length;
                @delegate.WriteBytes(b, offset, length);
            }

            public override long Checksum
            {
                get
                {
                    return @delegate.Checksum;
                }
            }
        }
    }
}