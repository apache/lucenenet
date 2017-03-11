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
    /// <para>
    /// Field that stores a per-document <see cref="long"/> value for scoring,
    /// sorting or value retrieval. Here's an example usage:
    ///
    /// <code>
    ///     document.Add(new Int64DocValuesField(name, 22L));
    /// </code></para>
    ///
    /// <para>
    /// If you also need to store the value, you should add a
    /// separate <see cref="StoredField"/> instance.</para>
    /// <para>
    /// NOTE: This was LongDocValuesField in Lucene
    /// </para>
    /// </summary>
    /// <seealso cref="NumericDocValuesField"/>
    [Obsolete("Use NumericDocValuesField instead")]
    public class Int64DocValuesField : NumericDocValuesField
    {
        /// <summary>
        /// Creates a new DocValues field with the specified 64-bit <see cref="long"/> value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="long"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c> </exception>
        public Int64DocValuesField(string name, long value)
            : base(name, value)
        {
        }
    }
}