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

using IndexFileNameFilter = Lucene.Net.Index.IndexFileNameFilter;
// Used only for WRITE_LOCK_NAME in deprecated create=true case:
using IndexWriter = Lucene.Net.Index.IndexWriter;

namespace Lucene.Net.Store
{
	
	/// <summary> Straightforward implementation of {@link Directory} as a directory of files.
	/// Locking implementation is by default the {@link SimpleFSLockFactory}, but
	/// can be changed either by passing in a {@link LockFactory} instance to
	/// <code>getDirectory</code>, or specifying the LockFactory class by setting
	/// <code>Lucene.Net.Store.FSDirectoryLockFactoryClass</code> Java system
	/// property, or by calling {@link #setLockFactory} after creating
	/// the Directory.
	/// <p>Directories are cached, so that, for a given canonical
	/// path, the same FSDirectory instance will always be
	/// returned by <code>getDirectory</code>.  This permits
	/// synchronization on directories.</p>
	/// 
	/// </summary>
	/// <seealso cref="Directory">
	/// </seealso>
	/// <author>  Doug Cutting
	/// </author>
	public class FSDirectory : Directory
	{
		
		/// <summary>This cache of directories ensures that there is a unique Directory
		/// instance per path, so that synchronization on the Directory can be used to
		/// synchronize access between readers and writers.  We use
		/// refcounts to ensure when the last use of an FSDirectory
		/// instance for a given canonical path is closed, we remove the
		/// instance from the cache.  See LUCENE-776
		/// for some relevant discussion.
		/// </summary>
		private static readonly System.Collections.Hashtable DIRECTORIES = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		
		private static bool disableLocks = false;
		
		// TODO: should this move up to the Directory base class?  Also: should we
		// make a per-instance (in addition to the static "default") version?
		
		/// <summary> Set whether Lucene's use of lock files is disabled. By default, 
		/// lock files are enabled. They should only be disabled if the index
		/// is on a read-only medium like a CD-ROM.
		/// </summary>
		public static void  SetDisableLocks(bool doDisableLocks)
		{
			FSDirectory.disableLocks = doDisableLocks;
		}
		
		/// <summary> Returns whether Lucene's use of lock files is disabled.</summary>
		/// <returns> true if locks are disabled, false if locks are enabled.
		/// </returns>
		public static bool GetDisableLocks()
		{
			return FSDirectory.disableLocks;
		}
		
		/// <summary> Directory specified by <code>Lucene.Net.lockDir</code>
		/// or <code>java.io.tmpdir</code> system property.
		/// </summary>
		/// <deprecated> As of 2.1, <code>LOCK_DIR</code> is unused
		/// because the write.lock is now stored by default in the
		/// index directory.  If you really want to store locks
		/// elsewhere you can create your own {@link
		/// SimpleFSLockFactory} (or {@link NativeFSLockFactory},
		/// etc.) passing in your preferred lock directory.  Then,
		/// pass this <code>LockFactory</code> instance to one of
		/// the <code>getDirectory</code> methods that take a
		/// <code>lockFactory</code> (for example, {@link #GetDirectory(String, LockFactory)}).
		/// </deprecated>
        
        //Deprecated. As of 2.1, LOCK_DIR is unused because the write.lock is now stored by default in the index directory. 
        //If you really want to store locks elsewhere you can create your own SimpleFSLockFactory (or NativeFSLockFactory, etc.) passing in your preferred lock directory. 
        //Then, pass this LockFactory instance to one of the getDirectory methods that take a lockFactory (for example, getDirectory(String, LockFactory)).
		//public static readonly System.String LOCK_DIR = SupportClass.AppSettings.Get("Lucene.Net.lockDir", System.IO.Path.GetTempPath());
		
		/// <summary>The default class which implements filesystem-based directories. </summary>
		private static System.Type IMPL;
		
        private static System.Security.Cryptography.HashAlgorithm DIGESTER;
		
		/// <summary>A buffer optionally used in renameTo method </summary>
		private byte[] buffer = null;
		
