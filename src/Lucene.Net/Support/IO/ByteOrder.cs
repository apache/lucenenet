// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;

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
    /// Defines byte order constants.
    /// </summary>
    public sealed class ByteOrder
    {
        /// <summary>
        /// This constant represents big endian.
        /// </summary>
        public static readonly ByteOrder BIG_ENDIAN = new ByteOrder("BIG_ENDIAN"); //$NON-NLS-1$

        /// <summary>
        /// This constant represents little endian.
        /// </summary>
        public static readonly ByteOrder LITTLE_ENDIAN = new ByteOrder("LITTLE_ENDIAN"); //$NON-NLS-1$

        private static readonly ByteOrder NATIVE_ORDER = LoadNativeOrder();

        private static ByteOrder LoadNativeOrder() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            // Read endianness from the current system.
            return BitConverter.IsLittleEndian ? LITTLE_ENDIAN : BIG_ENDIAN;
        }

        /// <summary>
        /// Returns the current platform byte order.
        /// </summary>
        public static ByteOrder NativeOrder
        {
            get { return NATIVE_ORDER; }
        }

        private readonly string name;

        private ByteOrder(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Returns a string that describes this object.
        /// </summary>
        /// <returns>
        /// "BIG_ENDIAN" for <see cref="ByteOrder.BIG_ENDIAN"/> objects,
        /// "LITTLE_ENDIAN" for <see cref="ByteOrder.LITTLE_ENDIAN"/> objects.
        /// </returns>
        public override string ToString()
        {
            return name;
        }
    }
}
