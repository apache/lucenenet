using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Util
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


	/// <summary>
	/// A simple class that stores key Strings as char[]'s in a
	/// hash table. Note that this is not a general purpose
	/// class.  For example, it cannot remove items from the
	/// map, nor does it resize its hash table to be smaller,
	/// etc.  It is designed to be quick to retrieve items
	/// by char[] keys without the necessity of converting
	/// to a String first.
	/// 
	/// <a name="version"></a>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating <seealso cref="CharArrayMap"/>:
	/// <ul>
	///   <li> As of 3.1, supplementary characters are
	///       properly lowercased.</li>
	/// </ul>
	/// Before 3.1 supplementary characters could not be
	/// lowercased correctly due to the lack of Unicode 4
	/// support in JDK 1.4. To use instances of
	/// <seealso cref="CharArrayMap"/> with the behavior before Lucene
	/// 3.1 pass a <seealso cref="Version"/> &lt; 3.1 to the constructors.
	/// </para>
	/// </summary>
	public class CharArrayMap<V> : IDictionary<object, V>
	{
	  // private only because missing generics
	  private static readonly CharArrayMap<char[]> EMPTY_MAP = new EmptyCharArrayMap<char[]>();

	  private const int INIT_SIZE = 8;
	  private readonly CharacterUtils charUtils;
	  private bool ignoreCase;
	  private int count;
	  internal readonly LuceneVersion matchVersion; // package private because used in CharArraySet
	  internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
	  internal V[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

	  /// <summary>
	  /// Create map with enough capacity to hold startSize terms
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          compatibility match version see <a href="#version">Version
	  ///          note</a> above for details. </param>
	  /// <param name="startSize">
	  ///          the initial capacity </param>
	  /// <param name="ignoreCase">
	  ///          <code>false</code> if and only if the set should be case sensitive
	  ///          otherwise <code>true</code>. </param>
	  public CharArrayMap(LuceneVersion matchVersion, int startSize, bool ignoreCase)
	  {
		this.ignoreCase = ignoreCase;
		int size = INIT_SIZE;
		while (startSize + (startSize >> 2) > size)
		{
		  size <<= 1;
		}
		keys = new char[size][];
		values = new V[size];
		this.charUtils = CharacterUtils.GetInstance(matchVersion);
		this.matchVersion = matchVersion;
	  }

	  /// <summary>
	  /// Creates a map from the mappings in another map. 
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          compatibility match version see <a href="#version">Version
	  ///          note</a> above for details. </param>
	  /// <param name="c">
	  ///          a map whose mappings to be copied </param>
	  /// <param name="ignoreCase">
	  ///          <code>false</code> if and only if the set should be case sensitive
	  ///          otherwise <code>true</code>. </param>
	  public CharArrayMap(LuceneVersion matchVersion, IDictionary<object, V> c, bool ignoreCase)
          : this(matchVersion, c.Count, ignoreCase)
	  {
	      foreach (var v in c)
	      {
	          Add(v);
	      }
	  }

	  /// <summary>
	  /// Create set from the supplied map (used internally for readonly maps...) </summary>
	  private CharArrayMap(CharArrayMap<V> toCopy)
	  {
		this.keys = toCopy.keys;
		this.values = toCopy.values;
		this.ignoreCase = toCopy.ignoreCase;
		this.count = toCopy.count;
		this.charUtils = toCopy.charUtils;
		this.matchVersion = toCopy.matchVersion;
	  }

	    public void Add(KeyValuePair<object, V> item)
	    {
	        Put(item.Key, item.Value);
	    }

	    /// <summary>
	  /// Clears all entries in this map. This method is supported for reusing, but not <seealso cref="Map#remove"/>. </summary>
	  public virtual void Clear()
	  {
		count = 0;
		Arrays.Fill(keys, null);
		Arrays.Fill(values, default(V));
	  }

	    public bool Contains(KeyValuePair<object, V> item)
	    {
	        throw new NotImplementedException();
	    }

	    /// <summary>
	  /// true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
	  /// are in the <seealso cref="#keySet()"/> 
	  /// </summary>
	  public virtual bool ContainsKey(char[] text, int off, int len)
	  {
		return keys[GetSlot(text, off, len)] != null;
	  }

	  /// <summary>
	  /// true if the <code>CharSequence</code> is in the <seealso cref="#keySet()"/> </summary>
	  public virtual bool ContainsKey(string cs)
	  {
		return keys[GetSlot(cs)] != null;
	  }

	    public virtual bool ContainsKey(object o)
	    {
	        var c = o as char[];
	        if (c != null)
	        {
	            var text = c;
	            return ContainsKey(text, 0, text.Length);
	        }
	        return ContainsKey(o.ToString());
	    }

	    public void Add(object key, V value)
	    {
	        Put(key, value);
	    }

	    /// <summary>
	  /// returns the value of the mapping of <code>len</code> chars of <code>text</code>
	  /// starting at <code>off</code> 
	  /// </summary>
	  public virtual V Get(char[] text, int off, int len)
	  {
		return values[GetSlot(text, off, len)];
	  }

	  /// <summary>
	  /// returns the value of the mapping of the chars inside this {@code CharSequence} </summary>
	  public virtual V Get(string cs)
	  {
		return values[GetSlot(cs)];
	  }

	  public virtual V Get(object o)
	  {
	      var text = o as char[];
		if (text != null)
		{
		    return Get(text, 0, text.Length);
		}
		return Get(o.ToString());
	  }

	  private int GetSlot(char[] text, int off, int len)
	  {
		int code = getHashCode(text, off, len);
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

	  /// <summary>
	  /// Returns true if the String is in the set </summary>
	  private int GetSlot(string text)
	  {
		int code = getHashCode(text);
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

	  /// <summary>
	  /// Add the given mapping. </summary>
	  public virtual V Put(ICharSequence text, V value)
	  {
		return Put(text.ToString(), value); // could be more efficient
	  }

	  public virtual V Put(object o, V value)
	  {
	      var c = o as char[];
	      if (c != null)
		{
		  return Put(c, value);
		}
		return Put(o.ToString(), value);
	  }

	  /// <summary>
	  /// Add the given mapping. </summary>
	  public virtual V Put(string text, V value)
	  {
		return Put(text.ToCharArray(), value);
	  }

	  /// <summary>
	  /// Add the given mapping.
	  /// If ignoreCase is true for this Set, the text array will be directly modified.
	  /// The user should never modify this text array after calling this method.
	  /// </summary>
	  public virtual V Put(char[] text, V value)
	  {
		if (ignoreCase)
		{
		  charUtils.ToLower(text, 0, text.Length);
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
		Debug.Assert(keys.Length == values.Length);
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
			int slot = GetSlot(text,0,text.Length);
			keys[slot] = text;
			values[slot] = oldvalues[i];
		  }
		}
	  }

	  private bool Equals(char[] text1, int off, int len, char[] text2)
	  {
		if (len != text2.Length)
		{
		  return false;
		}
		int limit = off + len;
		if (ignoreCase)
		{
		  for (int i = 0;i < len;)
		  {
			var codePointAt = charUtils.CodePointAt(text1, off + i, limit);
			if (char.ToLower((char)codePointAt) != charUtils.CodePointAt(text2, i, text2.Length))
			{
			  return false;
			}
			i += Character.CharCount(codePointAt);
		  }
		}
		else
		{
		  for (int i = 0;i < len;i++)
		  {
			if (text1[off + i] != text2[i])
			{
			  return false;
			}
		  }
		}
		return true;
	  }

	    private bool Equals(ICharSequence text1, char[] text2)
	  {
		int len = text1.Length;
		if (len != text2.Length)
		{
		  return false;
		}
		if (ignoreCase)
		{
		  for (int i = 0;i < len;)
		  {
			int codePointAt = charUtils.CodePointAt(text1, i);
			if (char.ToLower((char)codePointAt) != charUtils.CodePointAt(text2, i, text2.Length))
			{
			  return false;
			}
			i += Character.CharCount(codePointAt);
		  }
		}
		else
		{
		  for (int i = 0;i < len;i++)
		  {
			if (text1.CharAt(i) != text2[i])
			{
			  return false;
			}
		  }
		}
		return true;
	  }

	  private int getHashCode(char[] text, int offset, int len)
	  {
		if (text == null)
		{
		  throw new ArgumentException("text can't be null", "text");
		}
		int code = 0;
		int stop = offset + len;
		if (ignoreCase)
		{
		  for (int i = offset; i < stop;)
		  {
			int codePointAt = charUtils.CodePointAt(text, i, stop);
			code = code * 31 + char.ToLower((char)codePointAt);
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

	  private int getHashCode(string text)
	  {
		if (text == null)
		{
            throw new ArgumentException("text can't be null", "text");
		}
		int code = 0;
		int len = text.Length;
		if (ignoreCase)
		{
		  for (int i = 0; i < len;)
		  {
			int codePointAt = charUtils.CodePointAt(text, i);
			code = code * 31 + char.ToLower((char)codePointAt);
			i += Character.CharCount(codePointAt);
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

	  public virtual bool Remove(object key)
	  {
		throw new System.NotSupportedException();
	  }

	    public bool TryGetValue(object key, out V value)
	    {
	        throw new NotImplementedException();
	    }

	    public V this[object key]
	    {
	        get { return Get(key); }
	        set { throw new NotSupportedException(); }
	    }

	    public bool Remove(KeyValuePair<object, V> item)
	    {
            throw new System.NotSupportedException();
	    }

	    public int Count
	    {
	        get
	        {
	            {
	                return count;
	            }
	        }
	    }

	    public bool IsReadOnly { get; private set; }

	    public override string ToString()
	  {
		var sb = new StringBuilder("{");
		foreach (KeyValuePair<object, V> entry in GetEntrySet())
		{
		  if (sb.Length > 1)
		  {
			  sb.Append(", ");
		  }
		  sb.Append(entry);
		}
		return sb.Append('}').ToString();
	  }

	  private EntrySet entrySet_Renamed = null;
	  private CharArraySet keySet_Renamed = null;

	  internal virtual EntrySet CreateEntrySet()
	  {
		return new EntrySet(this, true);
	  }

	  public EntrySet GetEntrySet()
	  {
		if (entrySet_Renamed == null)
		{
		  entrySet_Renamed = CreateEntrySet();
		}
		return entrySet_Renamed;
	  }

	  // helper for CharArraySet to not produce endless recursion
	  internal HashSet<object> originalKeySet()
	  {
		return base.Keys;
	  }

	  /// <summary>
	  /// Returns an <seealso cref="CharArraySet"/> view on the map's keys.
	  /// The set will use the same {@code matchVersion} as this map. 
	  /// </summary>
	  public CharArraySet KeySet()
	  {
		if (keySet_Renamed == null)
		{
		  // prevent adding of entries
		  keySet_Renamed = new CharArraySetAnonymousInnerClassHelper(this);
		}
		return keySet_Renamed;
	  }

	  private sealed class CharArraySetAnonymousInnerClassHelper : CharArraySet
	  {
	      internal CharArraySetAnonymousInnerClassHelper(CharArrayMap<object> map) : base(map)
	      {
              // TODO
	      }

	      public override bool Add(object o)
		  {
			throw new System.NotSupportedException();
		  }
		  public bool Add(ICharSequence text)
		  {
			throw new System.NotSupportedException();
		  }
		  public override bool Add(string text)
		  {
			throw new System.NotSupportedException();
		  }
		  public override bool Add(char[] text)
		  {
			throw new System.NotSupportedException();
		  }
	  }

	  /// <summary>
	  /// public iterator class so efficient methods are exposed to users
	  /// </summary>
	  public class EntryIterator : IEnumerator<KeyValuePair<object, V>>
	  {
		  private readonly CharArrayMap<V> outerInstance;

		internal int pos = -1;
		internal int lastPos;
		internal readonly bool allowModify;

		internal EntryIterator(CharArrayMap<V> outerInstance, bool allowModify)
		{
			this.outerInstance = outerInstance;
		  this.allowModify = allowModify;
		  GoNext();
		}

		internal void GoNext()
		{
		  lastPos = pos;
		  pos++;
		  while (pos < outerInstance.keys.Length && outerInstance.keys[pos] == null)
		  {
			  pos++;
		  }
		}

		public bool HasNext()
		{
		  return pos < outerInstance.keys.Length;
		}

		/// <summary>
		/// gets the next key... do not modify the returned char[] </summary>
		public virtual char[] NextKey()
		{
		  GoNext();
		  return outerInstance.keys[lastPos];
		}

		/// <summary>
		/// gets the next key as a newly created String object </summary>
		public virtual string NextKeyString()
		{
		  return new string(NextKey());
		}

		/// <summary>
		/// returns the value associated with the last key returned </summary>
		public virtual V CurrentValue()
		{
		  return outerInstance.values[lastPos];
		}

		/// <summary>
		/// sets the value associated with the last key returned </summary>
		public virtual V SetValue(V value)
		{
		  if (!allowModify)
		  {
			throw new System.NotSupportedException();
		  }
		  V old = outerInstance.values[lastPos];
		  outerInstance.values[lastPos] = value;
		  return old;
		}

		/// <summary>
		/// use nextCharArray() + currentValue() for better efficiency.
		/// </summary>
		public KeyValuePair<object, V> Next()
		{
		  GoNext();
            //return new KeyValuePair<object, V>();
		  return new MapEntry(outerInstance, lastPos, allowModify);
		}

		public void Remove()
		{
		  throw new System.NotSupportedException();
		}

        #region Added for better .NET support
        public void Dispose()
	      {	          
	      }

	      public bool MoveNext()
	      {
	          if (!HasNext()) return false;
	          GoNext();
	          return true;
	      }

	      public void Reset()
	      {
	          pos = -1;
              GoNext();
	      }

          public KeyValuePair<object, V> Current { get { return new KeyValuePair<object, V>(outerInstance.keys[lastPos], outerInstance.values[lastPos]); } private set { } }

	      object IEnumerator.Current
	      {
	          get { return CurrentValue(); }
          }
          #endregion
      }

	  private sealed class MapEntry : KeyValuePair<object, V>
	  {
		  private readonly CharArrayMap<V> outerInstance;

		internal readonly int pos;
		internal readonly bool allowModify;

		internal MapEntry(CharArrayMap<V> outerInstance, int pos, bool allowModify)
		{
			this.outerInstance = outerInstance;
		  this.pos = pos;
		  this.allowModify = allowModify;
		}

		public object Key
		{
			get
			{
			  // we must clone here, as putAll to another CharArrayMap
			  // with other case sensitivity flag would corrupt the keys
			  return outerInstance.keys[pos].Clone();
			}
		}

		public V Value
		{
			get
			{
			  return outerInstance.values[pos];
			}
		}

		public V setValue(V value)
		{
		  if (!allowModify)
		  {
			throw new System.NotSupportedException();
		  }
		  V old = outerInstance.values[pos];
		  outerInstance.values[pos] = value;
		  return old;
		}

		public override string ToString()
		{
		  return (new StringBuilder())
              .Append(outerInstance.keys[pos])
              .Append('=')
              .Append((outerInstance.values[pos] == outerInstance) ? "(this Map)" : outerInstance.values[pos]).ToString();
		}
	  }

	  /// <summary>
	  /// public EntrySet class so efficient methods are exposed to users </summary>
	  public sealed class EntrySet : ISet<KeyValuePair<object, V>>
	  {
		  private readonly CharArrayMap<V> outerInstance;

		internal readonly bool allowModify;

		internal EntrySet(CharArrayMap<V> outerInstance, bool allowModify)
		{
			this.outerInstance = outerInstance;
		  this.allowModify = allowModify;
		}

	      public IEnumerator GetEnumerator()
	      {
              return new EntryIterator(outerInstance, allowModify);
	      }

		public override bool Contains(object o)
		{
		  if (!(o is DictionaryEntry))
		  {
			return false;
		  }
		  var e = (KeyValuePair<object, V>)o;
		  object key = e.Key;
		  object val = e.Value;
		  object v = outerInstance.Get(key);
		  return v == null ? val == null : v.Equals(val);
		}

	      public bool Remove(KeyValuePair<object, V> item)
	      {
              throw new System.NotSupportedException();
	      }

          public int Count { get { return outerInstance.count; } private set { throw new NotSupportedException(); } }

		public void Clear()
		{
		  if (!allowModify)
		  {
			throw new System.NotSupportedException();
		  }
		  outerInstance.Clear();
		}
	  }

	  /// <summary>
	  /// Returns an unmodifiable <seealso cref="CharArrayMap"/>. This allows to provide
	  /// unmodifiable views of internal map for "read-only" use.
	  /// </summary>
	  /// <param name="map">
	  ///          a map for which the unmodifiable map is returned. </param>
	  /// <returns> an new unmodifiable <seealso cref="CharArrayMap"/>. </returns>
	  /// <exception cref="NullPointerException">
	  ///           if the given map is <code>null</code>. </exception>
	  public static CharArrayMap<V> UnmodifiableMap<V>(CharArrayMap<V> map)
	  {
		if (map == null)
		{
		  throw new System.NullReferenceException("Given map is null");
		}
		if (map == EmptyMap() || map.Empty)
		{
		  return EmptyMap();
		}
		if (map is UnmodifiableCharArrayMap)
		{
		  return map;
		}
		return new UnmodifiableCharArrayMap<V>(map);
	  }

	  /// <summary>
	  /// Returns a copy of the given map as a <seealso cref="CharArrayMap"/>. If the given map
	  /// is a <seealso cref="CharArrayMap"/> the ignoreCase property will be preserved.
	  /// <para>
	  /// <b>Note:</b> If you intend to create a copy of another <seealso cref="CharArrayMap"/> where
	  /// the <seealso cref="Version"/> of the source map differs from its copy
	  /// <seealso cref="#CharArrayMap(Version, Map, boolean)"/> should be used instead.
	  /// The <seealso cref="#copy(Version, Map)"/> will preserve the <seealso cref="Version"/> of the
	  /// source map it is an instance of <seealso cref="CharArrayMap"/>.
	  /// </para>
	  /// </summary>
	  /// <param name="matchVersion">
	  ///          compatibility match version see <a href="#version">Version
	  ///          note</a> above for details. This argument will be ignored if the
	  ///          given map is a <seealso cref="CharArrayMap"/>. </param>
	  /// <param name="map">
	  ///          a map to copy </param>
	  /// <returns> a copy of the given map as a <seealso cref="CharArrayMap"/>. If the given map
	  ///         is a <seealso cref="CharArrayMap"/> the ignoreCase property as well as the
	  ///         matchVersion will be of the given map will be preserved. </returns>
	 public static CharArrayMap<V> copy(LuceneVersion matchVersion, IDictionary<object, V> map)
	  {
		if (map == EMPTY_MAP)
		{
		  return EmptyMap();
		}
		if (map is CharArrayMap)
		{
		  var m = (CharArrayMap<V>) map;
		  // use fast path instead of iterating all values
		  // this is even on very small sets ~10 times faster than iterating
		  var keys = new char[m.keys.Length][];
		  Array.Copy(m.keys, 0, keys, 0, keys.Length);
		  var values = new V[m.values.Length];
		  Array.Copy(m.values, 0, values, 0, values.Length);
		  m = new CharArrayMap<V>(m) {keys = keys, values = values};
		    return m;
		}
		return new CharArrayMap<V>(matchVersion, map, false);
	  }

	  /// <summary>
	  /// Returns an empty, unmodifiable map. </summary>
	  public static CharArrayMap<char[]> EmptyMap()
	  {
		return EMPTY_MAP;
	  }

	  // package private CharArraySet instanceof check in CharArraySet
	  class UnmodifiableCharArrayMap<V> : CharArrayMap<V>
	  {

		public UnmodifiableCharArrayMap(CharArrayMap<V> map) : base(map)
		{
		  
		}

		public override void Clear()
		{
		  throw new System.NotSupportedException();
		}

		public override V Put(object o, V val)
		{
		  throw new System.NotSupportedException();
		}

		public override V Put(char[] text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public override V Put(ICharSequence text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public override V Put(string text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public override bool Remove(object key)
		{
		  throw new System.NotSupportedException();
		}

		override EntrySet CreateEntrySet()
		{
		  return new EntrySet(this, false);
		}
	  }

	  /// <summary>
	  /// Empty <seealso cref="CharArrayMap{V}.UnmodifiableCharArrayMap"/> optimized for speed.
	  /// Contains checks will always return <code>false</code> or throw
	  /// NPE if necessary.
	  /// </summary>
	  private class EmptyCharArrayMap<V> : UnmodifiableCharArrayMap<V>
	  {
		public EmptyCharArrayMap():base(new CharArrayMap<V>(LuceneVersion.LUCENE_CURRENT, 0, false))
		{
		  
		}

		public override bool ContainsKey(char[] text, int off, int len)
		{
		  if (text == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public bool ContainsKey(ICharSequence cs)
		{
		  if (cs == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public override bool ContainsKey(object o)
		{
		  if (o == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public override V Get(char[] text, int off, int len)
		{
		  if (text == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return default(V);
		}

		public V Get(ICharSequence cs)
		{
		  if (cs == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return default(V);
		}

		public override V Get(object o)
		{
		  if (o == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return default(V);
		}
	  }
	}
}