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

// FastCharStream.java

using System;

namespace Lucene.Net.QueryParsers
{
	
	/// <summary>An efficient implementation of JavaCC's CharStream interface.  <p/>Note that
	/// this does not do line-number counting, but instead keeps track of the
	/// character position of the token in the input, as required by Lucene's <see cref="Lucene.Net.Analysis.Token" />
	/// API. 
	/// 
	/// </summary>
	public sealed class FastCharStream : ICharStream
	{
		internal char[] buffer = null;
		
		internal int bufferLength = 0; // end of valid chars
		internal int bufferPosition = 0; // next char to read
		
		internal int tokenStart = 0; // offset in buffer
		internal int bufferStart = 0; // position in file of buffer
		
		internal System.IO.TextReader input; // source of chars
		
		/// <summary>Constructs from a Reader. </summary>
		public FastCharStream(System.IO.TextReader r)
		{
			input = r;
		}
		
		public char ReadChar()
		{
			bool? systemIoException = null;
			if (bufferPosition >= bufferLength)
			{
				Refill(ref systemIoException);
			}
			return buffer[bufferPosition++];
		}
		
		public char ReadChar(ref bool? systemIoException)
		{
			if (bufferPosition >= bufferLength)
			{
				Refill(ref systemIoException);
				// If using this Nullable as System.IO.IOException signal and is signaled.
				if (systemIoException.HasValue && systemIoException.Value == true)
				{
					return '\0';
				}
			}
			return buffer[bufferPosition++];
		}
		
		// You may ask to be signaled of a System.IO.IOException through the systemIoException parameter.
		// Set it to false if you are interested, it will be set to true to signal a System.IO.IOException.
		// Set it to null if you are not interested.
		// This is used to avoid having a lot of System.IO.IOExceptions thrown while running the code under a debugger.
		// Having a lot of exceptions thrown under a debugger causes the code to execute a lot more slowly.
		// So use this if you are experimenting a lot of slow parsing at runtime under a debugger.
		private void Refill(ref bool? systemIoException)
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
				else if (bufferLength == buffer.Length)
				{
					// grow buffer
					char[] newBuffer = new char[buffer.Length * 2];
					Array.Copy(buffer, 0, newBuffer, 0, bufferLength);
					buffer = newBuffer;
				}
			}
			else
			{
				// shift token to front
				Array.Copy(buffer, tokenStart, buffer, 0, newPosition);
			}
			
			bufferLength = newPosition; // update state
			bufferPosition = newPosition;
			bufferStart += tokenStart;
			tokenStart = 0;
			
			int charsRead = input.Read(buffer, newPosition, buffer.Length - newPosition);
			if (charsRead <= 0)
			{
				// If interested in using this Nullable to signal a System.IO.IOException
				if (systemIoException.HasValue)
				{
					systemIoException = true;
					return;
				}
				else
				{
					throw new System.IO.IOException("read past eof");
				}
			}
			else
				bufferLength += charsRead;
		}
		
		public char BeginToken()
		{
			tokenStart = bufferPosition;
			return ReadChar();
		}

		public char BeginToken(ref bool? systemIoException)
		{
			tokenStart = bufferPosition;
			return ReadChar(ref systemIoException);
		}
		
		public void  Backup(int amount)
		{
			bufferPosition -= amount;
		}

		public string Image
		{
			get { return new System.String(buffer, tokenStart, bufferPosition - tokenStart); }
		}

		public char[] GetSuffix(int len)
		{
			char[] value_Renamed = new char[len];
			Array.Copy(buffer, bufferPosition - len, value_Renamed, 0, len);
			return value_Renamed;
		}
		
		public void  Done()
		{
			try
			{
				input.Close();
			}
			catch (System.IO.IOException e)
			{
				System.Console.Error.WriteLine("Caught: " + e + "; ignoring.");
			}
		}

		public int Column
		{
			get { return bufferStart + bufferPosition; }
		}

		public int Line
		{
			get { return 1; }
		}

		public int EndColumn
		{
			get { return bufferStart + bufferPosition; }
		}

		public int EndLine
		{
			get { return 1; }
		}

		public int BeginColumn
		{
			get { return bufferStart + tokenStart; }
		}

		public int BeginLine
		{
			get { return 1; }
		}
	}
}
