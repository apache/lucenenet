using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;
#nullable enable

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
    /// <list type="bullet">
    ///   <item><description>(two bytes) length of the following byte array,</description></item>
    ///   <item><description>exactly the above count of bytes for the sequence to be sorted.</description></item>
    /// </list>
    /// </summary>
    public sealed class OfflineSorter
    {
        /// <summary>
        /// The default encoding (UTF-8 without a byte order mark) used by <see cref="ByteSequencesReader"/> and <see cref="ByteSequencesWriter"/>.
        /// This encoding should always be used when calling the constructor overloads that accept <see cref="BinaryReader"/> or <see cref="BinaryWriter"/>.
        /// </summary>
        public static readonly Encoding DEFAULT_ENCODING = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// The recommended buffer size to use on <see cref="Sort(FileStream, FileStream)"/> or when creating a
        /// <see cref="ByteSequencesReader"/> and <see cref="ByteSequencesWriter"/>.
        /// </summary>
        public const int DEFAULT_FILESTREAM_BUFFER_SIZE = 8192;

        /// <summary>
        /// Convenience constant for megabytes </summary>
        public const long MB = 1024 * 1024;
        /// <summary>
        /// Convenience constant for gigabytes </summary>
        public const long GB = MB * 1024;

        /// <summary>
        /// Minimum recommended buffer size for sorting.
        /// </summary>
        public const long MIN_BUFFER_SIZE_MB = 32;

        /// <summary>
        /// Absolute minimum required buffer size for sorting.
        /// </summary>
        public const long ABSOLUTE_MIN_SORT_BUFFER_SIZE = MB / 2;
        private const string MIN_BUFFER_SIZE_MSG = "At least 0.5MB RAM buffer is needed";

        /// <summary>
        /// Maximum number of temporary files before doing an intermediate merge.
        /// </summary>
        public const int MAX_TEMPFILES = 128;

        /// <summary>
        /// A bit more descriptive unit for constructors.
        /// </summary>
        /// <seealso cref="Automatic()"/>
        /// <seealso cref="Megabytes(long)"/>
        public sealed class BufferSize
        {
            internal readonly int bytes;

            private BufferSize(long bytes)
            {
                if (bytes > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(bytes), "Buffer too large for .NET (" + (int.MaxValue / MB) + "mb max): " + bytes); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }

                if (bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
                {
                    throw new ArgumentOutOfRangeException(nameof(bytes), MIN_BUFFER_SIZE_MSG + ": " + bytes); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }

                this.bytes = (int)bytes;
            }

            /// <summary>
            /// Creates a <see cref="BufferSize"/> in MB. The given
            /// values must be &gt; 0 and &lt; 2048.
            /// </summary>
            public static BufferSize Megabytes(long mb)
            {
                // LUCENENET: Added guard clause
                if (mb < 0 || mb > 2048)
                    throw new ArgumentOutOfRangeException(nameof(mb), "MB must be greater than 0 and less than or equal to 2048.");

                return new BufferSize(mb * MB);
            }

            /// <summary>
            /// Approximately half of the currently available free heap, but no less
            /// than <see cref="ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>. However if current heap allocation
            /// is insufficient or if there is a large portion of unallocated heap-space available
            /// for sorting consult with max allowed heap size.
            /// </summary>
            public static BufferSize Automatic()
            {
                long max, total, free;
                using (var proc = Process.GetCurrentProcess())
                {
                    // take sizes in "conservative" order
                    max = proc.PeakVirtualMemorySize64; // max allocated; java has it as Runtime.maxMemory();
                    total = proc.VirtualMemorySize64; // currently allocated; java has it as Runtime.totalMemory();
                    free = proc.PrivateMemorySize64; // unused portion of currently allocated; java has it as Runtime.freeMemory();
                }
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
            /// <summary>
            /// Number of temporary files created when merging partitions </summary>
            public int TempMergeFiles { get; set; }
            /// <summary>
            /// Number of partition merges </summary>
            public int MergeRounds { get; set; }
            /// <summary>
            /// Number of lines of data read </summary>
            public int Lines { get; set; }
            /// <summary>
            /// Time spent merging sorted partitions (in milliseconds) </summary>
            public long MergeTime { get; set; }
            /// <summary>
            /// Time spent sorting data (in milliseconds) </summary>
            public long SortTime { get; set; }
            /// <summary>
            /// Total time spent (in milliseconds) </summary>
            public long TotalTime { get; set; }
            /// <summary>
            /// Time spent in i/o read (in milliseconds) </summary>
            public long ReadTime { get; set; }
            /// <summary>
            /// Read buffer size (in bytes) </summary>
            public long BufferSize { get; private set; }

            /// <summary>
            /// Create a new <see cref="SortInfo"/> (with empty statistics) for debugging. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="offlineSorter"/> is <c>null</c>.</exception>
            public SortInfo(OfflineSorter offlineSorter)
            {
                if (offlineSorter is null)
                    throw new ArgumentNullException(nameof(offlineSorter)); // LUCENENET: Added guard clause

                BufferSize = offlineSorter.ramBufferSize.bytes;
            }

            /// <summary>
            /// Returns a string representation of this object.
            /// </summary>
            public override string ToString()
            {
                return string.Format(CultureInfo.InvariantCulture, 
                    "time={0:0.00} sec. total ({1:0.00} reading, {2:0.00} sorting, {3:0.00} merging), lines={4}, temp files={5}, merges={6}, soft ram limit={7:0.00} MB", 
                    TotalTime / 1000.0d, ReadTime / 1000.0d, SortTime / 1000.0d, MergeTime / 1000.0d, 
                    Lines, TempMergeFiles, MergeRounds, 
                    (double)BufferSize / MB);
            }
        }

        private readonly BufferSize ramBufferSize;
        private readonly string tempDirectory;

        private readonly Counter bufferBytesUsed = Counter.NewCounter();
        private readonly BytesRefArray buffer;
        private SortInfo? sortInfo; // LUCENENET: Not sure what the line of thought was here - this is declared at the class level, but instantated in the Sort() method and not passed down through methods.
        private readonly int maxTempFiles;
        private readonly IComparer<BytesRef> comparer;

        /// <summary>
        /// Default comparer: sorts in binary (codepoint) order </summary>
        public static readonly IComparer<BytesRef> DEFAULT_COMPARER = Utf8SortedAsUnicodeComparer.Instance;

        /// <summary>
        /// LUCENENET specific - cache the temp directory path so we can return it from a property.
        /// </summary>
        private static readonly string DEFAULT_TEMP_DIR = Path.GetTempPath();

        /// <summary>
        /// Defaults constructor.
        /// </summary>
        /// <seealso cref="DefaultTempDir"/>
        /// <seealso cref="BufferSize.Automatic()"/>
        public OfflineSorter()
            : this(DEFAULT_COMPARER, BufferSize.Automatic(), DefaultTempDir, MAX_TEMPFILES)
        {
        }

        /// <summary>
        /// Defaults constructor with a custom comparer.
        /// </summary>
        /// <seealso cref="DefaultTempDir"/>
        /// <seealso cref="BufferSize.Automatic()"/>
        public OfflineSorter(IComparer<BytesRef> comparer)
            : this(comparer, BufferSize.Automatic(), DefaultTempDir, MAX_TEMPFILES)
        {
        }

        /// <summary>
        /// All-details constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/>, <paramref name="ramBufferSize"/> or <paramref name="tempDirectory"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="ramBufferSize"/> bytes are less than <see cref="ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTempfiles"/> is less than 2.</exception>
        public OfflineSorter(IComparer<BytesRef> comparer, BufferSize ramBufferSize, DirectoryInfo tempDirectory, int maxTempfiles)
            : this(comparer, ramBufferSize, tempDirectory?.FullName ?? throw new ArgumentNullException(nameof(tempDirectory)), maxTempfiles)
        {

        }

        /// <summary>
        /// All-details constructor.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/>, <paramref name="ramBufferSize"/> or <paramref name="tempDirectoryPath"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="ramBufferSize"/> bytes are less than <see cref="ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTempfiles"/> is less than 2.</exception>
        // LUCENENET specific
        public OfflineSorter(IComparer<BytesRef> comparer, BufferSize ramBufferSize, string tempDirectoryPath, int maxTempfiles)
        {
            if (comparer is null)
                throw new ArgumentNullException(nameof(comparer)); // LUCENENET: Added guard clauses
            if (ramBufferSize is null)
                throw new ArgumentNullException(nameof(ramBufferSize));
            if (tempDirectoryPath is null)
                throw new ArgumentNullException(nameof(tempDirectoryPath));

            buffer = new BytesRefArray(bufferBytesUsed);
            if (ramBufferSize.bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
            {
                throw new ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + ramBufferSize.bytes);
            }

            if (maxTempfiles < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTempfiles), "maxTempFiles must be >= 2"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            this.ramBufferSize = ramBufferSize;
            this.tempDirectory = tempDirectoryPath;
            this.maxTempFiles = maxTempfiles;
            this.comparer = comparer;
        }

        /// <summary>
        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
        /// memory may deviate from the hint (may be smaller or larger).
        /// </summary>
        /// <param name="input">The input stream. Must be both seekable and readable.</param>
        /// <param name="output">The output stream. Must be seekable and writable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="input"/> or <paramref name="output"/> is not seekable.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="input"/> is not readable.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="output"/> is not writable.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="input"/> or <paramref name="output"/> is not seekable.</exception>
        public SortInfo Sort(FileStream input, FileStream output)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input)); // LUCENENET: Added guard clauses
            if (output is null)
                throw new ArgumentNullException(nameof(output));
            if (!input.CanSeek)
                throw new ArgumentException($"{nameof(input)} stream must be seekable.");
            if (!output.CanSeek)
                throw new ArgumentException($"{nameof(output)} stream must be seekable.");

            sortInfo = new SortInfo(this) { TotalTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond }; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            // LUCENENET specific - the output is an open stream. We know we don't have to delete it before we start.

            var merges = new JCG.List<FileStream>();
            try
            {
                var inputStream = new ByteSequencesReader(input, leaveOpen: true);
                bool success = false;
                try
                {
                    int lines = 0;
                    while ((lines = ReadPartition(inputStream)) > 0)
                    {
                        merges.Add(SortPartition(/*lines*/)); // LUCENENET specific - removed unused parameter
                        sortInfo.TempMergeFiles++;
                        sortInfo.Lines += lines;

                        // Handle intermediate merges.
                        if (merges.Count == maxTempFiles)
                        {
                            var intermediate = FileSupport.CreateTempFileAsStream("sort", "intermediate", tempDirectory);
                            try
                            {
                                MergePartitions(merges, intermediate);
                            }
                            finally
                            {
                                // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
                                IOUtils.Dispose(merges);
                                merges.Clear();
                                intermediate.Position = 0;
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
                        IOUtils.Dispose(inputStream);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(inputStream);
                    }
                }

                // One partition, try to rename or copy if unsuccessful.
                if (merges.Count == 1)
                {
                    // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
                    using FileStream single = merges[0];
                    Copy(single, output);
                }
                else
                {
                    // otherwise merge the partitions with a priority queue.
                    MergePartitions(merges, output);
                }
            }
            finally
            {
                // LUCENENET: Reset the position to the beginning of the streams so we don't have to reopen the files
                input.Position = 0;
                output.Position = 0;
                IOUtils.Dispose(merges);
                // LUCENENET specific - we are using the FileOptions.DeleteOnClose FileStream option to delete the file when it is disposed.
            }

            sortInfo.TotalTime = ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - sortInfo.TotalTime); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            return sortInfo;
        }

        /// <summary>
        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
        /// memory may deviate from the hint (may be smaller or larger).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is <c>null</c>.</exception>
        public SortInfo Sort(FileInfo input, FileInfo output)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input)); // LUCENENET: Added guard clauses
            if (output is null)
                throw new ArgumentNullException(nameof(output));

            output.Delete();

            using FileStream inputStream = new FileStream(input.FullName, FileMode.Open, FileAccess.ReadWrite,
                FileShare.Read, bufferSize: DEFAULT_FILESTREAM_BUFFER_SIZE, FileOptions.DeleteOnClose | FileOptions.RandomAccess);
            using FileStream outputStream = new FileStream(output.FullName, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.Read, bufferSize: DEFAULT_FILESTREAM_BUFFER_SIZE, FileOptions.RandomAccess);
            bool success = false;
            try
            {
                var sort = Sort(inputStream, outputStream);
                success = true;
                return sort;
            }
            finally
            {
                if (!success)
                {
                    try
                    {
                        outputStream.Dispose();
                    }
                    finally
                    {
                        output.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the default temporary directory. By default, the System's temp folder.
        /// </summary>
        public static string DefaultTempDir => DEFAULT_TEMP_DIR;

        /// <summary>
        /// Copies one file to another.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Copy(FileStream file, FileStream output)
        {
            file.CopyTo(output);
        }

        /// <summary>
        /// Sort a single partition in-memory. </summary>
        private FileStream SortPartition(/*int len*/) // LUCENENET NOTE: made private, since protected is not valid in a sealed class. Also eliminated unused parameter.
        {
            var data = this.buffer;
            FileStream tempFile = FileSupport.CreateTempFileAsStream("sort", "partition", tempDirectory);

            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            sortInfo!.SortTime += ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            using (var @out = new ByteSequencesWriter(tempFile, leaveOpen: true))
            {
                IBytesRefEnumerator iter = buffer.GetEnumerator(comparer);
                while (iter.MoveNext())
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(iter.Current.Length <= ushort.MaxValue);
                    @out.Write(iter.Current);
                }
            }

            // Clean up the buffer for the next partition.
            data.Clear();
            tempFile.Position = 0;
            return tempFile;
        }

        /// <summary>
        /// Merge a list of sorted temporary files (partitions) into an output file. </summary>
        internal void MergePartitions(IList<FileStream> merges, FileStream outputFile)
        {
            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            var @out = new ByteSequencesWriter(outputFile, leaveOpen: true);

            PriorityQueue<FileAndTop> queue = new PriorityQueueAnonymousClass(this, merges.Count);

            var streams = new ByteSequencesReader[merges.Count];
            try
            {
                // Open streams and read the top for each file
                for (int i = 0; i < merges.Count; i++)
                {
                    streams[i] = new ByteSequencesReader(merges[i], leaveOpen: true);
                    byte[]? line = streams[i].Read();
                    if (line is not null)
                    {
                        queue.InsertWithOverflow(new FileAndTop(i, line));
                    }
                }

                // Unix utility sort() uses ordered array of files to pick the next line from, updating
                // it as it reads new lines. The PQ used here is a more elegant solution and has
                // a nicer theoretical complexity bound :) The entire sorting process is I/O bound anyway
                // so it shouldn't make much of a difference (didn't check).
                FileAndTop top;
                while ((top = queue.Top) is not null)
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

                sortInfo!.MergeTime += (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                sortInfo.MergeRounds++;
            }
            finally
            {
                // The logic below is: if an exception occurs in closing out, it has a priority over exceptions
                // happening in closing streams.
                try
                {
                    IOUtils.Dispose(streams);
                }
                finally
                {
                    IOUtils.Dispose(@out);
                }
            }
        }

        private sealed class PriorityQueueAnonymousClass : PriorityQueue<FileAndTop>
        {
            private readonly OfflineSorter outerInstance;

            public PriorityQueueAnonymousClass(OfflineSorter outerInstance, int size)
                : base(size)
            {
                this.outerInstance = outerInstance;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected internal override bool LessThan(FileAndTop a, FileAndTop b)
            {
                // LUCENENET: Added guard clauses
                if (a is null)
                    throw new ArgumentNullException(nameof(a));
                if (b is null)
                    throw new ArgumentNullException(nameof(b));

                return outerInstance.comparer.Compare(a.Current, b.Current) < 0;
            }
        }

        /// <summary>
        /// Read in a single partition of data. </summary>
        internal int ReadPartition(ByteSequencesReader reader)
        {
            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            var scratch = new BytesRef();
            while ((scratch.Bytes = reader.Read()) is not null)
            {
                scratch.Length = scratch.Bytes.Length;
                buffer.Append(scratch);
                // Account for the created objects.
                // (buffer slots do not account to buffer size.)
                if (ramBufferSize.bytes < bufferBytesUsed)
                {
                    break;
                }
            }
            sortInfo!.ReadTime += ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - start); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            return buffer.Length;
        }

        internal class FileAndTop
        {
            internal int Fd { get; private set; }
            internal BytesRef Current { get; private set; }

            internal FileAndTop(int fd, byte[] firstLine)
            {
                this.Fd = fd;
                this.Current = new BytesRef(firstLine);
            }
        }

        /// <summary>
        /// Utility class to emit length-prefixed <see cref="T:byte[]"/> entries to an output stream for sorting.
        /// Complementary to <see cref="ByteSequencesReader"/>.
        /// </summary>
        public class ByteSequencesWriter : IDisposable
        {
            private readonly BinaryWriter os;
            private bool disposed; // LUCENENET specific

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="FileStream"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
            public ByteSequencesWriter(FileStream stream)
                : this(new BinaryWriter(stream, DEFAULT_ENCODING, leaveOpen: false))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="FileStream"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
            public ByteSequencesWriter(FileStream stream, bool leaveOpen)
                : this(new BinaryWriter(stream, DEFAULT_ENCODING, leaveOpen))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided file path. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
            public ByteSequencesWriter(string path)
                : this(new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read, bufferSize: DEFAULT_FILESTREAM_BUFFER_SIZE))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="FileInfo"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="file"/> is <c>null</c>.</exception>
            // LUCENENET specific - This is for bw compatibility with an earlier approach using FileInfo (similar to how it worked in Java)
            public ByteSequencesWriter(FileInfo file)
                : this(file?.FullName ?? throw new ArgumentNullException(nameof(file)))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesWriter"/> to the provided <see cref="BinaryWriter"/>.
            /// <b>NOTE:</b> To match Lucene, pass the <paramref name="writer"/>'s constructor the
            /// <see cref="DEFAULT_ENCODING"/>, which is UTF-8 without a byte order mark.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
            public ByteSequencesWriter(BinaryWriter writer)
            {
                this.os = writer ?? throw new ArgumentNullException(nameof(writer)); // LUCENENET: Added guard clause
            }

            /// <summary>
            /// Writes a <see cref="BytesRef"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="ref"/> is <c>null</c>.</exception>
            /// <seealso cref="Write(byte[], int, int)"/>
            public virtual void Write(BytesRef @ref)
            {
                if (@ref is null)
                    throw new ArgumentNullException(nameof(@ref)); // LUCENENET: Changed assert to guard clause
                Write(@ref.Bytes, @ref.Offset, @ref.Length);
            }

            /// <summary>
            /// Writes a byte array. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
            /// <seealso cref="Write(byte[], int, int)"/>
            public virtual void Write(byte[] bytes)
            {
                if (bytes is null)
                    throw new ArgumentNullException(nameof(bytes)); // LUCENENET: Added guard clause

                Write(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// Writes a byte array.
            /// <para/>
            /// The length is written as a <see cref="short"/>, followed
            /// by the bytes.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="off"/> or <paramref name="len"/> is less than 0.</exception>
            /// <exception cref="ArgumentException"><paramref name="off"/> and <paramref name="len"/> refer to a position outside of the array.</exception>
            public virtual void Write(byte[] bytes, int off, int len)
            {
                if (bytes is null) // LUCENENET specific - Changed from asserts to guard clauses
                    throw new ArgumentNullException(nameof(bytes));
                if (off < 0)
                    throw new ArgumentOutOfRangeException(nameof(off), "Non-negative number required.");
                if (len < 0)
                    throw new ArgumentOutOfRangeException(nameof(len), "Non-negative number required.");
                if (off > bytes.Length - len) // Checks for int overflow
                    throw new ArgumentException("Index and length must refer to a location within the array.");

                os.Write((short)len);
                os.Write(bytes, off, len);
            }

            /// <summary>
            /// Disposes the provided <see cref="DataOutput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Disposes the provided <see cref="DataOutput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            protected virtual void Dispose(bool disposing) // LUCENENET specific - implemented proper dispose pattern
            {
                if (!disposed && disposing)
                {
                    os.Dispose();
                    disposed = true;
                }
            }
        }

        /// <summary>
        /// Utility class to read length-prefixed <see cref="T:byte[]"/> entries from an input.
        /// Complementary to <see cref="ByteSequencesWriter"/>.
        /// </summary>
        public class ByteSequencesReader : IDisposable
        {
            private readonly BinaryReader @is;
            private bool disposed; // LUCENENET specific

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <see cref="FileStream"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
            public ByteSequencesReader(FileStream stream)
                : this(new BinaryReader(stream, DEFAULT_ENCODING, leaveOpen: false))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <see cref="FileStream"/>. </summary>
            /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
            public ByteSequencesReader(FileStream stream, bool leaveOpen)
                : this(new BinaryReader(stream, DEFAULT_ENCODING, leaveOpen))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <paramref name="path"/>. </summary>
            /// <exception cref="ArgumentException"><paramref name="path"/> is <c>null</c> or whitespace.</exception>
            // LUCENENET specific
            public ByteSequencesReader(string path)
                : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: DEFAULT_FILESTREAM_BUFFER_SIZE))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <paramref name="file"/>. </summary>
            /// <exception cref="ArgumentException"><paramref name="file"/> is <c>null</c> or whitespace.</exception>
            // LUCENENET specific - This is for bw compatibility with an earlier approach using FileInfo (similar to how it worked in Java)
            public ByteSequencesReader(FileInfo file)
                : this(file?.FullName ?? throw new ArgumentNullException(nameof(file)))
            {
            }

            /// <summary>
            /// Constructs a <see cref="ByteSequencesReader"/> from the provided <see cref="BinaryReader"/>.
            /// <para/>
            /// <b>NOTE:</b> To match Lucene, pass the <paramref name="reader"/>'s constructor the
            /// <see cref="DEFAULT_ENCODING"/>, which is UTF-8 without a byte order mark.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
            public ByteSequencesReader(BinaryReader reader)
            {
                this.@is = reader ?? throw new ArgumentNullException(nameof(reader)); // LUCENENET: Added guard clause
            }

            /// <summary>
            /// Reads the next entry into the provided <see cref="BytesRef"/>. The internal
            /// storage is resized if needed.
            /// </summary>
            /// <returns> Returns <c>false</c> if EOF occurred when trying to read
            /// the header of the next sequence. Returns <c>true</c> otherwise. </returns>
            /// <exception cref="EndOfStreamException"> If the file ends before the full sequence is read. </exception>
            /// <exception cref="ArgumentNullException"><paramref name="ref"/> is <c>null</c>.</exception>
            public virtual bool Read(BytesRef @ref)
            {
                if (@ref is null)
                    throw new ArgumentNullException(nameof(@ref)); // LUCENENET: Added guard clause

                ushort length;
                try
                {
                    length = (ushort)@is.ReadInt16();
                }
                catch (Exception e) when (e.IsEOFException())
                {
                    return false;
                }

                @ref.Grow(length);
                @ref.Offset = 0;
                @ref.Length = length;
                @is.Read(@ref.Bytes, 0, length);
                return true;
            }

            /// <summary>
            /// Reads the next entry and returns it if successful.
            /// </summary>
            /// <seealso cref="Read(BytesRef)"/>
            /// <returns> Returns <c>null</c> if EOF occurred before the next entry
            /// could be read. </returns>
            /// <exception cref="EndOfStreamException"> If the file ends before the full sequence is read. </exception>
            public virtual byte[]? Read()
            {
                ushort length;
                try
                {
                    length = (ushort)@is.ReadInt16();
                }
                catch (Exception e) when (e.IsEOFException())
                {
                    return null;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(length >= 0, "Sanity: sequence length < 0: {0}", length);
                byte[] result = new byte[length];
                @is.Read(result, 0, length);
                return result;
            }

            /// <summary>
            /// Disposes the provided <see cref="DataInput"/> if it is <see cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing) // LUCENENET specific - implemented proper dispose pattern
            {
                if (!disposed && disposing)
                {
                    @is.Dispose();
                    disposed = true;
                }
            }
        }

        /// <summary>
        /// Returns the comparer in use to sort entries </summary>
        public IComparer<BytesRef> Comparer => comparer;
    }
}