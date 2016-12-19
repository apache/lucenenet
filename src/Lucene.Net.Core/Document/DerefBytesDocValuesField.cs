using Lucene.Net.Util;
using System;

namespace Lucene.Net.Documents
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
    /// Field that stores
    /// a per-document <seealso cref="BytesRef"/> value. Here's an example usage:
    ///
    /// <pre class="prettyprint">
    ///   document.add(new DerefBytesDocValuesField(name, new BytesRef("hello")));
    /// </pre>
    ///
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.
    /// </summary>
    /// <seealso cref="BinaryDocValues"/>
    [Obsolete("Use BinaryDocValuesField instead.")]
    public class DerefBytesDocValuesField : BinaryDocValuesField
    {
        /// <summary>
        /// Type for bytes DocValues: all with the same length
        /// </summary>
        public static readonly FieldType TYPE_FIXED_LEN = BinaryDocValuesField.fType;

        /// <summary>
        /// Type for bytes DocValues: can have variable lengths
        /// </summary>
        public static readonly FieldType TYPE_VAR_LEN = BinaryDocValuesField.fType;

        /// <summary>
        /// Create a new fixed or variable-length DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public DerefBytesDocValuesField(string name, BytesRef bytes)
            : base(name, bytes)
        {
        }

        /// <summary>
        /// Create a new fixed or variable length DocValues field.
        /// <p> </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <param name="isFixedLength"> (ignored) </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public DerefBytesDocValuesField(string name, BytesRef bytes, bool isFixedLength)
            : base(name, bytes)
        {
        }
    }
}