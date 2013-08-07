using Lucene.Net.Analysis.Support;
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
        internal static readonly CharArrayMap<V> EMPTY_MAP = new CharArrayMap.EmptyCharArrayMap<V>();

        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private bool ignoreCase;
        private int count;
        internal readonly Lucene.Net.Util.Version? matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal V[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        public CharArrayMap(Lucene.Net.Util.Version? matchVersion, int startSize, bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            int size = INIT_SIZE;
            while (startSize + (startSize >> 2) > size)
                size <<= 1;
            keys = new char[size][];
            values = new V[size];
            this.charUtils = CharacterUtils.GetInstance(matchVersion.GetValueOrDefault());
            this.matchVersion = matchVersion;
        }

        public CharArrayMap(Lucene.Net.Util.Version? matchVersion, IDictionary<object, V> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            foreach (var kvp in c)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        internal CharArrayMap(CharArrayMap<V> toCopy)
        {
            this.keys = toCopy.keys;
            this.values = toCopy.values;
            this.ignoreCase = toCopy.ignoreCase;
            this.count = toCopy.count;
            this.charUtils = toCopy.charUtils;
            this.matchVersion = toCopy.matchVersion;
        }

        public virtual void Clear()
        {
            count = 0;
            Arrays.Fill(keys, null);
            Arrays.Fill(values, default(V));
        }

        public virtual bool ContainsKey(char[] text, int off, int len)
        {
            return keys[GetSlot(text, off, len)] != null;
        }

        public virtual bool ContainsKey(ICharSequence cs)
        {
            return keys[GetSlot(cs)] != null;
        }

        public virtual bool ContainsKey(Object o)
        {
            if (o is char[])
            {
                char[] text = (char[])o;
                return ContainsKey(text, 0, text.Length);
            }
            return ContainsKey(o.ToString());
        }

        public virtual V Get(char[] text, int off, int len)
        {
            return values[GetSlot(text, off, len)];
        }

        public virtual V Get(ICharSequence cs)
        {
            return values[GetSlot(cs)];
        }

        public virtual V Get(object o)
        {
            if (o is char[])
            {
                char[] text = (char[])o;
                return Get(text, 0, text.Length);
            }
            return Get(o.ToString());
        }

        public V this[Object o]
        {
            get
            {
                return Get(o); 
            }
            set
            {
                Put(o, value);
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

        public virtual V Put(object o, V value)
        {
            if (o is char[])
            {
                return Put((char[])o, value);
            }
            return Put(o.ToString(), value);
        }

        public virtual V Put(ICharSequence text, V value)
        {
            return Put(text.ToString(), value); // could be more efficient
        }

        public virtual V Put(string text, V value)
        {
            return Put(text.ToCharArray(), value);
        }

        public virtual V Put(char[] text, V value)
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

        public virtual void Remove(object key)
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
            foreach (KeyValuePair<Object, V> entry in this.GetEntrySet())
            {
                if (sb.Length > 1) sb.Append(", ");
                sb.Append(entry);
            }
            return sb.Append('}').ToString();
        }

        private EntrySet entrySet = null;
        private CharArraySet keySet = null;

        internal virtual EntrySet CreateEntrySet()
        {
            return new EntrySet(this, true);
        }

        public EntrySet GetEntrySet()
        {
            if (entrySet == null)
            {
                entrySet = CreateEntrySet();
            }
            return entrySet;
        }

        internal ISet<object> OriginalKeySet
        {
            get
            {
                return Keys as ISet<object>;
            }
        }

        public ICollection<object> Keys
        {
            get
            {
                if (keySet == null)
                {
                    // prevent adding of entries
                    keySet = new AnonymousCharArraySet(new CharArrayMap<object>(matchVersion, this.ToDictionary(i => (object)i.Key, i => (object)i.Value), ignoreCase));
                }

                return keySet;
            }
        }

        private sealed class AnonymousCharArraySet : CharArraySet
        {
            public AnonymousCharArraySet(CharArrayMap<object> map)
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

            private MapEntry current; // .NET Port: need to store current as IEnumerator != Iterator

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
                current = new MapEntry(parent, lastPos, allowModify);
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
                get { return current.AsKeyValuePair(); }
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

        private sealed class MapEntry // : KeyValuePair<object, V> -- this doesn't work in .NET as KVP is a struct, so we wrap it instead
        {
            private readonly CharArrayMap<V> parent;
            private readonly int pos;
            private readonly bool allowModify;

            public MapEntry(CharArrayMap<V> parent, int pos, bool allowModify)
            {
                this.parent = parent;
                this.pos = pos;
                this.allowModify = allowModify;
            }

            public object Key
            {
                get
                {
                    // we must clone here, as putAll to another CharArrayMap
                    // with other case sensitivity flag would corrupt the keys
                    return parent.keys[pos].Clone();
                }
            }

            public V Value
            {
                get
                {
                    return parent.values[pos];
                }
                set
                {
                    if (!allowModify)
                        throw new NotSupportedException();

                    parent.values[pos] = value;
                }
            }

            public override string ToString()
            {
                return new StringBuilder().Append(parent.keys[pos]).Append('=')
                    .Append((parent.values[pos].Equals(parent)) ? "(this Map)" : parent.values[pos].ToString())
                    .ToString();
            }

            public KeyValuePair<object, V> AsKeyValuePair()
            {
                return new KeyValuePair<object, V>(Key, Value);
            }
        }

        public sealed class EntrySet : AbstractSet<KeyValuePair<object, V>>
        {
            private readonly CharArrayMap<V> parent;
            private readonly bool allowModify;

            public EntrySet(CharArrayMap<V> parent, bool allowModify)
            {
                this.parent = parent;
                this.allowModify = allowModify;
            }

            public override IEnumerator<KeyValuePair<object, V>> GetEnumerator()
            {
                return new EntryIterator(parent, allowModify);
            }

            public override bool Contains(KeyValuePair<object, V> e)
            {
                //if (!(o instanceof Map.Entry))
                //  return false;
                //Map.Entry<Object,V> e = (Map.Entry<Object,V>)o;
                Object key = e.Key;
                Object val = e.Value;
                Object v = parent[key];
                return v == null ? val == null : v.Equals(val);
            }

            public override bool Remove(KeyValuePair<object, V> item)
            {
                throw new NotSupportedException();
            }

            public override int Count
            {
                get { return parent.count; }
            }

            public override void Clear()
            {
                if (!allowModify)
                    throw new NotSupportedException();
                parent.Clear();
            }
        }

        public void Add(object key, V value)
        {
            Put(key, value);
        }

        bool IDictionary<object, V>.Remove(object key)
        {
            Remove(key);
            return true;
        }

        public bool TryGetValue(object key, out V value)
        {
            value = Get(key);

            return value != null;
        }

        public ICollection<V> Values
        {
            get { return values; }
        }

        public void Add(KeyValuePair<object, V> item)
        {
            Put(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<object, V> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<object, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<object, V> item)
        {
            Remove(item.Key);
            return true;
        }

        public IEnumerator<KeyValuePair<object, V>> GetEnumerator()
        {
            return GetEntrySet().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // .NET Port: non-generic static clas to hold nested types and static methods
    public static class CharArrayMap
    {
        public static CharArrayMap<V> UnmodifiableMap<V>(CharArrayMap<V> map)
        {
            if (map == null)
                throw new NullReferenceException("Given map is null");
            if (map == EmptyMap<V>() || map.Count == 0)
                return EmptyMap<V>();
            if (map is UnmodifiableCharArrayMap<V>)
                return map;
            return new UnmodifiableCharArrayMap<V>(map);
        }

        public static CharArrayMap<V> Copy<V>(Lucene.Net.Util.Version? matchVersion, IDictionary<object, V> map)
        {
            if (map == CharArrayMap<V>.EMPTY_MAP)
                return EmptyMap<V>();
            if (map is CharArrayMap<V>)
            {
                CharArrayMap<V> m = (CharArrayMap<V>)map;
                // use fast path instead of iterating all values
                // this is even on very small sets ~10 times faster than iterating
                char[][] keys = new char[m.keys.Length][];
                Array.Copy(m.keys, 0, keys, 0, keys.Length);
                V[] values = new V[m.values.Length];
                Array.Copy(m.values, 0, values, 0, values.Length);
                m = new CharArrayMap<V>(m);
                m.keys = keys;
                m.values = values;
                return m;
            }
            return new CharArrayMap<V>(matchVersion, map, false);
        }

        public static CharArrayMap<V> EmptyMap<V>()
        {
            return CharArrayMap<V>.EMPTY_MAP;
        }

        internal class UnmodifiableCharArrayMap<V> : CharArrayMap<V>
        {
            public UnmodifiableCharArrayMap(CharArrayMap<V> map)
                : base(map)
            {
            }

            public override void Clear()
            {
                throw new NotSupportedException();
            }

            public override V Put(char[] text, V value)
            {
                throw new NotSupportedException();
            }

            public override V Put(ICharSequence text, V value)
            {
                throw new NotSupportedException();
            }

            public override V Put(string text, V value)
            {
                throw new NotSupportedException();
            }

            public override void Remove(object key)
            {
                throw new NotSupportedException();
            }

            internal override CharArrayMap<V>.EntrySet CreateEntrySet()
            {
                throw new NotSupportedException();
            }
        }

        internal sealed class EmptyCharArrayMap<V> : UnmodifiableCharArrayMap<V>
        {
            public EmptyCharArrayMap()
                : base(new CharArrayMap<V>(Lucene.Net.Util.Version.LUCENE_CURRENT, 0, false))
            {
            }

            public override bool ContainsKey(char[] text, int off, int len)
            {
                if (text == null)
                    throw new NullReferenceException();
                return false;
            }

            public override bool ContainsKey(ICharSequence cs)
            {
                if (cs == null)
                    throw new NullReferenceException();
                return false;
            }

            public override bool ContainsKey(object o)
            {
                if (o == null)
                    throw new NullReferenceException();
                return false;
            }

            public override V Get(char[] text, int off, int len)
            {
                if (text == null)
                    throw new NullReferenceException();
                return default(V);
            }

            public override V Get(ICharSequence cs)
            {
                if (cs == null)
                    throw new NullReferenceException();
                return default(V);
            }


        }
    }
}
