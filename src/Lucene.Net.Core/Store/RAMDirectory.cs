using Lucene.Net.Support;
using Lucene.Net.Support.Compatibility;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>
    /// A memory-resident <seealso cref="Directory"/> implementation.  Locking
    /// implementation is by default the <seealso cref="SingleInstanceLockFactory"/>
    /// but can be changed with <seealso cref="#setLockFactory"/>.
    ///
    /// <p><b>Warning:</b> this class is not intended to work with huge
    /// indexes. Everything beyond several hundred megabytes will waste
    /// resources (GC cycles), because it uses an internal buffer size
    /// of 1024 bytes, producing millions of {@code byte[1024]} arrays.
    /// this class is optimized for small memory-resident indexes.
    /// It also has bad concurrency on multithreaded environments.
    ///
    /// <p>It is recommended to materialize large indexes on disk and use
    /// <seealso cref="MMapDirectory"/>, which is a high-performance directory
    /// implementation working directly on the file system cache of the
    /// operating system, so copying data to Java heap space is not useful.
    /// </summary>
    public class RAMDirectory : BaseDirectory
    {
        protected internal readonly IDictionary<string, RAMFile> m_fileMap = new ConcurrentDictionary<string, RAMFile>();
        protected internal readonly AtomicLong m_sizeInBytes = new AtomicLong(0);

        // *****
        // Lock acquisition sequence:  RAMDirectory, then RAMFile
        // *****

        /// <summary>
        /// Constructs an empty <seealso cref="Directory"/>. </summary>
        public RAMDirectory()
        {
            try
            {
                SetLockFactory(new SingleInstanceLockFactory());
            }
            catch (System.IO.IOException e)
            {
                // Cannot happen
            }
        }

        /// <summary>
        /// Creates a new <code>RAMDirectory</code> instance from a different
        /// <code>Directory</code> implementation.  this can be used to load
        /// a disk-based index into memory.
        ///
        /// <p><b>Warning:</b> this class is not intended to work with huge
        /// indexes. Everything beyond several hundred megabytes will waste
        /// resources (GC cycles), because it uses an internal buffer size
        /// of 1024 bytes, producing millions of {@code byte[1024]} arrays.
        /// this class is optimized for small memory-resident indexes.
        /// It also has bad concurrency on multithreaded environments.
        ///
        /// <p>For disk-based indexes it is recommended to use
        /// <seealso cref="MMapDirectory"/>, which is a high-performance directory
        /// implementation working directly on the file system cache of the
        /// operating system, so copying data to Java heap space is not useful.
        ///
        /// <p>Note that the resulting <code>RAMDirectory</code> instance is fully
        /// independent from the original <code>Directory</code> (it is a
        /// complete copy).  Any subsequent changes to the
        /// original <code>Directory</code> will not be visible in the
        /// <code>RAMDirectory</code> instance.
        /// </summary>
        /// <param name="dir"> a <code>Directory</code> value </param>
        /// <exception cref="System.IO.IOException"> if an error occurs </exception>
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
            return "lucene-" + GetHashCode().ToString("x");
        }

        public override sealed string[] ListAll()
        {
            EnsureOpen();
            // NOTE: fileMap.keySet().toArray(new String[0]) is broken in non Sun JDKs,
            // and the code below is resilient to map changes during the array population.
            //IDictionary<string, RAMFile>.KeyCollection fileNames = FileMap.Keys;
            ISet<string> fileNames = SetFactory.CreateHashSet(m_fileMap.Keys);//just want a set of strings
            IList<string> names = new List<string>(fileNames.Count);
            foreach (string name in fileNames)
            {
                names.Add(name);
            }
            return names.ToArray();
        }

        /// <summary>
        /// Returns true iff the named file exists in this directory. </summary>
        public override sealed bool FileExists(string name) // LUCENENET TODO: Find out what happens when you inherit a deprecated member in Java
        {
            EnsureOpen();
            return m_fileMap.ContainsKey(name);
        }

        /// <summary>
        /// Returns the length in bytes of a file in the directory. </summary>
        /// <exception cref="System.IO.IOException"> if the file does not exist </exception>
        public override sealed long FileLength(string name)
        {
            EnsureOpen();
            RAMFile file = m_fileMap[name];
            if (file == null)
            {
                throw new FileNotFoundException(name);
            }
            return file.Length;
        }

        /// <summary>
        /// Return total size in bytes of all files in this directory. this is
        /// currently quantized to RAMOutputStream.BUFFER_SIZE.
        /// </summary>
        public long SizeInBytes()
        {
            EnsureOpen();
            return m_sizeInBytes.Get();
        }

        /// <summary>
        /// Removes an existing file in the directory. </summary>
        /// <exception cref="System.IO.IOException"> if the file does not exist </exception>
        public override void DeleteFile(string name)
        {
            EnsureOpen();
            RAMFile file;
            m_fileMap.TryGetValue(name, out file);
            m_fileMap.Remove(name);
            if (file != null)
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
            RAMFile existing;
            m_fileMap.TryGetValue(name, out existing);
            if (existing != null)
            {
                m_sizeInBytes.AddAndGet(-existing.m_sizeInBytes);
                existing.directory = null;
            }
            m_fileMap[name] = file;
            return new RAMOutputStream(file);
        }

        /// <summary>
        /// Returns a new <seealso cref="RAMFile"/> for storing data. this method can be
        /// overridden to return different <seealso cref="RAMFile"/> impls, that e.g. override
        /// <seealso cref="RAMFile#newBuffer(int)"/>.
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
            RAMFile file;
            if (!m_fileMap.TryGetValue(name, out file))
            {
                throw new FileNotFoundException(name);
            }
            return new RAMInputStream(name, file);
        }

        /// <summary>
        /// Closes the store to future operations, releasing associated memory. </summary>
        public override void Dispose()
        {
            IsOpen = false;
            m_fileMap.Clear();
        }
    }
}