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
    /// Field that stores a per-document <see cref="int"/> value for scoring,
    /// sorting or value retrieval. Here's an example usage:
    ///
    /// <code>
    ///     document.Add(new Int32DocValuesField(name, 22));
    /// </code>
    ///
    /// If you also need to store the value, you should add a
    /// separate <see cref="StoredField"/> instance. 
    /// <para/>
    /// NOTE: This was IntDocValuesField in Lucene
    /// </summary>
    /// <seealso cref="NumericDocValuesField"/>
    [Obsolete("Deprecated, use NumericDocValuesField instead")]
    public class Int32DocValuesField : NumericDocValuesField
    {
        /// <summary>
        /// Creates a new DocValues field with the specified 32-bit <see cref="int"/> value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="int"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c> </exception>
        public Int32DocValuesField(string name, int value)
            : base(name, value)
        {
        }

        public override void SetInt32Value(int value)
        {
            SetInt64Value(value);
        }
    }
}