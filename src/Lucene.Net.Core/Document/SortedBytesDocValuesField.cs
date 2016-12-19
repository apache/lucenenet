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
    /// <p>
    /// Field that stores
    /// a per-document <seealso cref="BytesRef"/> value, indexed for
    /// sorting.  Here's an example usage:
    ///
    /// <pre class="prettyprint">
    ///   document.add(new SortedBytesDocValuesField(name, new BytesRef("hello")));
    /// </pre>
    ///
    /// <p>
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.
    /// </summary>
    /// <seealso cref= SortedDocValues </seealso>
    /// @deprecated Use <seealso cref="SortedDocValuesField"/> instead.
    [Obsolete("Use SortedDocValuesField instead.")]
    public class SortedBytesDocValuesField : SortedDocValuesField
    {
        /// <summary>
        /// Type for sorted bytes DocValues: all with the same length
        /// </summary>
        public static readonly FieldType TYPE_FIXED_LEN = SortedDocValuesField.TYPE;

        /// <summary>
        /// Type for sorted bytes DocValues: can have variable lengths
        /// </summary>
        public static readonly FieldType TYPE_VAR_LEN = SortedDocValuesField.TYPE;

        /// <summary>
        /// Create a new fixed or variable-length sorted DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public SortedBytesDocValuesField(string name, BytesRef bytes)
            : base(name, bytes)
        {
        }

        /// <summary>
        /// Create a new fixed or variable length sorted DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <param name="isFixedLength"> (ignored) </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public SortedBytesDocValuesField(string name, BytesRef bytes, bool isFixedLength)
            : base(name, bytes)
        {
        }
    }
}