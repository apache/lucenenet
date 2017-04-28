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
    /// Equivalent to Java's DataOutut interface
    /// </summary>
    public interface IDataOutput
    {
        void Write(byte[] buffer);

        void Write(byte[] buffer, int offset, int count);

        void Write(int oneByte);
        
        void WriteBoolean(bool val);

        void WriteByte(int val);

        void WriteBytes(string str);

        void WriteChar(int val);

        void WriteChars(string str);

        void WriteDouble(double val);

        /// <summary>
        /// NOTE: This was writeFloat() in Java
        /// </summary>
        void WriteSingle(float val);

        /// <summary>
        /// NOTE: This was writeInt() in Java
        /// </summary>
        void WriteInt32(int val);

        /// <summary>
        /// NOTE: This was writeInt64() in Java
        /// </summary>
        void WriteInt64(long val);

        /// <summary>
        /// NOTE: This was writeShort() in Java
        /// </summary>
        void WriteInt16(int val);
        
        void WriteUTF(string str);
    }
}
