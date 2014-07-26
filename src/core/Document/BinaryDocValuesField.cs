namespace Lucene.Net.Document
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;

    /// <summary>
    /// Field that stores a per-document <seealso cref="BytesRef"/> value.
    /// <p>
    /// The values are stored directly with no sharing, which is a good fit when
    /// the fields don't share (many) values, such as a title field.  If values
    /// may be shared and sorted it's better to use <seealso cref="SortedDocValuesField"/>.
    /// Here's an example usage:
    ///
    /// <pre class="prettyprint">
    ///   document.add(new BinaryDocValuesField(name, new BytesRef("hello")));
    /// </pre>
    ///
    /// <p>
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.
    /// </summary>
    /// <seealso cref= BinaryDocValues
    ///  </seealso>
    public class BinaryDocValuesField : Field
    {
        /// <summary>
        /// Type for straight bytes DocValues.
        /// </summary>
        public static readonly FieldType fType = new FieldType();

        static BinaryDocValuesField()
        {
            fType.DocValueType = FieldInfo.DocValuesType_e.BINARY;
            fType.Freeze();
        }

        /// <summary>
        /// Create a new binary DocValues field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> binary content </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        public BinaryDocValuesField(string name, BytesRef value)
            : base(name, fType)
        {
            FieldsData = value;
        }
    }
}