using Lucene.Net.Index;
using System;
using Int64 = J2N.Numerics.Int64;

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
    /// Field that stores a per-document <see cref="long"/> value for scoring,
    /// sorting or value retrieval. Here's an example usage:
    ///
    /// <code>
    ///     document.Add(new NumericDocValuesField(name, 22L));
    /// </code>
    ///
    /// If you also need to store the value, you should add a
    /// separate <see cref="StoredField"/> instance.
    /// </summary>
    public class NumericDocValuesField : Field
    {
        /// <summary>
        /// Type for numeric <see cref="DocValues"/>.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE = new FieldType
        {
            DocValueType = DocValuesType.NUMERIC
        }.Freeze();

        /// <summary>
        /// Creates a new <see cref="DocValues"/> field with the specified 64-bit <see cref="long"/> value </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="long"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c> </exception>
        public NumericDocValuesField(string name, long value)
            : base(name, TYPE)
        {
            FieldsData = Int64.GetInstance(value);
        }
    }
}