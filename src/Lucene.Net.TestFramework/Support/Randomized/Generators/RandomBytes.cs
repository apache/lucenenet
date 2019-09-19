using System;

namespace Lucene.Net.Randomized.Generators
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
    /// Random byte sequence generators.
    /// </summary>
    public static class RandomBytes
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">Random generator.</param>
        /// <param name="length">The length of the byte array. Can be zero.</param>
        /// <returns>Returns a byte array with random content.</returns>
        public static byte[] RandomBytesOfLength(Random r, int length)
        {
            return RandomBytesOfLengthBetween(r, length, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="r">Random generator.</param>
        /// <param name="minLength">The minimum length of the byte array. Can be zero.</param>
        /// <param name="maxLength">The maximum length of the byte array. Can be zero.</param>
        /// <returns>Returns a byte array with random content.</returns>
        public static byte[] RandomBytesOfLengthBetween(Random r, int minLength, int maxLength)
        {
            byte[] bytes = new byte[RandomInts.RandomInt32Between(r, minLength, maxLength)];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)r.Next();
            }
            return bytes;
        }
    }
}
