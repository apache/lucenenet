using System;
using System.Diagnostics;
using System.Collections.Generic;

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

	using Constants = Lucene.Net.Util.Constants;
	using IOUtils = Lucene.Net.Util.IOUtils;


	/// <summary>
	/// Base class for Directory implementations that store index
	/// files in the file system.  
	/// <a name="subclasses"/>
	/// There are currently three core
	/// subclasses:
	/// 
	/// <ul>
	/// 
	///  <li> <seealso cref="SimpleFSDirectory"/> is a straightforward
	///       implementation using java.io.RandomAccessFile.
	///       However, it has poor concurrent performance
	///       (multiple threads will bottleneck) as it
	///       synchronizes when multiple threads read from the
	///       same file.
	/// 
	///  <li> <seealso cref="NIOFSDirectory"/> uses java.nio's
	///       FileChannel's positional io when reading to avoid
	///       synchronization when reading from the same file.
	///       Unfortunately, due to a Windows-only <a
	///       href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734">Sun
	///       JRE bug</a> this is a poor choice for Windows, but
	///       on all other platforms this is the preferred
	///       choice. Applications using <seealso cref="Thread#interrupt()"/> or
	///       <seealso cref="Future#cancel(boolean)"/> should use
	///       <seealso cref="SimpleFSDirectory"/> instead. See <seealso cref="NIOFSDirectory"/> java doc
	///       for details.
	///        
	///        
	/// 
	///  <li> <seealso cref="MMapDirectory"/> uses memory-mapped IO when
	///       reading. this is a good choice if you have plenty
	///       of virtual memory relative to your index size, eg
	///       if you are running on a 64 bit JRE, or you are
	///       running on a 32 bit JRE but your index sizes are
	///       small enough to fit into the virtual memory space.
	///       Java has currently the limitation of not being able to
	///       unmap files from user code. The files are unmapped, when GC
	///       releases the byte buffers. Due to
	///       <a href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4724038">
	///       this bug</a> in Sun's JRE, MMapDirectory's <seealso cref="IndexInput#close"/>
	///       is unable to close the underlying OS file handle. Only when
	///       GC finally collects the underlying objects, which could be
	///       quite some time later, will the file handle be closed.
	///       this will consume additional transient disk usage: on Windows,
	///       attempts to delete or overwrite the files will result in an
	///       exception; on other platforms, which typically have a &quot;delete on
	///       last close&quot; semantics, while such operations will succeed, the bytes
	///       are still consuming space on disk.  For many applications this
	///       limitation is not a problem (e.g. if you have plenty of disk space,
	///       and you don't rely on overwriting files on Windows) but it's still
	///       an important limitation to be aware of. this class supplies a
	///       (possibly dangerous) workaround mentioned in the bug report,
	///       which may fail on non-Sun JVMs.
	///       
	///       Applications using <seealso cref="Thread#interrupt()"/> or
	///       <seealso cref="Future#cancel(boolean)"/> should use
	///       <seealso cref="SimpleFSDirectory"/> instead. See <seealso cref="MMapDirectory"/>
	///       java doc for details.
	/// </ul>
	/// 
	/// Unfortunately, because of system peculiarities, there is
	/// no single overall best implementation.  Therefore, we've
	/// added the <seealso cref="#open"/> method, to allow Lucene to choose
	/// the best FSDirectory implementation given your
	/// environment, and the known limitations of each
	/// implementation.  For users who have no reason to prefer a
	/// specific implementation, it's best to simply use {@link
	/// #open}.  For all others, you should instantiate the
	/// desired implementation directly.
	/// 
	/// <p>The locking implementation is by default {@link
	/// NativeFSLockFactory}, but can be changed by
	/// passing in a custom <seealso cref="LockFactory"/> instance.
	/// </summary>
	/// <seealso cref= Directory </seealso>
	public abstract class FSDirectory : BaseDirectory
	{

	  /// <summary>
	  /// Default read chunk size: 8192 bytes (this is the size up to which the JDK
	  ///   does not allocate additional arrays while reading/writing) </summary>
	  ///   @deprecated this constant is no longer used since Lucene 4.5. 
	  [Obsolete("this constant is no longer used since Lucene 4.5.")]
	  public const int DEFAULT_READ_CHUNK_SIZE = 8192;

	  protected internal readonly File Directory_Renamed; // The underlying filesystem directory
	  protected internal readonly Set<string> StaleFiles = synchronizedSet(new HashSet<string>()); // Files written, but not yet sync'ed
	  private int ChunkSize = DEFAULT_READ_CHUNK_SIZE;

	  // returns the canonical version of the directory, creating it if it doesn't exist.
	  private static File GetCanonicalPath(File file)
	  {
		return new File(file.CanonicalPath);
	  }

	  /// <summary>
	  /// Create a new FSDirectory for the named location (ctor for subclasses). </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <param name="lockFactory"> the lock factory to use, or null for the default
	  /// (<seealso cref="NativeFSLockFactory"/>); </param>
	  /// <exception cref="IOException"> if there is a low-level I/O error </exception>
	  protected internal FSDirectory(File path, LockFactory lockFactory)
	  {
		// new ctors use always NativeFSLockFactory as default:
		if (lockFactory == null)
		{
		  lockFactory = new NativeFSLockFactory();
		}
		Directory_Renamed = GetCanonicalPath(path);

		if (Directory_Renamed.exists() && !Directory_Renamed.Directory)
		{
		  throw new NoSuchDirectoryException("file '" + Directory_Renamed + "' exists but is not a directory");
		}

		LockFactory = lockFactory;

	  }

	  /// <summary>
	  /// Creates an FSDirectory instance, trying to pick the
	  ///  best implementation given the current environment.
	  ///  The directory returned uses the <seealso cref="NativeFSLockFactory"/>.
	  /// 
	  ///  <p>Currently this returns <seealso cref="MMapDirectory"/> for most Solaris
	  ///  and Windows 64-bit JREs, <seealso cref="NIOFSDirectory"/> for other
	  ///  non-Windows JREs, and <seealso cref="SimpleFSDirectory"/> for other
	  ///  JREs on Windows. It is highly recommended that you consult the
	  ///  implementation's documentation for your platform before
	  ///  using this method.
	  /// 
	  /// <p><b>NOTE</b>: this method may suddenly change which
	  /// implementation is returned from release to release, in
	  /// the event that higher performance defaults become
	  /// possible; if the precise implementation is important to
	  /// your application, please instantiate it directly,
	  /// instead. For optimal performance you should consider using
	  /// <seealso cref="MMapDirectory"/> on 64 bit JVMs.
	  /// 
	  /// <p>See <a href="#subclasses">above</a> 
	  /// </summary>
	  public static FSDirectory Open(File path)
	  {
		return Open(path, null);
	  }

	  /// <summary>
	  /// Just like <seealso cref="#open(File)"/>, but allows you to
	  ///  also specify a custom <seealso cref="LockFactory"/>. 
	  /// </summary>
	  public static FSDirectory Open(File path, LockFactory lockFactory)
	  {
		if ((Constants.WINDOWS || Constants.SUN_OS || Constants.LINUX) && Constants.JRE_IS_64BIT && MMapDirectory.UNMAP_SUPPORTED)
		{
		  return new MMapDirectory(path, lockFactory);
		}
		else if (Constants.WINDOWS)
		{
		  return new SimpleFSDirectory(path, lockFactory);
		}
		else
		{
		  return new NIOFSDirectory(path, lockFactory);
		}
	  }

	  public override LockFactory LockFactory
	  {
		  set
		  {
			base.LockFactory = value;
    
			// for filesystem based LockFactory, delete the lockPrefix, if the locks are placed
			// in index dir. If no index dir is given, set ourselves
			if (value is FSLockFactory)
			{
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final FSLockFactory lf = (FSLockFactory) value;
			  FSLockFactory lf = (FSLockFactory) value;
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final java.io.File dir = lf.getLockDir();
			  File dir = lf.LockDir;
			  // if the lock factory has no lockDir set, use the this directory as lockDir
			  if (dir == null)
			  {
				lf.LockDir = Directory_Renamed;
				lf.LockPrefix = null;
			  }
			  else if (dir.CanonicalPath.Equals(Directory_Renamed.CanonicalPath))
			  {
				lf.LockPrefix = null;
			  }
			}
    
		  }
	  }

	  /// <summary>
	  /// Lists all files (not subdirectories) in the
	  ///  directory.  this method never returns null (throws
	  ///  <seealso cref="IOException"/> instead).
	  /// </summary>
	  ///  <exception cref="NoSuchDirectoryException"> if the directory
	  ///   does not exist, or does exist but is not a
	  ///   directory. </exception>
	  ///  <exception cref="IOException"> if list() returns null  </exception>
	  public static string[] ListAll(File dir)
	  {
		if (!dir.exists())
		{
		  throw new NoSuchDirectoryException("directory '" + dir + "' does not exist");
		}
		else if (!dir.Directory)
		{
		  throw new NoSuchDirectoryException("file '" + dir + "' exists but is not a directory");
		}

		// Exclude subdirs
		string[] result = dir.list(new FilenameFilterAnonymousInnerClassHelper(dir));

		if (result == null)
		{
		  throw new IOException("directory '" + dir + "' exists and is a directory, but cannot be listed: list() returned null");
		}

		return result;
	  }

	  private class FilenameFilterAnonymousInnerClassHelper : FilenameFilter
	  {
		  private File Dir;

		  public FilenameFilterAnonymousInnerClassHelper(File dir)
		  {
			  this.Dir = dir;
		  }

		  public override bool Accept(File dir, string file)
		  {
			return !(new File(dir, file)).Directory;
		  }
	  }

	  /// <summary>
	  /// Lists all files (not subdirectories) in the
	  /// directory. </summary>
	  /// <seealso cref= #listAll(File)  </seealso>
	  public override string[] ListAll()
	  {
		EnsureOpen();
		return ListAll(Directory_Renamed);
	  }

	  /// <summary>
	  /// Returns true iff a file with the given name exists. </summary>
	  public override bool FileExists(string name)
	  {
		EnsureOpen();
		File file = new File(Directory_Renamed, name);
		return file.exists();
	  }

	  /// <summary>
	  /// Returns the length in bytes of a file in the directory. </summary>
	  public override long FileLength(string name)
	  {
		EnsureOpen();
		File file = new File(Directory_Renamed, name);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long len = file.length();
		long len = file.length();
		if (len == 0 && !file.exists())
		{
		  throw new FileNotFoundException(name);
		}
		else
		{
		  return len;
		}
	  }

	  /// <summary>
	  /// Removes an existing file in the directory. </summary>
	  public override void DeleteFile(string name)
	  {
		EnsureOpen();
		File file = new File(Directory_Renamed, name);
		if (!file.delete())
		{
		  throw new IOException("Cannot delete " + file);
		}
		StaleFiles.remove(name);
	  }

	  /// <summary>
	  /// Creates an IndexOutput for the file with the given name. </summary>
	  public override IndexOutput CreateOutput(string name, IOContext context)
	  {
		EnsureOpen();

		EnsureCanWrite(name);
		return new FSIndexOutput(this, name);
	  }

	  protected internal virtual void EnsureCanWrite(string name)
	  {
		if (!Directory_Renamed.exists())
		{
		  if (!Directory_Renamed.mkdirs())
		  {
			throw new IOException("Cannot create directory: " + Directory_Renamed);
		  }
		}

		File file = new File(Directory_Renamed, name);
		if (file.exists() && !file.delete()) // delete existing, if any
		{
		  throw new IOException("Cannot overwrite: " + file);
		}
	  }

	  protected internal virtual void OnIndexOutputClosed(FSIndexOutput io)
	  {
		StaleFiles.add(io.Name);
	  }

	  public override void Sync(ICollection<string> names)
	  {
		EnsureOpen();
		Set<string> toSync = new HashSet<string>(names);
		toSync.retainAll(StaleFiles);

		foreach (string name in toSync)
		{
		  Fsync(name);
		}

		// fsync the directory itsself, but only if there was any file fsynced before
		// (otherwise it can happen that the directory does not yet exist)!
		if (!toSync.Empty)
		{
		  IOUtils.Fsync(Directory_Renamed, true);
		}

		StaleFiles.removeAll(toSync);
	  }

	  public override string LockID
	  {
		  get
		  {
			EnsureOpen();
			string dirName; // name to be hashed
			try
			{
			  dirName = Directory_Renamed.CanonicalPath;
			}
			catch (IOException e)
			{
			  throw new Exception(e.ToString(), e);
			}
    
			int digest = 0;
			for (int charIDX = 0;charIDX < dirName.Length;charIDX++)
			{
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final char ch = dirName.charAt(charIDX);
			  char ch = dirName[charIDX];
			  digest = 31 * digest + ch;
			}
			return "lucene-" + digest.ToString("x");
		  }
	  }

	  /// <summary>
	  /// Closes the store to future operations. </summary>
	  public override void Close()
	  {
		  lock (this)
		  {
			IsOpen = false;
		  }
	  }

	  /// <returns> the underlying filesystem directory </returns>
	  public virtual File Directory
	  {
		  get
		  {
			EnsureOpen();
			return Directory_Renamed;
		  }
	  }

	  /// <summary>
	  /// For debug output. </summary>
	  public override string ToString()
	  {
		return this.GetType().Name + "@" + Directory_Renamed + " lockFactory=" + LockFactory;
	  }

	  /// <summary>
	  /// this setting has no effect anymore. </summary>
	  /// @deprecated this is no longer used since Lucene 4.5. 
	  [Obsolete("this is no longer used since Lucene 4.5.")]
	  public int ReadChunkSize
	  {
		  set
		  {
			if (value <= 0)
			{
			  throw new System.ArgumentException("chunkSize must be positive");
			}
			this.ChunkSize = value;
		  }
		  get
		  {
			return ChunkSize;
		  }
	  }


	  /// <summary>
	  /// Writes output with <seealso cref="RandomAccessFile#write(byte[], int, int)"/>
	  /// </summary>
	  protected internal class FSIndexOutput : BufferedIndexOutput
	  {
		/// <summary>
		/// The maximum chunk size is 8192 bytes, because <seealso cref="RandomAccessFile"/> mallocs
		/// a native buffer outside of stack if the write buffer size is larger.
		/// </summary>
		internal const int CHUNK_SIZE = 8192;

		internal readonly FSDirectory Parent;
		internal readonly string Name;
		internal readonly RandomAccessFile File;
		internal volatile bool IsOpen; // remember if the file is open, so that we don't try to close it more than once

		public FSIndexOutput(FSDirectory parent, string name) : base(CHUNK_SIZE)
		{
		  this.Parent = parent;
		  this.Name = name;
		  File = new RandomAccessFile(new File(parent.Directory_Renamed, name), "rw");
		  IsOpen = true;
		}

		protected internal override void FlushBuffer(sbyte[] b, int offset, int size)
		{
		  Debug.Assert(IsOpen);
		  while (size > 0)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int toWrite = Math.min(CHUNK_SIZE, size);
			int toWrite = Math.Min(CHUNK_SIZE, size);
			File.write(b, offset, toWrite);
			offset += toWrite;
			size -= toWrite;
		  }
		  Debug.Assert(size == 0);
		}

		public override void Close()
		{
		  Parent.OnIndexOutputClosed(this);
		  // only close the file if it has not been closed yet
		  if (IsOpen)
		  {
			IOException priorE = null;
			try
			{
			  base.Close();
			}
			catch (IOException ioe)
			{
			  priorE = ioe;
			}
			finally
			{
			  IsOpen = false;
			  IOUtils.CloseWhileHandlingException(priorE, File);
			}
		  }
		}

		/// <summary>
		/// Random-access methods </summary>
		public override void Seek(long pos)
		{
		  base.Seek(pos);
		  File.seek(pos);
		}

		public override long Length()
		{
		  return File.length();
		}

		public override long Length
		{
			set
			{
			  File.Length = value;
			}
		}
	  }

	  protected internal virtual void Fsync(string name)
	  {
		IOUtils.Fsync(new File(Directory_Renamed, name), false);
	  }
	}

}