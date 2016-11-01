using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.QueryParsers.Ext
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
    /// The <see cref="ExtendableQueryParser"/> enables arbitrary query parser extension
    /// based on a customizable field naming scheme. The lucene query syntax allows
    /// implicit and explicit field definitions as query prefix followed by a colon
    /// (':') character. The <see cref="ExtendableQueryParser"/> allows to encode extension
    /// keys into the field symbol associated with a registered instance of
    /// <see cref="ParserExtension"/>. A customizable separation character separates the
    /// extension key from the actual field symbol. The <see cref="ExtendableQueryParser"/>
    /// splits (<see cref="Extensions.SplitExtensionField(string, string)"/>) the
    /// extension key from the field symbol and tries to resolve the associated
    /// <see cref="ParserExtension"/>. If the parser can't resolve the key or the field
    /// token does not contain a separation character, <see cref="ExtendableQueryParser"/>
    /// yields the same behavior as its super class <see cref="QueryParser"/>. Otherwise,
    /// if the key is associated with a <see cref="ParserExtension"/> instance, the parser
    /// builds an instance of <see cref="ExtensionQuery"/> to be processed by
    /// <see cref="ParserExtension.Parse(ExtensionQuery)"/>.If a extension field does not
    /// contain a field part the default field for the query will be used.
    /// <p>
    /// To guarantee that an extension field is processed with its associated
    /// extension, the extension query part must escape any special characters like
    /// '*' or '['. If the extension query contains any whitespace characters, the
    /// extension query part must be enclosed in quotes.
    /// Example ('_' used as separation character):
    /// <pre>
    ///   title_customExt:"Apache Lucene\?" OR content_customExt:prefix\*
    /// </pre>
    /// 
    /// Search on the default field:
    /// <pre>
    ///   _customExt:"Apache Lucene\?" OR _customExt:prefix\*
    /// </pre>
    /// </p>
    /// <p>
    /// The <see cref="ExtendableQueryParser"/> itself does not implement the logic how
    /// field and extension key are separated or ordered. All logic regarding the
    /// extension key and field symbol parsing is located in <see cref="Extensions"/>.
    /// Customized extension schemes should be implemented by sub-classing
    /// <see cref="Extensions"/>.
    /// </p>
    /// <p>
    /// For details about the default encoding scheme see <see cref="Extensions"/>.
    /// </p>
    /// 
    /// <see cref="Extensions"/>
    /// <see cref="ParserExtension"/>
    /// <see cref="ExtensionQuery"/>
    /// </summary>
    public class ExtendableQueryParser : Classic.QueryParser
    {
        private readonly string defaultField;
        private readonly Extensions extensions;

  
        /// <summary>
        ///  Default empty extensions instance
        /// </summary>
        private static readonly Extensions DEFAULT_EXTENSION = new Extensions();

        /// <summary>
        /// Creates a new <see cref="ExtendableQueryParser"/> instance
        /// </summary>
        /// <param name="matchVersion">the lucene version to use.</param>
        /// <param name="f">the default query field</param>
        /// <param name="a">the analyzer used to find terms in a query string</param>
        public ExtendableQueryParser(LuceneVersion matchVersion, string f, Analyzer a)
            : this(matchVersion, f, a, DEFAULT_EXTENSION)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ExtendableQueryParser"/> instance
        /// </summary>
        /// <param name="matchVersion">the lucene version to use.</param>
        /// <param name="f">the default query field</param>
        /// <param name="a">the analyzer used to find terms in a query string</param>
        /// <param name="ext">the query parser extensions</param>
        public ExtendableQueryParser(LuceneVersion matchVersion, string f, Analyzer a, Extensions ext)
            : base(matchVersion, f, a)
        {
            this.defaultField = f;
            this.extensions = ext;
        }

        /// <summary>
        /// Returns the extension field delimiter character.
        /// </summary>
        /// <returns>the extension field delimiter character.</returns>
        public virtual char ExtensionFieldDelimiter
        {
            get { return extensions.ExtensionFieldDelimiter; }
        }

        protected internal override Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            Tuple<string, string> splitExtensionField = this.extensions
                .SplitExtensionField(defaultField, field);
            ParserExtension extension = this.extensions
                .GetExtension(splitExtensionField.Item2);
            if (extension != null)
            {
                return extension.Parse(new ExtensionQuery(this, splitExtensionField.Item1,
                    queryText));
            }
            return base.GetFieldQuery(field, queryText, quoted);
        }
    }
}
