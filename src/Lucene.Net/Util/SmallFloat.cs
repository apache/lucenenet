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
    /// Floating point numbers smaller than 32 bits.
    /// <para/>
    /// NOTE: This was SmallFloat in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public static class SmallSingle // LUCENENET specific - made static
    {
        /// <summary>
        /// Converts a 32 bit <see cref="float"/> to an 8 bit <see cref="float"/>.
        /// <para/>Values less than zero are all mapped to zero.
        /// <para/>Values are truncated (rounded down) to the nearest 8 bit value.
        /// <para/>Values between zero and the smallest representable value
        /// are rounded up.
        /// </summary>
        /// <param name="f"> The 32 bit <see cref="float"/> to be converted to an 8 bit <see cref="float"/> (<see cref="byte"/>).  </param>
        /// <param name="numMantissaBits"> The number of mantissa bits to use in the byte, with the remainder to be used in the exponent. </param>
        /// <param name="zeroExp"> The zero-point in the range of exponent values. </param>
        /// <returns> The 8 bit float representation. </returns>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SingleToByte(float f, int numMantissaBits, int zeroExp)
        {
            return (byte)SingleToSByte(f, numMantissaBits, zeroExp);
        }

        /// <summary>
        /// Converts a 32 bit <see cref="float"/> to an 8 bit <see cref="float"/>.
        /// <para/>Values less than zero are all mapped to zero.
        /// <para/>Values are truncated (rounded down) to the nearest 8 bit value.
        /// <para/>Values between zero and the smallest representable value
        /// are rounded up.
        /// <para/>
        /// NOTE: This was floatToByte() in Lucene
        /// </summary>
        /// <param name="f"> The 32 bit <see cref="float"/> to be converted to an 8 bit <see cref="float"/> (<see cref="sbyte"/>). </param>
        /// <param name="numMantissaBits"> The number of mantissa bits to use in the byte, with the remainder to be used in the exponent. </param>
        /// <param name="zeroExp"> The zero-point in the range of exponent values. </param>
        /// <returns> The 8 bit float representation. </returns>
        [CLSCompliant(false)]
        public static sbyte SingleToSByte(float f, int numMantissaBits, int zeroExp)
        {
            // Adjustment from a float zero exponent to our zero exponent,
            // shifted over to our exponent position.
            int fzero = (63 - zeroExp) << numMantissaBits;
            int bits = J2N.BitConversion.SingleToRawInt32Bits(f);
            int smallfloat = bits >> (24 - numMantissaBits);
            if (smallfloat <= fzero)
            {
                return (bits <= 0) ? (sbyte)0 : (sbyte)1; // underflow is mapped to smallest non-zero number. -  negative numbers and zero both map to 0 byte
            }
            else if (smallfloat >= fzero + 0x100)
            {
                return -1; // overflow maps to largest number
            }
            else
            {
                return (sbyte)(smallfloat - fzero);
            }
        }

        /// <summary>
        /// Converts an 8 bit <see cref="float"/> to a 32 bit <see cref="float"/>. 
        /// <para/>
        /// NOTE: This was byteToFloat() in Lucene
        /// </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ByteToSingle(byte b, int numMantissaBits, int zeroExp)
        {
            return SByteToSingle((sbyte)b, numMantissaBits, zeroExp);
        }

        /// <summary>
        /// Converts an 8 bit <see cref="float"/> to a 32 bit <see cref="float"/>. 
        /// <para/>
        /// NOTE: This was byteToFloat() in Lucene
        /// </summary>
        [CLSCompliant(false)]
        public static float SByteToSingle(sbyte b, int numMantissaBits, int zeroExp)
        {
            // on Java1.5 & 1.6 JVMs, prebuilding a decoding array and doing a lookup
            // is only a little bit faster (anywhere from 0% to 7%)
            if (b == 0)
            {
                return 0.0f;
            }
            int bits = (b & 0xff) << (24 - numMantissaBits);
            bits += (63 - zeroExp) << 24;
            return J2N.BitConversion.Int32BitsToSingle(bits);
        }

        //
        // Some specializations of the generic functions follow.
        // The generic functions are just as fast with current (1.5)
        // -server JVMs, but still slower with client JVMs.
        //

        /// <summary>
        /// SingleToSByte((byte)b, mantissaBits=3, zeroExponent=15)
        /// <para/>smallest non-zero value = 5.820766E-10
        /// <para/>largest value = 7.5161928E9
        /// <para/>epsilon = 0.125
        /// <para/>
        /// NOTE: This was floatToByte315() in Lucene
        /// </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SingleToByte315(float f)
        {
            return (byte)SingleToSByte315(f);
        }

        /// <summary>
        /// SingleToSByte(b, mantissaBits=3, zeroExponent=15)
        /// <para/>smallest non-zero value = 5.820766E-10
        /// <para/>largest value = 7.5161928E9
        /// <para/>epsilon = 0.125
        /// <para/>
        /// NOTE: This was floatToByte315() in Lucene
        /// </summary>
        [CLSCompliant(false)]
        public static sbyte SingleToSByte315(float f) 
        {
            int bits = J2N.BitConversion.SingleToRawInt32Bits(f);
            int smallfloat = bits >> (24 - 3);
            if (smallfloat <= ((63 - 15) << 3))
            {
                return (bits <= 0) ? (sbyte)0 : (sbyte)1;
            }
            if (smallfloat >= ((63 - 15) << 3) + 0x100)
            {
                return -1;
            }
            return (sbyte)(smallfloat - ((63 - 15) << 3));
        }

        /// <summary>
        /// ByteToSingle(b, mantissaBits=3, zeroExponent=15) 
        /// <para/>
        /// NOTE: This was byte315ToFloat() in Lucene
        /// </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Byte315ToSingle(byte b)
        {
            return SByte315ToSingle((sbyte)b);
        }

        /// <summary>
        /// SByteToSingle(b, mantissaBits=3, zeroExponent=15) 
        /// <para/>
        /// NOTE: This was byte315ToFloat() in Lucene
        /// </summary>
        [CLSCompliant(false)]
        public static float SByte315ToSingle(sbyte b)
        {
            // on Java1.5 & 1.6 JVMs, prebuilding a decoding array and doing a lookup
            // is only a little bit faster (anywhere from 0% to 7%)
            if (b == 0)
            {
                return 0.0f;
            }
            int bits = (b & 0xff) << (24 - 3);
            bits += (63 - 15) << 24;
            return J2N.BitConversion.Int32BitsToSingle(bits);
        }

        /// <summary>
        /// SingleToByte(b, mantissaBits=5, zeroExponent=2)
        /// <para/>smallest nonzero value = 0.033203125
        /// <para/>largest value = 1984.0
        /// <para/>epsilon = 0.03125
        /// <para/>
        /// NOTE: This was floatToByte52() in Lucene
        /// </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte SingleToByte52(float f)
        {
            return (byte)SingleToSByte315(f);
        }

        /// <summary>
        /// SingleToSByte(b, mantissaBits=5, zeroExponent=2)
        /// <para/>smallest nonzero value = 0.033203125
        /// <para/>largest value = 1984.0
        /// <para/>epsilon = 0.03125
        /// <para/>
        /// NOTE: This was floatToByte52() in Lucene
        /// </summary>
        [CLSCompliant(false)]
        public static sbyte SingleToSByte52(float f)
        {
            int bits = J2N.BitConversion.SingleToRawInt32Bits(f);
            int smallfloat = bits >> (24 - 5);
            if (smallfloat <= (63 - 2) << 5)
            {
                return (bits <= 0) ? (sbyte)0 : (sbyte)1;
            }
            if (smallfloat >= ((63 - 2) << 5) + 0x100)
            {
                return -1;
            }
            return (sbyte)(smallfloat - ((63 - 2) << 5));
        }

        /// <summary>
        /// ByteToFloat(b, mantissaBits=5, zeroExponent=2) 
        /// <para/>
        /// NOTE: This was byte52ToFloat() in Lucene
        /// </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Byte52ToSingle(byte b)
        {
            return SByte52ToSingle((sbyte)b);
        }

        /// <summary>
        /// SByteToFloat(b, mantissaBits=5, zeroExponent=2) 
        /// <para/>
        /// NOTE: This was byte52ToFloat() in Lucene
        /// </summary>
        [CLSCompliant(false)]
        public static float SByte52ToSingle(sbyte b)
        {
            // on Java1.5 & 1.6 JVMs, prebuilding a decoding array and doing a lookup
            // is only a little bit faster (anywhere from 0% to 7%)
            if (b == 0)
            {
                return 0.0f;
            }
            int bits = (b & 0xff) << (24 - 5);
            bits += (63 - 2) << 24;
            return J2N.BitConversion.Int32BitsToSingle(bits);
        }
    }
}