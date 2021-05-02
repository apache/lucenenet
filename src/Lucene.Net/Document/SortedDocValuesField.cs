using Lucene.Net.Index;
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
    /// <para>
    /// Field that stores
    /// a per-document <see cref="BytesRef"/> value, indexed for
    /// sorting.  Here's an example usage:
    ///
    /// <code>
    ///     document.Add(new SortedDocValuesField(name, new BytesRef("hello")));
    /// </code></para>
    ///
    /// <para>
    /// If you also need to store the value, you should add a
    /// separate <see cref="StoredField"/> instance.</para>
    /// </summary>
    public class SortedDocValuesField : Field
    {
        /// <summary>
        /// Type for sorted bytes <see cref="DocValues"/>
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE = new FieldType
        {
            DocValueType = DocValuesType.SORTED
        }.Freeze();

        /// <summary>
        /// Create a new sorted <see cref="DocValues"/> field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="bytes"> binary content </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c> </exception>
        public SortedDocValuesField(string name, BytesRef bytes)
            : base(name, TYPE)
        {
            FieldsData = bytes;
        }
    }
}