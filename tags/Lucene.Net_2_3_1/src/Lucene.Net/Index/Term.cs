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

using System;

namespace Lucene.Net.Index
{
	
	/// <summary>A Term represents a word from text.  This is the unit of search.  It is
	/// composed of two elements, the text of the word, as a string, and the name of
	/// the field that the text occured in, an interned string.
	/// Note that terms may represent more than words from text fields, but also
	/// things like dates, email addresses, urls, etc.  
	/// </summary>
	
	[Serializable]
	public sealed class Term : System.IComparable
	{
		internal System.String field;
		internal System.String text;

        // For testing only
        public System.String text_ForNUnitTest
        {
            get { return text; }
        }

		/// <summary>Constructs a Term with the given field and text.
		/// <p>Note that a null field or null text value results in undefined
		/// behavior for most Lucene APIs that accept a Term parameter. 
		/// </summary>
		public Term(System.String fld, System.String txt) : this(fld, txt, true)
		{
		}
		internal Term(System.String fld, System.String txt, bool intern)
		{
			field = intern ? String.Intern(fld) : fld; // field names are interned
			text = txt; // unless already known to be
		}
		
		/// <summary>Returns the field of this term, an interned string.   The field indicates
		/// the part of a document which this term came from. 
		/// </summary>
		public System.String Field()
		{
			return field;
		}
		
		/// <summary>Returns the text of this term.  In the case of words, this is simply the
		/// text of the word.  In the case of dates and other types, this is an
		/// encoding of the object as a string.  
		/// </summary>
		public System.String Text()
		{
			return text;
		}
		
		/// <summary> Optimized construction of new Terms by reusing same field as this Term
		/// - avoids field.intern() overhead 
		/// </summary>
		/// <param name="text">The text of the new term (field is implicitly same as this Term instance)
		/// </param>
		/// <returns> A new Term
		/// </returns>
		public Term CreateTerm(System.String text)
		{
			return new Term(field, text, false);
		}
		
		/// <summary>Compares two terms, returning true iff they have the same
		/// field and text. 
		/// </summary>
		public  override bool Equals(System.Object o)
		{
			if (o == this)
				return true;
			if (o == null)
				return false;
			if (!(o is Term))
				return false;
			Term other = (Term) o;
			return field == other.field && text.Equals(other.text);
		}
		
		/// <summary>Combines the hashCode() of the field and the text. </summary>
		public override int GetHashCode()
		{
			return field.GetHashCode() + text.GetHashCode();
		}
		
		public int CompareTo(System.Object other)
		{
			return CompareTo((Term) other);
		}
		
		/// <summary>Compares two terms, returning a negative integer if this
		/// term belongs before the argument, zero if this term is equal to the
		/// argument, and a positive integer if this term belongs after the argument.
		/// The ordering of terms is first by field, then by text.
		/// </summary>
		public int CompareTo(Term other)
		{
			if (field == other.field)
			// fields are interned
				return String.CompareOrdinal(text, other.text);
			else
				return String.CompareOrdinal(field, other.field);
		}
		
		/// <summary>Resets the field and text of a Term. </summary>
		internal void  Set(System.String fld, System.String txt)
		{
			field = fld;
			text = txt;
		}
		
		public override System.String ToString()
		{
			return field + ":" + text;
		}
		
		public void  GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			System.Type thisType = this.GetType();
			System.Reflection.MemberInfo[] mi = System.Runtime.Serialization.FormatterServices.GetSerializableMembers(thisType, context);
			for (int i = 0 ; i < mi.Length; i++) 
			{
				info.AddValue(mi[i].Name, ((System.Reflection.FieldInfo) mi[i]).GetValue(this));
			}
		}
	}
}