using J2N.Numerics;
using System.Globalization;
using System.Text;
using Float = J2N.Numerics.Single;

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
    /// Helper methods to ease implementing <see cref="object.ToString()"/>.
    /// </summary>
    public static class ToStringUtils // LUCENENET specific - made static
    {
        /// <summary>
        /// For printing boost only if not 1.0.
        /// </summary>
        public static string Boost(float boost)
        {
            if (boost != 1.0f)
            {
                // .NET compatibility fix
                return "^" + Float.ToString(boost, CultureInfo.InvariantCulture);
            }
            else
                return "";
        }

        public static void ByteArray(StringBuilder buffer, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                buffer.Append("b[").Append(i).Append("]=").Append(bytes[i]);
                if (i < bytes.Length - 1)
                {
                    buffer.Append(',');
                }
            }
        }

        private static readonly char[] HEX = "0123456789abcdef".ToCharArray();

        /// <summary>
        /// NOTE: This was longHex() in Lucene
        /// </summary>
        public static string Int64Hex(long x)
        {
            char[] asHex = new char[16];
            for (int i = 16; --i >= 0; x = x.TripleShift(4))
            {
                asHex[i] = HEX[(int)x & 0x0F];
            }
            return "0x" + new string(asHex);
        }
    }
}