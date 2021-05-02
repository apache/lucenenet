using Lucene.Net.Analysis;
using System;
using System.IO;

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
    /// A field that is indexed and tokenized, without term
    /// vectors.  For example this would be used on a 'body'
    /// field, that contains the bulk of a document's text.
    /// </summary>
    public sealed class TextField : Field
    {
        /// <summary>
        /// Indexed, tokenized, not stored. </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_NOT_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true
        }.Freeze();

        /// <summary>
        /// Indexed, tokenized, stored. </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            IsStored = true
        }.Freeze();

        // TODO: add sugar for term vectors...?

        /// <summary>
        /// Creates a new un-stored <see cref="TextField"/> with <see cref="TextReader"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="reader"> <see cref="TextReader"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> or <paramref name="reader"/> is <c>null</c> </exception>
        public TextField(string name, TextReader reader)
            : base(name, reader, TYPE_NOT_STORED)
        {
        }

        /// <summary>
        /// Creates a new <see cref="TextField"/> with <see cref="string"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="string"/> value </param>
        /// <param name="store"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> or <paramref name="value"/> is <c>null</c>. </exception>
        public TextField(string name, string value, Store store)
            : base(name, value, store == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
        }

        /// <summary>
        /// Creates a new un-stored <see cref="TextField"/> with <see cref="TokenStream"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="stream"> <see cref="TokenStream"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> or <paramref name="stream"/> is <c>null</c>. </exception>
        public TextField(string name, TokenStream stream)
            : base(name, stream, TYPE_NOT_STORED)
        {
        }
    }
}