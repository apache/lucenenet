using System.IO;
using Lucene.Net.Analysis;

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
    ///  vectors.  For example this would be used on a 'body'
    ///  field, that contains the bulk of a document's text.
    /// </summary>

    public sealed class TextField : Field
    {
        /// <summary>
        /// Indexed, tokenized, not stored. </summary>
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();

        /// <summary>
        /// Indexed, tokenized, stored. </summary>
        public static readonly FieldType TYPE_STORED = new FieldType();

        static TextField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.Tokenized = true;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.Tokenized = true;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Freeze();
        }

        // TODO: add sugar for term vectors...?

        /// <summary>
        /// Creates a new un-stored TextField with TextReader value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="reader"> reader value </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null </exception>
        /// <exception cref="NullPointerException"> if the reader is null </exception>
        public TextField(string name, TextReader reader)
            : base(name, reader, TYPE_NOT_STORED)
        {
        }

        /// <summary>
        /// Creates a new TextField with String value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> string value </param>
        /// <param name="store"> Store.YES if the content should also be stored </param>
        /// <exception cref="IllegalArgumentException"> if the field name or value is null. </exception>
        public TextField(string name, string value, Store store)
            : base(name, value, store == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
        }

        /// <summary>
        /// Creates a new un-stored TextField with TokenStream value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="stream"> TokenStream value </param>
        /// <exception cref="IllegalArgumentException"> if the field name is null. </exception>
        /// <exception cref="NullPointerException"> if the tokenStream is null </exception>
        public TextField(string name, TokenStream stream)
            : base(name, stream, TYPE_NOT_STORED)
        {
        }
    }
}