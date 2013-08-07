using Lucene.Net.Analysis.Support;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public class CharArraySet : AbstractSet<object>
    {
        public static readonly CharArraySet EMPTY_SET = new CharArraySet(CharArrayMap.EmptyMap<object>());
        private static readonly object PLACEHOLDER = new object();

        private readonly CharArrayMap<object> map;

        public CharArraySet(Lucene.Net.Util.Version matchVersion, int startSize, bool ignoreCase)
            : this(new CharArrayMap<Object>(matchVersion, startSize, ignoreCase))
        {
        }

        public CharArraySet(Lucene.Net.Util.Version matchVersion, ICollection<object> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            AddAll(c);
        }

        internal CharArraySet(CharArrayMap<Object> map)
        {
            this.map = map;
        }

        public override void Clear()
        {
            map.Clear();
        }

        public bool Contains(char[] text, int off, int len)
        {
            return map.ContainsKey(text, off, len);
        }

        public bool Contains(ICharSequence cs)
        {
            return map.ContainsKey(cs);
        }

        public override bool Contains(object o)
        {
            return map.ContainsKey(o);
        }

        public override bool Add(object o)
        {
            return map.Put(o, PLACEHOLDER) == null;
        }

        public bool Add(ICharSequence text)
        {
            return map.Put(text, PLACEHOLDER) == null;
        }

        public bool Add(string text)
        {
            return map.Put(text, PLACEHOLDER) == null;
        }

        public bool Add(char[] text)
        {
            return map.Put(text, PLACEHOLDER) == null;
        }

        public override int Count
        {
            get { return map.Count; }
        }

        public static CharArraySet UnmodifiableSet(CharArraySet set)
        {
            if (set == null)
                throw new NullReferenceException("Given set is null");
            if (set == EMPTY_SET)
                return EMPTY_SET;
            if (set.map is CharArrayMap.UnmodifiableCharArrayMap<object>)
                return set;
            return new CharArraySet(CharArrayMap.UnmodifiableMap(set.map));
        }

        public static CharArraySet Copy(Lucene.Net.Util.Version matchVersion, ICollection<object> set)
        {
            if (set == EMPTY_SET)
                return EMPTY_SET;
            if (set is CharArraySet)
            {
                CharArraySet source = (CharArraySet)set;
                return new CharArraySet(CharArrayMap.Copy(source.map.matchVersion, source.map));
            }
            return new CharArraySet(matchVersion, set, false);
        }

        public override IEnumerator<object> GetEnumerator()
        {
            // use the AbstractSet#keySet()'s iterator (to not produce endless recursion)
            return map.OriginalKeySet.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[");
            foreach (Object item in this)
            {
                if (sb.Length > 1) sb.Append(", ");
                if (item is char[])
                {
                    sb.Append((char[])item);
                }
                else
                {
                    sb.Append(item);
                }
            }
            return sb.Append(']').ToString();
        }
    }
}
