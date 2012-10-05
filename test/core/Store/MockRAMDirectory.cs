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
using NUnit.Framework;

namespace Lucene.Net.Store
{

    /// <summary> This is a subclass of RAMDirectory that adds methods
    /// intended to be used only by unit tests.
    /// </summary>
    [Serializable]
    public class MockRAMDirectory : RAMDirectory
    {
        internal long maxSize;

        // Max actual bytes used. This is set by MockRAMOutputStream:
        internal long maxUsedSize;
        internal double randomIOExceptionRate;
        Random randomState;
        internal bool noDeleteOpenFile = true;
        internal bool preventDoubleWrite = true;
        private ISet<string> unSyncedFiles;
        private ISet<string> createdFiles;
        internal volatile bool crashed;

        // NOTE: we cannot initialize the Map here due to the
        // order in which our constructor actually does this
        // member initialization vs when it calls super.  It seems
        // like super is called, then our members are initialized:
        internal IDictionary<string, int> openFiles;

        // Only tracked if noDeleteOpenFile is true: if an attempt
        // is made to delete an open file, we enroll it here.
        internal ISet<string> openFilesDeleted;

        private void Init()
        {
            lock (this)
            {
                if (openFiles == null)
                {
                    openFiles = new Dictionary<string, int>();
                    openFilesDeleted = Support.Compatibility.SetFactory.CreateHashSet<string>();
                }

                if (createdFiles == null)
                    createdFiles = Support.Compatibility.SetFactory.CreateHashSet<string>();
                if (unSyncedFiles == null)
                    unSyncedFiles = Support.Compatibility.SetFactory.CreateHashSet<string>();
            }
        }

        public MockRAMDirectory()
            : base()
        {
            Init();
        }
        public MockRAMDirectory(Directory dir)
            : base(dir)
        {
            Init();
        }

        /* If set to true, we throw an IOException if the same
         *  file is opened by createOutput, ever. */
        public virtual void SetPreventDoubleWrite(bool value)
        {
            preventDoubleWrite = value;
        }

