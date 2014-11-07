using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.analysis.util
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


	using Version = org.apache.lucene.util.Version;


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
	public class CharArrayMap<V> : AbstractMap<object, V>
	{
	  // private only because missing generics
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private static final CharArrayMap<?> EMPTY_MAP = new EmptyCharArrayMap<>();
	  private static readonly CharArrayMap<?> EMPTY_MAP = new EmptyCharArrayMap<?>();

	  private const int INIT_SIZE = 8;
	  private readonly CharacterUtils charUtils;
	  private bool ignoreCase;
	  private int count;
	  internal readonly Version matchVersion; // package private because used in CharArraySet
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
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") public CharArrayMap(org.apache.lucene.util.Version matchVersion, int startSize, boolean ignoreCase)
	  public CharArrayMap(Version matchVersion, int startSize, bool ignoreCase)
	  {
		this.ignoreCase = ignoreCase;
		int size_Renamed = INIT_SIZE;
		while (startSize + (startSize >> 2) > size_Renamed)
		{
		  size_Renamed <<= 1;
		}
		keys = new char[size_Renamed][];
		values = (V[]) new object[size_Renamed];
		this.charUtils = CharacterUtils.getInstance(matchVersion);
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
	  public CharArrayMap<T1>(Version matchVersion, IDictionary<T1> c, bool ignoreCase) where T1 : V : this(matchVersion, c.Count, ignoreCase)
	  {
		putAll(c);
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

	  /// <summary>
	  /// Clears all entries in this map. This method is supported for reusing, but not <seealso cref="Map#remove"/>. </summary>
	  public override void clear()
	  {
		count = 0;
		Arrays.fill(keys, null);
		Arrays.fill(values, null);
	  }

	  /// <summary>
	  /// true if the <code>len</code> chars of <code>text</code> starting at <code>off</code>
	  /// are in the <seealso cref="#keySet()"/> 
	  /// </summary>
	  public virtual bool containsKey(char[] text, int off, int len)
	  {
		return keys[getSlot(text, off, len)] != null;
	  }

	  /// <summary>
	  /// true if the <code>CharSequence</code> is in the <seealso cref="#keySet()"/> </summary>
	  public virtual bool containsKey(CharSequence cs)
	  {
		return keys[getSlot(cs)] != null;
	  }

	  public override bool containsKey(object o)
	  {
		if (o is char[])
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] text = (char[])o;
		  char[] text = (char[])o;
		  return containsKey(text, 0, text.Length);
		}
		return containsKey(o.ToString());
	  }

	  /// <summary>
	  /// returns the value of the mapping of <code>len</code> chars of <code>text</code>
	  /// starting at <code>off</code> 
	  /// </summary>
	  public virtual V get(char[] text, int off, int len)
	  {
		return values[getSlot(text, off, len)];
	  }

	  /// <summary>
	  /// returns the value of the mapping of the chars inside this {@code CharSequence} </summary>
	  public virtual V get(CharSequence cs)
	  {
		return values[getSlot(cs)];
	  }

	  public override V get(object o)
	  {
		if (o is char[])
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[] text = (char[])o;
		  char[] text = (char[])o;
		  return get(text, 0, text.Length);
		}
		return get(o.ToString());
	  }

	  private int getSlot(char[] text, int off, int len)
	  {
		int code = getHashCode(text, off, len);
		int pos = code & (keys.Length - 1);
		char[] text2 = keys[pos];
		if (text2 != null && !Equals(text, off, len, text2))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int inc = ((code>>8)+code)|1;
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
	  private int getSlot(CharSequence text)
	  {
		int code = getHashCode(text);
		int pos = code & (keys.Length - 1);
		char[] text2 = keys[pos];
		if (text2 != null && !Equals(text, text2))
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int inc = ((code>>8)+code)|1;
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
	  public virtual V put(CharSequence text, V value)
	  {
		return put(text.ToString(), value); // could be more efficient
	  }

	  public override V put(object o, V value)
	  {
		if (o is char[])
		{
		  return put((char[])o, value);
		}
		return put(o.ToString(), value);
	  }

	  /// <summary>
	  /// Add the given mapping. </summary>
	  public virtual V put(string text, V value)
	  {
		return put(text.ToCharArray(), value);
	  }

	  /// <summary>
	  /// Add the given mapping.
	  /// If ignoreCase is true for this Set, the text array will be directly modified.
	  /// The user should never modify this text array after calling this method.
	  /// </summary>
	  public virtual V put(char[] text, V value)
	  {
		if (ignoreCase)
		{
		  charUtils.ToLower(text, 0, text.Length);
		}
		int slot = getSlot(text, 0, text.Length);
		if (keys[slot] != null)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final V oldValue = values[slot];
		  V oldValue = values[slot];
		  values[slot] = value;
		  return oldValue;
		}
		keys[slot] = text;
		values[slot] = value;
		count++;

		if (count + (count >> 2) > keys.Length)
		{
		  rehash();
		}

		return null;
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") private void rehash()
	  private void rehash()
	  {
		Debug.Assert(keys.Length == values.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newSize = 2*keys.length;
		int newSize = 2 * keys.Length;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[][] oldkeys = keys;
		char[][] oldkeys = keys;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final V[] oldvalues = values;
		V[] oldvalues = values;
		keys = new char[newSize][];
		values = (V[]) new object[newSize];

		for (int i = 0; i < oldkeys.Length; i++)
		{
		  char[] text = oldkeys[i];
		  if (text != null)
		  {
			// todo: could be faster... no need to compare strings on collision
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int slot = getSlot(text,0,text.length);
			int slot = getSlot(text,0,text.Length);
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
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int limit = off+len;
		int limit = off + len;
		if (ignoreCase)
		{
		  for (int i = 0;i < len;)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePointAt = charUtils.codePointAt(text1, off+i, limit);
			int codePointAt = charUtils.codePointAt(text1, off + i, limit);
			if (char.ToLower(codePointAt) != charUtils.codePointAt(text2, i, text2.Length))
			{
			  return false;
			}
			i += char.charCount(codePointAt);
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

	  private bool Equals(CharSequence text1, char[] text2)
	  {
		int len = text1.length();
		if (len != text2.Length)
		{
		  return false;
		}
		if (ignoreCase)
		{
		  for (int i = 0;i < len;)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePointAt = charUtils.codePointAt(text1, i);
			int codePointAt = charUtils.codePointAt(text1, i);
			if (char.ToLower(codePointAt) != charUtils.codePointAt(text2, i, text2.Length))
			{
			  return false;
			}
			i += char.charCount(codePointAt);
		  }
		}
		else
		{
		  for (int i = 0;i < len;i++)
		  {
			if (text1.charAt(i) != text2[i])
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
		  throw new System.NullReferenceException();
		}
		int code = 0;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int stop = offset + len;
		int stop = offset + len;
		if (ignoreCase)
		{
		  for (int i = offset; i < stop;)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int codePointAt = charUtils.codePointAt(text, i, stop);
			int codePointAt = charUtils.codePointAt(text, i, stop);
			code = code * 31 + char.ToLower(codePointAt);
			i += char.charCount(codePointAt);
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

	  private int getHashCode(CharSequence text)
	  {
		if (text == null)
		{
		  throw new System.NullReferenceException();
		}
		int code = 0;
		int len = text.length();
		if (ignoreCase)
		{
		  for (int i = 0; i < len;)
		  {
			int codePointAt = charUtils.codePointAt(text, i);
			code = code * 31 + char.ToLower(codePointAt);
			i += char.charCount(codePointAt);
		  }
		}
		else
		{
		  for (int i = 0; i < len; i++)
		  {
			code = code * 31 + text.charAt(i);
		  }
		}
		return code;
	  }

	  public override V remove(object key)
	  {
		throw new System.NotSupportedException();
	  }

	  public override int size()
	  {
		return count;
	  }

	  public override string ToString()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder sb = new StringBuilder("{");
		StringBuilder sb = new StringBuilder("{");
		foreach (KeyValuePair<object, V> entry in entrySet())
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

	  internal virtual EntrySet createEntrySet()
	  {
		return new EntrySet(this, true);
	  }

	  public override EntrySet entrySet()
	  {
		if (entrySet_Renamed == null)
		{
		  entrySet_Renamed = createEntrySet();
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
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @SuppressWarnings({"unchecked","rawtypes"}) public final CharArraySet keySet()
	  public override CharArraySet keySet()
	  {
		if (keySet_Renamed == null)
		{
		  // prevent adding of entries
		  keySet_Renamed = new CharArraySetAnonymousInnerClassHelper(this, (CharArrayMap) this);
		}
		return keySet_Renamed;
	  }

	  private class CharArraySetAnonymousInnerClassHelper : CharArraySet
	  {
		  private readonly CharArrayMap<V> outerInstance;

		  public CharArraySetAnonymousInnerClassHelper(CharArrayMap<V> outerInstance, CharArrayMap (CharArrayMap) this) : base((CharArrayMap) this)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override bool add(object o)
		  {
			throw new System.NotSupportedException();
		  }
		  public override bool add(CharSequence text)
		  {
			throw new System.NotSupportedException();
		  }
		  public override bool add(string text)
		  {
			throw new System.NotSupportedException();
		  }
		  public override bool add(char[] text)
		  {
			throw new System.NotSupportedException();
		  }
	  }

	  /// <summary>
	  /// public iterator class so efficient methods are exposed to users </summary>
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
		  goNext();
		}

		internal virtual void goNext()
		{
		  lastPos = pos;
		  pos++;
		  while (pos < outerInstance.keys.Length && outerInstance.keys[pos] == null)
		  {
			  pos++;
		  }
		}

		public override bool hasNext()
		{
		  return pos < outerInstance.keys.Length;
		}

		/// <summary>
		/// gets the next key... do not modify the returned char[] </summary>
		public virtual char[] nextKey()
		{
		  goNext();
		  return outerInstance.keys[lastPos];
		}

		/// <summary>
		/// gets the next key as a newly created String object </summary>
		public virtual string nextKeyString()
		{
		  return new string(nextKey());
		}

		/// <summary>
		/// returns the value associated with the last key returned </summary>
		public virtual V currentValue()
		{
		  return outerInstance.values[lastPos];
		}

		/// <summary>
		/// sets the value associated with the last key returned </summary>
		public virtual V setValue(V value)
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
		/// use nextCharArray() + currentValue() for better efficiency. </summary>
		public override KeyValuePair<object, V> next()
		{
		  goNext();
		  return new MapEntry(outerInstance, lastPos, allowModify);
		}

		public override void remove()
		{
		  throw new System.NotSupportedException();
		}
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

		public override object Key
		{
			get
			{
			  // we must clone here, as putAll to another CharArrayMap
			  // with other case sensitivity flag would corrupt the keys
			  return outerInstance.keys[pos].clone();
			}
		}

		public override V Value
		{
			get
			{
			  return outerInstance.values[pos];
			}
		}

		public override V setValue(V value)
		{
		  if (!allowModify)
		  {
			throw new System.NotSupportedException();
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final V old = values[pos];
		  V old = outerInstance.values[pos];
		  outerInstance.values[pos] = value;
		  return old;
		}

		public override string ToString()
		{
		  return (new StringBuilder()).Append(outerInstance.keys[pos]).Append('=').Append((outerInstance.values[pos] == outerInstance) ? "(this Map)" : outerInstance.values[pos]).ToString();
		}
	  }

	  /// <summary>
	  /// public EntrySet class so efficient methods are exposed to users </summary>
	  public sealed class EntrySet : AbstractSet<KeyValuePair<object, V>>
	  {
		  private readonly CharArrayMap<V> outerInstance;

		internal readonly bool allowModify;

		internal EntrySet(CharArrayMap<V> outerInstance, bool allowModify)
		{
			this.outerInstance = outerInstance;
		  this.allowModify = allowModify;
		}

		public override EntryIterator iterator()
		{
		  return new EntryIterator(outerInstance, allowModify);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @SuppressWarnings("unchecked") public boolean contains(Object o)
		public override bool contains(object o)
		{
		  if (!(o is DictionaryEntry))
		  {
			return false;
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map.Entry<Object,V> e = (java.util.Map.Entry<Object,V>)o;
		  KeyValuePair<object, V> e = (KeyValuePair<object, V>)o;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object key = e.getKey();
		  object key = e.Key;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object val = e.getValue();
		  object val = e.Value;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object v = get(key);
		  object v = outerInstance.get(key);
		  return v == null ? val == null : v.Equals(val);
		}

		public override bool remove(object o)
		{
		  throw new System.NotSupportedException();
		}

		public override int size()
		{
		  return outerInstance.count;
		}

		public override void clear()
		{
		  if (!allowModify)
		  {
			throw new System.NotSupportedException();
		  }
		  outerInstance.clear();
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
	  public static CharArrayMap<V> unmodifiableMap<V>(CharArrayMap<V> map)
	  {
		if (map == null)
		{
		  throw new System.NullReferenceException("Given map is null");
		}
		if (map == emptyMap() || map.Empty)
		{
		  return emptyMap();
		}
		if (map is UnmodifiableCharArrayMap)
		{
		  return map;
		}
		return new UnmodifiableCharArrayMap<>(map);
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
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
	  SuppressWarnings("unchecked") public static <V> CharArrayMap<V> copy(final org.apache.lucene.util.Version matchVersion, final java.util.Map<?,? extends V> map)
	  {
		if (map == EMPTY_MAP)
		{
		  return emptyMap();
		}
		if (map is CharArrayMap)
		{
		  CharArrayMap<V> m = (CharArrayMap<V>) map;
		  // use fast path instead of iterating all values
		  // this is even on very small sets ~10 times faster than iterating
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final char[][] keys = new char[m.keys.length][];
		  char[][] keys = new char[m.keys.Length][];
		  Array.Copy(m.keys, 0, keys, 0, keys.Length);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final V[] values = (V[]) new Object[m.values.length];
		  V[] values = (V[]) new object[m.values.Length];
		  Array.Copy(m.values, 0, values, 0, values.Length);
		  m = new CharArrayMap<>(m);
		  m.keys = keys;
		  m.values = values;
		  return m;
		}
		return new CharArrayMap<>(matchVersion, map, false);
	  }

	  /// <summary>
	  /// Returns an empty, unmodifiable map. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") public static <V> CharArrayMap<V> emptyMap()
	  public static <V> CharArrayMap<V> emptyMap()
	  {
		return (CharArrayMap<V>) EMPTY_MAP;
	  }

	  // package private CharArraySet instanceof check in CharArraySet
	  static class UnmodifiableCharArrayMap<V> extends CharArrayMap<V>
	  {

		UnmodifiableCharArrayMap(CharArrayMap<V> map)
		{
		  base(map);
		}

		public void clear()
		{
		  throw new System.NotSupportedException();
		}

		public V put(object o, V val)
		{
		  throw new System.NotSupportedException();
		}

		public V put(char[] text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public V put(CharSequence text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public V put(string text, V val)
		{
		  throw new System.NotSupportedException();
		}

		public V remove(object key)
		{
		  throw new System.NotSupportedException();
		}

		EntrySet createEntrySet()
		{
		  return new EntrySet(this, false);
		}
	  }

	  /// <summary>
	  /// Empty <seealso cref="org.apache.lucene.analysis.util.CharArrayMap.UnmodifiableCharArrayMap"/> optimized for speed.
	  /// Contains checks will always return <code>false</code> or throw
	  /// NPE if necessary.
	  /// </summary>
	  private static final class EmptyCharArrayMap<V> extends UnmodifiableCharArrayMap<V>
	  {
		EmptyCharArrayMap()
		{
		  base(new CharArrayMap<V>(Version.LUCENE_CURRENT, 0, false));
		}

		public bool containsKey(char[] text, int off, int len)
		{
		  if (text == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public bool containsKey(CharSequence cs)
		{
		  if (cs == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public bool containsKey(object o)
		{
		  if (o == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return false;
		}

		public V get(char[] text, int off, int len)
		{
		  if (text == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return null;
		}

		public V get(CharSequence cs)
		{
		  if (cs == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return null;
		}

		public V get(object o)
		{
		  if (o == null)
		  {
			throw new System.NullReferenceException();
		  }
		  return null;
		}
	  }
	}

}