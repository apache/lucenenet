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

using NUnit.Framework;

namespace Lucene.Net.Store
{

    /// <summary> This is a subclass of RAMDirectory that adds methods
    /// intended to be used only by unit tests.
    /// </summary>
    /// <version>  $Id: RAMDirectory.java 437897 2006-08-29 01:13:10Z yonik $
    /// </version>

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
        private System.Collections.Hashtable unSyncedFiles;
        private System.Collections.Hashtable createdFiles;
        internal volatile bool crashed;

        // NOTE: we cannot initialize the Map here due to the
        // order in which our constructor actually does this
        // member initialization vs when it calls super.  It seems
        // like super is called, then our members are initialized:
        internal System.Collections.IDictionary openFiles;

        // Only tracked if noDeleteOpenFile is true: if an attempt
        // is made to delete an open file, we enroll it here.
        internal System.Collections.Hashtable openFilesDeleted;

        private void Init()
        {
            lock (this)
            {
                if (openFiles == null)
                {
                    openFiles = new System.Collections.Hashtable();
                    openFilesDeleted = new System.Collections.Hashtable();
                }

                if (createdFiles == null)
                    createdFiles = new System.Collections.Hashtable();
                if (unSyncedFiles == null)
                    unSyncedFiles = new System.Collections.Hashtable();
            }
        }

        public MockRAMDirectory()
            : base()
        {
            Init();
        }
        public MockRAMDirectory(String dir)
            : base(dir)
        {
            Init();
        }
        public MockRAMDirectory(Directory dir)
            : base(dir)
        {
            Init();
        }
        public MockRAMDirectory(System.IO.FileInfo dir)
            : base(dir)
        {
            Init();
        }

        /** If set to true, we throw an IOException if the same
         *  file is opened by createOutput, ever. */
        public virtual void SetPreventDoubleWrite(bool value)
        {
            preventDoubleWrite = value;
        }

        public void Sync(String name)
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

        /** Simulates a crash of OS or machine by overwriting
         *  unsynced files. */
        public virtual void Crash()
        {
            lock (this)
            {
                crashed = true;
                openFiles = new System.Collections.Hashtable();
                openFilesDeleted = new System.Collections.Hashtable();
                System.Collections.IEnumerator it = unSyncedFiles.GetEnumerator();
                unSyncedFiles = new System.Collections.Hashtable();
                int count = 0;
                while (it.MoveNext())
                {
                    String name = (String)it.Current;
                    RAMFile file = (RAMFile)fileMap[name];
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
                        file.SetLength(file.GetLength() / 2);
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

        /**
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

        /**
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

        /**
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
                    if (openFiles.Contains(name))
                    {
                        openFilesDeleted[name]=name;
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

        public System.Collections.IDictionary GetOpenDeletedFiles()
        {
            lock (this)
            {
                return new System.Collections.Hashtable(openFilesDeleted);
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
                if (noDeleteOpenFile && openFiles.Contains(name))
                    throw new System.IO.IOException("MockRAMDirectory: file \"" + name + "\" is still open: cannot overwrite");
                RAMFile file = new RAMFile(this);
                if (crashed)
                    throw new System.IO.IOException("cannot createOutput after crash");
                unSyncedFiles[name]=name;
                createdFiles[name]=name;
                RAMFile existing = (RAMFile)fileMap[name];
                // Enforce write once:
                if (existing != null && !name.Equals("segments.gen") && preventDoubleWrite)
                    throw new System.IO.IOException("file " + name + " already exists");
                else
                {
                    if (existing != null)
                    {
                        sizeInBytes -= existing.sizeInBytes_ForNUnit;
                        existing.directory_ForNUnit = null;
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
                RAMFile file = (RAMFile)fileMap[name];
                if (file == null)
                    throw new System.IO.FileNotFoundException(name);
                else
                {
                    if (openFiles.Contains(name))
                    {
                        int v = (int)openFiles[name]; 
                        v = (System.Int32)(v + 1);
                        openFiles[name]= v;
                    }
                    else
                    {
                        openFiles[name]=1;
                    }
                }
                return new MockRAMInputStream(this, name, file);
            }
        }

        /** Provided for testing purposes.  Use sizeInBytes() instead. */
        public long GetRecomputedSizeInBytes()
        {
            lock (this)
            {
                long size = 0;
                System.Collections.IEnumerator it = fileMap.Values.GetEnumerator();
                while (it.MoveNext())
                    size += ((RAMFile)it.Current).GetSizeInBytes();
                return size;
            }
        }

        /** Like getRecomputedSizeInBytes(), but, uses actual file
         * lengths rather than buffer allocations (which are
         * quantized up to nearest
         * RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
         */

        public long GetRecomputedActualSizeInBytes()
        {
            lock (this)
            {
                long size = 0;
                System.Collections.IEnumerator it = fileMap.Values.GetEnumerator();
                while (it.MoveNext())
                    size += ((RAMFile)it.Current).length_ForNUnit;
                return size;
            }
        }

        public override void Close()
        {
            lock (this)
            {
                if (openFiles == null)
                {
                    openFiles = new System.Collections.Hashtable();
                    openFilesDeleted = new System.Collections.Hashtable();
                }
                if (noDeleteOpenFile && openFiles.Count > 0)
                {
                    // RuntimeException instead of IOException because
                    // super() does not throw IOException currently:
                    throw new System.SystemException("MockRAMDirectory: cannot close: there are still open files: " + openFiles);
                }
            }
        }

        /**
         * Objects that represent fail-able conditions. Objects of a derived
         * class are created and registered with the mock directory. After
         * register, each object will be invoked once for each first write
         * of a file, giving the object a chance to throw an IOException.
         */
        public class Failure
        {
            /**
             * eval is called on the first write of every new file.
             */
            public virtual void Eval(MockRAMDirectory dir) { }

            /**
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

        /**
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

        /**
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