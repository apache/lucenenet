using System;
using System.Runtime.InteropServices;

namespace org.apache.lucene.store
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements. See the NOTICE file distributed with this
	 * work for additional information regarding copyright ownership. The ASF
	 * licenses this file to You under the Apache License, Version 2.0 (the
	 * "License"); you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 * 
	 * http://www.apache.org/licenses/LICENSE-2.0
	 * 
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
	 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
	 * License for the specific language governing permissions and limitations under
	 * the License.
	 */



	/// <summary>
	/// Native <seealso cref="Directory"/> implementation for Microsoft Windows.
	/// <para>
	/// Steps:
	/// <ol> 
	///   <li>Compile the source code to create WindowsDirectory.dll:
	///       <blockquote>
	/// c:\mingw\bin\g++ -Wall -D_JNI_IMPLEMENTATION_ -Wl,--kill-at 
	/// -I"%JAVA_HOME%\include" -I"%JAVA_HOME%\include\win32" -static-libgcc 
	/// -static-libstdc++ -shared WindowsDirectory.cpp -o WindowsDirectory.dll
	///       </blockquote> 
	///       For 64-bit JREs, use mingw64, with the -m64 option. 
	///   <li>Put WindowsDirectory.dll into some directory in your windows PATH
	///   <li>Open indexes with WindowsDirectory and use it.
	/// </ol>
	/// </para>
	/// @lucene.experimental
	/// </summary>
	public class WindowsDirectory : FSDirectory
	{
	  private const int DEFAULT_BUFFERSIZE = 4096; // default pgsize on ia32/amd64

	  static WindowsDirectory()
	  {
//JAVA TO C# CONVERTER TODO TASK: The library is specified in the 'DllImport' attribute for .NET:
//		System.loadLibrary("WindowsDirectory");
	  }

	  /// <summary>
	  /// Create a new WindowsDirectory for the named location.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <param name="lockFactory"> the lock factory to use, or null for the default
	  /// (<seealso cref="NativeFSLockFactory"/>); </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public WindowsDirectory(java.io.File path, LockFactory lockFactory) throws java.io.IOException
	  public WindowsDirectory(File path, LockFactory lockFactory) : base(path, lockFactory)
	  {
	  }

	  /// <summary>
	  /// Create a new WindowsDirectory for the named location and <seealso cref="NativeFSLockFactory"/>.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <exception cref="IOException"> If there is a low-level I/O error </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public WindowsDirectory(java.io.File path) throws java.io.IOException
	  public WindowsDirectory(File path) : base(path, null)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public IndexInput openInput(String name, IOContext context) throws java.io.IOException
	  public override IndexInput openInput(string name, IOContext context)
	  {
		ensureOpen();
		return new WindowsIndexInput(new File(Directory, name), Math.Max(BufferedIndexInput.bufferSize(context), DEFAULT_BUFFERSIZE));
	  }

	  internal class WindowsIndexInput : BufferedIndexInput
	  {
		internal readonly long fd;
		internal readonly long length_Renamed;
		internal bool isClone;
		internal bool isOpen;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public WindowsIndexInput(java.io.File file, int bufferSize) throws java.io.IOException
		public WindowsIndexInput(File file, int bufferSize) : base("WindowsIndexInput(path=\"" + file.Path + "\")", bufferSize)
		{
		  fd = WindowsDirectory.open(file.Path);
		  length_Renamed = WindowsDirectory.length(fd);
		  isOpen = true;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected void readInternal(byte[] b, int offset, int length) throws java.io.IOException
		protected internal override void readInternal(sbyte[] b, int offset, int length)
		{
		  int bytesRead;
		  try
		  {
			bytesRead = WindowsDirectory.read(fd, b, offset, length, FilePointer);
		  }
		  catch (IOException ioe)
		  {
			throw new IOException(ioe.Message + ": " + this, ioe);
		  }

		  if (bytesRead != length)
		  {
			throw new EOFException("read past EOF: " + this);
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected void seekInternal(long pos) throws java.io.IOException
		protected internal override void seekInternal(long pos)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public synchronized void close() throws java.io.IOException
		public override void close()
		{
			lock (this)
			{
			  // NOTE: we synchronize and track "isOpen" because Lucene sometimes closes IIs twice!
			  if (!isClone && isOpen)
			  {
				WindowsDirectory.close(fd);
				isOpen = false;
			  }
			}
		}

		public override long length()
		{
		  return length_Renamed;
		}

		public override WindowsIndexInput clone()
		{
		  WindowsIndexInput clone = (WindowsIndexInput)base.clone();
		  clone.isClone = true;
		  return clone;
		}
	  }

	  /// <summary>
	  /// Opens a handle to a file. </summary>
//JAVA TO C# CONVERTER TODO TASK: Replace 'unknown' with the appropriate dll name:
	  [DllImport("unknown")]
	  private static extern long open(string filename);

	  /// <summary>
	  /// Reads data from a file at pos into bytes </summary>
//JAVA TO C# CONVERTER TODO TASK: Replace 'unknown' with the appropriate dll name:
	  [DllImport("unknown")]
	  private static extern int read(long fd, sbyte[] bytes, int offset, int length, long pos);

	  /// <summary>
	  /// Closes a handle to a file </summary>
//JAVA TO C# CONVERTER TODO TASK: Replace 'unknown' with the appropriate dll name:
	  [DllImport("unknown")]
	  private static extern void close(long fd);

	  /// <summary>
	  /// Returns the length of a file </summary>
//JAVA TO C# CONVERTER TODO TASK: Replace 'unknown' with the appropriate dll name:
	  [DllImport("unknown")]
	  private static extern long length(long fd);
	}

}