/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Store
{
	/// <summary>
	/// A
	/// <see cref="Directory">Directory</see>
	/// implementation for all Unixes that uses
	/// DIRECT I/O to bypass OS level IO caching during
	/// merging.  For all other cases (searching, writing) we delegate
	/// to the provided Directory instance.
	/// <p>See &lt;a
	/// href="
	/// <docRoot></docRoot>
	/// /overview-summary.html#NativeUnixDirectory"&gt;Overview</a>
	/// for more details.
	/// <p>To use this you must compile
	/// NativePosixUtil.cpp (exposes Linux-specific APIs through
	/// JNI) for your platform, by running <code>ant
	/// build-native-unix</code>, and then putting the resulting
	/// <code>libNativePosixUtil.so</code> (from
	/// <code>lucene/build/native</code>) onto your dynamic
	/// linker search path.
	/// <p><b>WARNING</b>: this code is very new and quite easily
	/// could contain horrible bugs.  For example, here's one
	/// known issue: if you use seek in <code>IndexOutput</code>, and then
	/// write more than one buffer's worth of bytes, then the
	/// file will be wrong.  Lucene does not do this today (only writes
	/// small number of bytes after seek), but that may change.
	/// <p>This directory passes Solr and Lucene tests on Linux
	/// and OS X; other Unixes should work but have not been
	/// tested!  Use at your own risk.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class NativeUnixDirectory : FSDirectory
	{
		private const long ALIGN = 512;

		private const long ALIGN_NOT_MASK = ~(ALIGN - 1);

		/// <summary>
		/// Default buffer size before writing to disk (256 KB);
		/// larger means less IO load but more RAM and direct
		/// buffer storage space consumed during merging.
		/// </summary>
		/// <remarks>
		/// Default buffer size before writing to disk (256 KB);
		/// larger means less IO load but more RAM and direct
		/// buffer storage space consumed during merging.
		/// </remarks>
		public const int DEFAULT_MERGE_BUFFER_SIZE = 262144;

		/// <summary>
		/// Default min expected merge size before direct IO is
		/// used (10 MB):
		/// </summary>
		public const long DEFAULT_MIN_BYTES_DIRECT = 10 * 1024 * 1024;

		private readonly int mergeBufferSize;

		private readonly long minBytesDirect;

		private readonly Directory delegate_;

		/// <summary>Create a new NIOFSDirectory for the named location.</summary>
		/// <remarks>Create a new NIOFSDirectory for the named location.</remarks>
		/// <param name="path">the path of the directory</param>
		/// <param name="mergeBufferSize">
		/// Size of buffer to use for
		/// merging.  See
		/// <see cref="DEFAULT_MERGE_BUFFER_SIZE">DEFAULT_MERGE_BUFFER_SIZE</see>
		/// .
		/// </param>
		/// <param name="minBytesDirect">
		/// Merges, or files to be opened for
		/// reading, smaller than this will
		/// not use direct IO.  See
		/// <see cref="DEFAULT_MIN_BYTES_DIRECT">DEFAULT_MIN_BYTES_DIRECT</see>
		/// </param>
		/// <param name="delegate_">fallback Directory for non-merges</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public NativeUnixDirectory(FilePath path, int mergeBufferSize, long minBytesDirect
			, Directory delegate_) : base(path, delegate_.GetLockFactory())
		{
			// javadoc
			// TODO
			//   - newer Linux kernel versions (after 2.6.29) have
			//     improved MADV_SEQUENTIAL (and hopefully also
			//     FADV_SEQUENTIAL) interaction with the buffer
			//     cache; we should explore using that instead of direct
			//     IO when context is merge
			// TODO: this is OS dependent, but likely 512 is the LCD
			if ((mergeBufferSize & ALIGN) != 0)
			{
				throw new ArgumentException("mergeBufferSize must be 0 mod " + ALIGN + " (got: " 
					+ mergeBufferSize + ")");
			}
			this.mergeBufferSize = mergeBufferSize;
			this.minBytesDirect = minBytesDirect;
			this.delegate_ = delegate_;
		}

		/// <summary>Create a new NIOFSDirectory for the named location.</summary>
		/// <remarks>Create a new NIOFSDirectory for the named location.</remarks>
		/// <param name="path">the path of the directory</param>
		/// <param name="delegate_">fallback Directory for non-merges</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public NativeUnixDirectory(FilePath path, Directory delegate_) : this(path, DEFAULT_MERGE_BUFFER_SIZE
			, DEFAULT_MIN_BYTES_DIRECT, delegate_)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IndexInput OpenInput(string name, IOContext context)
		{
			EnsureOpen();
			if (context.context != IOContext.Context.MERGE || context.mergeInfo.estimatedMergeBytes
				 < minBytesDirect || FileLength(name) < minBytesDirect)
			{
				return delegate_.OpenInput(name, context);
			}
			else
			{
				return new NativeUnixDirectory.NativeUnixIndexInput(new FilePath(GetDirectory(), 
					name), mergeBufferSize);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IndexOutput CreateOutput(string name, IOContext context)
		{
			EnsureOpen();
			if (context.context != IOContext.Context.MERGE || context.mergeInfo.estimatedMergeBytes
				 < minBytesDirect)
			{
				return delegate_.CreateOutput(name, context);
			}
			else
			{
				EnsureCanWrite(name);
				return new NativeUnixDirectory.NativeUnixIndexOutput(new FilePath(GetDirectory(), 
					name), mergeBufferSize);
			}
		}

		private sealed class NativeUnixIndexOutput : IndexOutput
		{
			private readonly ByteBuffer buffer;

			private readonly FileOutputStream fos;

			private readonly FileChannel channel;

			private readonly int bufferSize;

			private int bufferPos;

			private long filePos;

			private long fileLength;

			private bool isOpen;

			/// <exception cref="System.IO.IOException"></exception>
			public NativeUnixIndexOutput(FilePath path, int bufferSize)
			{
				//private final File path;
				//this.path = path;
				FileDescriptor fd = NativePosixUtil.Open_direct(path.ToString(), false);
				fos = new FileOutputStream(fd);
				//fos = new FileOutputStream(path);
				channel = fos.GetChannel();
				buffer = ByteBuffer.AllocateDirect(bufferSize);
				this.bufferSize = bufferSize;
				isOpen = true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteByte(byte b)
			{
				//HM:revisit
				//assert bufferPos == buffer.position(): "bufferPos=" + bufferPos + " vs buffer.position()=" + buffer.position();
				buffer.Put(b);
				if (++bufferPos == bufferSize)
				{
					Dump();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteBytes(byte[] src, int offset, int len)
			{
				int toWrite = len;
				while (true)
				{
					int left = bufferSize - bufferPos;
					if (left <= toWrite)
					{
						buffer.Put(src, offset, left);
						toWrite -= left;
						offset += left;
						bufferPos = bufferSize;
						Dump();
					}
					else
					{
						buffer.Put(src, offset, toWrite);
						bufferPos += toWrite;
						break;
					}
				}
			}

			//@Override
			//public void setLength() throws IOException {
			//   TODO -- how to impl this?  neither FOS nor
			//   FileChannel provides an API?
			//}
			public override void Flush()
			{
			}

			// TODO -- I don't think this method is necessary?
			/// <exception cref="System.IO.IOException"></exception>
			private void Dump()
			{
				buffer.Flip();
				long limit = filePos + buffer.Limit();
				if (limit > fileLength)
				{
					// this dump extends the file
					fileLength = limit;
				}
				// we had seek'd back & wrote some changes
				// must always round to next block
				buffer.Limit((int)((buffer.Limit() + ALIGN - 1) & ALIGN_NOT_MASK));
				//HM:revisit
				//System.out.println(Thread.currentThread().getName() + ": dump to " + filePos + " limit=" + buffer.limit() + " fos=" + fos);
				channel.Write(buffer, filePos);
				filePos += bufferPos;
				bufferPos = 0;
				buffer.Clear();
			}

			//System.out.println("dump: done");
			// TODO: the case where we'd seek'd back, wrote an
			// entire buffer, we must here read the next buffer;
			// likely Lucene won't trip on this since we only
			// write smallish amounts on seeking back
			public override long GetFilePointer()
			{
				return filePos + bufferPos;
			}

			// TODO: seek is fragile at best; it can only properly
			// handle seek & then change bytes that fit entirely
			// within one buffer
			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(long pos)
			{
				if (pos != GetFilePointer())
				{
					Dump();
					long alignedPos = pos & ALIGN_NOT_MASK;
					filePos = alignedPos;
					int n = (int)NativePosixUtil.Pread(fos.GetFD(), filePos, buffer);
					if (n < bufferSize)
					{
						buffer.Limit(n);
					}
					//System.out.println("seek refill=" + n);
					int delta = (int)(pos - alignedPos);
					buffer.Position(delta);
					bufferPos = delta;
				}
			}

			public override long Length()
			{
				return fileLength + bufferPos;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetChecksum()
			{
				throw new NotSupportedException("this directory currently does not work at all!");
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				if (isOpen)
				{
					isOpen = false;
					try
					{
						Dump();
					}
					finally
					{
						try
						{
							//System.out.println("direct close set len=" + fileLength + " vs " + channel.size() + " path=" + path);
							channel.Truncate(fileLength);
						}
						finally
						{
							//System.out.println("  now: " + channel.size());
							try
							{
								channel.Close();
							}
							finally
							{
								fos.Close();
							}
						}
					}
				}
			}
			//System.out.println("  final len=" + path.length());
		}

		private sealed class NativeUnixIndexInput : IndexInput
		{
			private readonly ByteBuffer buffer;

			private readonly FileInputStream fis;

			private readonly FileChannel channel;

			private readonly int bufferSize;

			private bool isOpen;

			private bool isClone;

			private long filePos;

			private int bufferPos;

			/// <exception cref="System.IO.IOException"></exception>
			public NativeUnixIndexInput(FilePath path, int bufferSize) : base("NativeUnixIndexInput(path=\""
				 + path.GetPath() + "\")")
			{
				FileDescriptor fd = NativePosixUtil.Open_direct(path.ToString(), true);
				fis = new FileInputStream(fd);
				channel = fis.GetChannel();
				this.bufferSize = bufferSize;
				buffer = ByteBuffer.AllocateDirect(bufferSize);
				isOpen = true;
				isClone = false;
				filePos = -bufferSize;
				bufferPos = bufferSize;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public NativeUnixIndexInput(NativeUnixDirectory.NativeUnixIndexInput other) : base
				(other.ToString())
			{
				//System.out.println("D open " + path + " this=" + this);
				// for clone
				this.fis = null;
				channel = other.channel;
				this.bufferSize = other.bufferSize;
				buffer = ByteBuffer.AllocateDirect(bufferSize);
				filePos = -bufferSize;
				bufferPos = bufferSize;
				isOpen = true;
				isClone = true;
				//System.out.println("D clone this=" + this);
				Seek(other.GetFilePointer());
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				if (isOpen && !isClone)
				{
					try
					{
						channel.Close();
					}
					finally
					{
						if (!isClone)
						{
							fis.Close();
						}
					}
				}
			}

			public override long GetFilePointer()
			{
				return filePos + bufferPos;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(long pos)
			{
				if (pos != GetFilePointer())
				{
					long alignedPos = pos & ALIGN_NOT_MASK;
					filePos = alignedPos - bufferSize;
					int delta = (int)(pos - alignedPos);
					if (delta != 0)
					{
						Refill();
						buffer.Position(delta);
						bufferPos = delta;
					}
					else
					{
						// force refill on next read
						bufferPos = bufferSize;
					}
				}
			}

			public override long Length()
			{
				try
				{
					return channel.Size();
				}
				catch (IOException ioe)
				{
					throw new RuntimeException("IOException during length(): " + this, ioe);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override byte ReadByte()
			{
				// NOTE: we don't guard against EOF here... ie the
				// "final" buffer will typically be filled to less
				// than bufferSize
				if (bufferPos == bufferSize)
				{
					Refill();
				}
				//HM:revisit
				//assert bufferPos == buffer.position() : "bufferPos=" + bufferPos + " vs buffer.position()=" + buffer.position();
				bufferPos++;
				return buffer.Get();
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void Refill()
			{
				buffer.Clear();
				filePos += bufferSize;
				bufferPos = 0;
				//HM:revisit
				//assert (filePos & ALIGN_NOT_MASK) == filePos : "filePos=" + filePos + " anded=" + (filePos & ALIGN_NOT_MASK);
				//System.out.println("X refill filePos=" + filePos);
				int n;
				try
				{
					n = channel.Read(buffer, filePos);
				}
				catch (IOException ioe)
				{
					throw new IOException(ioe.Message + ": " + this, ioe);
				}
				if (n < 0)
				{
					throw new EOFException("read past EOF: " + this);
				}
				buffer.Rewind();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void ReadBytes(byte[] dst, int offset, int len)
			{
				int toRead = len;
				//System.out.println("\nX readBytes len=" + len + " fp=" + getFilePointer() + " size=" + length() + " this=" + this);
				while (true)
				{
					int left = bufferSize - bufferPos;
					if (left < toRead)
					{
						//System.out.println("  copy " + left);
						buffer.Get(dst, offset, left);
						toRead -= left;
						offset += left;
						Refill();
					}
					else
					{
						//System.out.println("  copy " + toRead);
						buffer.Get(dst, offset, toRead);
						bufferPos += toRead;
						//System.out.println("  readBytes done");
						break;
					}
				}
			}

			public override DataInput Clone()
			{
				try
				{
					return new NativeUnixDirectory.NativeUnixIndexInput(this);
				}
				catch (IOException ioe)
				{
					throw new RuntimeException("IOException during clone: " + this, ioe);
				}
			}
		}
	}
}
