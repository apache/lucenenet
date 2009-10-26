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

// using FileChannel = java.nio.channels.FileChannel;
// using FileLock = java.nio.channels.FileLock;

namespace Lucene.Net.Store
{
	
	/// <summary> <p>Implements {@link LockFactory} using native OS file
	/// locks.  Note that because this LockFactory relies on
	/// java.nio.* APIs for locking, any problems with those APIs
	/// will cause locking to fail.  Specifically, on certain NFS
	/// environments the java.nio.* locks will fail (the lock can
	/// incorrectly be double acquired) whereas {@link
	/// SimpleFSLockFactory} worked perfectly in those same
	/// environments.  For NFS based access to an index, it's
	/// recommended that you try {@link SimpleFSLockFactory}
	/// first and work around the one limitation that a lock file
	/// could be left when the JVM exits abnormally.</p>
	/// 
	/// <p>The primary benefit of {@link NativeFSLockFactory} is
	/// that lock files will be properly removed (by the OS) if
	/// the JVM has an abnormal exit.</p>
	/// 
	/// <p>Note that, unlike {@link SimpleFSLockFactory}, the existence of
	/// leftover lock files in the filesystem on exiting the JVM
	/// is fine because the OS will free the locks held against
	/// these files even though the files still remain.</p>
	/// 
	/// <p>If you suspect that this or any other LockFactory is
	/// not working properly in your environment, you can easily
	/// test it by using {@link VerifyingLockFactory}, {@link
	/// LockVerifyServer} and {@link LockStressTest}.</p>
	/// 
	/// </summary>
	/// <seealso cref="LockFactory">
	/// </seealso>
	
	public class NativeFSLockFactory : LockFactory
	{
		
		/// <summary> Directory specified by <code>Lucene.Net.lockDir</code>
		/// system property.  If that is not set, then <code>java.io.tmpdir</code>
		/// system property is used.
		/// </summary>
		
		private System.IO.FileInfo lockDir;
		
		// Simple test to verify locking system is "working".  On
		// NFS, if it's misconfigured, you can hit long (35
		// second) timeouts which cause Lock.obtain to take far
		// too long (it assumes the obtain() call takes zero
		// time).  Since it's a configuration problem, we test up
		// front once on creating the LockFactory:
		private void  AcquireTestLock()
		{
			System.String randomLockName = "lucene-" + System.Convert.ToString(new System.Random().Next(), 16) + "-test.lock";
			
			Lock l = MakeLock(randomLockName);
			try
			{
				l.Obtain();
			}
			catch (System.IO.IOException e)
			{
				System.IO.IOException e2 = new System.IO.IOException("Failed to acquire random test lock; please verify filesystem for lock directory '" + lockDir + "' supports locking", e);
				throw e2;
			}
			
			l.Release();
		}
		
		/// <summary> Create a NativeFSLockFactory instance, with null (unset)
		/// lock directory.  This is package-private and is only
		/// used by FSDirectory when creating this LockFactory via
		/// the System property
		/// Lucene.Net.Store.FSDirectoryLockFactoryClass.
		/// </summary>
		internal NativeFSLockFactory() : this((System.IO.FileInfo) null)
		{
		}
		
		/// <summary> Create a NativeFSLockFactory instance, storing lock
		/// files into the specified lockDirName:
		/// 
		/// </summary>
		/// <param name="lockDirName">where lock files are created.
		/// </param>
		public NativeFSLockFactory(System.String lockDirName) : this(new System.IO.FileInfo(lockDirName))
		{
		}
		
		/// <summary> Create a NativeFSLockFactory instance, storing lock
		/// files into the specified lockDir:
		/// 
		/// </summary>
		/// <param name="lockDir">where lock files are created.
		/// </param>
		public NativeFSLockFactory(System.IO.FileInfo lockDir)
		{
			SetLockDir(lockDir);
		}
		
