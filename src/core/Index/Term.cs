/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Util;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using StringHelper = Lucene.Net.Util.StringHelper;

namespace Lucene.Net.Index
{

    /// <summary>A Term represents a word from text.  This is the unit of search.  It is
    /// composed of two elements, the text of the word, as a string, and the name of
    /// the field that the text occured in, an interned string.
    /// Note that terms may represent more than words from text fields, but also
    /// things like dates, email addresses, urls, etc.  
    /// </summary>
    [Serializable]
    public sealed class Term : IComparable<Term>
    {
        private string field;
        private BytesRef bytes;

        /// <summary>Constructs a Term with the given field and text.
        /// <p/>Note that a null field or null text value results in undefined
        /// behavior for most Lucene APIs that accept a Term parameter. 
        /// </summary>
        public Term(string fld, BytesRef bytes)
        {
            field = string.Intern(fld);
            this.bytes = bytes;
        }

        public Term(string fld, string text)
            : this(fld, new BytesRef(text))
        {
        }

        /// <summary>Constructs a Term with the given field and empty text.
        /// This serves two purposes: 1) reuse of a Term with the same field.
        /// 2) pattern for a query.
        /// 
        /// </summary>
        /// <param name="fld">
        /// </param>
        public Term(string fld)
            : this(fld, new BytesRef())
        {
        }

        /// <summary>Returns the field of this term, an interned string.   The field indicates
        /// the part of a document which this term came from. 
        /// </summary>
        public string Field
        {
            get { return field; }
            set { field = string.Intern(value); }
        }

        /// <summary>Returns the text of this term.  In the case of words, this is simply the
        /// text of the word.  In the case of dates and other types, this is an
        /// encoding of the object as a string.  
        /// </summary>
        public string Text
        {
            get { return ToString(bytes); }
        }

        public static string ToString(BytesRef termText)
        {
            //var decoder = IOUtils.CHARSET_UTF_8;

            try
            {
                // .Net port: termText already has this handy UTF8ToString method, so we're using that instead
                return termText.Utf8ToString();
            }
            catch
            {
                return termText.ToString();
            }
        }

        public BytesRef Bytes
        {
            get { return bytes; }
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            Term other = (Term)obj;
            if (field == null)
            {
                if (other.field != null)
                    return false;
            }
            else if (!field.Equals(other.field))
                return false;
            if (bytes == null)
            {
                if (other.bytes != null)
                    return false;
            }
            else if (!bytes.Equals(other.bytes))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((field == null) ? 0 : field.GetHashCode());
            result = prime * result + ((bytes == null) ? 0 : bytes.GetHashCode());
            return result;
        }

        /// <summary>Compares two terms, returning a negative integer if this
        /// term belongs before the argument, zero if this term is equal to the
        /// argument, and a positive integer if this term belongs after the argument.
        /// The ordering of terms is first by field, then by text.
        /// </summary>
        public int CompareTo(Term other)
        {
            if (ReferenceEquals(field, other.field))
                // fields are interned
                return bytes.CompareTo(other.bytes);
            else
                return string.CompareOrdinal(field, other.field);
        }

        internal void Set(string fld, BytesRef bytes)
        {
            field = string.Intern(fld);
            this.bytes = bytes;
        }

        public override string ToString()
        {
            return field + ":" + Text;
        }
        
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            field = string.Intern(field);
        }
    }
}