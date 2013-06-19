using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Store
{
    internal sealed class CompoundFileWriter : IDisposable
    {
        internal sealed class FileEntry
        {
            public string File { get; set; }

            public long Length { get; set; }

            public long Offset { get; set; }

            public Directory Dir { get; set; }
        }

        // Before versioning started.
        internal const int FORMAT_PRE_VERSION = 0;

        // Segment name is not written in the file names.
        internal const int FORMAT_NO_SEGMENT_PREFIX = -1;

        // versioning for the .cfs file
        internal const String DATA_CODEC = "CompoundFileWriterData";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        // versioning for the .cfe file
        internal const String ENTRY_CODEC = "CompoundFileWriterEntries";

        private readonly Directory directory;
        private readonly IDictionary<String, FileEntry> entries = new HashMap<String, FileEntry>();
        private readonly ISet<String> seenIDs = new HashSet<String>();
        // all entries that are written to a sep. file but not yet moved into CFS
        private readonly Queue<FileEntry> pendingEntries = new Queue<FileEntry>();
        private bool closed = false;
        private IndexOutput dataOut;
        private readonly AtomicBoolean outputTaken = new AtomicBoolean(false);
        readonly String entryTableName;
        readonly String dataFileName;

        public CompoundFileWriter(Directory dir, string name)
        {
            if (dir == null)
                throw new ArgumentNullException("directory");
            if (name == null)
                throw new ArgumentNullException("name");
            directory = dir;
            entryTableName = IndexFileNames.SegmentFileName(
                IndexFileNames.StripExtension(name), "",
                IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
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

        public Directory Directory
        {
            get { return directory; }
        }

        public string Name
        {
            get { return dataFileName; }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (closed)
            {
                return;
            }
            Exception priorException = null;
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
                //assert dataOut != null;
            }
            catch (Exception e)
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
            catch (Exception e)
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

        private long CopyFileEntry(IndexOutput dataOut, FileEntry fileEntry)
        {
            IndexInput input = fileEntry.Dir.OpenInput(fileEntry.File, IOContext.READONCE);
            bool success = false;
            try
            {
                long startPtr = dataOut.FilePointer;
                long length = fileEntry.Length;
                dataOut.CopyBytes(input, length);
                // Verify that the output length diff is equal to original file
                long endPtr = dataOut.FilePointer;
                long diff = endPtr - startPtr;
                if (diff != length)
                    throw new System.IO.IOException("Difference in the output file offsets " + diff
                        + " does not match the original file length " + length);
                fileEntry.Offset = startPtr;
                success = true;
                return length;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                    // copy successful - delete file
                    fileEntry.Dir.DeleteFile(fileEntry.File);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
        }

        protected void WriteEntryTable(ICollection<FileEntry> entries, IndexOutput entryOut)
        {
            CodecUtil.WriteHeader(entryOut, ENTRY_CODEC, VERSION_CURRENT);
            entryOut.WriteVInt(entries.Count);
            foreach (FileEntry fe in entries)
            {
                entryOut.WriteString(IndexFileNames.StripSegmentName(fe.File));
                entryOut.WriteLong(fe.Offset);
                entryOut.WriteLong(fe.Length);
            }
        }

        internal IndexOutput CreateOutput(String name, IOContext context)
        {
            EnsureOpen();
            bool success = false;
            bool outputLocked = false;
            try
            {
                //assert name != null : "name must not be null";
                if (entries.ContainsKey(name))
                {
                    throw new ArgumentException("File " + name + " already exists");
                }
                FileEntry entry = new FileEntry();
                entry.File = name;
                entries[name] = entry;
                String id = IndexFileNames.StripSegmentName(name);
                //assert !seenIDs.contains(id): "file=\"" + name + "\" maps to id=\"" + id + "\", which was already written";
                seenIDs.Add(id);
                DirectCFSIndexOutput output;

                if ((outputLocked = outputTaken.CompareAndSet(false, true)))
                {
                    output = new DirectCFSIndexOutput(this, GetOutput(), entry, false);
                }
                else
                {
                    entry.Dir = this.directory;
                    if (directory.FileExists(name))
                    {
                        throw new ArgumentException("File " + name + " already exists");
                    }
                    output = new DirectCFSIndexOutput(this, directory.CreateOutput(name, context), entry,
                        true);
                }
                success = true;
                return output;
            }
            finally
            {
                if (!success)
                {
                    entries.Remove(name);
                    if (outputLocked)
                    { // release the output lock if not successful
                        //assert outputTaken.get();
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
                        FileEntry entry = pendingEntries.Dequeue();
                        CopyFileEntry(GetOutput(), entry);
                        entries[entry.File] = entry;
                    }
                }
                finally
                {
                    bool compareAndSet = outputTaken.CompareAndSet(true, false);
                    //assert compareAndSet;
                }
            }
        }

        internal long FileLength(string name)
        {
            FileEntry fileEntry = entries[name];
            if (fileEntry == null)
            {
                throw new System.IO.FileNotFoundException(name + " does not exist");
            }
            return fileEntry.Length;
        }

        internal bool FileExists(String name)
        {
            return entries.ContainsKey(name);
        }

        internal string[] ListAll()
        {
            return entries.Keys.ToArray();
        }

        private sealed class DirectCFSIndexOutput : IndexOutput
        {
            private readonly IndexOutput del;
            private readonly long offset;
            private bool closed;
            private FileEntry entry;
            private long writtenBytes;
            private readonly bool isSeparate;

            private readonly CompoundFileWriter parent;

            public DirectCFSIndexOutput(CompoundFileWriter parent, IndexOutput del, FileEntry entry,
                bool isSeparate)
                : base()
            {
                this.parent = parent;
                this.del = del;
                this.entry = entry;
                entry.Offset = offset = del.FilePointer;
                this.isSeparate = isSeparate;
            }

            public override void Flush()
            {
                del.Flush();
            }

            protected override void Dispose(bool disposing)
            {
                if (!closed)
                {
                    closed = true;
                    entry.Length = writtenBytes;
                    if (isSeparate)
                    {
                        del.Dispose();
                        // we are a separate file - push into the pending entries
                        parent.pendingEntries.Enqueue(entry);
                    }
                    else
                    {
                        // we have been written into the CFS directly - release the lock
                        parent.ReleaseOutputLock();
                    }
                    // now prune all pending entries and push them into the CFS
                    parent.PrunePendingEntries();
                }
            }

            public override long FilePointer
            {
                get { return del.FilePointer - offset; }
            }

            public override void Seek(long pos)
            {
                del.Seek(offset + pos);
            }

            public override long Length
            {
                get { return del.Length - offset; }
            }

            public override void WriteByte(byte b)
            {
                writtenBytes++;
                del.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                writtenBytes += length;
                del.WriteBytes(b, offset, length);
            }
        }
    }
}
