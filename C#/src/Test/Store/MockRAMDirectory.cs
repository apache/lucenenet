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
		internal System.Random randomState;
		internal bool noDeleteOpenFile = true;
        internal bool preventDoubleWrite = true;
        private System.Collections.Generic.IDictionary<string, string> unSyncedFiles;
        private System.Collections.Generic.IDictionary<string, string> createdFiles;
        internal volatile bool crashed;

		// NOTE: we cannot initialize the Map here due to the
		// order in which our constructor actually does this
		// member initialization vs when it calls super.  It seems
		// like super is called, then our members are initialized:
		internal System.Collections.IDictionary openFiles;

        private void Init()
        {
            if (openFiles == null)
                openFiles = new System.Collections.Hashtable();
            if (createdFiles == null)
                createdFiles = new System.Collections.Generic.Dictionary<string, string>();
            if (unSyncedFiles == null)
                unSyncedFiles = new System.Collections.Generic.Dictionary<string, string>();

        }

		public MockRAMDirectory() : base()
		{
            Init();
		}
		public MockRAMDirectory(System.String dir) : base(dir)
		{
            Init();
        }
		public MockRAMDirectory(Directory dir) : base(dir)
		{
            Init();
        }
		public MockRAMDirectory(System.IO.FileInfo dir) : base(dir)
		{
            Init();
        }

        /// <summary>
        /// If set to true, we throw an IOException if the same file is opened by createOutput, ever.
        /// </summary>
        /// <param name="value"></param>
        public void SetPreventDoubleWrite(bool value)
        {
            preventDoubleWrite = value;
        }

        override public void Sync(string name)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();
                if (crashed)
                    throw new System.IO.IOException("cannot sync after crash");
                if (unSyncedFiles.ContainsKey(name))
                    unSyncedFiles.Remove(name);
            }
        }

        /// <summary>
        /// Simulates a crash of OS or machine by overwriting unsynced files.
        /// </summary>
        public void Crash()
        {
            lock (this)
            {
                crashed = true;
                openFiles = new System.Collections.Hashtable();
            }
            System.Collections.Generic.IEnumerator<string> it = unSyncedFiles.Keys.GetEnumerator();
            unSyncedFiles = new System.Collections.Generic.Dictionary<string, string>();
            int count = 0;
            while (it.MoveNext())
            {
                string name = it.Current;
                RAMFile file = (RAMFile)fileMap_ForNUnitTest[name];
                if (count % 3 == 0)
                {
                    DeleteFile(name, true);
                }
                else if (count % 3 == 1)
                {
                    // Zero out file entirely
                    int numBuffers = file.NumBuffers_ForNUnitTest();
                    for (int i = 0; i < numBuffers; i++)
                    {
                        byte[] buffer = file.GetBuffer_ForNUnitTest(i);
                        SupportClass.CollectionsSupport.ArrayFill(buffer, (byte)0);
                    }
                }
                else if (count % 3 == 2)
                {
                    // truncate the file:
                    file.SetLength_ForNUnitTest(file.GetLength_ForNUnitTest() / 2);
                }
                count++;
            }
        }

        public void ClearCrash()
        {
            lock (this) { crashed = false; }
        }

		public virtual void  SetMaxSizeInBytes(long maxSize)
		{
			this.maxSize = maxSize;
		}
		public virtual long GetMaxSizeInBytes()
		{
			return this.maxSize;
		}
		
		/// <summary> Returns the peek actual storage used (bytes) in this
		/// directory.
		/// </summary>
		public virtual long GetMaxUsedSizeInBytes()
		{
			return this.maxUsedSize;
		}
		public virtual void  ResetMaxUsedSizeInBytes()
		{
			this.maxUsedSize = GetRecomputedActualSizeInBytes();
		}
		
		/// <summary> Emulate windows whereby deleting an open file is not
		/// allowed (raise IOException).
		/// </summary>
		public virtual void  SetNoDeleteOpenFile(bool value_Renamed)
		{
			this.noDeleteOpenFile = value_Renamed;
		}
		public virtual bool GetNoDeleteOpenFile()
		{
			return noDeleteOpenFile;
		}
		
		/// <summary> If 0.0, no exceptions will be thrown.  Else this should
		/// be a double 0.0 - 1.0.  We will randomly throw an
		/// IOException on the first write to an OutputStream based
		/// on this probability.
		/// </summary>
		public virtual void  SetRandomIOExceptionRate(double rate, long seed)
		{
			randomIOExceptionRate = rate;
			// seed so we have deterministic behaviour:
			randomState = new System.Random((System.Int32) seed);
		}
		public virtual double GetRandomIOExceptionRate()
		{
			return randomIOExceptionRate;
		}
		
		internal virtual void  MaybeThrowIOException()
		{
			if (randomIOExceptionRate > 0.0)
			{
				int number = System.Math.Abs(randomState.Next() % 1000);
				if (number < randomIOExceptionRate * 1000)
				{
					throw new System.IO.IOException("a random IOException");
				}
			}
		}
		
		public override void  DeleteFile(System.String name)
		{
			lock (this)
			{
                DeleteFile(name, false);
            }
        }

        private void DeleteFile(string name, bool forced)
        {
            lock (this)
            {
                MaybeThrowDeterministicException();

                if (crashed && !forced)
                    throw new System.IO.IOException("cannot delete after crash");

                if (unSyncedFiles.ContainsKey(name))
                    unSyncedFiles.Remove(name);

                if (!forced)
                {
                    lock (openFiles.SyncRoot)
                    {
                        if (noDeleteOpenFile && openFiles.Contains(name))
                        {
                            throw new System.IO.IOException("MockRAMDirectory: file \"" + name + "\" is still open: cannot delete");
                        }
                    }
                }
				base.DeleteFile(name);
			}
		}
		
		public override IndexOutput CreateOutput(System.String name)
		{
            if (crashed)
                throw new System.IO.IOException("cannot create output after crash");
            Init();
            lock (openFiles.SyncRoot)
            {
                if (preventDoubleWrite && createdFiles.ContainsKey(name) && !name.Equals("segments.gen"))
                    throw new System.IO.IOException("file \"" + name + "\" is still open: cannot overwrite");
                if (noDeleteOpenFile && openFiles.Contains(name))
                    throw new System.IO.IOException("MockRAMDirectory: file \"" + name + "\" is still open: cannot overwrite");
            }
			RAMFile file = new RAMFile(this);
			lock (this)
			{
                if (crashed)
                    throw new System.IO.IOException("cannot create output after crash");
                unSyncedFiles[name] = name;
                createdFiles[name] = name;
                RAMFile existing = (RAMFile)fileMap_ForNUnitTest[name];
				// Enforce write once:
				if (existing != null && !name.Equals("segments.gen"))
					throw new System.IO.IOException("file " + name + " already exists");
				else
				{
					if (existing != null)
					{
						sizeInBytes_ForNUnitTest -= existing.sizeInBytes_ForNUnitTest;
						existing.directory_ForNUnitTest = null;
					}

					fileMap_ForNUnitTest[name] = file;
				}
			}
			
			return new MockRAMOutputStream(this, file);
		}
		
		public override IndexInput OpenInput(System.String name)
		{
			RAMFile file;
			lock (this)
			{
				file = (RAMFile)fileMap_ForNUnitTest[name];
			}
			if (file == null)
				throw new System.IO.FileNotFoundException(name);
			else
			{
				lock (openFiles.SyncRoot)
				{
					if (openFiles.Contains(name))
					{
						System.Int32 v = (System.Int32) openFiles[name];
						v = (System.Int32) (v + 1);
						openFiles[name] = v;
					}
					else
					{
						openFiles[name] = 1;
					}
				}
			}
			return new MockRAMInputStream(this, name, file);
		}
		
		/// <summary>Provided for testing purposes.  Use sizeInBytes() instead. </summary>
		public long GetRecomputedSizeInBytes()
		{
			lock (this)
			{
				long size = 0;
				System.Collections.IEnumerator it = fileMap_ForNUnitTest.Values.GetEnumerator();
				while (it.MoveNext())
				{
					size += ((RAMFile)it.Current).GetSizeInBytes_ForNUnitTest();
				}
				return size;
			}
		}
		
		/// <summary>Like getRecomputedSizeInBytes(), but, uses actual file
		/// lengths rather than buffer allocations (which are
		/// quantized up to nearest
		/// RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
		/// </summary>
		
		public long GetRecomputedActualSizeInBytes()
		{
			lock (this)
			{
				long size = 0;
				System.Collections.IEnumerator it = fileMap_ForNUnitTest.Values.GetEnumerator();
				while (it.MoveNext())
				{
					size += ((RAMFile)it.Current).length_ForNUnitTest;
				}
				return size;
			}
		}
		
		public override void  Close()
		{
			if (openFiles == null)
			{
				openFiles = new System.Collections.Hashtable();
			}
			lock (openFiles.SyncRoot)
			{
				if (noDeleteOpenFile && openFiles.Count > 0)
				{
					// RuntimeException instead of IOException because
					// super() does not throw IOException currently:
					throw new System.SystemException("MockRAMDirectory: cannot close: there are still open files: " + openFiles.ToString());
				}
			}
		}
		
		/// <summary> Objects that represent fail-able conditions. Objects of a derived
		/// class are created and registered with the mock directory. After
		/// register, each object will be invoked once for each first write
		/// of a file, giving the object a chance to throw an IOException.
		/// </summary>
		public class Failure
		{
			/// <summary> eval is called on the first write of every new file.</summary>
			public virtual void  Eval(MockRAMDirectory dir)
			{
			}
			
			/// <summary> reset should set the state of the failure to its default
			/// (freshly constructed) state. Reset is convenient for tests
			/// that want to create one failure object and then reuse it in
			/// multiple cases. This, combined with the fact that Failure
			/// subclasses are often anonymous classes makes reset difficult to
			/// do otherwise.
			/// 
			/// A typical example of use is
			/// Failure failure = new Failure() { ... };
			/// ...
			/// mock.failOn(failure.reset())
			/// </summary>
			public virtual Failure Reset()
			{
				return this;
			}
			
			protected internal bool doFail;
			
			public virtual void  SetDoFail()
			{
				doFail = true;
			}
			
			public virtual void  ClearDoFail()
			{
				doFail = false;
			}
		}
		
		internal System.Collections.ArrayList failures;
		
		/// <summary> add a Failure object to the list of objects to be evaluated
		/// at every potential failure point
		/// </summary>
		public virtual void  FailOn(Failure fail)
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
		
		/// <summary> Iterate through the failures list, giving each object a
		/// chance to throw an IOE
		/// </summary>
		internal virtual void  MaybeThrowDeterministicException()
		{
			lock (this)
			{
				if (failures != null)
				{
					for (int i = 0; i < failures.Count; i++)
					{
						((Failure) failures[i]).Eval(this);
					}
				}
			}
		}
	}
}