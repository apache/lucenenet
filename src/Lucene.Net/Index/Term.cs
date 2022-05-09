using J2N.Text;
using System;
using System.Text;

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
    /// A <see cref="Term"/> represents a word from text.  This is the unit of search.  It is
    /// composed of two elements, the text of the word, as a string, and the name of
    /// the field that the text occurred in.
    /// <para/>
    /// Note that terms may represent more than words from text fields, but also
    /// things like dates, email addresses, urls, etc.
    /// </summary>
    public sealed class Term : IComparable<Term>, IEquatable<Term> // LUCENENET specific - class implements IEquatable<T>
    {
        /// <summary>
        /// Constructs a <see cref="Term"/> with the given field and bytes.
        /// <para/>Note that a null field or null bytes value results in undefined
        /// behavior for most Lucene APIs that accept a Term parameter.
        ///
        /// <para/>WARNING: the provided <see cref="BytesRef"/> is not copied, but used directly.
        /// Therefore the bytes should not be modified after construction, for
        /// example, you should clone a copy by <see cref="BytesRef.DeepCopyOf(BytesRef)"/>
        /// rather than pass reused bytes from a <see cref="TermsEnum"/>.
        /// </summary>
        public Term(string fld, BytesRef bytes)
        {
            Field = fld;
            Bytes = bytes;
        }

        /// <summary>
        /// Constructs a <see cref="Term"/> with the given field and text.
        /// <para/>Note that a <c>null</c> field or null text value results in undefined
        /// behavior for most Lucene APIs that accept a <see cref="Term"/> parameter.
        /// </summary>
        public Term(string fld, string text)
            : this(fld, new BytesRef(text))
        {
        }

        /// <summary>
        /// Constructs a <see cref="Term"/> with the given field and empty text.
        /// this serves two purposes: 1) reuse of a <see cref="Term"/> with the same field.
        /// 2) pattern for a query.
        /// </summary>
        /// <param name="fld"> field's name </param>
        public Term(string fld)
            : this(fld, new BytesRef())
        {
        }

        /// <summary>
        /// Returns the field of this term.  The field indicates
        /// the part of a document which this term came from.
        /// </summary>
        public string Field { get; internal set; }

        /// <summary>
        /// Returns the text of this term.  In the case of words, this is simply the
        /// text of the word.  In the case of dates and other types, this is an
        /// encoding of the object as a string.
        /// </summary>
        public string Text => ToString(Bytes); // LUCENENET: Changed to a property. While this calls a method internally, its expected usage is that it will return a deterministic value.

        /// <summary>
        /// Returns human-readable form of the term text. If the term is not unicode,
        /// the raw bytes will be printed instead.
        /// </summary>
        public static string ToString(BytesRef termText)
        {
            // the term might not be text, but usually is. so we make a best effort
            Encoding decoder = new UTF8Encoding(false, true);
            try
            {
                return decoder.GetString(termText.Bytes, termText.Offset, termText.Length);
            }
            catch
            {
                return termText.ToString();
            }
        }

        /// <summary>
        /// Returns the bytes of this term.
        /// </summary>
        public BytesRef Bytes { get; internal set; }

        public override bool Equals(object obj)
        {
            if (obj is Term other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((Field is null) ? 0 : Field.GetHashCode());
            result = prime * result + ((Bytes is null) ? 0 : Bytes.GetHashCode());
            return result;
        }

        /// <summary>
        /// Compares two terms, returning a negative integer if this
        /// term belongs before the argument, zero if this term is equal to the
        /// argument, and a positive integer if this term belongs after the argument.
        /// <para/>
        /// The ordering of terms is first by field, then by text.
        /// </summary>
        public int CompareTo(Term other)
        {
            int compare = Field.CompareToOrdinal(other.Field);
            if (compare == 0)
            {
                return Bytes.CompareTo(other.Bytes);
            }
            else
            {
                return compare;
            }
        }

        /// <summary>
        /// Resets the field and text of a <see cref="Term"/>.
        /// <para/>WARNING: the provided <see cref="BytesRef"/> is not copied, but used directly.
        /// Therefore the bytes should not be modified after construction, for
        /// example, you should clone a copy rather than pass reused bytes from
        /// a TermsEnum.
        /// </summary>
        internal void Set(string fld, BytesRef bytes)
        {
            Field = fld;
            this.Bytes = bytes;
        }

        public bool Equals(Term other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            if (this.GetType() != other.GetType())
                return false;

            if (!StringComparer.Ordinal.Equals(Field, other.Field))
            {
                return false;
            }

            if (Bytes is null)
            {
                if (other.Bytes != null)
                {
                    return false;
                }
            }
            else if (!Bytes.BytesEquals(other.Bytes))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return Field + ":" + Text;
        }
    }
}