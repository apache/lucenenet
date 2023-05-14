using J2N.Collections.Generic.Extensions;
using J2N.Threading.Atomic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>
    /// A memory-resident <see cref="Directory"/> implementation.  Locking
    /// implementation is by default the <see cref="SingleInstanceLockFactory"/>
    /// but can be changed with <see cref="Directory.SetLockFactory(LockFactory)"/>.
    ///
    /// <para/><b>Warning:</b> This class is not intended to work with huge
    /// indexes. Everything beyond several hundred megabytes will waste
    /// resources (GC cycles), because it uses an internal buffer size
    /// of 1024 bytes, producing millions of <see cref="T:byte[1024]"/> arrays.
    /// This class is optimized for small memory-resident indexes.
    /// It also has bad concurrency on multithreaded environments.
    ///
    /// <para/>It is recommended to materialize large indexes on disk and use
    /// <see cref="MMapDirectory"/>, which is a high-performance directory
    /// implementation working directly on the file system cache of the
    /// operating system, so copying data to heap space is not useful.
    /// </summary>
    public class RAMDirectory : BaseDirectory
    {
        protected internal readonly ConcurrentDictionary<string, RAMFile> m_fileMap = new ConcurrentDictionary<string, RAMFile>();
        protected internal readonly AtomicInt64 m_sizeInBytes = new AtomicInt64(0);

        // *****
        // Lock acquisition sequence:  RAMDirectory, then RAMFile
        // *****

        /// <summary>
        /// Constructs an empty <see cref="Directory"/>. </summary>
        public RAMDirectory()
        {
            try
            {
                SetLockFactory(new SingleInstanceLockFactory());
            }
            catch (Exception e) when (e.IsIOException())
            {
                // Cannot happen
            }
        }

        /// <summary>
        /// Creates a new <see cref="RAMDirectory"/> instance from a different
        /// <see cref="Directory"/> implementation.  This can be used to load
        /// a disk-based index into memory.
        ///
        /// <para/><b>Warning:</b> this class is not intended to work with huge
        /// indexes. Everything beyond several hundred megabytes will waste
        /// resources (GC cycles), because it uses an internal buffer size
        /// of 1024 bytes, producing millions of <see cref="T:byte[1024]"/> arrays.
        /// this class is optimized for small memory-resident indexes.
        /// It also has bad concurrency on multithreaded environments.
        ///
        /// <para/>For disk-based indexes it is recommended to use
        /// <see cref="MMapDirectory"/>, which is a high-performance directory
        /// implementation working directly on the file system cache of the
        /// operating system, so copying data to heap space is not useful.
        ///
        /// <para/>Note that the resulting <see cref="RAMDirectory"/> instance is fully
        /// independent from the original <see cref="Directory"/> (it is a
        /// complete copy).  Any subsequent changes to the
        /// original <see cref="Directory"/> will not be visible in the
        /// <see cref="RAMDirectory"/> instance.
        /// </summary>
        /// <param name="dir"> a <see cref="Directory"/> value </param>
        /// <param name="context">io context</param>
        /// <exception cref="IOException"> if an error occurs </exception>
        public RAMDirectory(Directory dir, IOContext context)
            : this(dir, false, context)
        {
        }

        private RAMDirectory(Directory dir, bool closeDir, IOContext context)
            : this()
        {
            foreach (string file in dir.ListAll())
            {
                dir.Copy(this, file, file, context);
            }
            if (closeDir)
            {
                dir.Dispose();
            }
        }

        public override string GetLockID()
        {
            return "lucene-" + GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        }

        public override sealed string[] ListAll()
        {
            EnsureOpen();
            // NOTE: fileMap.keySet().toArray(new String[0]) is broken in non Sun JDKs,
            // and the code below is resilient to map changes during the array population.
            // LUCENENET NOTE: Just because it is broken in Java, doesn't mean we can't use it in .NET.
            return m_fileMap.Keys.ToArray();
        }

        /// <summary>
        /// Returns true iff the named file exists in this directory. </summary>
        [Obsolete("this method will be removed in 5.0")]
        public override sealed bool FileExists(string name)
        {
            EnsureOpen();
            return m_fileMap.ContainsKey(name);
        }

        /// <summary>
        /// Returns the length in bytes of a file in the directory. </summary>
        /// <exception cref="IOException"> if the file does not exist </exception>
        public override sealed long FileLength(string name)
        {
            EnsureOpen();
            if (!m_fileMap.TryGetValue(name, out RAMFile file) || file is null)
            {
                throw new FileNotFoundException(name);
            }
            return file.Length;
        }

        /// <summary>
        /// Return total size in bytes of all files in this directory. This is
        /// currently quantized to <see cref="RAMOutputStream.BUFFER_SIZE"/>.
        /// </summary>
        public long GetSizeInBytes()
        {
            EnsureOpen();
            return m_sizeInBytes;
        }

        /// <summary>
        /// Removes an existing file in the directory. </summary>
        /// <exception cref="IOException"> if the file does not exist </exception>
        public override void DeleteFile(string name)
        {
            EnsureOpen();
            if (m_fileMap.TryRemove(name, out RAMFile file) && file != null)
            {
                file.directory = null;
                m_sizeInBytes.AddAndGet(-file.m_sizeInBytes);
            }
            else
            {
                throw new FileNotFoundException(name);
            }
        }

        /// <summary>
        /// Creates a new, empty file in the directory with the given name. Returns a stream writing this file. </summary>
        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
            RAMFile file = NewRAMFile();
            if (m_fileMap.TryRemove(name, out RAMFile existing) && existing != null)
            {
                m_sizeInBytes.AddAndGet(-existing.m_sizeInBytes);
                existing.directory = null;
            }
            m_fileMap[name] = file;
            return new RAMOutputStream(file);
        }

        /// <summary>
        /// Returns a new <see cref="RAMFile"/> for storing data. this method can be
        /// overridden to return different <see cref="RAMFile"/> impls, that e.g. override
        /// <see cref="RAMFile.NewBuffer(int)"/>.
        /// </summary>
        protected virtual RAMFile NewRAMFile()
        {
            return new RAMFile(this);
        }

        public override void Sync(ICollection<string> names)
        {
        }

        /// <summary>
        /// Returns a stream reading an existing file. </summary>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            EnsureOpen();
            if (!m_fileMap.TryGetValue(name, out RAMFile file) || file is null)
            {
                throw new FileNotFoundException(name);
            }
            return new RAMInputStream(name, file);
        }

        /// <summary>
        /// Closes the store to future operations, releasing associated memory. </summary>
        protected override void Dispose(bool disposing)
        {
            if (!CompareAndSetIsOpen(expect: true, update: false)) return; // LUCENENET: allow dispose more than once as per https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern

            if (disposing)
            {
                // LUCENENET: Removed setter for isOpen and put it above in the if check so it is atomic
                m_fileMap.Clear();
            }
        }
    }
}