        public override void Sync(String name)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                if (crashed)
                    throw new System.IO.IOException("cannot sync after crash");
                if (unSyncedFiles.Contains(name))
                    unSyncedFiles.Remove(name);
            }
        }

        /* Simulates a crash of OS or machine by overwriting
         *  unsynced files. */
        public virtual void Crash()
        {
            lock (this)
            {
                crashed = true;
                openFiles = new Dictionary<string, int>();
                openFilesDeleted = Support.Compatibility.SetFactory.CreateHashSet<string>();
                var it = unSyncedFiles.GetEnumerator();
                unSyncedFiles = Support.Compatibility.SetFactory.CreateHashSet<string>();
                int count = 0;
                while (it.MoveNext())
                {
                    string name = it.Current;
                    RAMFile file = fileMap[name];
                    if (count % 3 == 0)
                    {
                        DeleteFile(name, true);
                    }
                    else if (count % 3 == 1)
                    {
                        // Zero out file entirely
                        int numBuffers = file.NumBuffers();
                        for (int i = 0; i < numBuffers; i++)
                        {
                            byte[] buffer = file.GetBuffer(i);
                            Array.Clear(buffer,0,buffer.Length);
                        }
                    }
                    else if (count % 3 == 2)
                    {
                        // Truncate the file:
                        file.Length = file.Length / 2;
                    }
                    count++;
                }
            }
        }

        public virtual void ClearCrash()
        {
            lock (this)
            {
                crashed = false;
            }
        }

        public virtual void SetMaxSizeInBytes(long maxSize)
        {
            this.maxSize = maxSize;
        }
        public virtual long GetMaxSizeInBytes()
        {
            return this.maxSize;
        }

        /*
         * Returns the peek actual storage used (bytes) in this
         * directory.
         */
        public virtual long GetMaxUsedSizeInBytes()
        {
            return this.maxUsedSize;
        }
        public virtual void ResetMaxUsedSizeInBytes()
        {
            this.maxUsedSize = GetRecomputedActualSizeInBytes();
        }

        /*
         * Emulate windows whereby deleting an open file is not
         * allowed (raise IOException).
        */
        public virtual void SetNoDeleteOpenFile(bool value)
        {
            this.noDeleteOpenFile = value;
        }
        public bool GetNoDeleteOpenFile()
        {
            return noDeleteOpenFile;
        }

        /*
         * If 0.0, no exceptions will be thrown.  Else this should
         * be a double 0.0 - 1.0.  We will randomly throw an
         * IOException on the first write to an OutputStream based
         * on this probability.
         */
        public virtual void SetRandomIOExceptionRate(double rate, long seed)
        {
            randomIOExceptionRate = rate;
            // seed so we have deterministic behaviour:
            randomState = new Random((int)(seed&0x7fffffff));
        }
        public virtual double GetRandomIOExceptionRate()
        {
            return randomIOExceptionRate;
        }

        internal virtual void MaybeThrowIOException()
        {
            if (randomIOExceptionRate > 0.0)
            {
                int number = Math.Abs(randomState.Next() % 1000);
                if (number < randomIOExceptionRate * 1000)
                {
                    throw new System.IO.IOException("a random IOException");
                }
            }
        }

        public override void DeleteFile(String name)
        {
            lock (this)
            {
                DeleteFile(name, false);
            }
        }

        private void DeleteFile(String name, bool forced)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();

                if (crashed && !forced)
                    throw new System.IO.IOException("cannot delete after crash");

                if (unSyncedFiles.Contains(name))
                    unSyncedFiles.Remove(name);
                if (!forced && noDeleteOpenFile)
                {
                    if (openFiles.ContainsKey(name))
                    {
                        openFilesDeleted.Add(name);
                        throw new System.IO.IOException("MockRAMDirectory: file \"" + name + "\" is still open: cannot delete");
                    }
                    else
                    {
                        openFilesDeleted.Remove(name);
                    }
                }
                base.DeleteFile(name);
            }
        }

        public ISet<string> GetOpenDeletedFiles()
        {
            lock (this)
            {
                return Support.Compatibility.SetFactory.CreateHashSet(openFilesDeleted);
            }
        }

        public override IndexOutput CreateOutput(String name)
        {
            lock (this)
            {
                if (crashed)
                    throw new System.IO.IOException("cannot createOutput after crash");
                Init();
                if (preventDoubleWrite && createdFiles.Contains(name) && !name.Equals("segments.gen"))
                    throw new System.IO.IOException("file \"" + name + "\" was already written to");
                if (noDeleteOpenFile && openFiles.ContainsKey(name))
                    throw new System.IO.IOException("MockRAMDirectory: file \"" + name + "\" is still open: cannot overwrite");
                RAMFile file = new RAMFile(this);
                if (crashed)
                    throw new System.IO.IOException("cannot createOutput after crash");
                unSyncedFiles.Add(name);
                createdFiles.Add(name);
                RAMFile existing = fileMap[name];
                // Enforce write once:
                if (existing != null && !name.Equals("segments.gen") && preventDoubleWrite)
                    throw new System.IO.IOException("file " + name + " already exists");
                else
                {
                    if (existing != null)
                    {
                        internalSizeInBytes -= existing.sizeInBytes;
                        existing.directory = null;
                    }

                    fileMap[name]=file;
                }

                return new MockRAMOutputStream(this, file, name);
            }
        }

        public override IndexInput OpenInput(String name)
        {
            lock (this)
            {
                RAMFile file = fileMap[name];
                if (file == null)
                    throw new System.IO.FileNotFoundException(name);
                else
                {
                    if (openFiles.ContainsKey(name))
                    {
                        int v = openFiles[name]; 
                        v = v + 1;
                        openFiles[name] = v;
                    }
                    else
                    {
                        openFiles[name] = 1;
                    }
                }
                return new MockRAMInputStream(this, name, file);
            }
        }

        /* Provided for testing purposes.  Use sizeInBytes() instead. */
        public long GetRecomputedSizeInBytes()
        {
            lock (this)
            {
                long size = 0;
                foreach(RAMFile file in fileMap.Values)
                    size += file.SizeInBytes;
                return size;
            }
        }

        /* Like getRecomputedSizeInBytes(), but, uses actual file
         * lengths rather than buffer allocations (which are
         * quantized up to nearest
         * RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
         */

        public long GetRecomputedActualSizeInBytes()
        {
            lock (this)
            {
                long size = 0;
                foreach(RAMFile file in fileMap.Values)
                    size += file.length;
                return size;
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                if (openFiles == null)
                {
                    openFiles = new Dictionary<string, int>();
                    openFilesDeleted = Support.Compatibility.SetFactory.CreateHashSet<string>();
                }
                if (noDeleteOpenFile && openFiles.Count > 0)
                {
                    // RuntimeException instead of IOException because
                    // super() does not throw IOException currently:
                    throw new System.SystemException("MockRAMDirectory: cannot close: there are still open files: " + openFiles);
                }
            }

            base.Dispose(disposing);
        }

        /*
         * Objects that represent fail-able conditions. Objects of a derived
         * class are created and registered with the mock directory. After
         * register, each object will be invoked once for each first write
         * of a file, giving the object a chance to throw an IOException.
         */
        public class Failure
        {
            /*
             * eval is called on the first write of every new file.
             */
            public virtual void Eval(MockRAMDirectory dir) { }

            /*
             * reset should set the state of the failure to its default
             * (freshly constructed) state. Reset is convenient for tests
             * that want to create one failure object and then reuse it in
             * multiple cases. This, combined with the fact that Failure
             * subclasses are often anonymous classes makes reset difficult to
             * do otherwise.
             *
             * A typical example of use is
             * Failure failure = new Failure() { ... };
             * ...
             * mock.failOn(failure.reset())
             */
            public virtual Failure Reset() { return this; }

            protected internal bool doFail;

            public virtual void SetDoFail()
            {
                doFail = true;
            }

            public virtual void ClearDoFail()
            {
                doFail = false;
            }
        }

        System.Collections.ArrayList failures;

        /*
         * add a Failure object to the list of objects to be evaluated
         * at every potential failure point
         */
        public virtual void FailOn(Failure fail)
        {
            lock (this)
            {
                if (failures == null)
                {
                    failures = new System.Collections.ArrayList();
                }
                failures.Add(fail);
            }
        }

        /*
         * Iterate through the failures list, giving each object a
         * chance to throw an IOE
         */
        internal virtual void MaybeThrowDeterministicException()
        {
            lock (this)
            {
                if (failures != null)
                {
                    for (int i = 0; i < failures.Count; i++)
                    {
                        ((Failure)failures[i]).Eval(this);
                    }
                }
            }
        }
    }
}