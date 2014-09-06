using System;

namespace Lucene.Net.Index
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    ///  A Term represents a word from text.  this is the unit of search.  It is
    ///  composed of two elements, the text of the word, as a string, and the name of
    ///  the field that the text occurred in.
    ///
    ///  Note that terms may represent more than words from text fields, but also
    ///  things like dates, email addresses, urls, etc.
    /// </summary>

    public sealed class Term : IComparable<Term>, IEquatable<Term>
    {
        internal string Field_Renamed;
        internal BytesRef Bytes_Renamed;

        /// <summary>
        /// Constructs a Term with the given field and bytes.
        /// <p>Note that a null field or null bytes value results in undefined
        /// behavior for most Lucene APIs that accept a Term parameter.
        ///
        /// <p>WARNING: the provided BytesRef is not copied, but used directly.
        /// Therefore the bytes should not be modified after construction, for
        /// example, you should clone a copy by <seealso cref="BytesRef#deepCopyOf"/>
        /// rather than pass reused bytes from a TermsEnum.
        /// </summary>
        public Term(string fld, BytesRef bytes)
        {
            Field_Renamed = fld;
            this.Bytes_Renamed = bytes;
        }

        /// <summary>
        /// Constructs a Term with the given field and text.
        /// <p>Note that a null field or null text value results in undefined
        /// behavior for most Lucene APIs that accept a Term parameter.
        /// </summary>
        public Term(string fld, string text)
            : this(fld, new BytesRef(text))
        {
        }

        /// <summary>
        /// Constructs a Term with the given field and empty text.
        /// this serves two purposes: 1) reuse of a Term with the same field.
        /// 2) pattern for a query.
        /// </summary>
        /// <param name="fld"> field's name </param>
        public Term(string fld)
            : this(fld, new BytesRef())
        {
        }

        /// <summary>
        /// Returns the field of this term.   The field indicates
        ///  the part of a document which this term came from.
        /// </summary>
        public string Field()
        {
            return Field_Renamed;
        }

        /// <summary>
        /// Returns the text of this term.  In the case of words, this is simply the
        ///  text of the word.  In the case of dates and other types, this is an
        ///  encoding of the object as a string.
        /// </summary>
        public string Text()
        {
            return ToString(Bytes_Renamed);
        }

        /// <summary>
        /// Returns human-readable form of the term text. If the term is not unicode,
        /// the raw bytes will be printed instead.
        /// </summary>
        public static string ToString(BytesRef termText)
        {
            /*// the term might not be text, but usually is. so we make a best effort
            CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);
            try
            {
              return decoder.decode(ByteBuffer.wrap(termText.Bytes, termText.Offset, termText.Length)).ToString();
            }
            catch (CharacterCodingException e)
            {
              return termText.ToString();
            }*/
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

        /// <summary>
        /// Returns the bytes of this term. </summary>
        public BytesRef Bytes()
        {
            return Bytes_Renamed;
        }

        public override bool Equals(object obj)
        {
            Term t = obj as Term;
            return this.Equals(t);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((Field_Renamed == null) ? 0 : Field_Renamed.GetHashCode());
            result = prime * result + ((Bytes_Renamed == null) ? 0 : Bytes_Renamed.GetHashCode());
            return result;
        }

        /// <summary>
        /// Compares two terms, returning a negative integer if this
        ///  term belongs before the argument, zero if this term is equal to the
        ///  argument, and a positive integer if this term belongs after the argument.
        ///
        ///  The ordering of terms is first by field, then by text.
        /// </summary>
        public int CompareTo(Term other)
        {
            if (Field_Renamed.Equals(other.Field_Renamed))
            {
                return Bytes_Renamed.CompareTo(other.Bytes_Renamed);
            }
            else
            {
                return Field_Renamed.CompareTo(other.Field_Renamed);
            }
        }

        /// <summary>
        /// Resets the field and text of a Term.
        /// <p>WARNING: the provided BytesRef is not copied, but used directly.
        /// Therefore the bytes should not be modified after construction, for
        /// example, you should clone a copy rather than pass reused bytes from
        /// a TermsEnum.
        /// </summary>
        public void Set(string fld, BytesRef bytes)
        {
            Field_Renamed = fld;
            this.Bytes_Renamed = bytes;
        }

        public bool Equals(Term other)
        {
            if (object.ReferenceEquals(null, other))
            {
                return object.ReferenceEquals(null, this);
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            if (string.Compare(this.Field_Renamed, other.Field_Renamed, StringComparison.Ordinal) != 0)
            {
                return false;
            }

            if (Bytes_Renamed == null)
            {
                if (other.Bytes_Renamed != null)
                {
                    return false;
                }
            }
            else if (!Bytes_Renamed.Equals(other.Bytes_Renamed))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return Field_Renamed + ":" + Text();
        }
    }
}