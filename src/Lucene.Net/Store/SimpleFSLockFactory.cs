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
	
	/// <summary> <p>Implements {@link LockFactory} using {@link
	/// File#createNewFile()}.  This is the default LockFactory
	/// for {@link FSDirectory}.</p>
	/// 
	/// <p><b>NOTE:</b> the <a target="_top"
	/// href="http://java.sun.com/j2se/1.4.2/docs/api/java/io/File.html#createNewFile()">javadocs
	/// for <code>File.createNewFile</code></a> contain a vague
	/// yet spooky warning about not using the API for file
	/// locking.  This warning was added due to <a target="_top"
	/// href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4676183">this
	/// bug</a>, and in fact the only known problem with using
	/// this API for locking is that the Lucene write lock may
	/// not be released when the JVM exits abnormally.</p>
	/// <p>When this happens, a {@link LockObtainFailedException}
	/// is hit when trying to create a writer, in which case you
	/// need to explicitly clear the lock file first.  You can
	/// either manually remove the file, or use the {@link
	/// Lucene.Net.Index.IndexReader#Unlock(Directory)}
	/// API.  But, first be certain that no writer is in fact
	/// writing to the index otherwise you can easily corrupt
	/// your index.</p>
	/// 
	/// <p>If you suspect that this or any other LockFactory is
	/// not working properly in your environment, you can easily
	/// test it by using {@link VerifyingLockFactory}, {@link
	/// LockVerifyServer} and {@link LockStressTest}.</p>
	/// 
	/// </summary>
	/// <seealso cref="LockFactory">
	/// </seealso>
	
	public class SimpleFSLockFactory : LockFactory
	{
		
		/// <summary> Directory specified by <code>Lucene.Net.lockDir</code>
		/// system property.  If that is not set, then <code>java.io.tmpdir</code>
		/// system property is used.
		/// </summary>
		
		private System.IO.FileInfo lockDir;
		
		/// <summary> Create a SimpleFSLockFactory instance, with null (unset)
		/// lock directory.  This is package-private and is only
		/// used by FSDirectory when creating this LockFactory via
		/// the System property
		/// Lucene.Net.Store.FSDirectoryLockFactoryClass.
		/// </summary>
		internal SimpleFSLockFactory() : this((System.IO.FileInfo) null)
		{
		}
		
		/// <summary> Instantiate using the provided directory (as a File instance).</summary>
		/// <param name="lockDir">where lock files should be created.
		/// </param>
		public SimpleFSLockFactory(System.IO.FileInfo lockDir)
		{
			SetLockDir(lockDir);
		}
		
		/// <summary> Instantiate using the provided directory name (String).</summary>
		/// <param name="lockDirName">where lock files should be created.
		/// </param>
		public SimpleFSLockFactory(System.String lockDirName)
		{
			lockDir = new System.IO.FileInfo(lockDirName);
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
		}
		
		public override Lock MakeLock(System.String lockName)
		{
			if (lockPrefix != null)
			{
				lockName = lockPrefix + "-" + lockName;
			}
			return new SimpleFSLock(lockDir, lockName);
		}
		
		public override void  ClearLock(System.String lockName)
		{
			bool tmpBool;
			if (System.IO.File.Exists(lockDir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(lockDir.FullName);
			if (tmpBool)
			{
				if (lockPrefix != null)
				{
					lockName = lockPrefix + "-" + lockName;
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
	
	
	class SimpleFSLock : Lock
	{
		
		internal System.IO.FileInfo lockFile;
		internal System.IO.FileInfo lockDir;
		
		public SimpleFSLock(System.IO.FileInfo lockDir, System.String lockFileName)
		{
			this.lockDir = lockDir;
			lockFile = new System.IO.FileInfo(System.IO.Path.Combine(lockDir.FullName, lockFileName));
		}
		
		public override bool Obtain()
		{
			
			// Ensure that lockDir exists and is a directory:
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
            try
            {
                System.IO.FileStream createdFile = new System.IO.FileStream(lockFile.FullName, System.IO.FileMode.CreateNew);
                createdFile.Close();
                return true;
            }
            catch
            {
                return false;
            }
		}
		
		public override void  Release()
		{
			bool tmpBool;
			if (System.IO.File.Exists(lockFile.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(lockFile.FullName);
			bool tmpBool2;
			if (System.IO.File.Exists(lockFile.FullName))
			{
				System.IO.File.Delete(lockFile.FullName);
				tmpBool2 = true;
			}
			else if (System.IO.Directory.Exists(lockFile.FullName))
			{
				System.IO.Directory.Delete(lockFile.FullName);
				tmpBool2 = true;
			}
			else
				tmpBool2 = false;
			if (tmpBool && !tmpBool2)
				throw new LockReleaseFailedException("failed to delete " + lockFile);
		}
		
		public override bool IsLocked()
		{
			bool tmpBool;
			if (System.IO.File.Exists(lockFile.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(lockFile.FullName);
			return tmpBool;
		}
		
		public override System.String ToString()
		{
			return "SimpleFSLock@" + lockFile;
		}
	}
}