		/// <summary> Set the lock directory.  This is package-private and is
		/// only used externally by FSDirectory when creating this
		/// LockFactory via the System property
		/// Lucene.Net.Store.FSDirectoryLockFactoryClass.
		/// </summary>
		internal virtual void  SetLockDir(System.IO.FileInfo lockDir)
		{
			this.lockDir = lockDir;
			if (lockDir != null)
			{
				// Ensure that lockDir exists and is a directory.
				bool tmpBool;
				if (System.IO.File.Exists(lockDir.FullName))
					tmpBool = true;
				else
					tmpBool = System.IO.Directory.Exists(lockDir.FullName);
				if (!tmpBool)
				{
	                try
	                {
	                    System.IO.Directory.CreateDirectory(lockDir.FullName);
	                }
	                catch
	                {
	                    throw new System.IO.IOException("Cannot create directory: " + lockDir.FullName);
	                }
				}
				else if (!System.IO.Directory.Exists(lockDir.FullName))
				{
					throw new System.IO.IOException("Found regular file where directory expected: " + lockDir.FullName);
				}
				
				AcquireTestLock();
			}
		}
		
		public override Lock MakeLock(System.String lockName)
		{
			lock (this)
			{
				if (lockPrefix != null)
					lockName = lockPrefix + "-n-" + lockName;
				return new NativeFSLock(lockDir, lockName);
			}
		}
		
