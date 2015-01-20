/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Queryparser.Classic;
using Sharpen;

namespace Lucene.Net.Queryparser.Classic
{
	/// <summary>An efficient implementation of JavaCC's CharStream interface.</summary>
	/// <remarks>
	/// An efficient implementation of JavaCC's CharStream interface.  <p>Note that
	/// this does not do line-number counting, but instead keeps track of the
	/// character position of the token in the input, as required by Lucene's
	/// <see cref="Lucene.Net.Analysis.Token">Lucene.Net.Analysis.Token</see>
	/// API.
	/// </remarks>
	public sealed class FastCharStream : CharStream
	{
		internal char[] buffer = null;

		internal int bufferLength = 0;

		internal int bufferPosition = 0;

		internal int tokenStart = 0;

		internal int bufferStart = 0;

		internal StreamReader input;

		/// <summary>Constructs from a Reader.</summary>
		/// <remarks>Constructs from a Reader.</remarks>
		public FastCharStream(StreamReader r)
		{
			// FastCharStream.java
			// end of valid chars
			// next char to read
			// offset in buffer
			// position in file of buffer
			// source of chars
			input = r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public char ReadChar()
		{
			if (bufferPosition >= bufferLength)
			{
				Refill();
			}
			return buffer[bufferPosition++];
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Refill()
		{
			int newPosition = bufferLength - tokenStart;
			if (tokenStart == 0)
			{
				// token won't fit in buffer
				if (buffer == null)
				{
					// first time: alloc buffer
					buffer = new char[2048];
				}
				else
				{
					if (bufferLength == buffer.Length)
					{
						// grow buffer
						char[] newBuffer = new char[buffer.Length * 2];
						System.Array.Copy(buffer, 0, newBuffer, 0, bufferLength);
						buffer = newBuffer;
					}
				}
			}
			else
			{
				// shift token to front
				System.Array.Copy(buffer, tokenStart, buffer, 0, newPosition);
			}
			bufferLength = newPosition;
			// update state
			bufferPosition = newPosition;
			bufferStart += tokenStart;
			tokenStart = 0;
			int charsRead = input.Read(buffer, newPosition, buffer.Length - newPosition);
			// fill space in buffer
			if (charsRead == -1)
			{
				throw new IOException("read past eof");
			}
			else
			{
				bufferLength += charsRead;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public char BeginToken()
		{
			tokenStart = bufferPosition;
			return ReadChar();
		}

		public void Backup(int amount)
		{
			bufferPosition -= amount;
		}

		public string GetImage()
		{
			return new string(buffer, tokenStart, bufferPosition - tokenStart);
		}

		public char[] GetSuffix(int len)
		{
			char[] value = new char[len];
			System.Array.Copy(buffer, bufferPosition - len, value, 0, len);
			return value;
		}

		public void Done()
		{
			try
			{
				input.Close();
			}
			catch (IOException)
			{
			}
		}

		public int GetColumn()
		{
			return bufferStart + bufferPosition;
		}

		public int GetLine()
		{
			return 1;
		}

		public int GetEndColumn()
		{
			return bufferStart + bufferPosition;
		}

		public int GetEndLine()
		{
			return 1;
		}

		public int GetBeginColumn()
		{
			return bufferStart + tokenStart;
		}

		public int GetBeginLine()
		{
			return 1;
		}
	}
}
