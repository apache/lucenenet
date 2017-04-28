// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

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
    /// Equivalent to Java's DataInput interface
    /// </summary>
    public interface IDataInput
    {
        void ReadFully(byte[] b);
        void ReadFully(byte[] b, int off, int len);
        int SkipBytes(int n);
        bool ReadBoolean();

        /// <summary>
        /// NOTE: This was readByte() in Java
        /// </summary>
        int ReadSByte();

        /// <summary>
        /// NOTE: This was readUnsignedByte() in Java
        /// </summary>
        int ReadByte();

        /// <summary>
        /// NOTE: This was readShort() in Java
        /// </summary>
        short ReadInt16();

        /// <summary>
        /// NOTE: This was readUnsignedShort() in Java
        /// </summary>
        int ReadUInt16();
        char ReadChar();

        /// <summary>
        /// NOTE: This was readInt() in Java
        /// </summary>
        int ReadInt32();

        /// <summary>
        /// NOTE: This was readLong() in Java
        /// </summary>
        long ReadInt64();
        float ReadSingle();
        double ReadDouble();
        string ReadLine();
        string ReadUTF();
    }
}
