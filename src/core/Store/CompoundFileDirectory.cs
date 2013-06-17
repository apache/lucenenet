using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public sealed class CompoundFileDirectory : Directory
    {
        public sealed class FileEntry
        {
            public long Offset { get; set; }
            public long Length { get; set; }
        }

        private readonly Directory directory;
        private readonly string fileName;
        protected readonly int readBufferSize;
        private readonly IDictionary<string, FileEntry> entries;
        private readonly bool openForWrite;
        private static readonly IDictionary<string, FileEntry> SENTINEL = Collections.EmptyMap<string, FileEntry>();
        private readonly CompoundFileWriter writer;
        private readonly IndexInputSlicer handle;

        public CompoundFileDirectory(Directory directory, string fileName, IOContext context, bool openForWrite)
        {
            this.directory = directory;
            this.fileName = fileName;
            this.readBufferSize = BufferedIndexInput.BufferSize(context);
            this.isOpen = false;
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
                        IOUtils.CloseWhileHandlingException((IDisposable)handle);
                    }
                }
                this.isOpen = true;
                writer = null;
            }
            else
            {
                //assert !(directory instanceof CompoundFileDirectory) : "compound file inside of compound file: " + fileName;
                this.entries = SENTINEL;
                this.isOpen = true;
                writer = new CompoundFileWriter(directory, fileName);
                handle = null;
            }
        }

        private static readonly byte CODEC_MAGIC_BYTE1 = (byte)Number.URShift(CodecUtil.CODEC_MAGIC, 24);
        private static readonly byte CODEC_MAGIC_BYTE2 = (byte)Number.URShift(CodecUtil.CODEC_MAGIC, 16);
        private static readonly byte CODEC_MAGIC_BYTE3 = (byte)Number.URShift(CodecUtil.CODEC_MAGIC, 8);
        private static readonly byte CODEC_MAGIC_BYTE4 = (byte)CodecUtil.CODEC_MAGIC;

        private static IDictionary<string, FileEntry> ReadEntries(IndexInputSlicer handle, Directory dir, string name)
        {
            System.IO.IOException priorE = null;
            IndexInput stream = null, entriesStream = null;
            // read the first VInt. If it is negative, it's the version number
            // otherwise it's the count (pre-3.1 indexes)
            try
            {
                IDictionary<String, FileEntry> mapping;
                stream = handle.OpenFullSlice();
                int firstInt = stream.ReadVInt();
                // impossible for 3.0 to have 63 files in a .cfs, CFS writer was not visible
                // and separate norms/etc are outside of cfs.
                if (firstInt == CODEC_MAGIC_BYTE1)
                {
                    byte secondByte = stream.ReadByte();
                    byte thirdByte = stream.ReadByte();
                    byte fourthByte = stream.ReadByte();
                    if (secondByte != CODEC_MAGIC_BYTE2 ||
                        thirdByte != CODEC_MAGIC_BYTE3 ||
                        fourthByte != CODEC_MAGIC_BYTE4)
                    {
                        throw new CorruptIndexException("Illegal/impossible header for CFS file: "
                                                       + secondByte + "," + thirdByte + "," + fourthByte);
                    }
                    CodecUtil.CheckHeaderNoMagic(stream, CompoundFileWriter.DATA_CODEC,
                        CompoundFileWriter.VERSION_START, CompoundFileWriter.VERSION_START);
                    String entriesFileName = IndexFileNames.SegmentFileName(
                                                          IndexFileNames.StripExtension(name), "",
                                                          IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION);
                    entriesStream = dir.OpenInput(entriesFileName, IOContext.READONCE);
                    CodecUtil.CheckHeader(entriesStream, CompoundFileWriter.ENTRY_CODEC, CompoundFileWriter.VERSION_START, CompoundFileWriter.VERSION_START);
                    int numEntries = entriesStream.ReadVInt();
                    mapping = new HashMap<String, FileEntry>(numEntries);
                    for (int i = 0; i < numEntries; i++)
                    {
                        FileEntry fileEntry = new FileEntry();
                        String id = entriesStream.ReadString();
                        FileEntry previous = mapping[id] = fileEntry;
                        if (previous != null)
                        {
                            throw new CorruptIndexException("Duplicate cfs entry id=" + id + " in CFS: " + entriesStream);
                        }
                        fileEntry.Offset = entriesStream.ReadLong();
                        fileEntry.Length = entriesStream.ReadLong();
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

        private static IDictionary<String, FileEntry> ReadLegacyEntries(IndexInput stream, int firstInt)
        {
            IDictionary<String, FileEntry> entries = new HashMap<String, FileEntry>();
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
                count = stream.ReadVInt();
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
                long offset = stream.ReadLong();
                if (offset < 0 || offset > streamLength)
                {
                    throw new CorruptIndexException("Invalid CFS entry offset: " + offset + " (resource: " + stream + ")");
                }
                String id = stream.ReadString();

                if (stripSegmentName)
                {
                    // Fix the id to not include the segment names. This is relevant for
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

                FileEntry previous = entries[id] = entry;
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

        public Directory Directory
        {
            get { return directory; }
        }

        public string Name
        {
            get { return fileName; }
        }

        protected override void Dispose(bool disposing)
        {
            if (!isOpen)
            {
                // allow double close - usually to be consistent with other closeables
                return; // already closed
            }
            isOpen = false;
            if (writer != null)
            {
                //assert openForWrite;
                writer.Dispose();
            }
            else
            {
                IOUtils.Close(handle);
            }
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            lock (this)
            {
                EnsureOpen();
                //assert !openForWrite;
                String id = IndexFileNames.StripSegmentName(name);
                FileEntry entry = entries[id];
                if (entry == null)
                {
                    throw new System.IO.FileNotFoundException("No sub-file with id " + id + " found (fileName=" + name + " files: " + string.Join(", ", entries.Keys) + ")");
                }
                return handle.OpenSlice(name, entry.Offset, entry.Length);
            }
        }

        public override string[] ListAll()
        {
            EnsureOpen();
            String[] res;
            if (writer != null)
            {
                res = writer.ListAll();
            }
            else
            {
                res = entries.Keys.ToArray();
                // Add the segment name
                String seg = IndexFileNames.ParseSegmentName(fileName);
                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = seg + res[i];
                }
            }
            return res;
        }

        public override bool FileExists(string name)
        {
            EnsureOpen();
            if (this.writer != null)
            {
                return writer.FileExists(name);
            }
            return entries.ContainsKey(IndexFileNames.StripSegmentName(name));
        }

        public override void DeleteFile(string name)
        {
            throw new NotSupportedException();
        }

        public void RenameFile(string from, string to)
        {
            throw new NotSupportedException();
        }

        public override long FileLength(string name)
        {
            EnsureOpen();
            if (this.writer != null)
            {
                return writer.FileLength(name);
            }
            FileEntry e = entries[IndexFileNames.StripSegmentName(name)];
            if (e == null)
                throw new System.IO.FileNotFoundException(name);
            return e.Length;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            return writer.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            throw new NotSupportedException();
        }

        public override Lock MakeLock(string name)
        {
            throw new NotSupportedException();
        }

        private class AnonymousIndexInputSlicer : IndexInputSlicer
        {
            private readonly FileEntry entry;
            private readonly IndexInputSlicer handle;

            public AnonymousIndexInputSlicer(FileEntry entry, IndexInputSlicer handle)
            {
                this.entry = entry;
                this.handle = handle;
            }

            public override void Dispose(bool disposing)
            {
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return handle.OpenSlice(sliceDescription, entry.Offset + offset, length);
            }

            public override IndexInput OpenFullSlice()
            {
                return OpenSlice("full-slice", 0, entry.Length);
            }
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            //assert !openForWrite;
            String id = IndexFileNames.StripSegmentName(name);
            FileEntry entry = entries[id];
            if (entry == null)
            {
                throw new System.IO.FileNotFoundException("No sub-file with id " + id + " found (fileName=" + name + " files: " + string.Join(", ", entries.Keys) + ")");
            }

            return new AnonymousIndexInputSlicer(entry, handle);
        }

        public override string ToString()
        {
            return "CompoundFileDirectory(file=\"" + fileName + "\" in dir=" + directory + ")";
        }
    }
}
