// lucene version compatibility level: 4.8.1
using ICU4N.Text;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Collation
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
    /// Indexes sort keys as a single-valued <see cref="SortedDocValuesField"/>.
    /// </summary>
    /// <remarks>
    /// This is more efficient that <see cref="ICUCollationKeyAnalyzer"/> if the field 
    /// only has one value: no uninversion is necessary to sort on the field, 
    /// locale-sensitive range queries can still work via <see cref="Search.FieldCacheRangeFilter"/>, 
    /// and the underlying data structures built at index-time are likely more efficient 
    /// and use less memory than FieldCache.
    /// </remarks>
    [ExceptionToClassNameConvention]
    public sealed class ICUCollationDocValuesField : Field
    {
        private readonly string name;
        internal readonly Collator collator; // LUCENENET: marked internal for testing
        private readonly BytesRef bytes = new BytesRef();
        private RawCollationKey key = new RawCollationKey();

        /// <summary>
        /// Create a new <see cref="ICUCollationDocValuesField"/>.
        /// <para/>
        /// NOTE: you should not create a new one for each document, instead
        /// just make one and reuse it during your indexing process, setting
        /// the value via <see cref="SetStringValue(string)"/>.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="collator">Collator for generating collation keys.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="name"/> or <paramref name="collator"/> is <c>null</c>.</exception>
        // TODO: can we make this trap-free? maybe just synchronize on the collator
        // instead? 
        public ICUCollationDocValuesField(string name, Collator collator)
            : base(name, SortedDocValuesField.TYPE)
        {
            if (collator is null)
                throw new ArgumentNullException(nameof(collator));

            this.name = name;
            this.collator = (Collator)collator.Clone();
            FieldsData = bytes; // so wrong setters cannot be called
        }

        public override string Name => name;

        public override void SetStringValue(string value)
        {
            key = collator.GetRawCollationKey(value, key);
            bytes.Bytes = key.Bytes;
            bytes.Offset = 0;
            bytes.Length = key.Length;
        }
    }
}
