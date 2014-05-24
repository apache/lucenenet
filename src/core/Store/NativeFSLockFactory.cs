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


	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// <p>Implements <seealso cref="LockFactory"/> using native OS file
	/// locks.  Note that because this LockFactory relies on
	/// java.nio.* APIs for locking, any problems with those APIs
	/// will cause locking to fail.  Specifically, on certain NFS
	/// environments the java.nio.* locks will fail (the lock can
	/// incorrectly be double acquired) whereas {@link
	/// SimpleFSLockFactory} worked perfectly in those same
	/// environments.  For NFS based access to an index, it's
	/// recommended that you try <seealso cref="SimpleFSLockFactory"/>
	/// first and work around the one limitation that a lock file
	/// could be left when the JVM exits abnormally.</p>
	/// 
	/// <p>The primary benefit of <seealso cref="NativeFSLockFactory"/> is
	/// that locks (not the lock file itsself) will be properly
	/// removed (by the OS) if the JVM has an abnormal exit.</p>
	/// 
	/// <p>Note that, unlike <seealso cref="SimpleFSLockFactory"/>, the existence of
	/// leftover lock files in the filesystem is fine because the OS
	/// will free the locks held against these files even though the
	/// files still remain. Lucene will never actively remove the lock
	/// files, so although you see them, the index may not be locked.</p>
	/// 
	/// <p>Special care needs to be taken if you change the locking
	/// implementation: First be certain that no writer is in fact
	/// writing to the index otherwise you can easily corrupt
	/// your index. Be sure to do the LockFactory change on all Lucene
	/// instances and clean up all leftover lock files before starting
	/// the new configuration for the first time. Different implementations
	/// can not work together!</p>
	/// 
	/// <p>If you suspect that this or any other LockFactory is
	/// not working properly in your environment, you can easily
	/// test it by using <seealso cref="VerifyingLockFactory"/>, {@link
	/// LockVerifyServer} and <seealso cref="LockStressTest"/>.</p>
	/// </summary>
	/// <seealso cref= LockFactory </seealso>

	public class NativeFSLockFactory : FSLockFactory
	{

	  /// <summary>
	  /// Create a NativeFSLockFactory instance, with null (unset)
	  /// lock directory. When you pass this factory to a <seealso cref="FSDirectory"/>
	  /// subclass, the lock directory is automatically set to the
	  /// directory itself. Be sure to create one instance for each directory
	  /// your create!
	  /// </summary>
	  public NativeFSLockFactory() : this((File) null)
	  {
	  }

	  /// <summary>
	  /// Create a NativeFSLockFactory instance, storing lock
	  /// files into the specified lockDirName:
	  /// </summary>
	  /// <param name="lockDirName"> where lock files are created. </param>
	  public NativeFSLockFactory(string lockDirName) : this(new File(lockDirName))
	  {
	  }

	  /// <summary>
	  /// Create a NativeFSLockFactory instance, storing lock
	  /// files into the specified lockDir:
	  /// </summary>
	  /// <param name="lockDir"> where lock files are created. </param>
	  public NativeFSLockFactory(File lockDir)
	  {
		LockDir = lockDir;
	  }

	  public override Lock MakeLock(string lockName)
	  {
		  lock (this)
		  {
			if (LockPrefix_Renamed != null)
			{
			  lockName = LockPrefix_Renamed + "-" + lockName;
			}
			return new NativeFSLock(LockDir_Renamed, lockName);
		  }
	  }

	  public override void ClearLock(string lockName)
	  {
		MakeLock(lockName).close();
	  }
	}

	internal class NativeFSLock : Lock
	{

	  private FileChannel Channel;
	  private FileLock @lock;
	  private File Path;
	  private File LockDir;

	  public NativeFSLock(File lockDir, string lockFileName)
	  {
		this.LockDir = lockDir;
		Path = new File(lockDir, lockFileName);
	  }

	  public override bool Obtain()
	  {
		  lock (this)
		  {
        
			if (@lock != null)
			{
			  // Our instance is already locked:
			  return false;
			}
        
			// Ensure that lockDir exists and is a directory.
			if (!LockDir.exists())
			{
			  if (!LockDir.mkdirs())
			  {
				throw new System.IO.IOException("Cannot create directory: " + LockDir.AbsolutePath);
			  }
			}
			else if (!LockDir.Directory)
			{
			  // TODO: NoSuchDirectoryException instead?
			  throw new System.IO.IOException("Found regular file where directory expected: " + LockDir.AbsolutePath);
			}
        
			Channel = FileChannel.open(Path.toPath(), StandardOpenOption.CREATE, StandardOpenOption.WRITE);
			bool success = false;
			try
			{
			  @lock = Channel.tryLock();
			  success = @lock != null;
			}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
			catch (System.IO.IOException | OverlappingFileLockException e)
			{
			  // At least on OS X, we will sometimes get an
			  // intermittent "Permission Denied" System.IO.IOException,
			  // which seems to simply mean "you failed to get
			  // the lock".  But other System.IO.IOExceptions could be
			  // "permanent" (eg, locking is not supported via
			  // the filesystem).  So, we record the failure
			  // reason here; the timeout obtain (usually the
			  // one calling us) will use this as "root cause"
			  // if it fails to get the lock.
			  FailureReason = e;
			}
			finally
			{
			  if (!success)
			  {
				try
				{
				  IOUtils.CloseWhileHandlingException(Channel);
				}
				finally
				{
				  Channel = null;
				}
			  }
			}
			return @lock != null;
		  }
	  }

	  public override void Close()
	  {
		  lock (this)
		  {
			try
			{
			  if (@lock != null)
			  {
				@lock.release();
				@lock = null;
			  }
			}
			finally
			{
			  if (Channel != null)
			  {
				Channel.close();
				Channel = null;
			  }
			}
		  }
	  }

	  public override bool Locked
	  {
		  get
		  {
			  lock (this)
			  {
				// The test for is isLocked is not directly possible with native file locks:
            
				// First a shortcut, if a lock reference in this instance is available
				if (@lock != null)
				{
					return true;
				}
            
				// Look if lock file is present; if not, there can definitely be no lock!
				if (!Path.exists())
				{
					return false;
				}
            
				// Try to obtain and release (if was locked) the lock
				try
				{
				  bool obtained = Obtain();
				  if (obtained)
				  {
					  Close();
				  }
				  return !obtained;
				}
				catch (System.IO.IOException ioe)
				{
				  return false;
				}
			  }
		  }
	  }

	  public override string ToString()
	  {
		return "NativeFSLock@" + Path;
	  }
	}

}