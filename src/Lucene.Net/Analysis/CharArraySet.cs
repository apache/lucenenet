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

namespace Lucene.Net.Analysis
{
	
	
	/// <summary> A simple class that stores Strings as char[]'s in a
	/// hash table.  Note that this is not a general purpose
	/// class.  For example, it cannot remove items from the
	/// set, nor does it resize its hash table to be smaller,
	/// etc.  It is designed to be quick to test if a char[]
	/// is in the set without the necessity of converting it
	/// to a String first.
	/// </summary>

    public class CharArraySet : System.Collections.Hashtable
	{
        public override int Count
        {
            get
            {
                return count;
            }
        }

		private const int INIT_SIZE = 8;
		private char[][] entries;
		private int count;
		private bool ignoreCase;
		
		/// <summary>Create set with enough capacity to hold startSize
		/// terms 
		/// </summary>
		public CharArraySet(int startSize, bool ignoreCase)
		{
			this.ignoreCase = ignoreCase;
			int size = INIT_SIZE;
			while (startSize + (startSize >> 2) > size)
				size <<= 1;
			entries = new char[size][];
		}
		
		/// <summary>Create set from a Collection of char[] or String </summary>
		public CharArraySet(System.Collections.ICollection c, bool ignoreCase) : this(c.Count, ignoreCase)
		{
			System.Collections.IEnumerator e = c.GetEnumerator();
			while (e.MoveNext())
			{
				Add(e.Current);
			}
		}
		
		/// <summary>true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
		/// are in the set 
		/// </summary>
		public virtual bool Contains(char[] text, int off, int len)
		{
			return entries[GetSlot(text, off, len)] != null;
		}
		
		private int GetSlot(char[] text, int off, int len)
		{
			int code = GetHashCode(text, off, len);
			int pos = code & (entries.Length - 1);
			char[] text2 = entries[pos];
			if (text2 != null && !Equals(text, off, len, text2))
			{
				int inc = ((code >> 8) + code) | 1;
				do 
				{
					code += inc;
					pos = code & (entries.Length - 1);
					text2 = entries[pos];
				}
				while (text2 != null && !Equals(text, off, len, text2));
			}
			return pos;
		}
		
		/// <summary>Add this String into the set </summary>
		public virtual bool Add(System.String text)
		{
			return Add(text.ToCharArray());
		}
		
		/// <summary>Add this char[] directly to the set.
		/// If ignoreCase is true for this Set, the text array will be directly modified.
		/// The user should never modify this text array after calling this method.
		/// </summary>
		public virtual bool Add(char[] text)
		{
			if (ignoreCase)
				for (int i = 0; i < text.Length; i++)
					text[i] = System.Char.ToLower(text[i]);
			int slot = GetSlot(text, 0, text.Length);
			if (entries[slot] != null)
				return false;
			entries[slot] = text;
			count++;
			
			if (count + (count >> 2) > entries.Length)
			{
				Rehash();
			}
			
			return true;
		}
		
		private bool Equals(char[] text1, int off, int len, char[] text2)
		{
			if (len != text2.Length)
				return false;
			if (ignoreCase)
			{
				for (int i = 0; i < len; i++)
				{
					if (System.Char.ToLower(text1[off + i]) != text2[i])
						return false;
				}
			}
			else
			{
				for (int i = 0; i < len; i++)
				{
					if (text1[off + i] != text2[i])
						return false;
				}
			}
			return true;
		}
		
		private void  Rehash()
		{
			int newSize = 2 * entries.Length;
			char[][] oldEntries = entries;
			entries = new char[newSize][];
			
			for (int i = 0; i < oldEntries.Length; i++)
			{
				char[] text = oldEntries[i];
				if (text != null)
				{
					// todo: could be faster... no need to compare strings on collision
					entries[GetSlot(text, 0, text.Length)] = text;
				}
			}
		}
		
		private int GetHashCode(char[] text, int offset, int len)
		{
			int code = 0;
			int stop = offset + len;
			if (ignoreCase)
			{
				for (int i = offset; i < stop; i++)
				{
					code = code * 31 + System.Char.ToLower(text[i]);
				}
			}
			else
			{
				for (int i = offset; i < stop; i++)
				{
					code = code * 31 + text[i];
				}
			}
			return code;
		}
		
		public virtual int Size()
		{
			return count;
		}
		
		public virtual bool IsEmpty()
		{
			return count == 0;
		}
		
		public override bool Contains(System.Object o)
		{
			if (o is char[])
			{
				char[] text = (char[]) o;
				return Contains(text, 0, text.Length);
			}
            else if (o is String)
            {
                String s = (String)o;
				return Contains(s.ToCharArray(), 0, s.Length);
			}
			return false;
		}
		
		public virtual bool Add(System.Object o)
		{
			if (o is char[])
			{
				return Add((char[]) o);
			}
			else if (o is System.String)
			{
				return Add((System.String) o);
			}
			else
			{
				return Add(o.ToString());
			}
		}
		
		/// <summary>The Iterator<String> for this set.  Strings are constructed on the fly, so
		/// use <code>nextCharArray</code> for more efficient access. 
		/// </summary>
		public class CharArraySetIterator : System.Collections.IEnumerator
		{
			private void  InitBlock(CharArraySet enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private CharArraySet enclosingInstance;
			/// <summary>Returns the next String, as a Set<String> would...
			/// use nextCharArray() for better efficiency. 
			/// </summary>
			public virtual System.Object Current
			{
				get
				{
					return new System.String(NextCharArray());
				}
				
			}
			public CharArraySet Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal int pos = - 1;
			internal char[] next_Renamed_Field;
			internal CharArraySetIterator(CharArraySet enclosingInstance)
			{
				InitBlock(enclosingInstance);
				GoNext();
			}
			
			private void  GoNext()
			{
				next_Renamed_Field = null;
				pos++;
				while (pos < Enclosing_Instance.entries.Length && (next_Renamed_Field = Enclosing_Instance.entries[pos]) == null)
					pos++;
			}
			
			public virtual bool MoveNext()
			{
				return next_Renamed_Field != null;
			}
			
			/// <summary>do not modify the returned char[] </summary>
			public virtual char[] NextCharArray()
			{
				char[] ret = next_Renamed_Field;
				GoNext();
				return ret;
			}
			
			public virtual void  Remove()
			{
				throw new System.NotSupportedException();
			}

			virtual public void  Reset()
			{
			}
		}
		
		
		public new System.Collections.IEnumerator GetEnumerator()
		{
			return new CharArraySetIterator(this);
		}
	}
}