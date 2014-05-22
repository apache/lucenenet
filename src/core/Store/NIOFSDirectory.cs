using System;
using System.Diagnostics;

namespace Lucene.Net.Store
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
	/// An <seealso cref="FSDirectory"/> implementation that uses java.nio's FileChannel's
	/// positional read, which allows multiple threads to read from the same file
	/// without synchronizing.
	/// <p>
	/// this class only uses FileChannel when reading; writing is achieved with
	/// <seealso cref="FSDirectory.FSIndexOutput"/>.
	/// <p>
	/// <b>NOTE</b>: NIOFSDirectory is not recommended on Windows because of a bug in
	/// how FileChannel.read is implemented in Sun's JRE. Inside of the
	/// implementation the position is apparently synchronized. See <a
	/// href="http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6265734">here</a>
	/// for details.
	/// </p>
	/// <p>
	/// <font color="red"><b>NOTE:</b> Accessing this class either directly or
	/// indirectly from a thread while it's interrupted can close the
	/// underlying file descriptor immediately if at the same time the thread is
	/// blocked on IO. The file descriptor will remain closed and subsequent access
	/// to <seealso cref="NIOFSDirectory"/> will throw a <seealso cref="ClosedChannelException"/>. If
	/// your application uses either <seealso cref="Thread#interrupt()"/> or
	/// <seealso cref="Future#cancel(boolean)"/> you should use <seealso cref="SimpleFSDirectory"/> in
	/// favor of <seealso cref="NIOFSDirectory"/>.</font>
	/// </p>
	/// </summary>
	public class NIOFSDirectory : FSDirectory
	{

	  /// <summary>
	  /// Create a new NIOFSDirectory for the named location.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <param name="lockFactory"> the lock factory to use, or null for the default
	  /// (<seealso cref="NativeFSLockFactory"/>); </param>
	  /// <exception cref="IOException"> if there is a low-level I/O error </exception>
	  public NIOFSDirectory(File path, LockFactory lockFactory) : base(path, lockFactory)
	  {
	  }

	  /// <summary>
	  /// Create a new NIOFSDirectory for the named location and <seealso cref="NativeFSLockFactory"/>.
	  /// </summary>
	  /// <param name="path"> the path of the directory </param>
	  /// <exception cref="IOException"> if there is a low-level I/O error </exception>
	  public NIOFSDirectory(File path) : base(path, null)
	  {
	  }

	  /// <summary>
	  /// Creates an IndexInput for the file with the given name. </summary>
	  public override IndexInput OpenInput(string name, IOContext context)
	  {
		EnsureOpen();
		File path = new File(Directory, name);
		FileChannel fc = FileChannel.open(path.toPath(), StandardOpenOption.READ);
		return new NIOFSIndexInput("NIOFSIndexInput(path=\"" + path + "\")", fc, context);
	  }

	  public override IndexInputSlicer CreateSlicer(string name, IOContext context)
	  {
		EnsureOpen();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.io.File path = new java.io.File(getDirectory(), name);
		File path = new File(Directory, name);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.channels.FileChannel descriptor = java.nio.channels.FileChannel.open(path.toPath(), java.nio.file.StandardOpenOption.READ);
		FileChannel descriptor = FileChannel.open(path.toPath(), StandardOpenOption.READ);
		return new IndexInputSlicerAnonymousInnerClassHelper(this, context, path, descriptor);
	  }

	  private class IndexInputSlicerAnonymousInnerClassHelper : Directory.IndexInputSlicer
	  {
		  private readonly NIOFSDirectory OuterInstance;

		  private Lucene.Net.Store.IOContext Context;
		  private File Path;
		  private FileChannel Descriptor;

		  public IndexInputSlicerAnonymousInnerClassHelper(NIOFSDirectory outerInstance, Lucene.Net.Store.IOContext context, File path, FileChannel descriptor) : base(outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  this.Context = context;
			  this.Path = path;
			  this.Descriptor = descriptor;
		  }


		  public override void Close()
		  {
			Descriptor.close();
		  }

		  public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
		  {
			return new NIOFSIndexInput("NIOFSIndexInput(" + sliceDescription + " in path=\"" + Path + "\" slice=" + offset + ":" + (offset + length) + ")", Descriptor, offset, length, BufferedIndexInput.BufferSize(Context));
		  }

		  public override IndexInput OpenFullSlice()
		  {
			try
			{
			  return openSlice("full-slice", 0, Descriptor.size());
			}
			catch (IOException ex)
			{
			  throw new Exception(ex);
			}
		  }
	  }

	  /// <summary>
	  /// Reads bytes with <seealso cref="FileChannel#read(ByteBuffer, long)"/>
	  /// </summary>
	  protected internal class NIOFSIndexInput : BufferedIndexInput
	  {
		/// <summary>
		/// The maximum chunk size for reads of 16384 bytes.
		/// </summary>
		internal const int CHUNK_SIZE = 16384;

		/// <summary>
		/// the file channel we will read from </summary>
		protected internal readonly FileChannel Channel;
		/// <summary>
		/// is this instance a clone and hence does not own the file to close it </summary>
		internal bool IsClone = false;
		/// <summary>
		/// start offset: non-zero in the slice case </summary>
		protected internal readonly long Off;
		/// <summary>
		/// end offset (start+length) </summary>
		protected internal readonly long End;

		internal ByteBuffer ByteBuf; // wraps the buffer for NIO

		public NIOFSIndexInput(string resourceDesc, FileChannel fc, IOContext context) : base(resourceDesc, context)
		{
		  this.Channel = fc;
		  this.Off = 0L;
		  this.End = fc.size();
		}

		public NIOFSIndexInput(string resourceDesc, FileChannel fc, long off, long length, int bufferSize) : base(resourceDesc, bufferSize)
		{
		  this.Channel = fc;
		  this.Off = off;
		  this.End = off + length;
		  this.IsClone = true;
		}

		public override void Close()
		{
		  if (!IsClone)
		  {
			Channel.close();
		  }
		}

		public override NIOFSIndexInput Clone()
		{
		  NIOFSIndexInput clone = (NIOFSIndexInput)base.Clone();
		  clone.IsClone = true;
		  return clone;
		}

		public override sealed long Length()
		{
		  return End - Off;
		}

		protected internal override void NewBuffer(sbyte[] newBuffer)
		{
		  base.NewBuffer(newBuffer);
		  ByteBuf = ByteBuffer.wrap(newBuffer);
		}

		protected internal override void ReadInternal(sbyte[] b, int offset, int len)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.nio.ByteBuffer bb;
		  ByteBuffer bb;

		  // Determine the ByteBuffer we should use
		  if (b == Buffer)
		  {
			// Use our own pre-wrapped byteBuf:
			Debug.Assert(ByteBuf != null);
			bb = ByteBuf;
			ByteBuf.clear().position(offset);
		  }
		  else
		  {
			bb = ByteBuffer.wrap(b, offset, len);
		  }

		  long pos = FilePointer + Off;

		  if (pos + len > End)
		  {
			throw new EOFException("read past EOF: " + this);
		  }

		  try
		  {
			int readLength = len;
			while (readLength > 0)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int toRead = Math.min(CHUNK_SIZE, readLength);
			  int toRead = Math.Min(CHUNK_SIZE, readLength);
			  bb.limit(bb.position() + toRead);
			  Debug.Assert(bb.remaining() == toRead);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int i = channel.read(bb, pos);
			  int i = Channel.read(bb, pos);
			  if (i < 0) // be defensive here, even though we checked before hand, something could have changed
			  {
				throw new EOFException("read past EOF: " + this + " off: " + offset + " len: " + len + " pos: " + pos + " chunkLen: " + toRead + " end: " + End);
			  }
			  Debug.Assert(i > 0, "FileChannel.read with non zero-length bb.remaining() must always read at least one byte (FileChannel is in blocking mode, see spec of ReadableByteChannel)");
			  pos += i;
			  readLength -= i;
			}
			Debug.Assert(readLength == 0);
		  }
		  catch (IOException ioe)
		  {
			throw new IOException(ioe.Message + ": " + this, ioe);
		  }
		}

		protected internal override void SeekInternal(long pos)
		{
		}
	  }
	}

}