using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util
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

    /// <summary>
    /// Provides support for converting byte sequences to <see cref="string"/>s and back again.
    /// The resulting <see cref="string"/>s preserve the original byte sequences' sort order.
    /// <para/>
    /// The <see cref="string"/>s are constructed using a Base 8000h encoding of the original
    /// binary data - each char of an encoded <see cref="string"/> represents a 15-bit chunk
    /// from the byte sequence.  Base 8000h was chosen because it allows for all
    /// lower 15 bits of char to be used without restriction; the surrogate range
    /// [U+D8000-U+DFFF] does not represent valid chars, and would require
    /// complicated handling to avoid them and allow use of char's high bit.
    /// <para/>
    /// Although unset bits are used as padding in the final char, the original
    /// byte sequence could contain trailing bytes with no set bits (null bytes):
    /// padding is indistinguishable from valid information.  To overcome this
    /// problem, a char is appended, indicating the number of encoded bytes in the
    /// final content char.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("Implement Analysis.TokenAttributes.ITermToBytesRefAttribute and store bytes directly instead. this class will be removed in Lucene 5.0")]
    public static class IndexableBinaryStringTools // LUCENENET specific - made static
    {
        private static readonly CodingCase[] CODING_CASES = new CodingCase[] {
            // CodingCase(int initialShift, int finalShift)
            new CodingCase(7, 1),
            // CodingCase(int initialShift, int middleShift, int finalShift)
            new CodingCase(14, 6, 2),
            new CodingCase(13, 5, 3),
            new CodingCase(12, 4, 4),
            new CodingCase(11, 3, 5),
            new CodingCase(10, 2, 6),
            new CodingCase(9, 1, 7),
            new CodingCase(8, 0)
        };

        /// <summary>
        /// Returns the number of chars required to encode the given <see cref="byte"/>s.
        /// </summary>
        /// <param name="inputArray"> Byte sequence to be encoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of bytes in <paramref name="inputArray"/> </param>
        /// <returns> The number of chars required to encode the number of <see cref="byte"/>s. </returns>
        // LUCENENET specific overload for CLS compliance

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060 // Remove unused parameter
        public static int GetEncodedLength(byte[] inputArray, int inputOffset, int inputLength)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // Use long for intermediaries to protect against overflow
            return (int)((8L * inputLength + 14L) / 15L) + 1;
        }

        /// <summary>
        /// Returns the number of chars required to encode the given <see cref="sbyte"/>s.
        /// </summary>
        /// <param name="inputArray"> <see cref="sbyte"/> sequence to be encoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of sbytes in <paramref name="inputArray"/> </param>
        /// <returns> The number of chars required to encode the number of <see cref="sbyte"/>s. </returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0060 // Remove unused parameter
        public static int GetEncodedLength(sbyte[] inputArray, int inputOffset, int inputLength)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // Use long for intermediaries to protect against overflow
            return (int)((8L * inputLength + 14L) / 15L) + 1;
        }

        /// <summary>
        /// Returns the number of <see cref="byte"/>s required to decode the given char sequence.
        /// </summary>
        /// <param name="encoded"> Char sequence to be decoded </param>
        /// <param name="offset"> Initial offset </param>
        /// <param name="length"> Number of characters </param>
        /// <returns> The number of <see cref="byte"/>s required to decode the given char sequence </returns>
        public static int GetDecodedLength(char[] encoded, int offset, int length)
        {
            int numChars = length - 1;
            if (numChars <= 0)
            {
                return 0;
            }
            else
            {
                // Use long for intermediaries to protect against overflow
                long numFullBytesInFinalChar = encoded[offset + length - 1];
                long numEncodedChars = numChars - 1;
                return (int)((numEncodedChars * 15L + 7L) / 8L + numFullBytesInFinalChar);
            }
        }

        /// <summary>
        /// Encodes the input <see cref="byte"/> sequence into the output char sequence.  Before
        /// calling this method, ensure that the output array has sufficient
        /// capacity by calling <see cref="GetEncodedLength(byte[], int, int)"/>.
        /// </summary>
        /// <param name="inputArray"> <see cref="byte"/> sequence to be encoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of bytes in <paramref name="inputArray"/> </param>
        /// <param name="outputArray"> <see cref="char"/> sequence to store encoded result </param>
        /// <param name="outputOffset"> Initial offset into outputArray </param>
        /// <param name="outputLength"> Length of output, must be GetEncodedLength(inputArray, inputOffset, inputLength) </param>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Encode(byte[] inputArray, int inputOffset, int inputLength, char[] outputArray, int outputOffset, int outputLength)
        {
            Encode((sbyte[])(Array)inputArray, inputOffset, inputLength, outputArray, outputOffset, outputLength);
        }

        /// <summary>
        /// Encodes the input <see cref="sbyte"/> sequence into the output char sequence.  Before
        /// calling this method, ensure that the output array has sufficient
        /// capacity by calling <see cref="GetEncodedLength(sbyte[], int, int)"/>.
        /// </summary>
        /// <param name="inputArray"> <see cref="sbyte"/> sequence to be encoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of bytes in <paramref name="inputArray"/> </param>
        /// <param name="outputArray"> <see cref="char"/> sequence to store encoded result </param>
        /// <param name="outputOffset"> Initial offset into outputArray </param>
        /// <param name="outputLength"> Length of output, must be getEncodedLength </param>
        [CLSCompliant(false)]
        public static void Encode(sbyte[] inputArray, int inputOffset, int inputLength, char[] outputArray, int outputOffset, int outputLength)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(outputLength == GetEncodedLength(inputArray, inputOffset, inputLength));
            if (inputLength > 0)
            {
                int inputByteNum = inputOffset;
                int caseNum = 0;
                int outputCharNum = outputOffset;
                CodingCase codingCase;
                for (; inputByteNum + CODING_CASES[caseNum].numBytes <= inputLength; ++outputCharNum)
                {
                    codingCase = CODING_CASES[caseNum];
                    if (2 == codingCase.numBytes)
                    {
                        outputArray[outputCharNum] = (char)(((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift)
                            + (((inputArray[inputByteNum + 1] & 0xFF).TripleShift(codingCase.finalShift)) & codingCase.finalMask) & /*(short)*/0x7FFF); // LUCENENET: Removed unnecessary cast
                    } // numBytes is 3
                    else
                    {
                        outputArray[outputCharNum] = (char)(((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift)
                            + ((inputArray[inputByteNum + 1] & 0xFF) << codingCase.middleShift)
                            + (((inputArray[inputByteNum + 2] & 0xFF).TripleShift(codingCase.finalShift)) & codingCase.finalMask) & /*(short)*/0x7FFF); // LUCENENET: Removed unnecessary cast
                    }
                    inputByteNum += codingCase.advanceBytes;
                    if (++caseNum == CODING_CASES.Length)
                    {
                        caseNum = 0;
                    }
                }
                // Produce final char (if any) and trailing count chars.
                codingCase = CODING_CASES[caseNum];

                if (inputByteNum + 1 < inputLength) // codingCase.numBytes must be 3
                {
                    outputArray[outputCharNum++] = (char)((((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) + ((inputArray[inputByteNum + 1] & 0xFF) <<
                        codingCase.middleShift)) & /*(short)*/0x7FFF); // LUCENENET: Removed unnecessary cast
                    // Add trailing char containing the number of full bytes in final char
                    outputArray[outputCharNum++] = (char)1;
                }
                else if (inputByteNum < inputLength)
                {
                    outputArray[outputCharNum++] = (char)(((inputArray[inputByteNum] & 0xFF) << codingCase.initialShift) & /*(short)*/0x7FFF); // LUCENENET: Removed unnecessary cast
                    // Add trailing char containing the number of full bytes in final char
                    outputArray[outputCharNum++] = caseNum == 0 ? (char)1 : (char)0;
                } // No left over bits - last char is completely filled.
                else
                {
                    // Add trailing char containing the number of full bytes in final char
                    outputArray[outputCharNum++] = (char)1;
                }
            }
        }

        /// <summary>
        /// Decodes the input <see cref="char"/> sequence into the output <see cref="byte"/> sequence. Before
        /// calling this method, ensure that the output array has sufficient capacity
        /// by calling <see cref="GetDecodedLength(char[], int, int)"/>.
        /// </summary>
        /// <param name="inputArray"> <see cref="char"/> sequence to be decoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of chars in <paramref name="inputArray"/> </param>
        /// <param name="outputArray"> <see cref="byte"/> sequence to store encoded result </param>
        /// <param name="outputOffset"> Initial offset into outputArray </param>
        /// <param name="outputLength"> Length of output, must be
        ///        GetDecodedLength(inputArray, inputOffset, inputLength) </param>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Decode(char[] inputArray, int inputOffset, int inputLength, byte[] outputArray, int outputOffset, int outputLength)
        {
            Decode(inputArray, inputOffset, inputLength, (sbyte[])(Array)outputArray, outputOffset, outputLength);
        }

        /// <summary>
        /// Decodes the input char sequence into the output sbyte sequence. Before
        /// calling this method, ensure that the output array has sufficient capacity
        /// by calling <see cref="GetDecodedLength(char[], int, int)"/>.
        /// </summary>
        /// <param name="inputArray"> <see cref="char"/> sequence to be decoded </param>
        /// <param name="inputOffset"> Initial offset into <paramref name="inputArray"/> </param>
        /// <param name="inputLength"> Number of chars in <paramref name="inputArray"/> </param>
        /// <param name="outputArray"> <see cref="byte"/> sequence to store encoded result </param>
        /// <param name="outputOffset"> Initial offset into outputArray </param>
        /// <param name="outputLength"> Length of output, must be
        ///        GetDecodedLength(inputArray, inputOffset, inputLength) </param>
        [CLSCompliant(false)]
        public static void Decode(char[] inputArray, int inputOffset, int inputLength, sbyte[] outputArray, int outputOffset, int outputLength)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(outputLength == GetDecodedLength(inputArray, inputOffset, inputLength));
            int numInputChars = inputLength - 1;
            int numOutputBytes = outputLength;

            if (numOutputBytes > 0)
            {
                int caseNum = 0;
                int outputByteNum = outputOffset;
                int inputCharNum = inputOffset;
                short inputChar;
                CodingCase codingCase;
                for (; inputCharNum < numInputChars - 1; ++inputCharNum)
                {
                    codingCase = CODING_CASES[caseNum];
                    inputChar = (short)inputArray[inputCharNum];
                    if (2 == codingCase.numBytes)
                    {
                        if (0 == caseNum)
                        {
                            outputArray[outputByteNum] = (sbyte)(inputChar.TripleShift(codingCase.initialShift));
                        }
                        else
                        {
                            outputArray[outputByteNum] += (sbyte)(inputChar.TripleShift(codingCase.initialShift));
                        }
                        outputArray[outputByteNum + 1] = (sbyte)((inputChar & codingCase.finalMask) << codingCase.finalShift);
                    } // numBytes is 3
                    else
                    {
                        outputArray[outputByteNum] += (sbyte)(inputChar.TripleShift(codingCase.initialShift));
                        outputArray[outputByteNum + 1] = (sbyte)((inputChar & codingCase.middleMask).TripleShift(codingCase.middleShift));
                        outputArray[outputByteNum + 2] = (sbyte)((inputChar & codingCase.finalMask) << codingCase.finalShift);
                    }
                    outputByteNum += codingCase.advanceBytes;
                    if (++caseNum == CODING_CASES.Length)
                    {
                        caseNum = 0;
                    }
                }
                // Handle final char
                inputChar = (short)inputArray[inputCharNum];
                codingCase = CODING_CASES[caseNum];
                if (0 == caseNum)
                {
                    outputArray[outputByteNum] = 0;
                }
                outputArray[outputByteNum] += (sbyte)(inputChar.TripleShift(codingCase.initialShift));
                int bytesLeft = numOutputBytes - outputByteNum;
                if (bytesLeft > 1)
                {
                    if (2 == codingCase.numBytes)
                    {
                        outputArray[outputByteNum + 1] = (sbyte)((inputChar & codingCase.finalMask).TripleShift(codingCase.finalShift));
                    } // numBytes is 3
                    else
                    {
                        outputArray[outputByteNum + 1] = (sbyte)((inputChar & codingCase.middleMask).TripleShift(codingCase.middleShift));
                        if (bytesLeft > 2)
                        {
                            outputArray[outputByteNum + 2] = (sbyte)((inputChar & codingCase.finalMask) << codingCase.finalShift);
                        }
                    }
                }
            }
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
                this.finalMask = /*(short)*/((short)0xFF.TripleShift(finalShift)); // LUCENENET: Removed unnecessary cast
                this.middleMask = (short)(/*(short)*/0xFF << middleShift); // LUCENENET: Removed unnecessary cast
            }

            internal CodingCase(int initialShift, int finalShift)
            {
                this.numBytes = 2;
                this.initialShift = initialShift;
                this.finalShift = finalShift;
                this.finalMask = /*(short)*/((short)0xFF.TripleShift(finalShift)); // LUCENENET: Removed unnecessary cast
                if (finalShift != 0)
                {
                    advanceBytes = 1;
                }
            }
        }
    }
}