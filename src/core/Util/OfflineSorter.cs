namespace Lucene.Net.Util
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

    /// <summary>
    /// On-disk sorting of byte arrays. Each byte array (entry) is a composed of the following
    /// fields:
    /// <ul>
    ///   <li>(two bytes) length of the following byte array,
    ///   <li>exactly the above count of bytes for the sequence to be sorted.
    /// </ul>
    /// </summary>
    /// <seealso cref= #sort(File, File)
    /// @lucene.experimental
    /// @lucene.internal </seealso>

    //LUCENE PORT NOTE: Marked as experimental and does not port well so it was left out
    /*public sealed class OfflineSorter
    {
        private bool InstanceFieldsInitialized = false;

        private void InitializeInstanceFields()
        {
            Buffer = new BytesRefArray(BufferBytesUsed);
        }

        /// <summary>
        /// Convenience constant for megabytes </summary>
        public const long MB = 1024 * 1024;
        /// <summary>
        /// Convenience constant for gigabytes </summary>
        public static readonly long GB = MB * 1024;

        /// <summary>
        /// Minimum recommended buffer size for sorting.
        /// </summary>
        public const long MIN_BUFFER_SIZE_MB = 32;

        /// <summary>
        /// Absolute minimum required buffer size for sorting.
        /// </summary>
        public static readonly long ABSOLUTE_MIN_SORT_BUFFER_SIZE = MB / 2;
        private const string MIN_BUFFER_SIZE_MSG = "At least 0.5MB RAM buffer is needed";

        /// <summary>
        /// Maximum number of temporary files before doing an intermediate merge.
        /// </summary>
        public const int MAX_TEMPFILES = 128;

        /// <summary>
        /// A bit more descriptive unit for constructors.
        /// </summary>
        /// <seealso cref= #automatic() </seealso>
        /// <seealso cref= #megabytes(long) </seealso>
        public sealed class BufferSize
        {
            internal readonly int Bytes;

            internal BufferSize(long bytes)
            {
                if (bytes > int.MaxValue)
                {
                    throw new System.ArgumentException("Buffer too large for Java (" + (int.MaxValue / MB) + "mb max): " + bytes);
                }

                if (bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
                {
                    throw new System.ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + bytes);
                }

                this.Bytes = (int)bytes;
            }

            /// <summary>
            /// Creates a <seealso cref="BufferSize"/> in MB. The given
            /// values must be &gt; 0 and &lt; 2048.
            /// </summary>
            public static BufferSize Megabytes(long mb)
            {
                return new BufferSize(mb * MB);
            }

            /// <summary>
            /// Approximately half of the currently available free heap, but no less
            /// than <seealso cref="#ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>. However if current heap allocation
            /// is insufficient or if there is a large portion of unallocated heap-space available
            /// for sorting consult with max allowed heap size.
            /// </summary>
            public static BufferSize Automatic()
            {
                Runtime rt = Runtime.Runtime;

                // take sizes in "conservative" order
                long max = rt.maxMemory(); // max allocated
                long total = rt.totalMemory(); // currently allocated
                long free = rt.freeMemory(); // unused portion of currently allocated
                long totalAvailableBytes = max - total + free;

                // by free mem (attempting to not grow the heap for this)
                long sortBufferByteSize = free / 2;
                long minBufferSizeBytes = MIN_BUFFER_SIZE_MB * MB;
                if (sortBufferByteSize < minBufferSizeBytes || totalAvailableBytes > 10 * minBufferSizeBytes) // lets see if we need/should to grow the heap
                {
                    if (totalAvailableBytes / 2 > minBufferSizeBytes) // there is enough mem for a reasonable buffer
                    {
                        sortBufferByteSize = totalAvailableBytes / 2; // grow the heap
                    }
                    else
                    {
                        //heap seems smallish lets be conservative fall back to the free/2
                        sortBufferByteSize = Math.Max(ABSOLUTE_MIN_SORT_BUFFER_SIZE, sortBufferByteSize);
                    }
                }
                return new BufferSize(Math.Min((long)int.MaxValue, sortBufferByteSize));
            }
        }

        /// <summary>
        /// Sort info (debugging mostly).
        /// </summary>
        public class SortInfo
        {
            internal bool InstanceFieldsInitialized = false;

            internal virtual void InitializeInstanceFields()
            {
                BufferSize = OuterInstance.RamBufferSize.Bytes;
            }

            private readonly OfflineSorter OuterInstance;

            /// <summary>
            /// number of temporary files created when merging partitions </summary>
            public int TempMergeFiles;
            /// <summary>
            /// number of partition merges </summary>
            public int MergeRounds;
            /// <summary>
            /// number of lines of data read </summary>
            public int Lines;
            /// <summary>
            /// time spent merging sorted partitions (in milliseconds) </summary>
            public long MergeTime;
            /// <summary>
            /// time spent sorting data (in milliseconds) </summary>
            public long SortTime;
            /// <summary>
            /// total time spent (in milliseconds) </summary>
            public long TotalTime;
            /// <summary>
            /// time spent in i/o read (in milliseconds) </summary>
            public long ReadTime;
            /// <summary>
            /// read buffer size (in bytes) </summary>
            public long BufferSize;

            /// <summary>
            /// create a new SortInfo (with empty statistics) for debugging </summary>
            public SortInfo(OfflineSorter outerInstance)
            {
                this.OuterInstance = outerInstance;

                if (!InstanceFieldsInitialized)
                {
                    InitializeInstanceFields();
                    InstanceFieldsInitialized = true;
                }
            }

            public override string ToString()
            {
                return string.Format(Locale.ROOT, "time=%.2f sec. total (%.2f reading, %.2f sorting, %.2f merging), lines=%d, temp files=%d, merges=%d, soft ram limit=%.2f MB", TotalTime / 1000.0d, ReadTime / 1000.0d, SortTime / 1000.0d, MergeTime / 1000.0d, Lines, TempMergeFiles, MergeRounds, (double)BufferSize / MB);
            }
        }

        private readonly BufferSize RamBufferSize;
        private readonly DirectoryInfo TempDirectory;

        private readonly Counter BufferBytesUsed = Counter.NewCounter();
        private BytesRefArray Buffer;
        private SortInfo sortInfo;
        private int MaxTempFiles;
        private readonly IComparer<BytesRef> comparator;

        /// <summary>
        /// Default comparator: sorts in binary (codepoint) order </summary>
        public static readonly IComparer<BytesRef> DEFAULT_COMPARATOR = BytesRef.UTF8SortedAsUnicodeComparator;

        /// <summary>
        /// Defaults constructor.
        /// </summary>
        /// <seealso cref= #defaultTempDir() </seealso>
        /// <seealso cref= BufferSize#automatic() </seealso>
        public OfflineSorter()
            : this(DEFAULT_COMPARATOR, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
        }

        /// <summary>
        /// Defaults constructor with a custom comparator.
        /// </summary>
        /// <seealso cref= #defaultTempDir() </seealso>
        /// <seealso cref= BufferSize#automatic() </seealso>
        public OfflineSorter(IComparer<BytesRef> comparator)
            : this(comparator, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
        }

        /// <summary>
        /// All-details constructor.
        /// </summary>
        public OfflineSorter(IComparer<BytesRef> comparator, BufferSize ramBufferSize, DirectoryInfo tempDirectory, int maxTempfiles)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
            if (ramBufferSize.Bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
            {
                throw new System.ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + ramBufferSize.Bytes);
            }

            if (maxTempfiles < 2)
            {
                throw new System.ArgumentException("maxTempFiles must be >= 2");
            }

            this.RamBufferSize = ramBufferSize;
            this.TempDirectory = tempDirectory;
            this.MaxTempFiles = maxTempfiles;
            this.comparator = comparator;
        }

        /// <summary>
        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
        /// memory may deviate from the hint (may be smaller or larger).
        /// </summary>
        public SortInfo Sort(FileInfo input, FileInfo output)
        {
            sortInfo = new SortInfo(this);
            sortInfo.TotalTime = DateTime.Now.Millisecond;

            output.Delete();

            List<FileInfo> merges = new List<FileInfo>();
            bool success2 = false;
            try
            {
                ByteSequencesReader @is = new ByteSequencesReader(input);
                bool success = false;
                try
                {
                    int lines = 0;
                    while ((lines = ReadPartition(@is)) > 0)
                    {
                        merges.Add(SortPartition(lines));
                        sortInfo.TempMergeFiles++;
                        sortInfo.Lines += lines;

                        // Handle intermediate merges.
                        if (merges.Count == MaxTempFiles)
                        {
                            FileInfo intermediate = FileInfo.createTempFile("sort", "intermediate", TempDirectory);
                            try
                            {
                                MergePartitions(merges, intermediate);
                            }
                            finally
                            {
                                foreach (FileInfo file in merges)
                                {
                                    file.Delete();
                                }
                                merges.Clear();
                                merges.Add(intermediate);
                            }
                            sortInfo.TempMergeFiles++;
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Close(@is);
                    }
                    else
                    {
                        IOUtils.CloseWhileHandlingException(@is);
                    }
                }

                // One partition, try to rename or copy if unsuccessful.
                if (merges.Count == 1)
                {
                    FileInfo single = merges[0];
                    // If simple rename doesn't work this means the output is
                    // on a different volume or something. Copy the input then.
                    if (!single.RenameTo(output))
                    {
                        Copy(single, output);
                    }
                }
                else
                {
                    // otherwise merge the partitions with a priority queue.
                    MergePartitions(merges, output);
                }
                success2 = true;
            }
            finally
            {
                foreach (FileInfo file in merges)
                {
                    file.Delete();
                }
                if (!success2)
                {
                    output.Delete();
                }
            }

            sortInfo.TotalTime = (DateTime.Now.Millisecond - sortInfo.TotalTime);
            return sortInfo;
        }

        /// <summary>
        /// Returns the default temporary directory. By default, java.io.tmpdir. If not accessible
        /// or not available, an IOException is thrown
        /// </summary>
        public static DirectoryInfo DefaultTempDir()
        {
            string tempDirPath = System.getProperty("java.io.tmpdir");
            if (tempDirPath == null)
            {
                throw new System.IO.IOException("Java has no temporary folder property (java.io.tmpdir)?");
            }

            DirectoryInfo tempDirectory = new DirectoryInfo(tempDirPath);
            if (!tempDirectory.Exists || !tempDirectory.CanWrite())
            {
                throw new System.IO.IOException("Java's temporary folder not present or writeable?: " + tempDirectory.AbsolutePath);
            }
            return tempDirectory;
        }

        /// <summary>
        /// Copies one file to another.
        /// </summary>
        private static void Copy(FileInfo file, FileInfo output)
        {
            // 64kb copy buffer (empirical pick).
            sbyte[] buffer = new sbyte[16 * 1024];
            InputStream @is = null;
            OutputStream os = null;
            try
            {
                @is = new FileInputStream(file);
                os = new FileOutputStream(output);
                int length;
                while ((length = @is.read(buffer)) > 0)
                {
                    os.write(buffer, 0, length);
                }
            }
            finally
            {
                IOUtils.close(@is, os);
            }
        }

        /// <summary>
        /// Sort a single partition in-memory. </summary>
        protected internal FileInfo SortPartition(int len)
        {
            BytesRefArray data = this.Buffer;
            FileInfo tempFile = FileInfo.createTempFile("sort", "partition", TempDirectory);

            long start = DateTime.Now.Millisecond;
            sortInfo.SortTime += (DateTime.Now.Millisecond - start);

            ByteSequencesWriter @out = new ByteSequencesWriter(tempFile);
            BytesRef spare;
            try
            {
                BytesRefIterator iter = Buffer.Iterator(comparator);
                while ((spare = iter.Next()) != null)
                {
                    Debug.Assert(spare.Length <= short.MaxValue);
                    @out.Write(spare);
                }

                @out.Dispose();

                // Clean up the buffer for the next partition.
                data.Clear();
                return tempFile;
            }
            finally
            {
                IOUtils.Close(@out);
            }
        }

        /// <summary>
        /// Merge a list of sorted temporary files (partitions) into an output file </summary>
        internal void MergePartitions(IList<FileInfo> merges, FileInfo outputFile)
        {
            long start = DateTime.Now.Millisecond;

            ByteSequencesWriter @out = new ByteSequencesWriter(outputFile);

            PriorityQueue<FileAndTop> queue = new PriorityQueueAnonymousInnerClassHelper(this, merges.Count);

            ByteSequencesReader[] streams = new ByteSequencesReader[merges.Count];
            try
            {
                // Open streams and read the top for each file
                for (int i = 0; i < merges.Count; i++)
                {
                    streams[i] = new ByteSequencesReader(merges[i]);
                    sbyte[] line = streams[i].Read();
                    if (line != null)
                    {
                        queue.InsertWithOverflow(new FileAndTop(i, line));
                    }
                }

                // Unix utility sort() uses ordered array of files to pick the next line from, updating
                // it as it reads new lines. The PQ used here is a more elegant solution and has
                // a nicer theoretical complexity bound :) The entire sorting process is I/O bound anyway
                // so it shouldn't make much of a difference (didn't check).
                FileAndTop top;
                while ((top = queue.Top()) != null)
                {
                    @out.Write(top.Current);
                    if (!streams[top.Fd].Read(top.Current))
                    {
                        queue.Pop();
                    }
                    else
                    {
                        queue.UpdateTop();
                    }
                }

                SortInfo.MergeTime += System.currentTimeMillis() - start;
                SortInfo.MergeRounds++;
            }
            finally
            {
                // The logic below is: if an exception occurs in closing out, it has a priority over exceptions
                // happening in closing streams.
                try
                {
                    IOUtils.Close(streams);
                }
                finally
                {
                    IOUtils.Close(@out);
                }
            }
        }

        private class PriorityQueueAnonymousInnerClassHelper : PriorityQueue<FileAndTop>
        {
            private readonly OfflineSorter OuterInstance;

            public PriorityQueueAnonymousInnerClassHelper(OfflineSorter outerInstance, int size)
                : base(size)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override bool LessThan(FileAndTop a, FileAndTop b)
            {
                return OuterInstance.comparator.Compare(a.Current, b.Current) < 0;
            }
        }

        /// <summary>
        /// Read in a single partition of data </summary>
        internal int ReadPartition(ByteSequencesReader reader)
        {
            long start = DateTime.Now.Millisecond;
            BytesRef scratch = new BytesRef();
            while ((scratch.Bytes = reader.Read()) != null)
            {
                scratch.Length = scratch.Bytes.Length;
                Buffer.Append(scratch);
                // Account for the created objects.
                // (buffer slots do not account to buffer size.)
                if (RamBufferSize.Bytes < BufferBytesUsed.Get())
                {
                    break;
                }
            }
            sortInfo.ReadTime += (DateTime.Now.Millisecond - start);
            return Buffer.Size();
        }

        internal class FileAndTop
        {
            internal readonly int Fd;
            internal readonly BytesRef Current;

            internal FileAndTop(int fd, sbyte[] firstLine)
            {
                this.Fd = fd;
                this.Current = new BytesRef(firstLine);
            }
        }

        /// <summary>
        /// Utility class to emit length-prefixed byte[] entries to an output stream for sorting.
        /// Complementary to <seealso cref="ByteSequencesReader"/>.
        /// </summary>
        public class ByteSequencesWriter : IDisposable
        {
            internal readonly DataOutput Os;

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided File </summary>
            public ByteSequencesWriter(FileInfo file)
                : this(new DataOutputStream(new BufferedOutputStream(new FileOutputStream(file))))
            {
            }

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided DataOutput </summary>
            public ByteSequencesWriter(DataOutput os)
            {
                this.Os = os;
            }

            /// <summary>
            /// Writes a BytesRef. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(BytesRef @ref)
            {
                Debug.Assert(@ref != null);
                Write(@ref.Bytes, @ref.Offset, @ref.Length);
            }

            /// <summary>
            /// Writes a byte array. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(sbyte[] bytes)
            {
                Write(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// Writes a byte array.
            /// <p>
            /// The length is written as a <code>short</code>, followed
            /// by the bytes.
            /// </summary>
            public virtual void Write(sbyte[] bytes, int off, int len)
            {
                Debug.Assert(bytes != null);
                Debug.Assert(off >= 0 && off + len <= bytes.Length);
                Debug.Assert(len >= 0);
                Os.writeShort(len);
                Os.write(bytes, off, len);
            }

            /// <summary>
            /// Closes the provided <seealso cref="DataOutput"/> if it is <seealso cref="IDisposable"/>.
            /// </summary>
            public override void Dispose()
            {
                if (Os is IDisposable)
                {
                    ((IDisposable)Os).Dispose();
                }
            }
        }

        /// <summary>
        /// Utility class to read length-prefixed byte[] entries from an input.
        /// Complementary to <seealso cref="ByteSequencesWriter"/>.
        /// </summary>
        public class ByteSequencesReader : IDisposable
        {
            internal readonly DataInput @is;

            /// <summary>
            /// Constructs a ByteSequencesReader from the provided File </summary>
            public ByteSequencesReader(FileInfo file)
                : this(new DataInputStream(new BufferedInputStream(new FileInputStream(file))))
            {
            }

            /// <summary>
            /// Constructs a ByteSequencesReader from the provided DataInput </summary>
            public ByteSequencesReader(DataInput @is)
            {
                this.@is = @is;
            }

            /// <summary>
            /// Reads the next entry into the provided <seealso cref="BytesRef"/>. The internal
            /// storage is resized if needed.
            /// </summary>
            /// <returns> Returns <code>false</code> if EOF occurred when trying to read
            /// the header of the next sequence. Returns <code>true</code> otherwise. </returns>
            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
            public virtual bool Read(BytesRef @ref)
            {
                short length;
                try
                {
                    length = @is.readShort();
                }
                catch (EOFException)
                {
                    return false;
                }

                @ref.Grow(length);
                @ref.Offset = 0;
                @ref.Length = length;
                @is.readFully(@ref.Bytes, 0, length);
                return true;
            }

            /// <summary>
            /// Reads the next entry and returns it if successful.
            /// </summary>
            /// <seealso cref= #read(BytesRef)
            /// </seealso>
            /// <returns> Returns <code>null</code> if EOF occurred before the next entry
            /// could be read. </returns>
            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
            public virtual sbyte[] Read()
            {
                short length;
                try
                {
                    length = @is.readShort();
                }
                catch (EOFException e)
                {
                    return null;
                }

                Debug.Assert(length >= 0, "Sanity: sequence length < 0: " + length);
                sbyte[] result = new sbyte[length];
                @is.readFully(result);
                return result;
            }

            /// <summary>
            /// Closes the provided <seealso cref="DataInput"/> if it is <seealso cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                if (@is is IDisposable)
                {
                    ((IDisposable)@is).Dispose();
                }
            }
        }

        /// <summary>
        /// Returns the comparator in use to sort entries </summary>
        public IComparer<BytesRef> Comparator
        {
            get
            {
                return comparator;
            }
        }
    }*/
}