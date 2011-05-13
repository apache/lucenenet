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
using System.Collections.Generic;

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
	
	public class CharArraySet : Support.Set<string>
	{
		private const int INIT_SIZE = 8;
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
            base.Capacity = size;
		}
		
		/// <summary>Create set from a Collection of char[] or String </summary>
		public CharArraySet(IList<string> c, bool ignoreCase):this(c.Count, ignoreCase)
		{
            foreach (string s in c)
            {
                if(ignoreCase)
                    base.Add(s.ToLower());
                else
                    base.Add(c);
            }
            
		}
		

        public bool Contains(object o)
        {
            if (o is char[])
            {
                char[] text = (char[])o;
                return Contains(text, 0, text.Length);
            }
            return Contains(o.ToString());
        }

        public virtual bool Contains(char[] text)
        {
            return Contains(text, 0, text.Length);
        }

		/// <summary>true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
		/// are in the set 
		/// </summary>
		public virtual bool Contains(char[] text, int off, int len)
		{
            if(ignoreCase)
                return base.Contains(new string(text, off, len).ToLower());
            else
                return base.Contains(new string(text, off, len));
		}

        public new bool Contains(string s)
        {
            if(ignoreCase)
                return base.Contains(s.ToLower());
            else
                return base.Contains(s);
        }
			
			
		/// <summary>Add this char[] directly to the set.
		/// If ignoreCase is true for this Set, the text array will be directly modified.
		/// The user should never modify this text array after calling this method.
		/// </summary>
		public virtual void Add(char[] text)
		{
            if (ignoreCase)
                base.Add(text.ToString().ToLower());
            else
                base.Add(text.ToString());
		}

        public override void Add(string s)
        {

            if (ignoreCase)
                base.Add(s.ToLower());
            else
                base.Add(s);
        }

        public void AddAll(List<string> c)
        {
            foreach (string s in c)
            {
                Add(s);
            }
        }

        public void AddAll(string[] c)
        {
            foreach (string s in c)
            {
                Add(s);
            }
        }
		

        private int GetHashCode(char[] text, int offset, int len)
		{

            return new string(text, offset, len).GetHashCode();
		}
		
		private int GetHashCode(System.String text)
		{
            return text.GetHashCode();
		}
		
        		
		public static CharArraySet UnmodifiableSet(CharArraySet set_Renamed)
		{
            CharArraySet set = new CharArraySet(set_Renamed, true);
            set.ReadOnly = true;
            return set;
		}
}
		
}