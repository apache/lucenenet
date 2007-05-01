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
	
	/// <summary> Implements {@link LockFactory} using {@link File#createNewFile()}.  This is
	/// currently the default LockFactory used for {@link FSDirectory} if no
	/// LockFactory instance is otherwise provided.
	/// 
	/// Note that there are known problems with this locking implementation on NFS.
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
		
		/// <summary> Instantiate using the provided directory (as a File instance).</summary>
		/// <param name="lockDir">where lock files should be created.
		/// </param>
		public SimpleFSLockFactory(System.IO.FileInfo lockDir)
		{
			Init(lockDir);
		}
		
		/// <summary> Instantiate using the provided directory name (String).</summary>
		/// <param name="lockDirName">where lock files should be created.
		/// </param>
		public SimpleFSLockFactory(System.String lockDirName)
		{
			lockDir = new System.IO.FileInfo(lockDirName);
			Init(lockDir);
		}
		
		protected internal virtual void  Init(System.IO.FileInfo lockDir)
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
                System.IO.FileStream createdFile = lockFile.Create();
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
			{
				System.IO.File.Delete(lockFile.FullName);
				tmpBool = true;
			}
			else if (System.IO.Directory.Exists(lockFile.FullName))
			{
				System.IO.Directory.Delete(lockFile.FullName);
				tmpBool = true;
			}
			else
				tmpBool = false;
			bool generatedAux = tmpBool;
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