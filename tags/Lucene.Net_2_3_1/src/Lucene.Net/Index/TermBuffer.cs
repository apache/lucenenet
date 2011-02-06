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

using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{
	
	sealed class TermBuffer : System.ICloneable
	{
		private static readonly char[] NO_CHARS = new char[0];
		
		private System.String field;
		private char[] text = NO_CHARS;
		private int textLength;
		private Term term; // cached
		
		public int CompareTo(TermBuffer other)
		{
			if (field == other.field)
				// fields are interned
				return CompareChars(text, textLength, other.text, other.textLength);
			else
				return String.CompareOrdinal(field, other.field);
		}
		
		private static int CompareChars(char[] v1, int len1, char[] v2, int len2)
		{
			int end = System.Math.Min(len1, len2);
			for (int k = 0; k < end; k++)
			{
				char c1 = v1[k];
				char c2 = v2[k];
				if (c1 != c2)
				{
					return c1 - c2;
				}
			}
			return len1 - len2;
		}
		
		private void  SetTextLength(int newLength)
		{
			if (text.Length < newLength)
			{
				char[] newText = new char[newLength];
				Array.Copy(text, 0, newText, 0, textLength);
				text = newText;
			}
			textLength = newLength;
		}
		
		public void  Read(IndexInput input, FieldInfos fieldInfos)
		{
			this.term = null; // invalidate cache
			int start = input.ReadVInt();
			int length = input.ReadVInt();
			int totalLength = start + length;
			SetTextLength(totalLength);
			input.ReadChars(this.text, start, length);
			this.field = fieldInfos.FieldName(input.ReadVInt());
		}
		
		public void  Set(Term term)
		{
			if (term == null)
			{
				Reset();
				return ;
			}
			
			// copy text into the buffer
			SetTextLength(term.Text().Length);

			System.String sourceString = term.Text();
			int sourceEnd = term.Text().Length;
			for (int i = 0; i < sourceEnd; i++)
			{
				text[i] = (char) sourceString[i];
			}
			
			this.field = term.Field();
			this.term = term;
		}
		
		public void  Set(TermBuffer other)
		{
			SetTextLength(other.textLength);
			Array.Copy(other.text, 0, text, 0, textLength);
			
			this.field = other.field;
			this.term = other.term;
		}
		
		public void  Reset()
		{
			this.field = null;
			this.textLength = 0;
			this.term = null;
		}
		
		public Term ToTerm()
		{
			if (field == null)
				// unset
				return null;
			
			if (term == null)
				term = new Term(field, new System.String(text, 0, textLength), false);
			
			return term;
		}
		
		public System.Object Clone()
		{
			TermBuffer clone = null;
			try
			{
				clone = (TermBuffer) base.MemberwiseClone();
			}
			catch (System.Exception)
			{
			}
			
			clone.text = new char[text.Length];
			Array.Copy(text, 0, clone.text, 0, textLength);
			
			return clone;
		}
	}
}