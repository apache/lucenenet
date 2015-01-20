/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Store
{
	/// <summary>
	/// Provides JNI access to native methods such as madvise() for
	/// <see cref="NativeUnixDirectory">NativeUnixDirectory</see>
	/// </summary>
	public sealed class NativePosixUtil
	{
		public const int NORMAL = 0;

		public const int SEQUENTIAL = 1;

		public const int RANDOM = 2;

		public const int WILLNEED = 3;

		public const int DONTNEED = 4;

		public const int NOREUSE = 5;

		static NativePosixUtil()
		{
			Runtime.LoadLibrary("NativePosixUtil");
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static int Posix_fadvise(FileDescriptor fd, long offset, long len, int advise
			)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static int Posix_madvise(ByteBuffer buf, int advise)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static int Madvise(ByteBuffer buf, int advise)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static FileDescriptor Open_direct(string filename, bool read)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static long Pread(FileDescriptor fd, long pos, ByteBuffer byteBuf)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void Advise(FileDescriptor fd, long offset, long len, int advise)
		{
			int code = Posix_fadvise(fd, offset, len, advise);
			if (code != 0)
			{
				throw new RuntimeException("posix_fadvise failed code=" + code);
			}
		}
	}
}
