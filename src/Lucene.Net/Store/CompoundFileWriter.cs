using J2N.Collections.Generic.Extensions;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="CompoundFileDirectory"/>
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
        private readonly ISet<string> seenIDs = new JCG.HashSet<string>();
        // all entries that are written to a sep. file but not yet moved into CFS
        private readonly LinkedList<FileEntry> pendingEntries = new LinkedList<FileEntry>();
        private bool closed = false;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput dataOut;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly AtomicBoolean outputTaken = new AtomicBoolean(false);
        internal readonly string entryTableName;
        internal readonly string dataFileName;

        /// <summary>
        /// Create the compound stream in the specified file. The file name is the
        /// entire name (no extensions are added).
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///           if <paramref name="dir"/> or <paramref name="name"/> is <c>null</c> </exception>
        internal CompoundFileWriter(Directory dir, string name)
        {
            // LUCENENET specific - changed order to take advantage of throw expression and
            // changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            directory = dir ?? throw new ArgumentNullException(nameof(directory), $"{nameof(directory)} cannot be null");
            dataFileName = name ?? throw new ArgumentNullException(nameof(name), $"{nameof(name)} cannot be null");
            entryTableName = IndexFileNames.SegmentFileName(IndexFileNames.StripExtension(name), "", IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
        }

        private IndexOutput GetOutput()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (dataOut is null)
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
                            IOUtils.DisposeWhileHandlingException(dataOut);
                        }
                    }
                }
                return dataOut;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the directory of the compound file. </summary>
        internal Directory Directory => directory;

        /// <summary>
        /// Returns the name of the compound file. </summary>
        internal string Name => dataFileName;

        /// <summary>
        /// Disposes all resources and writes the entry table
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///           if <see cref="Dispose"/> had been called before or if no file has been added to
        ///           this object </exception>
        public void Dispose()
        {
            if (closed)
            {
                return;
            }
            Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
            IndexOutput entryTableOut = null;
            // TODO this code should clean up after itself
            // (remove partial .cfs/.cfe)
            try
            {
                if (pendingEntries.Count > 0 || outputTaken)
                {
                    throw IllegalStateException.Create("CFS has pending open files");
                }
                closed = true;
                // open the compound stream
                GetOutput();
                if (Debugging.AssertsEnabled) Debugging.Assert(dataOut != null);
                CodecUtil.WriteFooter(dataOut);
            }
            catch (Exception e) when (e.IsIOException())
            {
                priorException = e;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(priorException, dataOut);
            }
            try
            {
                entryTableOut = directory.CreateOutput(entryTableName, IOContext.DEFAULT);
                WriteEntryTable(entries.Values, entryTableOut);
            }
            catch (Exception e) when (e.IsIOException())
            {
                priorException = e;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(priorException, entryTableOut);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureOpen()
        {
            if (closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "CFS Directory is already disposed.");
            }
        }

        /// <summary>
        /// Copy the contents of the file with specified extension into the provided
        /// output stream.
        /// </summary>
        private static long CopyFileEntry(IndexOutput dataOut, FileEntry fileEntry) // LUCENENET: CA1822: Mark members as static
        {
            IndexInput @is = fileEntry.Dir.OpenInput(fileEntry.File, IOContext.READ_ONCE);
            bool success = false;
            try
            {
                long startPtr = dataOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                long length = fileEntry.Length;
                dataOut.CopyBytes(@is, length);
                // Verify that the output length diff is equal to original file
                long endPtr = dataOut.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                long diff = endPtr - startPtr;
                if (diff != length)
                {
                    throw new IOException("Difference in the output file offsets " + diff + " does not match the original file length " + length);
                }
                fileEntry.Offset = startPtr;
                success = true;
                return length;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(@is);
                    // copy successful - delete file
                    fileEntry.Dir.DeleteFile(fileEntry.File);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(@is);
                }
            }
        }

        private static void WriteEntryTable(ICollection<FileEntry> entries, IndexOutput entryOut) // LUCENENET: CA1822: Mark members as static
        {
            CodecUtil.WriteHeader(entryOut, ENTRY_CODEC, VERSION_CURRENT);
            entryOut.WriteVInt32(entries.Count);
            foreach (FileEntry fe in entries)
            {
                entryOut.WriteString(IndexFileNames.StripSegmentName(fe.File));
                entryOut.WriteInt64(fe.Offset);
                entryOut.WriteInt64(fe.Length);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(name != null, "name must not be null");
                if (entries.ContainsKey(name))
                {
                    throw new ArgumentException("File " + name + " already exists");
                }
                FileEntry entry = new FileEntry();
                entry.File = name;
                entries[name] = entry;
                string id = IndexFileNames.StripSegmentName(name);
                if (Debugging.AssertsEnabled) Debugging.Assert(!seenIDs.Contains(id), "file=\"{0}\" maps to id=\"{1}\", which was already written", name, id);
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(outputTaken);
                        ReleaseOutputLock();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        FileEntry entry = pendingEntries.First.Value;
                        pendingEntries.Remove(entry);
                        CopyFileEntry(GetOutput(), entry);
                        entries[entry.File] = entry;
                    }
                }
                finally
                {
                    bool compareAndSet = outputTaken.CompareAndSet(true, false);
                    if (Debugging.AssertsEnabled) Debugging.Assert(compareAndSet);
                }
            }
        }

        internal long FileLength(string name)
        {
            FileEntry fileEntry = entries[name];
            if (fileEntry is null)
            {
                throw new FileNotFoundException(name + " does not exist");
            }
            return fileEntry.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool FileExists(string name)
        {
            return entries.ContainsKey(name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            private readonly FileEntry entry; // LUCENENET: marked readonly
            private long writtenBytes;
            private readonly bool isSeparate;

            internal DirectCFSIndexOutput(CompoundFileWriter outerInstance, IndexOutput @delegate, FileEntry entry, bool isSeparate)
                : base()
            {
                this.outerInstance = outerInstance;
                this.@delegate = @delegate;
                this.entry = entry;
                entry.Offset = offset = @delegate.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                this.isSeparate = isSeparate;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public override void Flush()
            {
                @delegate.Flush();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && !closed)
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

            public override long Position => @delegate.Position - offset; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Seek(long pos)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!closed);
                @delegate.Seek(offset + pos);
            }

            public override long Length
            {
                get
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!closed);
                    return @delegate.Length - offset;
                }
            }

            public override void WriteByte(byte b)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!closed);
                writtenBytes++;
                @delegate.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!closed);
                writtenBytes += length;
                @delegate.WriteBytes(b, offset, length);
            }

            public override long Checksum => @delegate.Checksum;
        }
    }
}