		/// <summary>Returns the directory instance for the named location.</summary>
		/// <param name="path">the path to the directory.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.String path)
		{
			return GetDirectory(new System.IO.FileInfo(path), null);
		}
		
		/// <summary>Returns the directory instance for the named location.</summary>
		/// <param name="path">the path to the directory.
		/// </param>
		/// <param name="lockFactory">instance of {@link LockFactory} providing the
		/// locking implementation.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.String path, LockFactory lockFactory)
		{
			return GetDirectory(new System.IO.FileInfo(path), lockFactory);
		}
		
		/// <summary>Returns the directory instance for the named location.</summary>
		/// <param name="file">the path to the directory.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.IO.FileInfo file)
		{
			return GetDirectory(file, null);
		}
		
		/// <summary>Returns the directory instance for the named location.</summary>
		/// <param name="file">the path to the directory.
		/// </param>
		/// <param name="lockFactory">instance of {@link LockFactory} providing the
		/// locking implementation.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.IO.FileInfo file, LockFactory lockFactory)
		{
			file = new System.IO.FileInfo(file.FullName);
			
			bool tmpBool;
			if (System.IO.File.Exists(file.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(file.FullName);
			if (tmpBool && !System.IO.Directory.Exists(file.FullName))
				throw new System.IO.IOException(file + " not a directory");
			
			bool tmpBool2;
			if (System.IO.File.Exists(file.FullName))
				tmpBool2 = true;
			else
				tmpBool2 = System.IO.Directory.Exists(file.FullName);
			if (!tmpBool2)
			{
                try
                {
                    System.IO.Directory.CreateDirectory(file.FullName);
                }
                catch
                {
                    throw new System.IO.IOException("Cannot create directory: " + file);
                }
			}
			
			FSDirectory dir;
			lock (DIRECTORIES.SyncRoot)
			{
				dir = (FSDirectory) DIRECTORIES[file.FullName];
				if (dir == null)
				{
					try
					{
						dir = (FSDirectory) System.Activator.CreateInstance(IMPL);
					}
					catch (System.Exception e)
					{
						throw new System.SystemException("cannot load FSDirectory class: " + e.ToString(), e);
					}
					dir.Init(file, lockFactory);
					DIRECTORIES[file.FullName] = dir;
				}
				else
				{
					// Catch the case where a Directory is pulled from the cache, but has a
					// different LockFactory instance.
					if (lockFactory != null && lockFactory != dir.GetLockFactory())
					{
						throw new System.IO.IOException("Directory was previously created with a different LockFactory instance; please pass null as the lockFactory instance and use setLockFactory to change it");
					}
				}
			}
			lock (dir)
			{
				dir.refCount++;
			}
			return dir;
		}
		
		
		/// <summary>Returns the directory instance for the named location.
		/// 
		/// </summary>
		/// <deprecated> Use IndexWriter's create flag, instead, to
		/// create a new index.
		/// 
		/// </deprecated>
		/// <param name="path">the path to the directory.
		/// </param>
		/// <param name="create">if true, create, or erase any existing contents.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.String path, bool create)
		{
			return GetDirectory(new System.IO.FileInfo(path), create);
		}
		
		/// <summary>Returns the directory instance for the named location.
		/// 
		/// </summary>
		/// <deprecated> Use IndexWriter's create flag, instead, to
		/// create a new index.
		/// 
		/// </deprecated>
		/// <param name="file">the path to the directory.
		/// </param>
		/// <param name="create">if true, create, or erase any existing contents.
		/// </param>
		/// <returns> the FSDirectory for the named file.  
		/// </returns>
		public static FSDirectory GetDirectory(System.IO.FileInfo file, bool create)
		{
			FSDirectory dir = GetDirectory(file, null);
			
			// This is now deprecated (creation should only be done
			// by IndexWriter):
			if (create)
			{
				dir.Create();
			}
			
			return dir;
		}
		
		private void  Create()
		{
			bool tmpBool;
			if (System.IO.File.Exists(directory.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(directory.FullName);
			if (tmpBool)
			{
                System.String[] files = SupportClass.FileSupport.GetLuceneIndexFiles(directory.FullName, IndexFileNameFilter.GetFilter());
				if (files == null)
					throw new System.IO.IOException("cannot read directory " + directory.FullName + ": list() returned null");
				for (int i = 0; i < files.Length; i++)
				{
					System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, files[i]));
					bool tmpBool2;
					if (System.IO.File.Exists(file.FullName))
					{
						System.IO.File.Delete(file.FullName);
						tmpBool2 = true;
					}
					else if (System.IO.Directory.Exists(file.FullName))
					{
						System.IO.Directory.Delete(file.FullName);
						tmpBool2 = true;
					}
					else
						tmpBool2 = false;
					if (!tmpBool2)
						throw new System.IO.IOException("Cannot delete " + file);
				}
			}
			lockFactory.ClearLock(IndexWriter.WRITE_LOCK_NAME);
		}
		
		private System.IO.FileInfo directory = null;
		private int refCount;
		
		public FSDirectory()
		{
		}
		
		// permit subclassing
		
		private void  Init(System.IO.FileInfo path, LockFactory lockFactory)
		{
			
			// Set up lockFactory with cascaded defaults: if an instance was passed in,
			// use that; else if locks are disabled, use NoLockFactory; else if the
			// system property Lucene.Net.Store.FSDirectoryLockFactoryClass is set,
			// instantiate that; else, use SimpleFSLockFactory:
			
			directory = path;
			
			bool doClearLockID = false;
			
			if (lockFactory == null)
			{
				
				if (disableLocks)
				{
					// Locks are disabled:
					lockFactory = NoLockFactory.GetNoLockFactory();
				}
				else
				{
					System.String lockClassName = SupportClass.AppSettings.Get("Lucene.Net.Store.FSDirectoryLockFactoryClass", "");
					
					if (lockClassName != null && !lockClassName.Equals(""))
					{
						System.Type c;
						
						try
						{
							c = System.Type.GetType(lockClassName);
						}
						catch (System.Exception)
						{
							throw new System.IO.IOException("unable to find LockClass " + lockClassName);
						}
						
						try
						{
							lockFactory = (LockFactory) System.Activator.CreateInstance(c, true);
						}
						catch (System.UnauthorizedAccessException e)
						{
							throw new System.IO.IOException("IllegalAccessException when instantiating LockClass " + lockClassName);
						}
						catch (System.InvalidCastException)
						{
							throw new System.IO.IOException("unable to cast LockClass " + lockClassName + " instance to a LockFactory");
						}
                        catch (System.Exception ex)
                        {
                            throw new System.IO.IOException("InstantiationException when instantiating LockClass " + lockClassName + "\nDetails:" + ex.Message);
                        }

                        if (lockFactory is NativeFSLockFactory)
                        {
                            ((NativeFSLockFactory) lockFactory).SetLockDir(path);
                        }
                        else if (lockFactory is SimpleFSLockFactory)
                        {
                            ((SimpleFSLockFactory) lockFactory).SetLockDir(path);
                        }
                    }
					else
					{
						// Our default lock is SimpleFSLockFactory;
						// default lockDir is our index directory:
						lockFactory = new SimpleFSLockFactory(path);
						doClearLockID = true;
					}
				}
			}
			
			SetLockFactory(lockFactory);
			
			if (doClearLockID)
			{
				// Clear the prefix because write.lock will be
				// stored in our directory:
				lockFactory.SetLockPrefix(null);
			}
		}
		
		/// <summary>Returns an array of strings, one for each Lucene index file in the directory. </summary>
		public override System.String[] List()
		{
            return SupportClass.FileSupport.GetLuceneIndexFiles(directory.FullName, IndexFileNameFilter.GetFilter());
		}
		
		/// <summary>Returns true iff a file with the given name exists. </summary>
		public override bool FileExists(System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			bool tmpBool;
			if (System.IO.File.Exists(file.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(file.FullName);
			return tmpBool;
		}
		
		/// <summary>Returns the time the named file was last modified. </summary>
		public override long FileModified(System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			return (file.LastWriteTime.Ticks);
		}
		
		/// <summary>Returns the time the named file was last modified. </summary>
		public static long FileModified(System.IO.FileInfo directory, System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			return (file.LastWriteTime.Ticks);
		}
		
		/// <summary>Set the modified time of an existing file to now. </summary>
		public override void  TouchFile(System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			file.LastWriteTime = System.DateTime.Now;
		}
		
		/// <summary>Returns the length in bytes of a file in the directory. </summary>
		public override long FileLength(System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			return file.Exists ? file.Length : 0;
		}
		
		/// <summary>Removes an existing file in the directory. </summary>
		public override void  DeleteFile(System.String name)
		{
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			bool tmpBool;
			if (System.IO.File.Exists(file.FullName))
			{
				System.IO.File.Delete(file.FullName);
				tmpBool = true;
			}
			else if (System.IO.Directory.Exists(file.FullName))
			{
				System.IO.Directory.Delete(file.FullName);
				tmpBool = true;
			}
			else
				tmpBool = false;
			if (!tmpBool)
				throw new System.IO.IOException("Cannot delete " + file);
		}
		
		/// <summary>Renames an existing file in the directory. 
		/// Warning: This is not atomic.
		/// </summary>
		/// <deprecated> 
		/// </deprecated>
		public override void  RenameFile(System.String from, System.String to)
		{
			lock (this)
			{
				System.IO.FileInfo old = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, from));
				System.IO.FileInfo nu = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, to));
				
				/* This is not atomic.  If the program crashes between the call to
				delete() and the call to renameTo() then we're screwed, but I've
				been unable to figure out how else to do this... */
				
				bool tmpBool;
				if (System.IO.File.Exists(nu.FullName))
					tmpBool = true;
				else
					tmpBool = System.IO.Directory.Exists(nu.FullName);
				if (tmpBool)
				{
					bool tmpBool2;
					if (System.IO.File.Exists(nu.FullName))
					{
						System.IO.File.Delete(nu.FullName);
						tmpBool2 = true;
					}
					else if (System.IO.Directory.Exists(nu.FullName))
					{
						System.IO.Directory.Delete(nu.FullName);
						tmpBool2 = true;
					}
					else
						tmpBool2 = false;
					if (!tmpBool2)
						throw new System.IO.IOException("Cannot delete " + nu);
				}
				
				// Rename the old file to the new one. Unfortunately, the renameTo()
				// method does not work reliably under some JVMs.  Therefore, if the
				// rename fails, we manually rename by copying the old file to the new one
                try
                {
                    old.MoveTo(nu.FullName);
                }
                catch
				{
					System.IO.Stream in_Renamed = null;
					System.IO.Stream out_Renamed = null;
					try
					{
						in_Renamed = new System.IO.FileStream(old.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
						out_Renamed = new System.IO.FileStream(nu.FullName, System.IO.FileMode.Create);
						// see if the buffer needs to be initialized. Initialization is
						// only done on-demand since many VM's will never run into the renameTo
						// bug and hence shouldn't waste 1K of mem for no reason.
						if (buffer == null)
						{
							buffer = new byte[1024];
						}
						int len;
						while ((len = in_Renamed.Read(buffer, 0, buffer.Length)) >= 0)
						{
							out_Renamed.Write(buffer, 0, len);
						}
						
						// delete the old file.
						bool tmpBool3;
						if (System.IO.File.Exists(old.FullName))
						{
							System.IO.File.Delete(old.FullName);
							tmpBool3 = true;
						}
						else if (System.IO.Directory.Exists(old.FullName))
						{
							System.IO.Directory.Delete(old.FullName);
							tmpBool3 = true;
						}
						else
							tmpBool3 = false;
						bool generatedAux = tmpBool3;
					}
					catch (System.IO.IOException ioe)
					{
						System.IO.IOException newExc = new System.IO.IOException("Cannot rename " + old + " to " + nu, ioe);
						throw newExc;
					}
					finally
					{
						try
						{
							if (in_Renamed != null)
							{
								try
								{
									in_Renamed.Close();
								}
								catch (System.IO.IOException e)
								{
									throw new System.SystemException("Cannot close input stream: " + e.ToString(), e);
								}
							}
						}
						finally
						{
							if (out_Renamed != null)
							{
								try
								{
									out_Renamed.Close();
								}
								catch (System.IO.IOException e)
								{
									throw new System.SystemException("Cannot close output stream: " + e.ToString(), e);
								}
							}
						}
					}
				}
			}
		}
		
		/// <summary>Creates a new, empty file in the directory with the given name.
		/// Returns a stream writing this file. 
		/// </summary>
		public override IndexOutput CreateOutput(System.String name)
		{
			
			System.IO.FileInfo file = new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name));
			bool tmpBool;
			if (System.IO.File.Exists(file.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(file.FullName);
			bool tmpBool2;
			if (System.IO.File.Exists(file.FullName))
			{
				System.IO.File.Delete(file.FullName);
				tmpBool2 = true;
			}
			else if (System.IO.Directory.Exists(file.FullName))
			{
				System.IO.Directory.Delete(file.FullName);
				tmpBool2 = true;
			}
			else
				tmpBool2 = false;
			if (tmpBool && !tmpBool2)
			// delete existing, if any
				throw new System.IO.IOException("Cannot overwrite: " + file);
			
			return new FSIndexOutput(file);
		}

        // Inherit javadoc
        public override IndexInput OpenInput(System.String name)
        {
            return OpenInput(name, BufferedIndexInput.BUFFER_SIZE);
        }

        // Inherit javadoc
        public override IndexInput OpenInput(System.String name, int bufferSize)
        {
            return new FSIndexInput(new System.IO.FileInfo(System.IO.Path.Combine(directory.FullName, name)), bufferSize);
        }

		/// <summary> So we can do some byte-to-hexchar conversion below</summary>
		private static readonly char[] HEX_DIGITS = new char[]{'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
		
		
		public override System.String GetLockID()
		{
			System.String dirName; // name to be hashed
			try
			{
				dirName = directory.FullName;
			}
			catch (System.IO.IOException e)
			{
				throw new System.SystemException(e.ToString(), e);
			}
			
			byte[] digest;
			lock (DIGESTER)
			{
				digest = DIGESTER.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dirName));
			}
			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			buf.Append("lucene-");
			for (int i = 0; i < digest.Length; i++)
			{
				int b = digest[i];
				buf.Append(HEX_DIGITS[(b >> 4) & 0xf]);
				buf.Append(HEX_DIGITS[b & 0xf]);
			}
			
			return buf.ToString();
		}
		
