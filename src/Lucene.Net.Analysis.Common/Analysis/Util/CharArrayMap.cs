﻿// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Globalization;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

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
    /// A simple class that stores key <see cref="string"/>s as <see cref="T:char[]"/>'s in a
    /// hash table. Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// map, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to retrieve items
    /// by <see cref="T:char[]"/> keys without the necessity of converting
    /// to a <see cref="string"/> first.
    /// 
    /// <a name="version"></a>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="CharArrayMap"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, supplementary characters are
    ///       properly lowercased.</description></item>
    /// </list>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArrayMap"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> &lt; 3.1 to the constructors.
    /// </para>
    /// </summary>
    public class CharArrayMap<TValue> : ICharArrayMap, IDictionary<string, TValue>
    {
        // private only because missing generics
        private static readonly CharArrayMap<TValue> EMPTY_MAP = new CharArrayMap.EmptyCharArrayMap<TValue>();

        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private readonly bool ignoreCase;
        private int count;
        private readonly LuceneVersion matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal MapValue[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        /// <summary>
        /// LUCENENET: Moved this from CharArraySet so it doesn't need to know the generic type of CharArrayMap
        /// </summary>
        internal static readonly MapValue PLACEHOLDER = new MapValue();

        /// <summary>
        /// LUCENENET SPECIFIC type used to act as a placeholder. Since <c>null</c>
        /// means that our value is not populated, we need an instance of something
        /// to indicate it is. Using an instance of <typeparamref name="TValue"/> would only work if
        /// we could constrain it with the new() constraint, which isn't possible because
        /// some types such as <see cref="string"/> don't have a default constructor.
        /// So, this is a workaround that allows any type regardless of the type of constructor.
        /// 
        /// <para>
        /// Note also that we gain the ability to use value types for <typeparamref name="TValue"/>, but
        /// also create a difference in behavior from Java Lucene where the actual values 
        /// returned could be <c>null</c>.
        /// </para>
        /// </summary>
        internal class MapValue
        {
            private TValue value = default;
            public TValue Value
            {
                get => value;
                set => this.value = value;
            }

            public MapValue()
            { }

            public MapValue(TValue value)
            {
                this.value = value;
            }
        }

        /// <summary>
        /// Create map with enough capacity to hold <paramref name="startSize"/> terms
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version - see <see cref="CharArrayMap{TValue}"/> for details. </param>
        /// <param name="startSize">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        public CharArrayMap(LuceneVersion matchVersion, int startSize, bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;
            var size = INIT_SIZE;
            while (startSize + (startSize >> 2) > size)
            {
                size <<= 1;
            }
            keys = new char[size][];
            values = new MapValue[size];
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
        ///          a map (<see cref="T:IDictionary{string, V}"/>) whose mappings to be copied </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        public CharArrayMap(LuceneVersion matchVersion, IDictionary<string, TValue> c, bool ignoreCase)
            : this(matchVersion, c.Count, ignoreCase)
        {
            foreach (var v in c)
            {
                Add(v);
            }
        }

        /// <summary>
        /// Create set from the supplied map (used internally for readonly maps...)
        /// </summary>
        internal CharArrayMap(CharArrayMap<TValue> toCopy)
        {
            this.keys = toCopy.keys;
            this.values = toCopy.values;
            this.ignoreCase = toCopy.ignoreCase;
            this.count = toCopy.count;
            this.charUtils = toCopy.charUtils;
            this.matchVersion = toCopy.matchVersion;
        }

        /// <summary>
        /// Adds the <see cref="T:KeyValuePair{string, V}.Value"/> for the passed in <see cref="T:KeyValuePair{string, V}.Key"/>.
        /// Note that the <see cref="T:KeyValuePair{string, V}"/> instance is not added to the dictionary.
        /// </summary>
        /// <param name="item">A <see cref="T:KeyValuePair{string, V}"/> whose <see cref="T:KeyValuePair{string, V}.Value"/> 
        /// will be added for the corresponding <see cref="T:KeyValuePair{string, V}.Key"/>. </param>
        public virtual void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string-able type to be added/updated in the dictionary.</param>
        /// <param name="value">The corresponding value for the given <paramref name="key"/>.</param>
        public virtual void Add(string key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("The key " + key + " already exists in the dictionary");
            }
            Put(key, value);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayMap{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <returns> an new unmodifiable <see cref="CharArrayMap{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public CharArrayMap<TValue> AsReadOnly()
        {
            return this is CharArrayMap.UnmodifiableCharArrayMap<TValue> readOnlyDictionary ?
                readOnlyDictionary :
                new CharArrayMap.UnmodifiableCharArrayMap<TValue>(this);
        }

        /// <summary>
        /// Clears all entries in this map. This method is supported for reusing, but not 
        /// <see cref="IDictionary{TKey, TValue}.Remove(TKey)"/>. 
        /// </summary>
        public virtual void Clear()
        {
            count = 0;
            keys.Fill(null);
            values.Fill(null);
        }

        /// <summary>
        /// Not supported. 
        /// </summary>
        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool Contains(KeyValuePair<string, TValue> item) // LUCENENET TODO: API - rather than marking this DesignerSerializationVisibility.Hidden, it would be better to make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Copies all items in the current dictionary the <paramref name="array"/> starting at the <paramref name="arrayIndex"/>.
        /// The array is assumed to already be dimensioned to fit the elements in this dictionary; otherwise a <see cref="ArgumentOutOfRangeException"/>
        /// will be thrown.
        /// </summary>
        /// <param name="array">The array to copy the items into.</param>
        /// <param name="arrayIndex">A 32-bit integer that represents the index in <paramref name="array"/> at which copying begins.</param>
        public virtual void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            using var iter = (EntryIterator)EntrySet().GetEnumerator();
            for (int i = arrayIndex; iter.MoveNext(); i++)
            {
                array[i] = new KeyValuePair<string, TValue>(iter.Current.Key, iter.CurrentValue);
            }
        }

        /// <summary>
        /// Copies all items in the current <see cref="CharArrayMap{TValue}"/> to the passed in
        /// <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        /// <param name="map"></param>
        public virtual void CopyTo(CharArrayMap<TValue> map)
        {
            using var iter = (EntryIterator)EntrySet().GetEnumerator();
            while (iter.MoveNext())
            {
                map.Put(iter.Current.Key, iter.CurrentValue);
            }
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="length"/> chars of <paramref name="text"/> starting at <paramref name="offset"/>
        /// are in the <see cref="Keys"/> 
        /// </summary>
        public virtual bool ContainsKey(char[] text, int offset, int length)
        {
            return keys[GetSlot(text, offset, length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the entire <see cref="Keys"/> is the same as the 
        /// <paramref name="text"/> <see cref="T:char[]"/> being passed in; 
        /// otherwise <c>false</c>.
        /// </summary>
        public virtual bool ContainsKey(char[] text)
        {
            return keys[GetSlot(text, 0, text.Length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="string"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public virtual bool ContainsKey(string text)
        {
            return keys[GetSlot(text)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="ICharSequence"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public virtual bool ContainsKey(ICharSequence text)
        {
            return keys[GetSlot(text)] != null;
        }


        /// <summary>
        /// <c>true</c> if the <paramref name="o"/> <see cref="object.ToString()"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public virtual bool ContainsKey(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o), "o can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            var c = o as char[];
            if (c != null)
            {
                var text = c;
                return ContainsKey(text, 0, text.Length);
            }
            return ContainsKey(o.ToString());
        }

        /// <summary>
        /// returns the value of the mapping of <paramref name="length"/> chars of <paramref name="text"/>
        /// starting at <paramref name="offset"/>
        /// </summary>
        public virtual TValue Get(char[] text, int offset, int length)
        {
            var value = values[GetSlot(text, offset, length)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public virtual TValue Get(char[] text)
        {
            var value = values[GetSlot(text, 0, text.Length)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <see cref="ICharSequence"/>
        /// </summary>
        public virtual TValue Get(ICharSequence text)
        {
            var value = values[GetSlot(text)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <see cref="string"/>
        /// </summary>
        public virtual TValue Get(string text)
        {
            var value = values[GetSlot(text)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <see cref="object.ToString()"/>
        /// </summary>
        public virtual TValue Get(object o)
        {
            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (o is char[])
            {
                var text = o as char[];
                return Get(text, 0, text.Length);
            }
            return Get(o.ToString());
        }

        private int GetSlot(char[] text, int offset, int length)
        {
            int code = GetHashCode(text, offset, length);
            int pos = code & (keys.Length - 1);
            char[] text2 = keys[pos];
            if (text2 != null && !Equals(text, offset, length, text2))
            {
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    pos = code & (keys.Length - 1);
                    text2 = keys[pos];
                } while (text2 != null && !Equals(text, offset, length, text2));
            }
            return pos;
        }

        /// <summary>
        /// Returns true if the <see cref="ICharSequence"/> is in the set
        /// </summary>
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

        /// <summary>
        /// Returns true if the <see cref="string"/> is in the set
        /// </summary>
        private int GetSlot(string text)
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

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public virtual TValue Put(ICharSequence text, TValue value)
        {
            MapValue oldValue = PutImpl(text, new MapValue(value)); // could be more efficient
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping using the <see cref="object.ToString()"/> representation
        /// of <paramref name="o"/> in the <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        public virtual TValue Put(object o, TValue value)
        {
            MapValue oldValue = PutImpl(o, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public virtual TValue Put(string text, TValue value)
        {
            MapValue oldValue = PutImpl(text, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping.
        /// If ignoreCase is true for this Set, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        public virtual TValue Put(char[] text, TValue value)
        {
            MapValue oldValue = PutImpl(text, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        private MapValue PutImpl(ICharSequence text, MapValue value)
        {
            return PutImpl(text.ToString(), value); // could be more efficient
        }

        private MapValue PutImpl(object o, MapValue value)
        {
            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087 
            if (o is char[])
            {
                var c = o as char[];
                return PutImpl(c, value);
            }
            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into PutImpl.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = o.ToString();
            }
            return PutImpl(s, value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        private MapValue PutImpl(string text, MapValue value)
        {
            return PutImpl(text.ToCharArray(), value);
        }

        /// <summary>
        /// LUCENENET specific. Centralizes the logic between Put()
        /// implementations that accept a value and those that don't. This value is
        /// so we know whether or not the value was set, since we can't reliably do
        /// a check for <c>null</c> on the <typeparamref name="TValue"/> type.
        /// </summary>
        private MapValue PutImpl(char[] text, MapValue value)
        {
            if (ignoreCase)
            {
                charUtils.ToLower(text, 0, text.Length);
            }
            int slot = GetSlot(text, 0, text.Length);
            if (keys[slot] != null)
            {
                MapValue oldValue = values[slot];
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

            return null;
        }

        #region PutAll

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{char[],TValue}"/>'s
        /// entries, and calls this map's <see cref="Put(char[], TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        public virtual void PutAll(IDictionary<char[], TValue> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{string,TValue}"/>'s
        /// entries, and calls this map's <see cref="Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        public virtual void PutAll(IDictionary<string, TValue> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{ICharSequence,TValue}"/>'s
        /// entries, and calls this map's <see cref="Put(ICharSequence, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        public virtual void PutAll(IDictionary<ICharSequence, TValue> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{object,TValue}"/>'s
        /// entries, and calls this map's <see cref="Put(object, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        public virtual void PutAll(IDictionary<object, TValue> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{char[],TValue}}"/>'s
        /// entries, and calls this map's <see cref="Put(char[], TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        public virtual void PutAll(IEnumerable<KeyValuePair<char[], TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{string,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        public virtual void PutAll(IEnumerable<KeyValuePair<string, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{ICharSequence,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Put(ICharSequence, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        public virtual void PutAll(IEnumerable<KeyValuePair<ICharSequence, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IEnumerable{KeyValuePair{object,TValue}}"/>'s
        /// entries, and calls this map's <see cref="Put(object, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current map.</param>
        public virtual void PutAll(IEnumerable<KeyValuePair<object, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        #endregion

        private void Rehash()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(keys.Length == values.Length);
            int newSize = 2 * keys.Length;
            char[][] oldkeys = keys;
            MapValue[] oldvalues = values;
            keys = new char[newSize][];
            values = new MapValue[newSize];

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

        private bool Equals(char[] text1, int offset, int length, char[] text2)
        {
            if (length != text2.Length)
            {
                return false;
            }
            int limit = offset + length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    var codePointAt = charUtils.CodePointAt(text1, offset + i, limit);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[offset + i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool Equals(ICharSequence text1, char[] text2)
        {
            int length = text1.Length;
            if (length != text2.Length)
            {
                return false;
            }
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool Equals(string text1, char[] text2)
        {
            int length = text1.Length;
            if (length != text2.Length)
            {
                return false;
            }
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text1, i);
                    if (Character.ToLower(codePointAt, CultureInfo.InvariantCulture) != charUtils.CodePointAt(text2, i, text2.Length)) // LUCENENET specific - need to use invariant culture to match Java
                    {
                        return false;
                    }
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// LUCENENET Specific - test for value equality similar to how it is done in Java
        /// </summary>
        /// <param name="obj">Another dictionary to test the values of</param>
        /// <returns><c>true</c> if the given object is an <see cref="T:IDictionary{object, V}"/> that contains
        /// the same key value pairs as the current map</returns>
        public override bool Equals(object obj)
        {
            var other = obj as IDictionary<string, TValue>;
            if (other is null)
                return false;

            if (this.Count != other.Count)
                return false;

            using (var iter = other.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    if (!this.TryGetValue(iter.Current.Key, out TValue value))
                        return false;

                    if (!EqualityComparer<TValue>.Default.Equals(value, iter.Current.Value))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// LUCENENET Specific - override required by .NET because we override Equals
        /// to simulate Java's value equality checking.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            const int PRIME = 31; // arbitrary prime
            int hash = PRIME;
            using (var iter = (EntryIterator)EntrySet().GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    hash = (hash * PRIME) ^ iter.Current.Key.GetHashCode();
                    hash = (hash * PRIME) ^ iter.Current.Value.GetHashCode();
                }
            }
            return hash;
        }

        private int GetHashCode(char[] text, int offset, int length)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            int code = 0;
            int stop = offset + length;
            if (ignoreCase)
            {
                for (int i = offset; i < stop;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i, stop);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
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
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            int code = 0;
            int length = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        private int GetHashCode(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            int code = 0;
            int length = text.Length;
            if (ignoreCase)
            {
                for (int i = 0; i < length;)
                {
                    int codePointAt = charUtils.CodePointAt(text, i);
                    code = code * 31 + Character.ToLower(codePointAt, CultureInfo.InvariantCulture); // LUCENENET specific - need to use invariant culture to match Java
                    i += Character.CharCount(codePointAt);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    code = code * 31 + text[i];
                }
            }
            return code;
        }

        #region For .NET Support LUCENENET

        /// <summary>
        /// The Lucene version corresponding to the compatibility behavior 
        /// that this instance emulates
        /// </summary>
        public virtual LuceneVersion MatchVersion => matchVersion;

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        public virtual bool Put(char[] text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        public virtual bool Put(ICharSequence text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        public virtual bool Put(string text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="o"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        public virtual bool Put(object o)
        {
            return PutImpl(o, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="offset">The position of the <paramref name="key"/> where the target key begins.</param>
        /// <param name="length">The total length of the <paramref name="key"/>.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(char[] key, int offset, int length, out TValue value)
        {
            var val = values[GetSlot(key, offset, length)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(char[] key, out TValue value)
        {
            var val = values[GetSlot(key, 0, key.Length)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(ICharSequence key, out TValue value)
        {
            var val = values[GetSlot(key)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(string key, out TValue value)
        {
            var val = values[GetSlot(key)];
            if (val != null)
            {
                value = val.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public virtual bool TryGetValue(object key, out TValue value)
        {
            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (key is char[])
            {
                var text = key as char[];
                return TryGetValue(text, 0, text.Length, out value);
            }
            return TryGetValue(key.ToString(), out value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <param name="offset">The position of the <paramref name="key"/> where the target key begins.</param>
        /// <param name="length">The total length of the <paramref name="key"/>.</param>
        public virtual TValue this[char[] key, int offset, int length]
        {
            get => Get(key, offset, length);
            set => Put(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        public virtual TValue this[char[] key]
        {
            get => Get(key);
            set => Put(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        public virtual TValue this[ICharSequence key]
        {
            get => Get(key);
            set => Put(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        public virtual TValue this[string key]
        {
            get => Get(key);
            set => Put(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        public virtual TValue this[object key]
        {
            get => Get(key);
            set => Put(key, value);
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        public virtual ICollection<string> Keys => KeySet;


        private volatile ICollection<TValue> valueSet;

        /// <summary>
        /// Gets a collection containing the values in the <see cref="CharArrayMap{TValue}"/>.
        /// This specialized collection can be enumerated in order to read its values and 
        /// overrides <see cref="object.ToString()"/> in order to display a string 
        /// representation of the values.
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                if (valueSet is null)
                {
                    valueSet = new ValueCollection(this);
                }
                return valueSet;
            }
        }

        /// <summary>
        /// LUCENENET specific class used to break the infinite recursion when the
        /// CharArraySet iterates the keys of this dictionary via <see cref="OriginalKeySet"/>. 
        /// In Java, the keyset of the abstract base class was used to break the infinite recursion, 
        /// however this class doesn't have an abstract base class so that is not an option. 
        /// This class is just a facade around the keys (not another collection of keys), so it 
        /// doesn't consume any additional RAM while providing another "virtual" collection to iterate over.
        /// </summary>
        internal class KeyCollection : ICollection<string>
        {
            private readonly CharArrayMap<TValue> outerInstance;

            public KeyCollection(CharArrayMap<TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Count => outerInstance.Count;

            public bool IsReadOnly => outerInstance.IsReadOnly;

            public void Add(string item) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            public void Clear()
            {
                outerInstance.Clear();
            }

            public bool Contains(string item)
            {
                return outerInstance.ContainsKey(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                using var iter = GetEnumerator();
                for (int i = arrayIndex; iter.MoveNext(); i++)
                {
                    array[i] = iter.Current;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                return new KeyEnumerator(outerInstance);
            }

            public bool Remove(string item) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// LUCENENET specific class to iterate the values in the <see cref="KeyCollection"/>.
            /// </summary>
            private class KeyEnumerator : IEnumerator<string>
            {
                private readonly EntryIterator entryIterator;

                public KeyEnumerator(CharArrayMap<TValue> outerInstance)
                {
                    this.entryIterator = new EntryIterator(outerInstance, !outerInstance.IsReadOnly);
                }

                public string Current => entryIterator.Current.Key;

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    // nothing to do
                }

                public bool MoveNext()
                {
                    return entryIterator.MoveNext();
                }

                public void Reset()
                {
                    entryIterator.Reset();
                }
            }
        }

        /// <summary>
        /// LUCENENET specific class that represents the values in the <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        internal class ValueCollection : ICollection<TValue>
        {
            private readonly CharArrayMap<TValue> outerInstance;

            public ValueCollection(CharArrayMap<TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Count => outerInstance.Count;

            public bool IsReadOnly => outerInstance.IsReadOnly;

            public void Add(TValue item) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            public void Clear()
            {
                outerInstance.Clear();
            }

            public bool Contains(TValue item)
            {
                for (int i = 0; i < outerInstance.values.Length; i++)
                {
                    var value = outerInstance.values[i];
                    if (J2N.Collections.Generic.EqualityComparer<TValue>.Equals(value, item))
                        return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new ValueEnumerator(outerInstance);
            }

            public bool Remove(TValue item) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                using var i = (ValueEnumerator)GetEnumerator();
                if (!i.HasNext)
                    return "[]";

                StringBuilder sb = new StringBuilder();
                sb.Append('[');
                while (i.MoveNext())
                {
                    TValue value = i.Current;
                    if (sb.Length > 1)
                    {
                        sb.Append(',').Append(' ');
                    }
                    sb.Append(value.ToString());
                }

                return sb.Append(']').ToString();
            }

            /// <summary>
            /// LUCENENET specific class to enumerate the values in the <see cref="ValueCollection"/>.
            /// </summary>
            private class ValueEnumerator : IEnumerator<TValue>
            {
                private readonly EntryIterator entryIterator;

                public ValueEnumerator(CharArrayMap<TValue> outerInstance)
                {
                    this.entryIterator = new EntryIterator(outerInstance, !outerInstance.IsReadOnly);
                }

                public TValue Current => entryIterator.CurrentValue;

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    entryIterator.Dispose();
                }

                public bool MoveNext()
                {
                    return entryIterator.MoveNext();
                }

                public void Reset()
                {
                    entryIterator.Reset();
                }

                public bool HasNext => entryIterator.HasNext;
            }
        }

        #endregion

        /// <summary>
        /// <c>true</c> if the <see cref="CharArrayMap{TValue}"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public virtual bool IsReadOnly { get; private set; }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        public virtual IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return new EntryIterator(this, false);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool Remove(string key) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        [Obsolete("Not applicable in this class.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool Remove(KeyValuePair<string, TValue> item) // LUCENENET TODO: API - make an explicit implementation that isn't public
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="CharArrayMap{TValue}"/>.
        /// </summary>
        public virtual int Count => count;

        /// <summary>
        /// Returns a string that represents the current object. (Inherited from <see cref="object"/>.)
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder("{");

            using (var iter1 = this.GetEnumerator())
            {
                while (iter1.MoveNext())
                {
                    KeyValuePair<string, TValue> entry = iter1.Current;
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(entry.Key);
                    sb.Append('=');
                    sb.Append(entry.Value);
                }
            }

            return sb.Append('}').ToString();
        }

        private EntrySet_ entrySet = null;
        private CharArraySet keySet = null;
        private KeyCollection originalKeySet = null;

        internal virtual EntrySet_ CreateEntrySet()
        {
            return new EntrySet_(this, true);
        }

        // LUCENENET NOTE: This MUST be a method, since there is an
        // extension method that this class needs to override the behavior of.
        public EntrySet_ EntrySet()
        {
            if (entrySet is null)
            {
                entrySet = CreateEntrySet();
            }
            return entrySet;
        }

        /// <summary>
        /// helper for <see cref="CharArraySet"/> to not produce endless recursion
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public ICollection<string> OriginalKeySet
        {
            get
            {
                if (originalKeySet is null)
                {
                    // prevent adding of entries
                    originalKeySet = new KeyCollection(this);
                }
                return originalKeySet;
            }
        }

        /// <summary>
        /// Returns an <see cref="CharArraySet"/> view on the map's keys.
        /// The set will use the same <see cref="matchVersion"/> as this map. 
        /// </summary>
        private CharArraySet KeySet
        {
            get
            {
                if (keySet is null)
                {
                    // prevent adding of entries
                    keySet = new UnmodifiableCharArraySet(this);
                }
                return keySet;
            }
        }

        private sealed class UnmodifiableCharArraySet : CharArraySet
        {
            internal UnmodifiableCharArraySet(ICharArrayMap map) 
                : base(map)
            {
            }

            public override bool Add(object o) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(ICharSequence text) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(string text) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(char[] text) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }
        }

        /// <summary>
        /// public iterator class so efficient methods are exposed to users
        /// </summary>
        public class EntryIterator : IEnumerator<KeyValuePair<string, TValue>>
        {
            private readonly CharArrayMap<TValue> outerInstance;

            internal int pos = -1;
            internal int lastPos;
            internal readonly bool allowModify;

            internal EntryIterator(CharArrayMap<TValue> outerInstance, bool allowModify)
            {
                this.outerInstance = outerInstance;
                this.allowModify = allowModify;
                GoNext();
            }

            internal void GoNext()
            {
                lastPos = pos;
                pos++;
                while (pos < outerInstance.keys.Length && outerInstance.keys[pos] is null)
                {
                    pos++;
                }
            }

            public virtual bool HasNext => pos < outerInstance.keys.Length;

            /// <summary>
            /// gets the next key... do not modify the returned char[]
            /// </summary>
            public virtual char[] NextKey()
            {
                GoNext();
                return outerInstance.keys[lastPos];
            }

            /// <summary>
            /// gets the next key as a newly created <see cref="string"/> object
            /// </summary>
            public virtual string NextKeyString()
            {
                return new string(NextKey());
            }

            /// <summary>
            /// returns the value associated with the current key
            /// </summary>
            public virtual TValue CurrentValue
            {
                get
                {
                    var val = outerInstance.values[lastPos];
                    return val != null ? val.Value : default;
                }
            }

            /// <summary>
            /// sets the value associated with the last key returned
            /// </summary>
            public virtual TValue SetValue(TValue value)
            {
                if (!allowModify)
                {
                    throw UnsupportedOperationException.Create();
                }
                TValue old = outerInstance.values[lastPos].Value;
                outerInstance.values[lastPos].Value = value;
                return old;
            }

            // LUCENENET: Next() and Remove() methods eliminated here

            #region Added for better .NET support LUCENENET
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // nothing to do
            }

            public virtual bool MoveNext()
            {
                if (!HasNext) return false;
                GoNext();
                return true;
            }

            public virtual void Reset()
            {
                pos = -1;
                GoNext();
            }

            public virtual KeyValuePair<string, TValue> Current
            {
                get
                {
                    var val = outerInstance.values[lastPos];
                    return new KeyValuePair<string, TValue>(
                        new string(outerInstance.keys[lastPos]), 
                        val != null ? val.Value : default);
                }
            }

            object IEnumerator.Current => Current;

            #endregion
        }

        // LUCENENET NOTE: The Java Lucene type MapEntry was removed here because it is not possible 
        // to inherit the value type KeyValuePair{TKey, TValue} in .NET.

        /// <summary>
        /// public EntrySet_ class so efficient methods are exposed to users
        /// 
        /// NOTE: In .NET this was renamed to EntrySet_ because it conflicted with the
        /// method EntrySet(). Since there is also an extension method named <see cref="T:IDictionary{K,V}.EntrySet()"/> 
        /// that this class needs to override, changing the name of the method was not
        /// possible because the extension method would produce incorrect results if it were
        /// inadvertently called, leading to hard-to-diagnose bugs.
        /// 
        /// Another difference between this set and the Java counterpart is that it implements
        /// <see cref="ICollection{T}"/> rather than <see cref="ISet{T}"/> so we don't have to implement
        /// a bunch of methods that we aren't really interested in. The <see cref="Keys"/> and <see cref="Values"/>
        /// properties both return <see cref="ICollection{T}"/>, and while there is no <see cref="EntrySet()"/> method
        /// or property in .NET, if there were it would certainly return <see cref="ICollection{T}"/>.
        /// </summary>
        public sealed class EntrySet_ : ICollection<KeyValuePair<string, TValue>>
        {
            private readonly CharArrayMap<TValue> outerInstance;

            internal readonly bool allowModify;

            internal EntrySet_(CharArrayMap<TValue> outerInstance, bool allowModify)
            {
                this.outerInstance = outerInstance;
                this.allowModify = allowModify;
            }

            public IEnumerator GetEnumerator()
            {
                return new EntryIterator(outerInstance, allowModify);
            }

            IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
            {
                return (IEnumerator<KeyValuePair<string, TValue>>)GetEnumerator();
            }

            public bool Contains(object o)
            {
                if (!(o is KeyValuePair<string, TValue>))
                {
                    return false;
                }
                var e = (KeyValuePair<string, TValue>)o;
                string key = e.Key;
                TValue val = e.Value;
                TValue v = outerInstance.Get(key);
                return v is null ? val is null : v.Equals(val);
            }

            [Obsolete("Not applicable in this class.")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public bool Remove(KeyValuePair<string, TValue> item) // LUCENENET TODO: API - make an explicit implementation that isn't public
            {
                throw UnsupportedOperationException.Create();
            }

            public int Count => outerInstance.count;

            public void Clear()
            {
                if (!allowModify)
                {
                    throw UnsupportedOperationException.Create();
                }
                outerInstance.Clear();
            }

            #region LUCENENET Added for better .NET support

            public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
            {
                outerInstance.CopyTo(array, arrayIndex);
            }

            [Obsolete("Not applicable in this class.")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public bool Contains(KeyValuePair<string, TValue> item)
            {
                return outerInstance.Contains(item);
            }

            public void Add(KeyValuePair<string, TValue> item)
            {
                outerInstance.Add(item);
            }

            public bool IsReadOnly => !allowModify;

            public override string ToString()
            {
                var sb = new StringBuilder("[");

                IEnumerator<KeyValuePair<string, TValue>> iter1 = new EntryIterator(this.outerInstance, false);
                while (iter1.MoveNext())
                {
                    KeyValuePair<string, TValue> entry = iter1.Current;
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(entry.Key);
                    sb.Append('=');
                    sb.Append(entry.Value);
                }

                return sb.Append(']').ToString();
            }
            #endregion
        }

        // LUCENENET: Moved UnmodifiableMap static methods to CharArrayMap class

        // LUCENENET: Moved Copy static methods to CharArrayMap class

        /// <summary>
        /// Returns an empty, unmodifiable map. </summary>
        public static CharArrayMap<TValue> EmptyMap()
        {
            return EMPTY_MAP;
        }

        // LUCENENET: Moved UnmodifiableCharArraymap to CharArrayMap class

        // LUCENENET: Moved EmptyCharArrayMap to CharArrayMap class
    }

    /// <summary>
    /// LUCENENET specific interface used so <see cref="CharArraySet"/>
    /// can hold a reference to <see cref="CharArrayMap{TValue}"/> without
    /// knowing its generic type.
    /// </summary>
    internal interface ICharArrayMap
    {
        void Clear();
        bool ContainsKey(char[] text, int offset, int length);
        bool ContainsKey(char[] text);
        bool ContainsKey(object o);
        bool ContainsKey(string text);
        bool ContainsKey(ICharSequence text);
        int Count { get; }
        LuceneVersion MatchVersion { get; }
        ICollection<string> OriginalKeySet { get; }
        bool Put(char[] text);
        bool Put(ICharSequence text);
        bool Put(object o);
        bool Put(string text);
    }

    public static class CharArrayMap // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Returns a copy of the given map as a <see cref="CharArrayMap{TValue}"/>. If the given map
        /// is a <see cref="CharArrayMap{TValue}"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArrayMap{TValue}"/> where
        /// the <see cref="LuceneVersion"/> of the source map differs from its copy
        /// <see cref="CharArrayMap{TValue}.CharArrayMap(LuceneVersion, IDictionary{string, TValue}, bool)"/> should be used instead.
        /// The <see cref="Copy{TValue}(LuceneVersion, IDictionary{string, TValue})"/> will preserve the <see cref="LuceneVersion"/> of the
        /// source map if it is an instance of <see cref="CharArrayMap{TValue}"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. This argument will be ignored if the
        ///          given map is a <see cref="CharArrayMap{TValue}"/>. </param>
        /// <param name="map">
        ///          a map to copy </param>
        /// <returns> a copy of the given map as a <see cref="CharArrayMap{TValue}"/>. If the given map
        ///         is a <see cref="CharArrayMap{TValue}"/> the ignoreCase property as well as the
        ///         <paramref name="matchVersion"/> will be of the given map will be preserved. </returns>
        public static CharArrayMap<TValue> Copy<TValue>(LuceneVersion matchVersion, IDictionary<string, TValue> map)
        {
            if (map == CharArrayMap<TValue>.EmptyMap())
            {
                return CharArrayMap<TValue>.EmptyMap();
            }

            if (map is CharArrayMap<TValue>)
            {
                var m = map as CharArrayMap<TValue>;
                // use fast path instead of iterating all values
                // this is even on very small sets ~10 times faster than iterating
                var keys = new char[m.keys.Length][];
                Array.Copy(m.keys, 0, keys, 0, keys.Length);
                var values = new CharArrayMap<TValue>.MapValue[m.values.Length];
                Array.Copy(m.values, 0, values, 0, values.Length);
                m = new CharArrayMap<TValue>(m) { keys = keys, values = values };
                return m;
            }
            return new CharArrayMap<TValue>(matchVersion, map, false);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to copy <see cref="CharArrayMap{TValue}"/> without knowing 
        /// its generic type.
        /// </summary>
        internal static CharArrayMap<TValue> Copy<TValue>(LuceneVersion matchVersion, ICharArrayMap map)
        {
            return Copy(matchVersion, map as IDictionary<string, TValue>);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayMap{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <param name="map">
        ///          a map for which the unmodifiable map is returned. </param>
        /// <returns> an new unmodifiable <see cref="CharArrayMap{TValue}"/>. </returns>
        /// <exception cref="ArgumentException">
        ///           if the given map is <c>null</c>. </exception>
        [Obsolete("Use the CharArrayMap<TValue>.AsReadOnly() instance method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArrayMap<TValue> UnmodifiableMap<TValue>(CharArrayMap<TValue> map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map), "Given map is null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayMap<TValue>.EmptyMap() || map.Count == 0)
            {
                return CharArrayMap<TValue>.EmptyMap();
            }
            if (map is CharArrayMap.UnmodifiableCharArrayMap<TValue>)
            {
                return map;
            }
            return new CharArrayMap.UnmodifiableCharArrayMap<TValue>(map);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to create an <see cref="UnmodifiableCharArrayMap{TValue}"/> instance
        /// without knowing the type of <typeparamref name="TValue"/>.
        /// </summary>
        internal static ICharArrayMap UnmodifiableMap<TValue>(ICharArrayMap map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map), "Given map is null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayMap<TValue>.EmptyMap() || map.Count == 0)
            {
                return CharArrayMap<TValue>.EmptyMap();
            }
            if (map is CharArrayMap.UnmodifiableCharArrayMap<TValue>)
            {
                return map;
            }
            return new CharArrayMap.UnmodifiableCharArrayMap<TValue>(map);
        }

        // package private CharArraySet instanceof check in CharArraySet
        internal class UnmodifiableCharArrayMap<TValue> : CharArrayMap<TValue>
        {
            public UnmodifiableCharArrayMap(CharArrayMap<TValue> map)
                : base(map)
            { }

            public UnmodifiableCharArrayMap(ICharArrayMap map)
                : base(map as CharArrayMap<TValue>)
            { }

            public override void Clear()
            {
                throw UnsupportedOperationException.Create();
            }

            public override TValue Put(char[] text, TValue val)
            {
                throw UnsupportedOperationException.Create();
            }

            public override TValue Put(ICharSequence text, TValue val)
            {
                throw UnsupportedOperationException.Create();
            }

            public override TValue Put(string text, TValue val)
            {
                throw UnsupportedOperationException.Create();
            }

            public override TValue Put(object o, TValue val)
            {
                throw UnsupportedOperationException.Create();
            }

            public override bool Put(char[] text)
            {
                throw UnsupportedOperationException.Create();
            }

            public override bool Put(ICharSequence text)
            {
                throw UnsupportedOperationException.Create();
            }

            public override bool Put(string text)
            {
                throw UnsupportedOperationException.Create();
            }

            public override bool Put(object o)
            {
                throw UnsupportedOperationException.Create();
            }

            [Obsolete("Not applicable in this class.")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public override bool Remove(string key)
            {
                throw UnsupportedOperationException.Create();
            }

            internal override EntrySet_ CreateEntrySet()
            {
                return new EntrySet_(this, false);
            }

            #region Added for better .NET support LUCENENET
            public override void Add(string key, TValue value)
            {
                throw UnsupportedOperationException.Create();
            }
            public override void Add(KeyValuePair<string, TValue> item)
            {
                throw UnsupportedOperationException.Create();
            }
            public override TValue this[char[] key, int offset, int length]
            {
                get => base[key, offset, length];
                set => throw UnsupportedOperationException.Create();
            }
            public override TValue this[char[] key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create();
            }
            public override TValue this[ICharSequence key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create();
            }
            public override TValue this[string key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create();
            }
            public override TValue this[object key]
            {
                get => base[key];
                set => throw UnsupportedOperationException.Create();
            }

            [Obsolete("Not applicable in this class.")]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
            public override bool Remove(KeyValuePair<string, TValue> item)
            {
                throw UnsupportedOperationException.Create();
            }
            #endregion
        }

        /// <summary>
        /// Empty <see cref="UnmodifiableCharArrayMap{V}"/> optimized for speed.
        /// Contains checks will always return <c>false</c> or throw
        /// NPE if necessary.
        /// </summary>
        internal class EmptyCharArrayMap<V> : UnmodifiableCharArrayMap<V>
        {
            public EmptyCharArrayMap()
#pragma warning disable 612, 618
                : base(new CharArrayMap<V>(LuceneVersion.LUCENE_CURRENT, 0, false))
#pragma warning restore 612, 618
            {
            }

            public override bool ContainsKey(char[] text, int offset, int length)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return false;
            }

            public override bool ContainsKey(char[] text)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return false;
            }

            public override bool ContainsKey(ICharSequence text)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return false;
            }

            public override bool ContainsKey(object o)
            {
                if (o is null)
                {
                    throw new ArgumentNullException(nameof(o));
                }
                return false;
            }

            public override V Get(char[] text, int offset, int length)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return default;
            }

            public override V Get(char[] text)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return default;
            }

            public override V Get(ICharSequence text)
            {
                if (text is null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                return default;
            }

            public override V Get(object o)
            {
                if (o is null)
                {
                    throw new ArgumentNullException(nameof(o));
                }
                return default;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific extension methods for CharArrayMap
    /// </summary>
    public static class CharArrayMapExtensions
    {
        #region ContainsKey

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="bool"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, bool key)
        {
            return map.ContainsKey(key.ToString());
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="byte"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, byte key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="char"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, char key)
        {
            return map.ContainsKey("" + key);
        }

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="decimal"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>
        ///// </summary>
        //public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, decimal key)
        //{
        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="double"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>
        ///// </summary>
        //public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, double key)
        //{
        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="float"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>
        ///// </summary>
        //public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, float key)
        //{
        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="int"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, int key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="long"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, long key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="sbyte"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, sbyte key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="short"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, short key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="uint"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, uint key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="ulong"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, ulong key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="ushort"/> is in the <see cref="CharArrayMap{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayMap<TValue> map, ushort key)
        {
            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Get

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, bool text)
        {
            return map.Get(text.ToString());
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, byte text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, char text)
        {
            return map.Get("" + text);
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, decimal text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, double text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, float text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, int text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, long text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, sbyte text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, short text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, uint text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, ulong text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// returns the value of the mapping of the chars inside this <paramref name="text"/>
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayMap<TValue> map, ushort text)
        {
            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Put

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, bool text, TValue value)
        {
            return map.Put(text.ToString(), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, byte text, TValue value)
        {
            return map.Put(text.ToString(), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, char text, TValue value)
        {
            return map.Put(text.ToString(), value);
        }

        ///// <summary>
        ///// Add the given mapping.
        ///// </summary>
        //public static TValue Put<TValue>(this CharArrayMap<TValue> map, decimal text, TValue value)
        //{
        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        ///// <summary>
        ///// Add the given mapping.
        ///// </summary>
        //public static TValue Put<TValue>(this CharArrayMap<TValue> map, double text, TValue value)
        //{
        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        ///// <summary>
        ///// Add the given mapping.
        ///// </summary>
        //public static TValue Put<TValue>(this CharArrayMap<TValue> map, float text, TValue value)
        //{
        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, int text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, long text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, sbyte text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, short text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, uint text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, ulong text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayMap<TValue> map, ushort text, TValue value)
        {
            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        #endregion

        #region PutAll

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<bool, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<byte, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<char, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put("" + kvp.Key, kvp.Value);
            }
        }

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<decimal, TValue> dictionary)
        //{
        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<double, TValue> dictionary)
        //{
        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<float, TValue> dictionary)
        //{
        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<int, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<long, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<sbyte, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<short, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<uint, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<ulong, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IDictionary<ushort, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }


        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<bool, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<byte, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<char, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put("" + kvp.Key, kvp.Value);
            }
        }

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<decimal, TValue>> collection)
        //{
        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<double, TValue>> collection)
        //{
        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        //public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<float, TValue>> collection)
        //{
        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<int, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<long, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<sbyte, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<short, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<uint, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<ulong, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayMap{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayMap<TValue> map, IEnumerable<KeyValuePair<ushort, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        #endregion

        #region TryGetValue

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, bool key, out TValue value)
        {
            return map.TryGetValue(key.ToString(), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, byte key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, char key, out TValue value)
        {
            return map.TryGetValue("" + key, out value);
        }

        ///// <summary>
        ///// Gets the value associated with the specified key.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        //public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, decimal key, out TValue value)
        //{
        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        ///// <summary>
        ///// Gets the value associated with the specified key.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        //public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, double key, out TValue value)
        //{
        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        ///// <summary>
        ///// Gets the value associated with the specified key.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        //public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, float key, out TValue value)
        //{
        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, int key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, long key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, sbyte key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, short key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, uint key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, ulong key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayMap{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayMap<TValue> map, ushort key, out TValue value)
        {
            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

#endregion
    }
}