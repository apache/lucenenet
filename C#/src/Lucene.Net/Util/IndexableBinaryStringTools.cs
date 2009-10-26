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

// {{Aroush-2.9}} Port issue?  Both of those were treated as: System.IO.MemoryStream
//using CharBuffer = java.nio.CharBuffer;
//using ByteBuffer = java.nio.ByteBuffer;

namespace Lucene.Net.Util
{
	
	/// <summary> Provides support for converting byte sequences to Strings and back again.
	/// The resulting Strings preserve the original byte sequences' sort order.
	/// 
	/// The Strings are constructed using a Base 8000h encoding of the original
	/// binary data - each char of an encoded String represents a 15-bit chunk
	/// from the byte sequence.  Base 8000h was chosen because it allows for all
	/// lower 15 bits of char to be used without restriction; the surrogate range 
	/// [U+D8000-U+DFFF] does not represent valid chars, and would require
	/// complicated handling to avoid them and allow use of char's high bit.
	/// 
	/// Although unset bits are used as padding in the final char, the original
	/// byte sequence could contain trailing bytes with no set bits (null bytes):
	/// padding is indistinguishable from valid information.  To overcome this
	/// problem, a char is appended, indicating the number of encoded bytes in the
	/// final content char.
	/// 
	/// This class's operations are defined over CharBuffers and ByteBuffers, to
	/// allow for wrapped arrays to be reused, reducing memory allocation costs for
	/// repeated operations.  Note that this class calls array() and arrayOffset()
	/// on the CharBuffers and ByteBuffers it uses, so only wrapped arrays may be
	/// used.  This class interprets the arrayOffset() and limit() values returned by
	/// its input buffers as beginning and end+1 positions on the wrapped array,
	/// resprectively; similarly, on the output buffer, arrayOffset() is the first
	/// position written to, and limit() is set to one past the final output array
	/// position.
	/// </summary>
	public class IndexableBinaryStringTools
	{
		
		private static readonly CodingCase[] CODING_CASES = new CodingCase[]{new CodingCase(7, 1), new CodingCase(14, 6, 2), new CodingCase(13, 5, 3), new CodingCase(12, 4, 4), new CodingCase(11, 3, 5), new CodingCase(10, 2, 6), new CodingCase(9, 1, 7), new CodingCase(8, 0)};
		
		// Export only static methods
		private IndexableBinaryStringTools()
		{
		}
		
		/// <summary> Returns the number of chars required to encode the given byte sequence.
		/// 
		/// </summary>
		/// <param name="original">The byte sequence to be encoded.  Must be backed by an array.
		/// </param>
		/// <returns> The number of chars required to encode the given byte sequence
		/// </returns>
		/// <throws>  IllegalArgumentException If the given ByteBuffer is not backed by an array </throws>
		public static int GetEncodedLength(System.IO.MemoryStream original)
		{
			// if (original.hasArray()) // {{Aroush-2.9}}
			{
				// Use long for intermediaries to protect against overflow
				// long length = (long) (original.limit() - original.arrayOffset()); // {{Aroush-2.9}}
                long length = (long) (original.Capacity - original.Position);
				return (int) ((length * 8L + 14L) / 15L) + 1;
			}
			// else // {{Aroush-2.9}}
			// { // {{Aroush-2.9}}
			// 	throw new System.ArgumentException("original argument must have a backing array"); // {{Aroush-2.9}}
			// } // {{Aroush-2.9}}
		}
		
