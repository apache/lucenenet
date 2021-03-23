// Lucene version compatibility level 4.8.1
namespace Lucene.Net.Analysis.Payloads
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
    /// Utility methods for encoding payloads.
    /// </summary>
    public static class PayloadHelper // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// NOTE: This was encodeFloat() in Lucene
        /// </summary>
        public static byte[] EncodeSingle(float payload)
        {
            return EncodeSingle(payload, new byte[4], 0);
        }

        /// <summary>
        /// NOTE: This was encodeFloat() in Lucene
        /// </summary>
        public static byte[] EncodeSingle(float payload, byte[] data, int offset)
        {
            return EncodeInt32(J2N.BitConversion.SingleToInt32Bits(payload), data, offset);
        }

        /// <summary>
        /// NOTE: This was encodeInt() in Lucene
        /// </summary>
        public static byte[] EncodeInt32(int payload)
        {
            return EncodeInt32(payload, new byte[4], 0);
        }

        /// <summary>
        /// NOTE: This was encodeInt() in Lucene
        /// </summary>
        public static byte[] EncodeInt32(int payload, byte[] data, int offset)
        {
            data[offset] = (byte)(payload >> 24);
            data[offset + 1] = (byte)(payload >> 16);
            data[offset + 2] = (byte)(payload >> 8);
            data[offset + 3] = (byte)payload;
            return data;
        }

        /// <summary>
        /// NOTE: This was decodeFloat() in Lucene
        /// </summary>
        /// <seealso cref="DecodeSingle(byte[], int)"/>
        /// <seealso cref="EncodeSingle(float)"/>
        /// <returns> the decoded float </returns>
        public static float DecodeSingle(byte[] bytes)
        {
            return DecodeSingle(bytes, 0);
        }

        /// <summary>
        /// Decode the payload that was encoded using <see cref="EncodeSingle(float)"/>.
        /// NOTE: the length of the array must be at least offset + 4 long. 
        /// <para/>
        /// NOTE: This was decodeFloat() in Lucene
        /// </summary>
        /// <param name="bytes"> The bytes to decode </param>
        /// <param name="offset"> The offset into the array. </param>
        /// <returns> The float that was encoded
        /// </returns>
        /// <seealso cref="EncodeSingle(float)"/>
        public static float DecodeSingle(byte[] bytes, int offset)
        {

            return J2N.BitConversion.Int32BitsToSingle(DecodeInt32(bytes, offset));
        }

        /// <summary>
        /// NOTE: This was decodeInt() in Lucene
        /// </summary>
        public static int DecodeInt32(byte[] bytes, int offset)
        {
            return ((bytes[offset] & 0xFF) << 24) | ((bytes[offset + 1] & 0xFF) << 16) | ((bytes[offset + 2] & 0xFF) << 8) | (bytes[offset + 3] & 0xFF);
        }
    }
}