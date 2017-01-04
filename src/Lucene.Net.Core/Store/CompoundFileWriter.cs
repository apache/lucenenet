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
            internal string File { get; set; }

            internal long Length { get; set; }

            /// <summary>
            /// temporary holder for the start of this file's data section </summary>
            internal long Offset { get; set; }

            /// <summary>
            /// the directory which contains the file. </summary>
            internal Directory Dir { get; set; }
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

        private readonly Directory directory;
        private readonly IDictionary<string, FileEntry> entries = new Dictionary<string, FileEntry>();
        private readonly ISet<string> seenIDs = new HashSet<string>();

        // all entries that are written to a sep. file but not yet moved into CFS
        private readonly LinkedList<FileEntry> pendingEntries = new LinkedList<FileEntry>();

        private bool closed = false;
        private IndexOutput dataOut;
        private readonly AtomicBoolean outputTaken = new AtomicBoolean(false);
        internal readonly string entryTableName;
        internal readonly string dataFileName;

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
            directory = dir;
            entryTableName = IndexFileNames.SegmentFileName(IndexFileNames.StripExtension(name), "", IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
            dataFileName = name;
        }

        private IndexOutput GetOutput()
        {
            lock (this)
            {
                if (dataOut == null)
                {
                    bool success = false;
                    try
                    {
                        dataOut = directory.CreateOutput(dataFileName, IOContext.DEFAULT);
                        CodecUtil.WriteHeader(dataOut, DATA_CODEC, VERSION_CURRENT);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            IOUtils.CloseWhileHandlingException((IDisposable)dataOut);
                        }
                    }
                }
                return dataOut;
            }
        }

        /// <summary>
        /// Returns the directory of the compound file. </summary>
        internal Directory Directory
        {
            get
            {
                return directory;
            }
        }

        /// <summary>
        /// Returns the name of the compound file. </summary>
        internal string Name
        {
            get
            {
                return dataFileName;
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
            if (closed)
            {
                return;
            }
            System.IO.IOException priorException = null;
            IndexOutput entryTableOut = null;
            // TODO this code should clean up after itself
            // (remove partial .cfs/.cfe)
            try
            {
                if (pendingEntries.Count > 0 || outputTaken.Get())
                {
                    throw new InvalidOperationException("CFS has pending open files");
                }
                closed = true;
                // open the compound stream
                GetOutput();
                Debug.Assert(dataOut != null);
                CodecUtil.WriteFooter(dataOut);
            }
            catch (System.IO.IOException e)
            {
                priorException = e;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(priorException, dataOut);
            }
            try
            {
                entryTableOut = directory.CreateOutput(entryTableName, IOContext.DEFAULT);
                WriteEntryTable(entries.Values, entryTableOut);
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
            if (closed)
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
            IndexInput @is = fileEntry.Dir.OpenInput(fileEntry.File, IOContext.READ_ONCE);
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
                if (entries.ContainsKey(name))
                {
                    throw new System.ArgumentException("File " + name + " already exists");
                }
                FileEntry entry = new FileEntry();
                entry.File = name;
                entries[name] = entry;
                string id = IndexFileNames.StripSegmentName(name);
                Debug.Assert(!seenIDs.Contains(id), "file=\"" + name + "\" maps to id=\"" + id + "\", which was already written");
                seenIDs.Add(id);
                DirectCFSIndexOutput @out;

                if ((outputLocked = outputTaken.CompareAndSet(false, true)))
                {
                    @out = new DirectCFSIndexOutput(this, GetOutput(), entry, false);
                }
                else
                {
                    entry.Dir = this.directory;
                    @out = new DirectCFSIndexOutput(this, directory.CreateOutput(name, context), entry, true);
                }
                success = true;
                return @out;
            }
            finally
            {
                if (!success)
                {
                    entries.Remove(name);
                    if (outputLocked) // release the output lock if not successful
                    {
                        Debug.Assert(outputTaken.Get());
                        ReleaseOutputLock();
                    }
                }
            }
        }

        internal void ReleaseOutputLock()
        {
            outputTaken.CompareAndSet(true, false);
        }

        private void PrunePendingEntries()
        {
            // claim the output and copy all pending files in
            if (outputTaken.CompareAndSet(false, true))
            {
                try
                {
                    while (pendingEntries.Count > 0)
                    {
                        FileEntry entry = pendingEntries.First();
                        pendingEntries.RemoveFirst(); ;
                        CopyFileEntry(GetOutput(), entry);
                        entries[entry.File] = entry;
                    }
                }
                finally
                {
                    bool compareAndSet = outputTaken.CompareAndSet(true, false);
                    Debug.Assert(compareAndSet);
                }
            }
        }

        internal long FileLength(string name)
        {
            FileEntry fileEntry = entries[name];
            if (fileEntry == null)
            {
                throw new Exception(name + " does not exist");
            }
            return fileEntry.Length;
        }

        internal bool FileExists(string name)
        {
            return entries.ContainsKey(name);
        }

        internal string[] ListAll()
        {
            return entries.Keys.ToArray();
        }

        private sealed class DirectCFSIndexOutput : IndexOutput
        {
            private readonly CompoundFileWriter outerInstance;

            private readonly IndexOutput @delegate;
            private readonly long offset;
            private bool closed;
            private FileEntry entry;
            private long writtenBytes;
            private readonly bool isSeparate;

            internal DirectCFSIndexOutput(CompoundFileWriter outerInstance, IndexOutput @delegate, FileEntry entry, bool isSeparate)
                : base()
            {
                this.outerInstance = outerInstance;
                this.@delegate = @delegate;
                this.entry = entry;
                entry.Offset = offset = @delegate.FilePointer;
                this.isSeparate = isSeparate;
            }

            public override void Flush()
            {
                @delegate.Flush();
            }

            public override void Dispose()
            {
                if (!closed)
                {
                    closed = true;
                    entry.Length = writtenBytes;
                    if (isSeparate)
                    {
                        @delegate.Dispose();
                        // we are a separate file - push into the pending entries
                        outerInstance.pendingEntries.AddLast(entry);
                    }
                    else
                    {
                        // we have been written into the CFS directly - release the lock
                        outerInstance.ReleaseOutputLock();
                    }
                    // now prune all pending entries and push them into the CFS
                    outerInstance.PrunePendingEntries();
                }
            }

            public override long FilePointer
            {
                get
                {
                    return @delegate.FilePointer - offset;
                }
            }

            public override void Seek(long pos)
            {
                Debug.Assert(!closed);
                @delegate.Seek(offset + pos);
            }

            public override long Length
            {
                get
                {
                    Debug.Assert(!closed);
                    return @delegate.Length - offset;
                }
            }

            public override void WriteByte(byte b)
            {
                Debug.Assert(!closed);
                writtenBytes++;
                @delegate.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                Debug.Assert(!closed);
                writtenBytes += length;
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