		/// <summary>Closes the store to future operations. </summary>
		public override void  Close()
		{
			lock (this)
			{
				if (--refCount <= 0)
				{
					lock (DIRECTORIES.SyncRoot)
					{
						DIRECTORIES.Remove(directory.FullName);
					}
				}
			}
		}
		
		public virtual System.IO.FileInfo GetFile()
		{
			return directory;
		}
		
		/// <summary>For debug output. </summary>
		public override System.String ToString()
		{
			return this.GetType().FullName + "@" + directory;
		}
		
		public /*protected internal*/ class FSIndexInput : BufferedIndexInput, System.ICloneable
		{
		
			private class Descriptor : System.IO.BinaryReader
			{
				// remember if the file is open, so that we don't try to close it
				// more than once
				private bool isOpen;
				internal long position;
				internal long length;
			
	            public Descriptor(FSIndexInput enclosingInstance, System.IO.FileInfo file, System.IO.FileAccess mode) 
	                : base(new System.IO.FileStream(file.FullName, System.IO.FileMode.Open, mode, System.IO.FileShare.ReadWrite))
	            {
					isOpen = true;
	                length = file.Length;
				}
			
				public override void  Close()
				{
					if (isOpen)
					{
						isOpen = false;
						base.Close();
					}
				}
			
				~Descriptor()
				{
					try
					{
						Close();
					}
					finally
					{
					}
				}
			}
		
			private Descriptor file;
			internal bool isClone;
			
	        public bool isClone_ForNUnitTest
	        {
	            get { return isClone; }
	        }
			
			public FSIndexInput(System.IO.FileInfo path) : this(path, BufferedIndexInput.BUFFER_SIZE)
			{
			}
            
			public FSIndexInput(System.IO.FileInfo path, int bufferSize) : base(bufferSize)
			{
                UnauthorizedAccessException ex = null;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        file = new Descriptor(this, path, System.IO.FileAccess.Read);
                        return;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        ex = e;
                        System.Threading.Thread.Sleep(100);
                        GC.Collect();
                    }
                }
                throw ex;
			}
		
