using Lucene.Net.Index;
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
    /// Field that stores a per-document <code>long</code> value for scoring,
    /// sorting or value retrieval. Here's an example usage:
    ///
    /// <pre class="prettyprint">
    ///   document.add(new NumericDocValuesField(name, 22L));
    /// </pre>
    ///
    /// If you also need to store the value, you should add a
    /// separate <seealso cref="StoredField"/> instance.
    /// </summary>
    public class NumericDocValuesField : Field
    {
        /// <summary>
        /// Type for numeric DocValues.
        /// </summary>
        public static readonly FieldType TYPE = new FieldType();

        static NumericDocValuesField()
        {
            TYPE.DocValueType = DocValuesType.NUMERIC;
            TYPE.Freeze();
        }

        /// <summary>
        /// Creates a new DocValues field with the specified 64-bit long value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit long value </param>
        /// <exception cref="ArgumentNullException"> if the field name is null </exception>
        public NumericDocValuesField(string name, long value)
            : base(name, TYPE)
        {
            m_fieldsData = Convert.ToInt64(value);
        }
    }
}