		/// <summary> Returns the number of bytes required to decode the given char sequence.
		/// 
		/// </summary>
		/// <param name="encoded">The char sequence to be encoded.  Must be backed by an array.
		/// </param>
		/// <returns> The number of bytes required to decode the given char sequence
		/// </returns>
		/// <throws>  IllegalArgumentException If the given CharBuffer is not backed by an array </throws>
        public static int GetDecodedLength(System.IO.MemoryStream encoded)
		{
			// if (encoded.hasArray()) // {{Aroush-2.9}}
			{
				// int numChars = encoded.limit() - encoded.arrayOffset() - 1; // {{Aroush-2.9}}
                long numChars = encoded.Capacity - encoded.Position - 1;
				if (numChars <= 0)
				{
					return 0;
				}
				else
				{
                    // int numFullBytesInFinalChar = encoded.charAt(encoded.limit() - 1); // {{Aroush-2.9}}
                    byte[] buf = new byte[1];
                    int numFullBytesInFinalChar = encoded.Read(buf, encoded.Capacity - 1, 1);
					long numEncodedChars = numChars - 1;
					return (int) ((numEncodedChars * 15 + 7) / 8 + numFullBytesInFinalChar);
				}
			}
			// else // {{Aroush-2.9}}
			// {
			// 	throw new System.ArgumentException("encoded argument must have a backing array"); // {{Aroush-2.9}}
			// } // {{Aroush-2.9}}
		}
		