			/// <summary>IndexInput methods </summary>
			protected internal override void  ReadInternal(byte[] b, int offset, int len)
			{
				lock (file)
				{
					long position = GetFilePointer();
					if (position != file.position)
					{
						file.BaseStream.Seek(position, System.IO.SeekOrigin.Begin);
						file.position = position;
					}
					int total = 0;
					do 
					{
						int i = file.Read(b, offset + total, len - total);
						if (i <= 0)
							throw new System.IO.IOException("read past EOF");
						file.position += i;
						total += i;
					}
					while (total < len);
				}
			}
			
			public override void  Close()
			{
				// only close the file if this is not a clone
				if (!isClone)
					file.Close();
	            System.GC.SuppressFinalize(this);
			}
		
			protected internal override void  SeekInternal(long position)
			{
			}
			
			public override long Length()
			{
				return file.length;
			}
			
			public override System.Object Clone()
			{
				FSIndexInput clone = (FSIndexInput) base.Clone();
				clone.isClone = true;
				return clone;
			}

			/// <summary>Method used for testing. Returns true if the underlying
			/// file descriptor is valid.
			/// </summary>
			public virtual bool IsFDValid()
			{
				return (file.BaseStream != null);
			}
		}
		
		protected internal class FSIndexOutput : BufferedIndexOutput
		{
			internal System.IO.BinaryWriter file = null;
		
			// remember if the file is open, so that we don't try to close it
			// more than once
			private bool isOpen;
		
			public FSIndexOutput(System.IO.FileInfo path)
			{
                UnauthorizedAccessException ex = null;
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
   				        file = new System.IO.BinaryWriter(new System.IO.FileStream(path.FullName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite));
				        isOpen = true;
                        return;
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        ex = e;
                        System.Threading.Thread.Sleep(100);
                        GC.Collect();
                    }
                }
                throw ex;
            }
		
			/// <summary>output methods: </summary>
			public override void  FlushBuffer(byte[] b, int offset, int size)
			{
				file.Write(b, offset, size);
			}
			public override void  Close()
			{
				// only close the file if it has not been closed yet
				if (isOpen)
				{
					base.Close();
					file.Close();
					isOpen = false;
	                System.GC.SuppressFinalize(this);
				}
			}
		
			/// <summary>Random-access methods </summary>
			public override void  Seek(long pos)
			{
				base.Seek(pos);
				file.BaseStream.Seek(pos, System.IO.SeekOrigin.Begin);
			}
			public override long Length()
			{
				return file.BaseStream.Length;
			}
		}
		static FSDirectory()
		{
			{
				try
				{
					System.String name = SupportClass.AppSettings.Get("Lucene.Net.FSDirectory.class", typeof(FSDirectory).FullName);
					IMPL = System.Type.GetType(name);
				}
				catch (System.Security.SecurityException)
				{
					try
					{
						IMPL = System.Type.GetType(typeof(FSDirectory).FullName);
					}
					catch (System.Exception e)
					{
						throw new System.SystemException("cannot load default FSDirectory class: " + e.ToString(), e);
					}
				}
                catch (System.Exception e)
                {
                    throw new System.SystemException("cannot load FSDirectory class: " + e.ToString(), e);
                }
            }
			{
				try
				{
                    DIGESTER = SupportClass.Cryptography.GetHashAlgorithm();
				}
				catch (System.Exception e)
				{
					throw new System.SystemException(e.ToString(), e);
				}
			}
		}
	}
}
