using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public class CharArrayMap<V> : IDictionary<object, V>
    {
        // private only because missing generics
        private static readonly CharArrayMap<V> EMPTY_MAP = new EmptyCharArrayMap<Object>();

        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private bool ignoreCase;
        private int count;
        internal readonly Lucene.Net.Util.Version matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal V[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        public CharArrayMap(Lucene.Net.Util.Version matchVersion, int startSize, bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            int size = INIT_SIZE;
            while (startSize + (startSize >> 2) > size)
                size <<= 1;
            keys = new char[size][];
            values = new V[size];
            this.charUtils = CharacterUtils.GetInstance(matchVersion);
            this.matchVersion = matchVersion;
        }

        public CharArrayMap(Lucene.Net.Util.Version matchVersion, IDictionary<object, V> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            PutAll(c);
        }

        private CharArrayMap(CharArrayMap<V> toCopy)
        {
            this.keys = toCopy.keys;
            this.values = toCopy.values;
            this.ignoreCase = toCopy.ignoreCase;
            this.count = toCopy.count;
            this.charUtils = toCopy.charUtils;
            this.matchVersion = toCopy.matchVersion;
        }

        public void Clear()
        {
            count = 0;
            Arrays.Fill(keys, null);
            Arrays.Fill(values, default(V));
        }

        public bool ContainsKey(char[] text, int off, int len)
        {
            return keys[GetSlot(text, off, len)] != null;
        }

        public bool ContainsKey(ICharSequence cs)
        {
            return keys[GetSlot(cs)] != null;
        }

        public bool ContainsKey(Object o)
        {
            if (o is char[])
            {
                char[] text = (char[])o;
                return ContainsKey(text, 0, text.Length);
            }
            return ContainsKey(o.ToString());
        }

        public V Get(char[] text, int off, int len)
        {
            return values[GetSlot(text, off, len)];
        }

        public V Get(ICharSequence cs)
        {
            return values[GetSlot(cs)];
        }

        public V this[Object o]
        {
            get
            {
                if (o is char[])
                {
                    char[] text = (char[])o;
                    return Get(text, 0, text.Length);
                }
                return this[o.ToString()];
            }
            set
            {
                if (o is char[])
                {
                    Put((char[])o, value);
                }
                Put(o.ToString(), value);
            }
        }

        private int GetSlot(char[] text, int off, int len)
        {
            int code = GetHashCode(text, off, len);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, off, len, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, off, len, text2));
            }
            return pos;
        }

        private int GetSlot(ICharSequence text)
        {
            int code = GetHashCode(text);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, text2));
            }
            return pos;
        }

        public V Put(ICharSequence text, V value)
        {
            return Put(text.ToString(), value); // could be more efficient
        }

        public V Put(string text, V value)
        {
            return Put(text.ToCharArray(), value);
        }

        public V Put(char[] text, V value)
        {
            if (ignoreCase)
            {
                charUtils.ToLowerCase(text, 0, text.Length);
            }
            int slot = GetSlot(text, 0, text.Length);
            if (keys[slot] != null)
            {
                V oldValue = values[slot];
                values[slot] = value;
                return oldValue;
            }
            keys[slot] = text;
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }

            return default(V);
        }

        private void Rehash()
        {
            //assert keys.length == values.length;
            int newSize = 2 * keys.Length;
            char[][] oldkeys = keys;
            V[] oldvalues = values;
            keys = new char[newSize][];
            values = new V[newSize];

            for (int i = 0; i < oldkeys.Length; i++)
            {
                char[] text = oldkeys[i];
                if (text != null)
                {
                    // todo: could be faster... no need to compare strings on collision
                    int slot = GetSlot(text, 0, text.Length);
                    keys[slot] = text;
                    values[slot] = oldvalues[i];
                }
            }
        }

        private bool Equals(char[] text1, int off, int len, char[] text2)
        {
            if (len != text2.Length)
                return false;
            int limit = off + len;
            if (ignoreCase)
            {
                for (int i = 0; i < len; )
                {
                    int codePointAt = charUtils.CodePointAt(text1, off + i, limit);
                    if (Character.ToLowerCase(codePointAt) != charUtils.CodePointAt(text2, i))
                        return false;
                    i += Character.CharCount(codePointAt);
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

        private bool Equals(ICharSequence text1, char[] text2)
        {
            int len = text1.Length;
            if (len != text2.Length)
                return false;
            if (ignoreCase)
            {
                for (int i = 0; i < len; )
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLowerCase(codePointAt) != charUtils.CodePointAt(text2, i))
                        return false;
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (text1.CharAt(i) != text2[i])
                        return false;
                }
            }
            return true;
        }

        private int GetHashCode(char[] text, int offset, int len)
        {
            if (text == null)
                throw new NullReferenceException();
            int code = 0;
            int stop = offset + len;
            if (ignoreCase)
            {
                for (int i = offset; i < stop; )
                {
                    int codePointAt = charUtils.CodePointAt(text, i, stop);
                    code = code * 31 + Character.ToLowerCase(codePointAt);
                    i += Character.CharCount(codePointAt);
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

        private int GetHashCode(ICharSequence text)
        {
            if (text == null)
                throw new NullReferenceException();
            int code = 0;
            int len = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < len; )
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLowerCase(codePointAt);
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    code = code * 31 + text.CharAt(i);
                }
            }
            return code;
        }

        public void Remove(object key)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return count; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("{");
            foreach (KeyValuePair<Object, V> entry in EntrySet)
            {
                if (sb.Length > 1) sb.Append(", ");
                sb.Append(entry);
            }
            return sb.Append('}').ToString();
        }

        private EntrySet entrySet = null;
        private CharArraySet keySet = null;

        internal EntrySet CreateEntrySet()
        {
            return new EntrySet(true);
        }

        public EntrySet EntrySet
        {
            get
            {
                if (entrySet == null)
                {
                    entrySet = CreateEntrySet();
                }
                return entrySet;
            }
        }

        internal ISet<object> OriginalKeySet
        {
            get
            {
                return Keys;
            }
        }

        public ICollection<object> Keys
        {
            get
            {
                if (keySet == null)
                {
                    // prevent adding of entries
                    keySet = new AnonymousCharArraySet(this);
                }

                return keySet;
            }
        }

        private sealed class AnonymousCharArraySet : CharArraySet
        {
            public AnonymousCharArraySet(CharArrayMap<V> map)
                : base(map)
            {
            }

            public override bool Add(object o)
            {
                throw new NotSupportedException();
            }

            public override bool Add(ICharSequence text)
            {
                throw new NotSupportedException();
            }

            public override bool Add(string text)
            {
                throw new NotSupportedException();
            }

            public override bool Add(char[] text)
            {
                throw new NotSupportedException();
            }
        }

        public class EntryIterator : IEnumerator<KeyValuePair<object, V>>
        {
            private readonly CharArrayMap<V> parent;

            private int pos = -1;
            private int lastPos;
            private readonly bool allowModify;

            private KeyValuePair<object, V> current; // .NET Port: need to store current as IEnumerator != Iterator

            public EntryIterator(CharArrayMap<V> parent, bool allowModify)
            {
                this.parent = parent;
                this.allowModify = allowModify;
                GoNext();
            }

            private void GoNext()
            {
                lastPos = pos;
                pos++;
                while (pos < parent.keys.Length && parent.keys[pos] == null) pos++;
            }

            public bool MoveNext()
            {
                if (pos < parent.keys.Length)
                {
                    GoNext();

                    return true;
                }
                current = new MapEntry(lastPos, allowModify);
                return false;
            }

            public char[] NextKey()
            {
                GoNext();
                return parent.keys[lastPos];
            }

            public string NextKeyString()
            {
                return new string(NextKey());
            }

            public V CurrentValue
            {
                get
                {
                    return parent.values[lastPos];
                }
            }

            public V SetValue(V value)
            {
                if (!allowModify)
                    throw new NotSupportedException();
                V old = parent.values[lastPos];
                parent.values[lastPos] = value;
                return old;
            }
            
            public KeyValuePair<object, V> Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return current; }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        
    }
}
