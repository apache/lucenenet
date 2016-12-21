using Lucene.Net.Index;
using Lucene.Net.Util;

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
    ///   document.add(new SortedDocValuesField(name, new BytesRef("hello")));
    /// </pre></p>
    ///
    /// <p>
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.</p>
    /// </summary>
    public class SortedDocValuesField : Field
    {
        /// <summary>
        /// Type for sorted bytes DocValues
        /// </summary>
        public static readonly FieldType TYPE = new FieldType();

        static SortedDocValuesField()
        {
            TYPE.DocValueType = DocValuesType.SORTED;
            TYPE.Freeze();
        }

        /// <summary>
        /// Create a new sorted DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <exception cref="ArgumentNullException"> if the field name is null </exception>
        public SortedDocValuesField(string name, BytesRef bytes)
            : base(name, TYPE)
        {
            fieldsData = bytes;
        }
    }
}