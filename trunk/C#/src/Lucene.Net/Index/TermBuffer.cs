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
using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

namespace Lucene.Net.Index
{
	
	sealed class TermBuffer : System.ICloneable
	{
		private System.String field;
		private Term term; // cached
        private bool preUTF8Strings; // true if strings are stored in "modified UTF-8" encoding
        private bool dirty; // true if text was set externally (i.e., not read via UTF-8 bytes)

        private UnicodeUtil.UTF16Result text = new UnicodeUtil.UTF16Result();
        private UnicodeUtil.UTF8Result bytes = new UnicodeUtil.UTF8Result();
		
		public int CompareTo(TermBuffer other)
		{
			if (field == other.field)
				// fields are interned
				return CompareChars(text.result, text.length, other.text.result, other.text.length);
			else
				return String.CompareOrdinal(field, other.field);
		}
		
		private static int CompareChars(char[] chars1, int len1, char[] chars2, int len2)
		{
			int end = len1 < len2 ? len1 : len2;
			for (int k = 0; k < end; k++)
			{
				char c1 = chars1[k];
				char c2 = chars2[k];
				if (c1 != c2)
				{
					return c1 - c2;
				}
			}
			return len1 - len2;
		}

        /// <summary>
        /// Call this if the IndexInput passed to Read() stores terms
        /// in the modified UTF-8 (pre-LUCENE-510) format.
        /// </summary>
        internal void SetPreUTF8Strings()
        {
            preUTF8Strings = true;
        }
		
		public void  Read(IndexInput input, FieldInfos fieldInfos)
		{
			this.term = null; // invalidate cache
			int start = input.ReadVInt();
			int length = input.ReadVInt();
			int totalLength = start + length;
            if (preUTF8Strings)
            {
                text.setLength(totalLength);
                input.ReadChars(text.result, start, length);
            }
            else
            {
                if (dirty)
                {
                    // fully convert all bytes since bytes is dirty
                    UnicodeUtil.UTF16toUTF8(text.result, 0, text.length, bytes);
                    bytes.setLength(totalLength);
                    input.ReadBytes(bytes.result, start, length);
                    UnicodeUtil.UTF8toUTF16(bytes.result, 0, totalLength, text);
                    dirty = false;
                }
                else
                {
                    // incrementally convert only the UTF-8 bytes that are new
                    bytes.setLength(totalLength);
                    input.ReadBytes(bytes.result, start, length);
                    UnicodeUtil.UTF8toUTF16(bytes.result, start, length, text);
                }
            }
            this.field = fieldInfos.FieldName(input.ReadVInt());
		}
		
		public void  Set(Term term)
		{
			if (term == null)
			{
				Reset();
				return ;
			}
            string termText = term.Text();
            int termLen = termText.Length;
            text.setLength(termLen);
			for (int i = 0; i < termLen; i++)
			{
				text.result[i] = (char) termText[i];
			}
			dirty = true;
			field = term.Field();
			this.term = term;
		}
		
		public void  Set(TermBuffer other)
		{
            text.copyText(other.text);
            dirty = true;
			field = other.field;
			term = other.term;
		}
		
		public void  Reset()
		{
			field = null;
			text.setLength(0);
			term = null;
            dirty = true;
		}
		
		public Term ToTerm()
		{
			if (field == null)
				// unset
				return null;
			
			if (term == null)
				term = new Term(field, new System.String(text.result, 0, text.length), false);
			
			return term;
		}
		
		public object Clone()
		{
			TermBuffer clone = null;
			try
			{
				clone = (TermBuffer) base.MemberwiseClone();
			}
			catch (System.Exception)
			{
			}

            clone.dirty = true;
            clone.bytes = new UnicodeUtil.UTF8Result();
            clone.text = new UnicodeUtil.UTF16Result();
            clone.text.copyText(text);
			
			return clone;
		}
	}
}