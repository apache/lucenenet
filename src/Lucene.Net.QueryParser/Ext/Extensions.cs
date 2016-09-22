using Lucene.Net.QueryParsers.Classic;
using System;
using System.Collections.Generic;
using System.Text;

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

    public class Extensions
    {
        private readonly IDictionary<string, ParserExtension> extensions = new Dictionary<string, ParserExtension>();
        private readonly char extensionFieldDelimiter;

        /// <summary>
        /// The default extension field delimiter character. This constant is set to ':'
        /// </summary>
        public static readonly char DEFAULT_EXTENSION_FIELD_DELIMITER = ':';

        /// <summary>
        /// Creates a new <see cref="Extensions"/> instance with the
        /// <see cref="#DEFAULT_EXTENSION_FIELD_DELIMITER"/> as a delimiter character.
        /// </summary>
        public Extensions()
            : this(DEFAULT_EXTENSION_FIELD_DELIMITER)
        {
        }

        /// <summary>
        /// Creates a new <see cref="Extensions"/> instance
        /// </summary>
        /// <param name="extensionFieldDelimiter">the extensions field delimiter character</param>
        public Extensions(char extensionFieldDelimiter)
        {
            this.extensionFieldDelimiter = extensionFieldDelimiter;
        }

        /// <summary>
        /// Adds a new <see cref="ParserExtension"/> instance associated with the given key.
        /// </summary>
        /// <param name="key">the parser extension key</param>
        /// <param name="extension">the parser extension</param>
        public virtual void Add(string key, ParserExtension extension)
        {
            this.extensions[key] = extension;
        }

        /// <summary>
        /// Returns the <see cref="ParserExtension"/> instance for the given key or
        /// <code>null</code> if no extension can be found for the key.
        /// </summary>
        /// <param name="key">the extension key</param>
        /// <returns>the <see cref="ParserExtension"/> instance for the given key or
        /// <code>null</code> if no extension can be found for the key.</returns>
        public ParserExtension GetExtension(string key)
        {
            if (key == null || !this.extensions.ContainsKey(key)) return null;
            return this.extensions[key];
        }

        /// <summary>
        /// Returns the extension field delimiter
        /// </summary>
        public virtual char ExtensionFieldDelimiter
        {
            get { return extensionFieldDelimiter; }
        }

        /// <summary>
        /// Splits a extension field and returns the field / extension part as a
        /// <see cref="Tuple{string,string}"/>. This method tries to split on the first occurrence of the
        /// extension field delimiter, if the delimiter is not present in the string
        /// the result will contain a <code>null</code> value for the extension key and
        /// the given field string as the field value. If the given extension field
        /// string contains no field identifier the result pair will carry the given
        /// default field as the field value.
        /// </summary>
        /// <param name="defaultField">the default query field</param>
        /// <param name="field">the extension field string</param>
        /// <returns>a {<see cref="Tuple{string,string}"/> with the field name as the <see cref="Tuple{string,string}.Item1"/> and the
        /// extension key as the <see cref="Tuple{string,string}.Item2"/></returns>
        public virtual Tuple<string, string> SplitExtensionField(string defaultField, string field)
        {
            int indexOf = field.IndexOf(this.extensionFieldDelimiter);
            if (indexOf < 0)
                return new Tuple<string, string>(field, null);
            string indexField = indexOf == 0 ? defaultField : field.Substring(0, indexOf);
            string extensionKey = field.Substring(indexOf + 1);
            return new Tuple<string, string>(indexField, extensionKey);
        }

        /// <summary>
        /// Escapes an extension field. The default implementation is equivalent to
        /// <see cref="QueryParser.Escape(string)"/>.
        /// </summary>
        /// <param name="extfield">the extension field identifier</param>
        /// <returns>the extension field identifier with all special chars escaped with
        /// a backslash character.</returns>
        public virtual string EscapeExtensionField(string extfield)
        {
            return QueryParserBase.Escape(extfield);
        }

        /// <summary>
        /// Builds an extension field string from a given extension key and the default
        /// query field. The default field and the key are delimited with the extension
        /// field delimiter character. This method makes no assumption about the order
        /// of the extension key and the field. By default the extension key is
        /// appended to the end of the returned string while the field is added to the
        /// beginning. Special Query characters are escaped in the result.
        /// <p>
        /// Note: <see cref="Extensions"/> subclasses must maintain the contract between
        /// <see cref="M:BuildExtensionField(string)"/> and
        /// <see cref="M:BuildExtensionField(string, string)"/> where the latter inverts the
        /// former.
        /// </p>
        /// </summary>
        /// <param name="extensionKey">the extension key</param>
        /// <returns>escaped extension field identifier</returns>
        public virtual string BuildExtensionField(string extensionKey)
        {
            return BuildExtensionField(extensionKey, "");
        }

        /// <summary>
        /// Builds an extension field string from a given extension key and the default
        /// query field. The default field and the key are delimited with the extension
        /// field delimiter character. This method makes no assumption about the order
        /// of the extension key and the field. By default the extension key is
        /// appended to the end of the returned string while the field is added to the
        /// beginning. Special Query characters are escaped in the result.
        /// <p>
        /// Note: <see cref="Extensions"/> subclasses must maintain the contract between
        /// <see cref="M:BuildExtensionField(string)"/> and
        /// <see cref="M:BuildExtensionField(string, string)"/> where the latter inverts the
        /// former.
        /// </summary>
        /// <param name="extensionKey">the extension key</param>
        /// <param name="field">the field to apply the extension on.</param>
        /// <returns>escaped extension field identifier</returns>
        /// <remarks>See <see cref="M:BuildExtensionField(string)"/> to use the default query field</remarks>
        public virtual string BuildExtensionField(string extensionKey, string field)
        {
            StringBuilder builder = new StringBuilder(field);
            builder.Append(this.extensionFieldDelimiter);
            builder.Append(extensionKey);
            return EscapeExtensionField(builder.ToString());
        }

        // NOTE: Pair<T, T> was eliminated in favor of the built in Tuple<T, T> type.
    }
}
