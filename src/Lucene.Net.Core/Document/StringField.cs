using Lucene.Net.Index;

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
    /// A field that is indexed but not tokenized: the entire
    ///  String value is indexed as a single token.  For example
    ///  this might be used for a 'country' field or an 'id'
    ///  field, or any field that you intend to use for sorting
    ///  or access through the field cache.
    /// </summary>
    public sealed class StringField : Field
    {
        /// <summary>
        /// Indexed, not tokenized, omits norms, indexes
        ///  DOCS_ONLY, not stored.
        /// </summary>
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();

        /// <summary>
        /// Indexed, not tokenized, omits norms, indexes
        ///  DOCS_ONLY, stored
        /// </summary>
        public static readonly FieldType TYPE_STORED = new FieldType();

        static StringField()
        {
            TYPE_NOT_STORED.IsIndexed = true;
            TYPE_NOT_STORED.OmitNorms = true;
            TYPE_NOT_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_NOT_STORED.IsTokenized = false;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.IsIndexed = true;
            TYPE_STORED.OmitNorms = true;
            TYPE_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_STORED.IsStored = true;
            TYPE_STORED.IsTokenized = false;
            TYPE_STORED.Freeze();
        }

        /// <summary>
        /// Creates a new StringField (a field that is indexed but not tokenized)
        /// </summary>
        ///  <param name="name"> field name </param>
        ///  <param name="value"> String value </param>
        ///  <param name="stored"> Store.YES if the content should also be stored </param>
        ///  <exception cref="ArgumentException"> if the field name or value is null. </exception>
        public StringField(string name, string value, Store stored)
            : base(name, value, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
        }
    }
}