using System.IO;

namespace Lucene.Net.Support.IO
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
    /// Extension methods that make a <see cref="Stream"/> effectively into a
    /// binary serializer with no encoding. We simply convert types into bytes
    /// and write them without any concern whether surrogate pairs are respected,
    /// similar to what BinaryFormatter does.
    /// This makes it possible to serialize/deserialize raw character arrays
    /// and get the data back in the same order without any exceptions warning
    /// that the order is not valid and without the need for BinaryFormatter.
    /// <para/>
    /// Byte order is little-endian (same as <see cref="BinaryReader"/> and <see cref="BinaryWriter"/>).
    /// </summary>
    internal static class StreamExtensions
    {
        public static void Write(this Stream stream, char[] chars)
        {
            byte[] newBytes = new byte[chars.Length * 2];
            for (int index = 0; index < chars.Length; index++)
            {
                int newIndex = index == 0 ? index : index * 2;
                newBytes[newIndex] = (byte)chars[index];
                newBytes[newIndex + 1] = (byte)(chars[index] >> 8);
            }
            stream.Write(newBytes, 0, newBytes.Length);
        }

        public static char[] ReadChars(this Stream stream, int count)
        {
            byte[] buff = new byte[2];
            char[] newChars = new char[count];
            for (int i = 0; i < count; i++)
            {
                stream.Read(buff, 0, 2);
                newChars[i] = (char)((buff[0] & 0xff) | ((buff[1] & 0xff) << 8));
            }
            return newChars;
        }

        public static void Write(this Stream stream, int value)
        {
            byte[] buff = new byte[4];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            stream.Write(buff, 0, buff.Length);
        }

        public static int ReadInt32(this Stream stream)
        {
            byte[] buff = new byte[4];
            stream.Read(buff, 0, buff.Length);
            return (buff[0] & 0xff) | ((buff[1] & 0xff) << 8) |
                ((buff[2] & 0xff) << 16) | ((buff[3] & 0xff) << 24);
        }

        public static void Write(this Stream stream, long value)
        {
            byte[] buff = new byte[8];
            buff[0] = (byte)value;
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            buff[4] = (byte)(value >> 32);
            buff[5] = (byte)(value >> 40);
            buff[6] = (byte)(value >> 48);
            buff[7] = (byte)(value >> 56);
            stream.Write(buff, 0, buff.Length);
        }

        public static long ReadInt64(this Stream stream)
        {
            byte[] buff = new byte[8];
            stream.Read(buff, 0, buff.Length);
            uint lo = (uint)(buff[0] | buff[1] << 8 |
                             buff[2] << 16 | buff[3] << 24);
            uint hi = (uint)(buff[4] | buff[5] << 8 |
                             buff[6] << 16 | buff[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }
    }
}
