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
    /// a set of per-document <seealso cref="BytesRef"/> values, indexed for
    /// faceting,grouping,joining.  Here's an example usage:
    ///
    /// <pre class="prettyprint">
    ///   document.add(new SortedSetDocValuesField(name, new BytesRef("hello")));
    ///   document.add(new SortedSetDocValuesField(name, new BytesRef("world")));
    /// </pre>
    ///
    /// <p>
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.
    ///
    /// </summary>
    public class SortedSetDocValuesField : Field
    {
        /// <summary>
        /// Type for sorted bytes DocValues
        /// </summary>
        public static readonly FieldType TYPE = new FieldType();

        static SortedSetDocValuesField()
        {
            TYPE.DocValueType = DocValuesType.SORTED_SET;
            TYPE.Freeze();
        }

        /// <summary>
        /// Create a new sorted DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public SortedSetDocValuesField(string name, BytesRef bytes)
            : base(name, TYPE)
        {
            fieldsData = bytes;
        }
    }
}