		/// <summary> Encodes the input byte sequence into the output char sequence.  Before
		/// calling this method, ensure that the output CharBuffer has sufficient
		/// capacity by calling {@link #GetEncodedLength(java.nio.ByteBuffer)}.
		/// 
		/// </summary>
		/// <param name="input">The byte sequence to encode
		/// </param>
		/// <param name="output">Where the char sequence encoding result will go.  The limit
		/// is set to one past the position of the final char.
		/// </param>
		/// <throws>  IllegalArgumentException If either the input or the output buffer </throws>
		/// <summary>  is not backed by an array
		/// </summary>
		public static void  Encode(System.IO.MemoryStream input, System.IO.MemoryStream output)
		{
			// if (input.hasArray() && output.hasArray()) // {{Aroush-2.9}}
			{
				// byte[] inputArray = input.array();   // {{Aroush-2.9}}
                byte[] inputArray = input.GetBuffer();
				// int inputOffset = input.arrayOffset(); // {{Aroush-2.9}}
                long inputOffset = input.Position;
				// int inputLength = input.limit() - inputOffset; // {{Aroush-2.9}}
                long inputLength = input.Capacity - inputOffset;
				// char[] outputArray = output.array(); // {{Aroush-2.9}}
                byte[] outputArray = output.GetBuffer();
				// int outputOffset = output.arrayOffset(); // {{Aroush-2.9}}
                long outputOffset = output.Position;
				int outputLength = GetEncodedLength(input);
				// output.limit(outputOffset + outputLength); // Set output final pos + 1 // {{Aroush-2.9}}
                output.Position = outputOffset + outputLength;
				// output.position(0); // {{Aroush-2.9}}
                output.Position = 0;
				if (inputLength > 0)
				{
					long inputByteNum = inputOffset;
					int caseNum = 0;
					long outputCharNum = outputOffset;
					CodingCase codingCase;
					for (; inputByteNum + CODING_CASES[caseNum].numBytes <= inputLength; ++outputCharNum)
					{
						codingCase = CODING_CASES[caseNum];
						if (2 == codingCase.numBytes)
						{
							outputArray[outputCharNum] = (byte) (((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) + ((SupportClass.Number.URShift((inputArray[inputByteNum + 1] & 0xFF), codingCase.finalShift)) & codingCase.finalMask) & (short) 0x7FFF);
						}
						else
						{
							// numBytes is 3
							outputArray[outputCharNum] = (byte) (((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) + ((inputArray[inputByteNum + 1] & 0xFF) << codingCase.middleShift) + ((SupportClass.Number.URShift((inputArray[inputByteNum + 2] & 0xFF), codingCase.finalShift)) & codingCase.finalMask) & (short) 0x7FFF);
						}
						inputByteNum += codingCase.advanceBytes;
						if (++caseNum == CODING_CASES.Length)
						{
							caseNum = 0;
						}
					}
					// Produce final char (if any) and trailing count chars.
					codingCase = CODING_CASES[caseNum];
					
					if (inputByteNum + 1 < inputLength)
					{
						// codingCase.numBytes must be 3
						outputArray[outputCharNum++] = (byte) ((((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) + ((inputArray[inputByteNum + 1] & 0xFF) << codingCase.middleShift)) & (short) 0x7FFF);
						// Add trailing char containing the number of full bytes in final char
						outputArray[outputCharNum++] = (byte) 1;
					}
					else if (inputByteNum < inputLength)
					{
						outputArray[outputCharNum++] = (byte) (((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) & (short) 0x7FFF);
						// Add trailing char containing the number of full bytes in final char
						outputArray[outputCharNum++] = caseNum == 0?(byte) 1:(byte) 0;
					}
					else
					{
						// No left over bits - last char is completely filled.
						// Add trailing char containing the number of full bytes in final char
						outputArray[outputCharNum++] = (byte) 1;
					}
				}
			}
            // else // {{Aroush-2.9}}
			// {
			// 	throw new System.ArgumentException("Arguments must have backing arrays"); // {{Aroush-2.9}}
			// }
		}
		
		/// <summary> Decodes the input char sequence into the output byte sequence.  Before
		/// calling this method, ensure that the output ByteBuffer has sufficient
		/// capacity by calling {@link #GetDecodedLength(java.nio.CharBuffer)}.
		/// 
		/// </summary>
		/// <param name="input">The char sequence to decode
		/// </param>
		/// <param name="output">Where the byte sequence decoding result will go.  The limit
		/// is set to one past the position of the final char.
		/// </param>
		/// <throws>  IllegalArgumentException If either the input or the output buffer </throws>
		/// <summary>  is not backed by an array
		/// </summary>
		public static void  Decode(System.IO.MemoryStream input, System.IO.MemoryStream output)
		{
			// if (input.hasArray() && output.hasArray()) // {{Aroush-2.9}}
			{
				// int numInputChars = input.limit() - input.arrayOffset() - 1; // {{Aroush-2.9}}
                long numInputChars = input.Capacity - input.Position - 1;
				int numOutputBytes = GetDecodedLength(input);
				// output.limit(numOutputBytes + output.arrayOffset()); // Set output final pos + 1 // {{Aroush-2.9}}
                output.Capacity = (int) (numOutputBytes + output.Position);
				// output.position(0); // {{Aroush-2.9}}
                output.Position = 0;
				// byte[] outputArray = output.array(); // {{Aroush-2.9}}
                byte[] outputArray = output.GetBuffer();
				// char[] inputArray = input.array(); // {{Aroush-2.9}}
                byte[] inputArray = (byte[]) input.GetBuffer(); 
				if (numOutputBytes > 0)
				{
					int caseNum = 0;
					// int outputByteNum = output.arrayOffset(); // {{Aroush-2.9}}
                    long outputByteNum = output.Position;
					// int inputCharNum = input.arrayOffset(); // {{Aroush-2.9}}
                    long inputCharNum = input.Position;
					short inputChar;
					CodingCase codingCase;
					for (; inputCharNum < numInputChars - 1; ++inputCharNum)
					{
						codingCase = CODING_CASES[caseNum];
						inputChar = (short) inputArray[inputCharNum];
						if (2 == codingCase.numBytes)
						{
							if (0 == caseNum)
							{
								outputArray[outputByteNum] = (byte) (SupportClass.Number.URShift(inputChar, codingCase.initialShift));
							}
							else
							{
								outputArray[outputByteNum] = (byte) (outputArray[outputByteNum] + (byte) (SupportClass.Number.URShift(inputChar, codingCase.initialShift)));
							}
							outputArray[outputByteNum + 1] = (byte) ((inputChar & codingCase.finalMask) << codingCase.finalShift);
						}
						else
						{
							// numBytes is 3
							outputArray[outputByteNum] = (byte) (outputArray[outputByteNum] + (byte) (SupportClass.Number.URShift(inputChar, codingCase.initialShift)));
							outputArray[outputByteNum + 1] = (byte) (SupportClass.Number.URShift((inputChar & codingCase.middleMask), codingCase.middleShift));
							outputArray[outputByteNum + 2] = (byte) ((inputChar & codingCase.finalMask) << codingCase.finalShift);
						}
						outputByteNum += codingCase.advanceBytes;
						if (++caseNum == CODING_CASES.Length)
						{
							caseNum = 0;
						}
					}
					// Handle final char
					inputChar = (short) inputArray[inputCharNum];
					codingCase = CODING_CASES[caseNum];
					if (0 == caseNum)
					{
						outputArray[outputByteNum] = 0;
					}
					outputArray[outputByteNum] = (byte) (outputArray[outputByteNum] + (byte) (SupportClass.Number.URShift(inputChar, codingCase.initialShift)));
					long bytesLeft = numOutputBytes - outputByteNum;
					if (bytesLeft > 1)
					{
						if (2 == codingCase.numBytes)
						{
							outputArray[outputByteNum + 1] = (byte) (SupportClass.Number.URShift((inputChar & codingCase.finalMask), codingCase.finalShift));
						}
						else
						{
							// numBytes is 3
							outputArray[outputByteNum + 1] = (byte) (SupportClass.Number.URShift((inputChar & codingCase.middleMask), codingCase.middleShift));
							if (bytesLeft > 2)
							{
								outputArray[outputByteNum + 2] = (byte) ((inputChar & codingCase.finalMask) << codingCase.finalShift);
							}
						}
					}
				}
			}
            // else // {{Aroush-2.9}}
			// {
			// 	throw new System.ArgumentException("Arguments must have backing arrays"); // {{Aroush-2.9}}
			// }
		}
		
		/// <summary> Decodes the given char sequence, which must have been encoded by
		/// {@link #Encode(java.nio.ByteBuffer)} or 
		/// {@link #Encode(java.nio.ByteBuffer, java.nio.CharBuffer)}.
		/// 
		/// </summary>
		/// <param name="input">The char sequence to decode
		/// </param>
		/// <returns> A byte sequence containing the decoding result.  The limit
		/// is set to one past the position of the final char.
		/// </returns>
		/// <throws>  IllegalArgumentException If the input buffer is not backed by an </throws>
		/// <summary>  array
		/// </summary>
		public static System.IO.MemoryStream Decode(System.IO.MemoryStream input)
		{
			byte[] outputArray = new byte[GetDecodedLength(input)];
			System.IO.MemoryStream output = new System.IO.MemoryStream(outputArray);
			Decode(input, output);
			return output;
		}
		
		/// <summary> Encodes the input byte sequence.
		/// 
		/// </summary>
		/// <param name="input">The byte sequence to encode
		/// </param>
		/// <returns> A char sequence containing the encoding result.  The limit is set
		/// to one past the position of the final char.
		/// </returns>
		/// <throws>  IllegalArgumentException If the input buffer is not backed by an </throws>
		/// <summary>  array
		/// </summary>
		public static System.IO.MemoryStream Encode(System.IO.MemoryStream input)
		{
			byte[] outputArray = new byte[GetEncodedLength(input)];
			System.IO.MemoryStream output = new System.IO.MemoryStream(outputArray);
			Encode(input, output);
			return output;
		}
		
		internal class CodingCase
		{
			internal int numBytes, initialShift, middleShift, finalShift, advanceBytes = 2;
			internal short middleMask, finalMask;
			
			internal CodingCase(int initialShift, int middleShift, int finalShift)
			{
				this.numBytes = 3;
				this.initialShift = initialShift;
				this.middleShift = middleShift;
				this.finalShift = finalShift;
				this.finalMask = (short) (SupportClass.Number.URShift((short) 0xFF, finalShift));
				this.middleMask = (short) ((short) 0xFF << middleShift);
			}
			
			internal CodingCase(int initialShift, int finalShift)
			{
				this.numBytes = 2;
				this.initialShift = initialShift;
				this.finalShift = finalShift;
				this.finalMask = (short) (SupportClass.Number.URShift((short) 0xFF, finalShift));
				if (finalShift != 0)
				{
					advanceBytes = 1;
				}
			}
		}
	}
}