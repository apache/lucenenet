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
using System.Linq;
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

    public class CharArraySet : ICollection<string>
    {

        bool _ReadOnly = false;
        const int INIT_SIZE = 8;
        char[][] _Entries;
        int _Count;
        bool _IgnoreCase;

        #region Constructors
        /// <summary>Create set with enough capacity to hold startSize
        /// terms 
        /// </summary>
        public CharArraySet(int startSize, bool ignoreCase)
        {
            this._IgnoreCase = ignoreCase;
            int size = INIT_SIZE;
            while (startSize + (startSize >> 2) > size)
                size <<= 1;
            _Entries = new char[size][];
        }


        /// <summary>Create set from a Collection of char[] or String </summary>
        public CharArraySet(IEnumerable<string> c, bool ignoreCase) : this(c.Count(), ignoreCase)
        {
            foreach (string s in c)
            {
                Add(s);
            }
        }

        /// <summary>Create set from entries </summary>
        private CharArraySet(char[][] entries, bool ignoreCase, int count)
        {
            this._Entries = entries;
            this._IgnoreCase = ignoreCase;
            this._Count = count;
        }
        #endregion


        #region public members

        public virtual bool Contains(char[] text)
        {
            return Contains(text, 0, text.Length);
        }

        /// <summary>true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
        /// are in the set 
        /// </summary>
        public virtual bool Contains(char[] text, int off, int len)
        {
            return _Entries[GetSlot(text, off, len)] != null;
        }

        /// <summary>Add this char[] directly to the set.
        /// If ignoreCase is true for this Set, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        public void Add(char[] text)
        {
            if (_ReadOnly) throw new NotSupportedException();

            if (_IgnoreCase)
                for (int i = 0; i < text.Length; i++)
                    text[i] = System.Char.ToLower(text[i]);
            int slot = GetSlot(text, 0, text.Length);
            if (_Entries[slot] != null)
                return;
            _Entries[slot] = text;
            _Count++;

            if (_Count + (_Count >> 2) > _Entries.Length)
            {
                Rehash();
            }
        }

        /// <summary>Adds all of the elements in the specified collection to this collection </summary>
        public void Add(IEnumerable<string> coll)
        {
            if (_ReadOnly) throw new NotSupportedException();

            foreach (string s in coll)
            {
                Add(s);
            }
        }

        public static CharArraySet UnmodifiableSet(ICollection<string> items)
        {
            CharArraySet set = new CharArraySet(items.Count,true);
            set.Add(items);
            set.IsReadOnly = true;
            return set;

        }
        #endregion


        #region ICollection<string>
        public void Add(string text)
        {
            if (_ReadOnly) throw new NotSupportedException();
            Add(text.ToCharArray());
        }

        public void Clear()
        {
            if (_ReadOnly) throw new NotSupportedException();
            _Entries = null;
            _Count = 0;
        }

        public bool Contains(string item)
        {
            return _Entries[GetSlot(item)] != null;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return _Count; }
        }

        public bool IsReadOnly
        {
            get { return _ReadOnly; }
            private set { _ReadOnly = value; }
        }



        public bool Remove(string item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new CharArraySetEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion


        #region Private Methods

        private int GetSlot(char[] text, int off, int len)
        {
            int code = GetHashCode(text, off, len);
            int pos = code & (_Entries.Length - 1);
            char[] text2 = _Entries[pos];
            if (text2 != null && !Equals(text, off, len, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (_Entries.Length - 1);
                    text2 = _Entries[pos];
                }
                while (text2 != null && !Equals(text, off, len, text2));
            }
            return pos;
        }

        /// <summary>Returns true if the String is in the set </summary>
        private int GetSlot(System.String text)
        {
            int code = GetHashCode(text);
            int pos = code & (_Entries.Length - 1);
            char[] text2 = _Entries[pos];
            if (text2 != null && !Equals(text, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (_Entries.Length - 1);
                    text2 = _Entries[pos];
                }
                while (text2 != null && !Equals(text, text2));
            }
            return pos;
        }

        private bool Equals(char[] text1, int off, int len, char[] text2)
        {
            if (len != text2.Length)
                return false;
            if (_IgnoreCase)
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

        private bool Equals(System.String text1, char[] text2)
        {
            int len = text1.Length;
            if (len != text2.Length)
                return false;
            if (_IgnoreCase)
            {
                for (int i = 0; i < len; i++)
                {
                    if (System.Char.ToLower(text1[i]) != text2[i])
                        return false;
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (text1[i] != text2[i])
                        return false;
                }
            }
            return true;
        }

        private void Rehash()
        {
            int newSize = 2 * _Entries.Length;
            char[][] oldEntries = _Entries;
            _Entries = new char[newSize][];

            for (int i = 0; i < oldEntries.Length; i++)
            {
                char[] text = oldEntries[i];
                if (text != null)
                {
                    // todo: could be faster... no need to compare strings on collision
                    _Entries[GetSlot(text, 0, text.Length)] = text;
                }
            }
        }

        private int GetHashCode(char[] text, int offset, int len)
        {
            int code = 0;
            int stop = offset + len;
            if (_IgnoreCase)
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

        private int GetHashCode(System.String text)
        {
            int code = 0;
            int len = text.Length;
            if (_IgnoreCase)
            {
                for (int i = 0; i < len; i++)
                {
                    code = code * 31 + System.Char.ToLower(text[i]);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }
        #endregion

        #region Unneeded methods(used only in test case)
        public void RemoveAll(ICollection<string> c)
        {
            throw new NotSupportedException();
        }

        public void RetainAll(ICollection<string> c)
        {
            throw new NotSupportedException();
        }
        #endregion



        class CharArraySetEnumerator : IEnumerator<string>
        {
            CharArraySet _Creator;
            int pos = -1;
            char[] next;

            public CharArraySetEnumerator(CharArraySet creator)
            {
                _Creator = creator;
                GoNext();
            }

            public string Current
            {
                get { return new string(NextCharArray()); }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return new string(NextCharArray()); }
            }

            public bool MoveNext()
            {
                return next != null;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            #region Private Methods

            private void GoNext()
            {
                next = null;
                pos++;
                while (pos < _Creator._Entries.Length && (next = _Creator._Entries[pos]) == null)
                    pos++;
            }

            /// <summary>do not modify the returned char[] </summary>
            char[] NextCharArray()
            {
                char[] ret = next;
                GoNext();
                return ret;
            }

            #endregion
        }

    }
		
}