		public override void  ClearLock(System.String lockName)
		{
			// Note that this isn't strictly required anymore
			// because the existence of these files does not mean
			// they are locked, but, still do this in case people
			// really want to see the files go away:
			bool tmpBool;
			if (System.IO.File.Exists(lockDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(lockDir.FullName);
			if (tmpBool)
			{
				if (lockPrefix != null)
				{
					lockName = lockPrefix + "-n-" + lockName;
				}
				System.IO.FileInfo lockFile = new System.IO.FileInfo(System.IO.Path.Combine(lockDir.FullName, lockName));
				bool tmpBool2;
				if (System.IO.File.Exists(lockFile.FullName))
					tmpBool2 = true;
				else
					tmpBool2 = System.IO.Directory.Exists(lockFile.FullName);
				bool tmpBool3;
				if (System.IO.File.Exists(lockFile.FullName))
				{
					System.IO.File.Delete(lockFile.FullName);
					tmpBool3 = true;
				}
				else if (System.IO.Directory.Exists(lockFile.FullName))
				{
					System.IO.Directory.Delete(lockFile.FullName);
					tmpBool3 = true;
				}
				else
					tmpBool3 = false;
				if (tmpBool2 && !tmpBool3)
				{
					throw new System.IO.IOException("Cannot delete " + lockFile);
				}
			}
		}
	}
	
	
	class NativeFSLock : Lock
	{
		
		private System.IO.FileStream f;
		private System.IO.FileStream channel;   // private FileChannel B; // {{Aroush-2.1}}
		private bool lock_Renamed;              // FileLock lock_Renamed;   // {{Aroush-2.1}}
		private System.IO.FileInfo path;
		private System.IO.FileInfo lockDir;
		
		/*
		* The javadocs for FileChannel state that you should have
		* a single instance of a FileChannel (per JVM) for all
		* locking against a given file.  To ensure this, we have
		* a single (static) HashSet that contains the file paths
		* of all currently locked locks.  This protects against
		* possible cases where different Directory instances in
		* one JVM (each with their own NativeFSLockFactory
		* instance) have set the same lock dir and lock prefix.
		*/
		private static System.Collections.Hashtable LOCK_HELD = new System.Collections.Hashtable();
		
		public NativeFSLock(System.IO.FileInfo lockDir, System.String lockFileName)
		{
			this.lockDir = lockDir;
			path = new System.IO.FileInfo(System.IO.Path.Combine(lockDir.FullName, lockFileName));
		}
		
		public override bool Obtain()
		{
			lock (this)
			{
				
				if (IsLocked())
				{
					// Our instance is already locked:
					return false;
				}
				
				// Ensure that lockDir exists and is a directory.
				bool tmpBool;
				if (System.IO.File.Exists(lockDir.FullName))
					tmpBool = true;
				else
					tmpBool = System.IO.Directory.Exists(lockDir.FullName);
				if (!tmpBool)
				{
                    try
                    {
                        System.IO.Directory.CreateDirectory(lockDir.FullName);
                    }
                    catch
                    {
                        throw new System.IO.IOException("Cannot create directory: " + lockDir.FullName);
                    }
				}
				else
				{
                    try
                    {
                         System.IO.Directory.Exists(lockDir.FullName);
                    }
                    catch
                    {
                        throw new System.IO.IOException("Found regular file where directory expected: " + lockDir.FullName);
                    }
				}
				
				System.String canonicalPath = path.FullName;
				
				bool markedHeld = false;
				
				try
				{
					
					// Make sure nobody else in-process has this lock held
					// already, and, mark it held if not:
					
					lock (LOCK_HELD)
					{
						if (LOCK_HELD.Contains(canonicalPath))
						{
							// Someone else in this JVM already has the lock:
							return false;
						}
						else
						{
							// This "reserves" the fact that we are the one
							// thread trying to obtain this lock, so we own
							// the only instance of a channel against this
							// file:
							LOCK_HELD.Add(canonicalPath, canonicalPath);
							markedHeld = true;
						}
					}
					
					try
					{
						f = new System.IO.FileStream(path.FullName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite); 
					}
					catch (System.IO.IOException e)
					{
						// On Windows, we can get intermittant "Access
						// Denied" here.  So, we treat this as failure to
						// acquire the lock, but, store the reason in case
						// there is in fact a real error case.
						failureReason = e;
						f = null;
					}
					
					if (f != null)
					{
						try
						{
							channel = f; // f.getChannel();     // {{Aroush-2.1}}
                            lock_Renamed = false;
							try
							{
								channel.Lock(0, channel.Length);
                                lock_Renamed = true;
							}
							catch (System.IO.IOException e)
							{
								// At least on OS X, we will sometimes get an
								// intermittant "Permission Denied" IOException,
								// which seems to simply mean "you failed to get
								// the lock".  But other IOExceptions could be
								// "permanent" (eg, locking is not supported via
								// the filesystem).  So, we record the failure
								// reason here; the timeout obtain (usually the
								// one calling us) will use this as "root cause"
								// if it fails to get the lock.
								failureReason = e;
							}
							finally
							{
								if (lock_Renamed == false)
								{
									try
									{
										channel.Close();
									}
									finally
									{
										channel = null;
									}
								}
							}
						}
						finally
						{
							if (channel == null)
							{
								try
								{
									f.Close();
								}
								finally
								{
									f = null;
								}
							}
						}
					}
				}
				finally
				{
					if (markedHeld && !IsLocked())
					{
						lock (LOCK_HELD)
						{
							if (LOCK_HELD.Contains(canonicalPath))
							{
								LOCK_HELD.Remove(canonicalPath);
							}
						}
					}
				}
				return IsLocked();
			}
		}
		
		public override void  Release()
		{
			lock (this)
			{
				if (IsLocked())
				{
					try
					{
                        channel.Unlock(0, channel.Length);
					}
					finally
					{
						lock_Renamed = false;
						try
						{
							channel.Close();
						}
						finally
						{
							channel = null;
							try
							{
								f.Close();
							}
							finally
							{
								f = null;
								lock (LOCK_HELD)
								{
									LOCK_HELD.Remove(path.FullName);
								}
							}
						}
					}
					bool tmpBool;
					if (System.IO.File.Exists(path.FullName))
					{
						System.IO.File.Delete(path.FullName);
						tmpBool = true;
					}
					else if (System.IO.Directory.Exists(path.FullName))
					{
						System.IO.Directory.Delete(path.FullName);
						tmpBool = true;
					}
					else
						tmpBool = false;
					if (!tmpBool)
						throw new LockReleaseFailedException("failed to delete " + path);
				}
			}
		}
		
		public override bool IsLocked()
		{
			lock (this)
			{
				return lock_Renamed;
			}
		}
		
		public override System.String ToString()
		{
			return "NativeFSLock@" + path;
		}
		
		~NativeFSLock()
		{
			try
			{
				if (IsLocked())
				{
					Release();
				}
			}
			finally
			{
			}
		}
	}
}