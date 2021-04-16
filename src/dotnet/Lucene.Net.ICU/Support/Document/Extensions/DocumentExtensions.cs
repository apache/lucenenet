using ICU4N.Text;
using Lucene.Net.Collation;
using System;

namespace Lucene.Net.Documents.Extensions
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
    /// LUCENENET specific extensions to the <see cref="Document"/> class.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Adds a new <see cref="ICUCollationDocValuesField"/>.
        /// <para/>
        /// NOTE: you should not create a new one for each document, instead
        /// just make one and reuse it during your indexing process, setting
        /// the value via <see cref="ICUCollationDocValuesField.SetStringValue(string)"/>.
        /// </summary>
        /// <param name="document">This <see cref="Document"/>.</param>
        /// <param name="name">Field name.</param>
        /// <param name="collator">Collator for generating collation keys.</param>
        /// <returns>The field that was added to this <see cref="Document"/>.</returns>
        /// <exception cref="ArgumentNullException">This <paramref name="document"/>, <paramref name="name"/> or <paramref name="collator"/> is <c>null</c>. </exception>
        public static ICUCollationDocValuesField AddICUCollationDocValuesField(this Document document, string name, Collator collator)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            var field = new ICUCollationDocValuesField(name, collator);
            document.Add(field);
            return field;
        }
    }
}
