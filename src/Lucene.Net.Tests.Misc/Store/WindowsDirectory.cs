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
	/// Native
	/// <see cref="Directory">Directory</see>
	/// implementation for Microsoft Windows.
	/// <p>
	/// Steps:
	/// <ol>
	/// <li>Compile the source code to create WindowsDirectory.dll:
	/// <blockquote>
	/// c:\mingw\bin\g++ -Wall -D_JNI_IMPLEMENTATION_ -Wl,--kill-at
	/// -I"%JAVA_HOME%\include" -I"%JAVA_HOME%\include\win32" -static-libgcc
	/// -static-libstdc++ -shared WindowsDirectory.cpp -o WindowsDirectory.dll
	/// </blockquote>
	/// For 64-bit JREs, use mingw64, with the -m64 option.
	/// <li>Put WindowsDirectory.dll into some directory in your windows PATH
	/// <li>Open indexes with WindowsDirectory and use it.
	/// </ol>
	/// </p>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class WindowsDirectory : FSDirectory
	{
		private const int DEFAULT_BUFFERSIZE = 4096;

		static WindowsDirectory()
		{
			// javadoc
			// javadoc
			Runtime.LoadLibrary("WindowsDirectory");
		}

		/// <summary>Create a new WindowsDirectory for the named location.</summary>
		/// <remarks>Create a new WindowsDirectory for the named location.</remarks>
		/// <param name="path">the path of the directory</param>
		/// <param name="lockFactory">
		/// the lock factory to use, or null for the default
		/// (
		/// <see cref="NativeFSLockFactory">NativeFSLockFactory</see>
		/// );
		/// </param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		protected WindowsDirectory(FilePath path, LockFactory lockFactory) : base(path, lockFactory
			)
		{
		}

		/// <summary>
		/// Create a new WindowsDirectory for the named location and
		/// <see cref="NativeFSLockFactory">NativeFSLockFactory</see>
		/// .
		/// </summary>
		/// <param name="path">the path of the directory</param>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		public WindowsDirectory(FilePath path) : base(path, null)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IndexInput OpenInput(string name, IOContext context)
		{
			EnsureOpen();
			return new WindowsDirectory.WindowsIndexInput(new FilePath(GetDirectory(), name), 
				Math.Max(BufferedIndexInput.BufferSize(context), DEFAULT_BUFFERSIZE));
		}

		internal class WindowsIndexInput : BufferedIndexInput
		{
			private readonly long fd;

			private readonly long length;

			internal bool isClone;

			internal bool isOpen;

			/// <exception cref="System.IO.IOException"></exception>
			public WindowsIndexInput(FilePath file, int bufferSize) : base("WindowsIndexInput(path=\""
				 + file.GetPath() + "\")", bufferSize)
			{
				fd = WindowsDirectory.Open(file.GetPath());
				length = WindowsDirectory.Length(fd);
				isOpen = true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void ReadInternal(byte[] b, int offset, int length)
			{
				int bytesRead;
				try
				{
					bytesRead = WindowsDirectory.Read(fd, b, offset, length, GetFilePointer());
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

			/// <exception cref="System.IO.IOException"></exception>
			protected override void SeekInternal(long pos)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				lock (this)
				{
					// NOTE: we synchronize and track "isOpen" because Lucene sometimes closes IIs twice!
					if (!isClone && isOpen)
					{
						WindowsDirectory.Close(fd);
						isOpen = false;
					}
				}
			}

			public override long Length()
			{
				return length;
			}

			public override DataInput Clone()
			{
				WindowsDirectory.WindowsIndexInput clone = (WindowsDirectory.WindowsIndexInput)base
					.Clone();
				clone.isClone = true;
				return clone;
			}
		}

		/// <summary>Opens a handle to a file.</summary>
		/// <remarks>Opens a handle to a file.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private static long Open(string filename)
		{
		}

		/// <summary>Reads data from a file at pos into bytes</summary>
		/// <exception cref="System.IO.IOException"></exception>
		private static int Read(long fd, byte[] bytes, int offset, int length, long pos)
		{
		}

		/// <summary>Closes a handle to a file</summary>
		/// <exception cref="System.IO.IOException"></exception>
		private static void Close(long fd)
		{
		}

		/// <summary>Returns the length of a file</summary>
		/// <exception cref="System.IO.IOException"></exception>
		private static long Length(long fd)
		{
		}
	}
}
