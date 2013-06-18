/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Lucene.Net.Support;
using Lucene.Net.Support.Compatibility;

namespace Lucene.Net.Store
{

    /// <summary> A memory-resident <see cref="Directory"/> implementation.  Locking
    /// implementation is by default the <see cref="SingleInstanceLockFactory"/>
    /// but can be changed with <see cref="Directory.SetLockFactory"/>.
    /// </summary>
    [Serializable]
    public class RAMDirectory : Directory
    {
        protected IDictionary<string, RAMFile> fileMap = new ConcurrentHashMap<string, RAMFile>();
        internal long sizeInBytes = 0;

        // *****
        // Lock acquisition sequence:  RAMDirectory, then RAMFile
        // *****

        /// <summary>Constructs an empty <see cref="Directory"/>. </summary>
        public RAMDirectory()
        {
            LockFactory = new SingleInstanceLockFactory();
        }

        /// <summary> Creates a new <c>RAMDirectory</c> instance from a different
        /// <c>Directory</c> implementation.  This can be used to load
        /// a disk-based index into memory.
        /// <p/>
        /// This should be used only with indices that can fit into memory.
        /// <p/>
        /// Note that the resulting <c>RAMDirectory</c> instance is fully
        /// independent from the original <c>Directory</c> (it is a
        /// complete copy).  Any subsequent changes to the
        /// original <c>Directory</c> will not be visible in the
        /// <c>RAMDirectory</c> instance.
        /// 
        /// </summary>
        /// <param name="dir">a <c>Directory</c> value
        /// </param>
        /// <exception cref="System.IO.IOException">if an error occurs
        /// </exception>
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
                dir.Dispose();
        }

        //https://issues.apache.org/jira/browse/LUCENENET-174
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (interalLockFactory == null)
            {
                LockFactory = new SingleInstanceLockFactory();
            }
        }

        public override String[] ListAll()
        {
            lock (this)
            {
                EnsureOpen();
                // NOTE: fileMap.keySet().toArray(new String[0]) is broken in non Sun JDKs,
                // and the code below is resilient to map changes during the array population.
                ISet<string> fileNames = SetFactory.CreateHashSet(fileMap.Keys);
                String[] names = new String[fileNames.Count];
                int i = 0;
                foreach (string filename in fileNames)
                {
                    names[i++] = filename;
                }
                return names;
            }
        }

        /// <summary>Returns true iff the named file exists in this directory. </summary>
        public override bool FileExists(String name)
        {
            EnsureOpen();
            return fileMap.ContainsKey(name);
        }

        /// <summary>Returns the length in bytes of a file in the directory.</summary>
        /// <throws>  IOException if the file does not exist </throws>
        public override long FileLength(String name)
        {
            EnsureOpen();
            RAMFile file = fileMap[name];

            if (file == null)
                throw new FileNotFoundException(name);

            return file.Length;
        }

        /// <summary>Return total size in bytes of all files in this
        /// directory.  This is currently quantized to
        /// RAMOutputStream.BUFFER_SIZE. 
        /// </summary>
        public long SizeInBytes
        {
            get
            {
                EnsureOpen();
                return Interlocked.Read(ref sizeInBytes);
            }
        }

        /// <summary>Removes an existing file in the directory.</summary>
        /// <throws>  IOException if the file does not exist </throws>
        public override void DeleteFile(String name)
        {
            EnsureOpen();
            RAMFile file = fileMap[name];
            if (file != null)
            {
                fileMap.Remove(name);
                file.directory = null;
                Interlocked.Add(ref sizeInBytes, -file.sizeInBytes);
            }
            else
                throw new FileNotFoundException(name);

        }

        /// <summary>Creates a new, empty file in the directory with the given name. Returns a stream writing this file. </summary>
        public override IndexOutput CreateOutput(String name, IOContext context)
        {
            EnsureOpen();
            RAMFile file = NewRAMFile();
            RAMFile existing = fileMap[name];
            if (existing != null)
            {
                fileMap.Remove(name);
                Interlocked.Add(ref sizeInBytes, -existing.sizeInBytes);
                existing.directory = null;
            }
            fileMap[name] = file;

            return new RAMOutputStream(file);
        }

        protected virtual RAMFile NewRAMFile()
        {
            return new RAMFile(this);
        }

        public override void Sync(ICollection<string> names)
        {
        }

        /// <summary>Returns a stream reading an existing file. </summary>
        public override IndexInput OpenInput(String name, IOContext context)
        {
            EnsureOpen();
            RAMFile file = fileMap[name];
            
            if (file == null)
                throw new FileNotFoundException(name);

            return new RAMInputStream(name, file);
        }

        /// <summary>Closes the store to future operations, releasing associated memory. </summary>
        protected override void Dispose(bool disposing)
        {
            isOpen = false;
            fileMap.Clear();
            fileMap = null;
        }
    }
}