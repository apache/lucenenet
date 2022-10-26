// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Globalization;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// compatibility when creating <see cref="CharArrayDictionary{TValue}"/>:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, supplementary characters are
    ///       properly lowercased.</description></item>
    /// </list>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArrayDictionary{TValue}"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> &lt; 3.1 to the constructors.
    /// </para>
    /// </summary>
    public class CharArrayDictionary<TValue> : ICharArrayDictionary, IDictionary<string, TValue>
    {
        // LUCENENET: Made public, renamed Empty
        /// <summary>
        /// Returns an empty, unmodifiable map. </summary>
        public static readonly CharArrayDictionary<TValue> Empty = new CharArrayDictionary.EmptyCharArrayDictionary<TValue>();

        private const int INIT_SIZE = 8;
        private readonly CharacterUtils charUtils;
        private readonly bool ignoreCase;
        private int count;
        private readonly LuceneVersion matchVersion; // package private because used in CharArraySet
        internal char[][] keys; // package private because used in CharArraySet's non Set-conform CharArraySetIterator
        internal MapValue[] values; // package private because used in CharArraySet's non Set-conform CharArraySetIterator

        /// <summary>
        /// LUCENENET: Moved this from CharArraySet so it doesn't need to know the generic type of CharArrayDictionary
        /// </summary>
        internal static readonly MapValue PLACEHOLDER = new MapValue();

        bool ICharArrayDictionary.IgnoreCase => ignoreCase;

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
        ///          lucene compatibility version - see <see cref="CharArrayDictionary{TValue}"/> for details. </param>
        /// <param name="startSize">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startSize"/> is less than zero.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, int startSize, bool ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (startSize < 0)
                throw new ArgumentOutOfRangeException(nameof(startSize), "Non-negative number required.");

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
        /// <exception cref="ArgumentNullException"><paramref name="c"/> is <c>null</c>.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, IDictionary<string, TValue> c, bool ignoreCase)
            : this(matchVersion, c?.Count ?? 0, ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (c is null)
                throw new ArgumentNullException(nameof(c));

            foreach (var v in c)
            {
                // LUCENENET: S1699: Don't call call protected members in the constructor
                if (ContainsKey(v.Key))
                {
                    throw new ArgumentException("The key " + v.Key + " already exists in the dictionary");
                }
                PutImpl(v.Key, new MapValue(v.Value));
            }
        }

        /// <summary>
        /// Create set from the supplied map (used internally for readonly maps...)
        /// </summary>
        internal CharArrayDictionary(CharArrayDictionary<TValue> toCopy)
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
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element with <paramref name="key"/> already exists in the dictionary.</exception>
        public virtual void Add(string key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("The key " + key + " already exists in the dictionary");
            }
            Put(key, value);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayDictionary{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <returns> an new unmodifiable <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public CharArrayDictionary<TValue> AsReadOnly()
        {
            return this is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue> readOnlyDictionary ?
                readOnlyDictionary :
                new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(this);
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
        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
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
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public virtual void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Non-negative number required.");
            if (count > array.Length - arrayIndex)
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");

            using var iter = GetEnumerator();
            for (int i = arrayIndex; iter.MoveNext(); i++)
            {
                array[i] = new KeyValuePair<string, TValue>(iter.CurrentKeyString, iter.CurrentValue);
            }
        }

        /// <summary>
        /// Copies all items in the current <see cref="CharArrayDictionary{TValue}"/> to the passed in
        /// <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        /// <param name="map">The destination <see cref="CharArrayDictionary{TValue}"/> to copy the elements to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
        public virtual void CopyTo(CharArrayDictionary<TValue> map)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            using var iter = GetEnumerator();
            while (iter.MoveNext())
            {
                map.Put((char[])iter.CurrentKey.Clone(), iter.CurrentValue);
            }
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="length"/> chars of <paramref name="text"/> starting at <paramref name="offset"/>
        /// are in the <see cref="Keys"/> 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        public virtual bool ContainsKey(char[] text, int offset, int length)
        {
            return keys[GetSlot(text, offset, length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the entire <see cref="Keys"/> is the same as the 
        /// <paramref name="text"/> <see cref="T:char[]"/> being passed in; 
        /// otherwise <c>false</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return keys[GetSlot(text, 0, text.Length)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="string"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(string text)
        {
            return keys[GetSlot(text)] != null;
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> <see cref="ICharSequence"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(ICharSequence text)
        {
            return keys[GetSlot(text)] != null;
        }


        /// <summary>
        /// <c>true</c> if the <paramref name="o"/> <see cref="object.ToString()"/> is in the <see cref="Keys"/>; 
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        public virtual bool ContainsKey(object o)
        {
            if (o is null)
                throw new ArgumentNullException(nameof(o), "o can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            if (o is string str)
                return ContainsKey(str);
            if (o is char[] charArray)
                return ContainsKey(charArray, 0, charArray.Length);
            if (o is StringCharSequence strCs)
                return ContainsKey(strCs.Value ?? string.Empty);
            if (o is CharArrayCharSequence charArrayCs)
                return ContainsKey(charArrayCs.Value ?? Arrays.Empty<char>());
            if (o is StringBuilderCharSequence stringBuilderCs)
                return ContainsKey(stringBuilderCs.Value?.ToString() ?? string.Empty);
            if (o is ICharSequence cs)
                return ContainsKey(cs.ToString());

            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into ContainsKey.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = o.ToString();
            }
            return ContainsKey(s);
        }

        #region Get

        /// <summary>
        /// Returns the value of the mapping of <paramref name="length"/> chars of <paramref name="text"/>
        /// starting at <paramref name="offset"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        public virtual TValue Get(char[] text, int offset, int length)
        {
            var value = values[GetSlot(text, offset, length)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Get(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            var value = values[GetSlot(text, 0, text.Length)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Get(ICharSequence text)
        {
            var value = values[GetSlot(text)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Get(string text)
        {
            var value = values[GetSlot(text)];
            return (value != null) ? value.Value : default;
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        public virtual TValue Get(object o)
        {
            if (o is null)
                throw new ArgumentNullException(nameof(o));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (o is string str)
                return Get(str);
            if (o is char[] charArray)
                return Get(charArray, 0, charArray.Length);
            if (o is StringCharSequence strCs)
                return Get(strCs.Value ?? string.Empty);
            if (o is CharArrayCharSequence charArrayCs)
                return Get(charArrayCs.Value ?? Arrays.Empty<char>());
            if (o is StringBuilderCharSequence stringBuilderCs)
                return Get(stringBuilderCs.Value?.ToString() ?? string.Empty);
            if (o is ICharSequence cs)
                return Get(cs.ToString());

            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into Get.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = o.ToString();
            }
            return Get(s);
        }

        #endregion Get

        #region GetSlot

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
        /// Returns <c>true</c> if the <see cref="ICharSequence"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
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
        /// Returns <c>true</c> if the <see cref="string"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
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

        #endregion GetSlot

        #region Put (value)

        /// <summary>
        /// Add the given mapping.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[ICharSequence]"/> setter is more efficient than this method if
        /// the return value is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <returns>The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Put(ICharSequence text, TValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            MapValue oldValue = PutImpl(text, new MapValue(value)); // could be more efficient
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping using the <see cref="object.ToString()"/> representation
        /// of <paramref name="o"/> in the <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[object]"/> setter is more efficient than this method if
        /// the return value is not required.
        /// </summary>
        /// <param name="o">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified object <paramref name="o"/>.</param>
        /// <returns>The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="o"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        public virtual TValue Put(object o, TValue value)
        {
            // LUCENENET: Added guard clause
            if (o is null)
                throw new ArgumentNullException(nameof(o));

            MapValue oldValue = PutImpl(o, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[string]"/> setter is more efficient than this method if
        /// the return value is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <returns>The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Put(string text, TValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            MapValue oldValue = PutImpl(text, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        /// <summary>
        /// Add the given mapping.
        /// If ignoreCase is true for this dictionary, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// <para/>
        /// <b>Note:</b> The <see cref="this[char[]]"/> setter is more efficient than this method if
        /// the return value is not required.
        /// </summary>
        /// <param name="text">A key with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <returns>The previous value associated with the key, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual TValue Put(char[] text, TValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            MapValue oldValue = PutImpl(text, new MapValue(value));
            return (oldValue != null) ? oldValue.Value : default;
        }

        #endregion Put (value)

        #region PutImpl

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue PutImpl(ICharSequence text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (text is CharArrayCharSequence charArrayCs)
                return PutImpl(charArrayCs.Value ?? Arrays.Empty<char>(), value);

            return PutImpl(text.ToString(), value); // could be more efficient
        }

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue PutImpl(object o, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (o is null)
                throw new ArgumentNullException(nameof(o));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087 
            if (o is string str)
                return PutImpl(str, value);
            if (o is char[] charArray)
                return PutImpl(charArray, value);
            if (o is StringCharSequence strCs)
                return PutImpl(strCs.Value ?? string.Empty, value);
            if (o is CharArrayCharSequence charArrayCs)
                return PutImpl(charArrayCs.Value ?? Arrays.Empty<char>(), value);
            if (o is StringBuilderCharSequence stringBuilderCs)
                return PutImpl(stringBuilderCs.Value?.ToString() ?? string.Empty, value);
            if (o is ICharSequence cs)
                return PutImpl(cs.ToString(), value);

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
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue PutImpl(string text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET specific - only allocate char array if it is required.
            if (ignoreCase)
            {
                return PutImpl(text.ToCharArray(), value);
            }
            int slot = GetSlot(text);
            if (keys[slot] != null)
            {
                MapValue oldValue = values[slot];
                values[slot] = value;
                return oldValue;
            }
            keys[slot] = text.ToCharArray();
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }

            return null;
        }

        /// <summary>
        /// LUCENENET specific. Centralizes the logic between Put()
        /// implementations that accept a value and those that don't. This value is
        /// so we know whether or not the value was set, since we can't reliably do
        /// a check for <c>null</c> on the <typeparamref name="TValue"/> type.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapValue PutImpl(char[] text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

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

        #endregion PutImpl

        #region Set

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(ICharSequence text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, PLACEHOLDER);
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        private void Set(object o)
        {
            if (o is null)
                throw new ArgumentNullException(nameof(o));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (o is string str)
            {
                Set(str);
                return;
            }
            if (o is char[] charArray)
            {
                Set(charArray);
                return;
            }
            if (o is StringCharSequence strCs)
            {
                Set(strCs.Value ?? string.Empty);
                return;
            }
            if (o is CharArrayCharSequence charArrayCs)
            {
                Set(charArrayCs.Value ?? Arrays.Empty<char>());
                return;
            }
            if (o is StringBuilderCharSequence stringBuilderCs)
            {
                Set(stringBuilderCs.Value?.ToString() ?? string.Empty);
                return;
            }
            if (o is ICharSequence cs)
            {
                Set(cs.ToString());
                return;
            }

            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into Get.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = o.ToString();
            }
            Set(s);
        }

        void ICharArrayDictionary.Set(char[] text) => Set(text);
        void ICharArrayDictionary.Set(ICharSequence text) => Set(text);
        void ICharArrayDictionary.Set(object o) => Set(o);
        void ICharArrayDictionary.Set(string text) => Set(text);

        #endregion Set

        #region Set (value)

        /// <summary>
        /// Sets the value of the mapping of <paramref name="length"/> chars of <paramref name="text"/>
        /// starting at <paramref name="offset"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(char[] text, int offset, int length, TValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, offset, length, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(char[] text, TValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="ICharSequence"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(ICharSequence text, TValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="string"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(string text, TValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, new MapValue(value));
        }

        /// <summary>
        /// Sets the value of the mapping of the chars inside this <see cref="object.ToString()"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(object o, TValue value)
        {
            if (o is null)
                throw new ArgumentNullException(nameof(o));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (o is string str)
            {
                Set(str, value);
                return;
            }
            if (o is char[] charArray)
            {
                Set(charArray, 0, charArray.Length, value);
                return;
            }
            if (o is StringCharSequence strCs)
            {
                Set(strCs.Value ?? string.Empty, value);
                return;
            }
            if (o is CharArrayCharSequence charArrayCs)
            {
                Set(charArrayCs.Value ?? Arrays.Empty<char>(), value);
                return;
            }
            if (o is StringBuilderCharSequence stringBuilderCs)
            {
                Set(stringBuilderCs.Value?.ToString() ?? string.Empty, value);
                return;
            }
            if (o is ICharSequence cs)
            {
                Set(cs.ToString(), value);
                return;
            }

            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into Get.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = o.ToString();
            }
            Set(s, value);
        }

        #endregion Set (value)

        #region SetImpl

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(ICharSequence text, MapValue value)
        {
            // LUCENENET: Added guard clause
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (text is CharArrayCharSequence charArrayCs)
            {
                SetImpl(charArrayCs.Value ?? Arrays.Empty<char>(), value);
            }

            SetImpl(text.ToString(), value); // could be more efficient
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(string text, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            // LUCENENET specific - only allocate char array if it is required.
            if (ignoreCase)
            {
                SetImpl(text.ToCharArray(), value);
                return;
            }
            int slot = GetSlot(text);
            if (keys[slot] != null)
            {
                values[slot] = value;
                return;
            }
            keys[slot] = text.ToCharArray();
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(char[] text, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            SetImpl(text, 0, text.Length, value);
        }

        /// <summary>
        /// LUCENENET specific. Like PutImpl, but doesn't have a return value or lookup to get the old value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetImpl(char[] text, int offset, int length, MapValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            if (ignoreCase)
            {
                charUtils.ToLower(text, offset, length);
            }
            int slot = GetSlot(text, offset, length);
            if (keys[slot] != null)
            {
                values[slot] = value;
                return;
            }
            keys[slot] = text;
            values[slot] = value;
            count++;

            if (count + (count >> 2) > keys.Length)
            {
                Rehash();
            }
        }

        #endregion SetImpl

        #region PutAll

        /// <summary>
        /// This implementation enumerates over the specified <see cref="T:IDictionary{char[],TValue}"/>'s
        /// entries, and calls this map's <see cref="Put(char[], TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IDictionary<char[], TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IDictionary<string, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IDictionary<ICharSequence, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IDictionary<object, TValue> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<char[], TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<string, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<ICharSequence, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public virtual void PutAll(IEnumerable<KeyValuePair<object, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                Put(kvp.Key, kvp.Value);
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (obj is null)
                return false;
            if (obj is not IDictionary<string, TValue> other)
                return false;
            if (this.Count != other.Count)
                return false;

            using (var iter = other.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    if (!this.TryGetValue(iter.Current.Key, out TValue value))
                        return false;

                    if (!JCG.EqualityComparer<TValue>.Default.Equals(value, iter.Current.Value))
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
            using (var iter = GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    hash = (hash * PRIME) ^ iter.CurrentKeyString.GetHashCode();
                    hash = (hash * PRIME) ^ JCG.EqualityComparer<TValue>.Default.GetHashCode(iter.CurrentValue);
                }
            }
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(char[] text, int offset, int length)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Non-negative number required.");
            if (offset > text.Length - length) // Checks for int overflow
                throw new ArgumentException("Offset and length must refer to a location within the text.");

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(ICharSequence text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetHashCode(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text), "text can't be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

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
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(char[] text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(ICharSequence text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="text"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Put(string text)
        {
            return PutImpl(text, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Adds a placeholder with the given <paramref name="o"/> as the key.
        /// Primarily for internal use by <see cref="CharArraySet"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="o"/> is <c>null</c>.</exception>
        public virtual bool Put(object o)
        {
            return PutImpl(o, PLACEHOLDER) is null;
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>.
        /// Preserves the value of <c>matchVersion</c> and <c>ignoreCase</c> from the current instance.
        /// </summary>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary()
        {
            return new CharArrayDictionary<TValue>(this.matchVersion, this.Count, ignoreCase: true);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> value. Preserves the value of <c>ignoreCase</c> from the current instance.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary(LuceneVersion matchVersion)
        {
            return new CharArrayDictionary<TValue>(matchVersion, this.Count, ignoreCase: true);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="CharArrayDictionary{TValue}"/> as a new instance of <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/> values.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns> A copy of the current <see cref="CharArrayDictionary{TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
        public virtual CharArrayDictionary<TValue> ToCharArrayDictionary(LuceneVersion matchVersion, bool ignoreCase)
        {
            return new CharArrayDictionary<TValue>(matchVersion, this.Count, ignoreCase);
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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="key"/>.</exception>
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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual bool TryGetValue(char[] key, out TValue value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual bool TryGetValue(object key, out TValue value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (key is string str)
                return TryGetValue(str, out value);
            if (key is char[] charArray)
                return TryGetValue(charArray, 0, charArray.Length, out value);
            if (key is StringCharSequence strCs)
                return TryGetValue(strCs.Value ?? string.Empty, out value);
            if (key is CharArrayCharSequence charArrayCs)
                return TryGetValue(charArrayCs.Value ?? Arrays.Empty<char>(), out value);
            if (key is StringBuilderCharSequence stringBuilderCs)
                return TryGetValue(stringBuilderCs.Value?.ToString() ?? string.Empty, out value);
            if (key is ICharSequence cs)
                return TryGetValue(cs.ToString(), out value);

            // LUCENENET: We need value types to be represented using the invariant
            // culture, so it is consistent regardless of the current culture. 
            // It's easy to work out if this is a value type, but difficult
            // to get to the ToString(IFormatProvider) overload of the type without
            // a lot of special cases. It's easier just to change the culture of the 
            // thread before calling ToString(), but we don't want that behavior to
            // bleed into ContainsKey.
            string s;
            using (var context = new CultureContext(CultureInfo.InvariantCulture))
            {
                s = key.ToString();
            }
            return TryGetValue(s, out value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <param name="offset">The position of the <paramref name="key"/> where the target key begins.</param>
        /// <param name="length">The total length of the <paramref name="key"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> refer to a position outside of <paramref name="key"/>.</exception>
        public virtual TValue this[char[] key, int offset, int length]
        {
            get => Get(key, offset, length);
            set => Set(key, offset, length, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[char[] key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[ICharSequence key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[string key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public virtual TValue this[object key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public virtual CharArraySet Keys => KeySet;

        ICollection<string> IDictionary<string, TValue>.Keys => KeySet;


        private volatile ICollection<TValue> valueSet;

        /// <summary>
        /// Gets a collection containing the values in the <see cref="CharArrayDictionary{TValue}"/>.
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

        #region Nested Class: ValueCollection

        /// <summary>
        /// LUCENENET specific class that represents the values in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        private sealed class ValueCollection : ICollection<TValue>
        {
            private readonly CharArrayDictionary<TValue> outerInstance;

            public ValueCollection(CharArrayDictionary<TValue> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Count => outerInstance.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item)
            {
                throw UnsupportedOperationException.Create();
            }

            void ICollection<TValue>.Clear()
            {
                throw UnsupportedOperationException.Create();
            }

            [SuppressMessage("Style", "IDE0002:Name can be simplified", Justification = "This is a false warning.")]
            public bool Contains(TValue item)
            {
                for (int i = 0; i < outerInstance.values.Length; i++)
                {
                    var value = outerInstance.values[i];
                    if (JCG.EqualityComparer<TValue>.Equals(value, item))
                        return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                using var iter = GetEnumerator();
                for (int i = arrayIndex; iter.MoveNext(); i++)
                {
                    array[i] = iter.Current;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(outerInstance);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool ICollection<TValue>.Remove(TValue item)
            {
                throw UnsupportedOperationException.Create();
            }

            public override string ToString()
            {
                using var i = GetEnumerator();
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
            public sealed class Enumerator : IEnumerator<TValue>
            {
                private readonly CharArrayDictionary<TValue>.Enumerator entryIterator;

                public Enumerator(CharArrayDictionary<TValue> outerInstance)
                {
                    this.entryIterator = new CharArrayDictionary<TValue>.Enumerator(outerInstance, !outerInstance.IsReadOnly);
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

        #endregion Nested Class: ValueCollection

        /// <summary>
        /// <c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public virtual bool IsReadOnly => false;

        #endregion For .NET Support LUCENENET

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public virtual Enumerator GetEnumerator()
        {
            return new Enumerator(this, !IsReadOnly);
        }

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ICharArrayDictionaryEnumerator ICharArrayDictionary.GetEnumerator() => GetEnumerator();

        bool IDictionary<string, TValue>.Remove(string key)
        {
            throw UnsupportedOperationException.Create();
        }

        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public virtual int Count => count;

        /// <summary>
        /// Returns a string that represents the current object. (Inherited from <see cref="object"/>.)
        /// </summary>
        public override string ToString()
        {
            if (count == 0)
                return "{}";

            var sb = new StringBuilder("{");

            using (var iter1 = this.GetEnumerator())
            {
                while (iter1.MoveNext())
                {
                    if (sb.Length > 1)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(iter1.CurrentKey);
                    sb.Append('=');
                    sb.Append(iter1.CurrentValue);
                }
            }

            return sb.Append('}').ToString();
        }

   

        // LUCENENET: Removed entrySet because in .NET we use the collection itself as the IEnumerable
        private CharArraySet keySet = null;

        // LUCENENET: Removed entrySet(), createEntrySet() because in .NET we use the collection itself as the IEnumerable

        // LUCENENET: Removed originalKeySet() because we fixed infinite recursion
        // by adding a custom enumerator for KeyCollection.

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
                    keySet = new KeyCollection(this);
                }
                return keySet;
            }
        }

        #region Nested Class: KeyCollection

        // LUCENENET: This was an anonymous class in Java
        private sealed class KeyCollection : CharArraySet
        {
            internal KeyCollection(CharArrayDictionary<TValue> map)
                : base(map)
            {
            }

            public override bool IsReadOnly => true;

            public override bool Add(object o)
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(ICharSequence text)
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(string text)
            {
                throw UnsupportedOperationException.Create();
            }
            public override bool Add(char[] text)
            {
                throw UnsupportedOperationException.Create();
            }
        }

        #endregion Nested Class: KeyCollection

        #region Nested Class: Enumerator

        /// <summary>
        /// Public enumerator class so efficient properties are exposed to users.
        /// <para/>
        /// <b>Note:</b> This enumerator has no checks to ensure the collection has
        /// not been modified during enumeration. The behavior after calling a method
        /// that mutates state such as
        /// <see cref="Clear()"/> or an overload of <see cref="Add(string, TValue)"/>,
        /// <see cref="Put(string)"/>, <see cref="PutAll(IDictionary{string, TValue})"/>
        /// or <see cref="this[string]"/> is undefined.
        /// </summary>
        public class Enumerator : IEnumerator<KeyValuePair<string, TValue>>, ICharArrayDictionaryEnumerator
        {
            private readonly CharArrayDictionary<TValue> outerInstance;

            internal int pos = -1;
            internal int lastPos;
            internal readonly bool allowModify;

            internal Enumerator(CharArrayDictionary<TValue> outerInstance, bool allowModify)
            {
                this.outerInstance = outerInstance;
                this.allowModify = allowModify;
                GoNext();
            }

            private void GoNext() // LUCENENET: Changed accessibility from internal to private
            {
                lastPos = pos;
                pos++;
                while (pos < outerInstance.keys.Length && outerInstance.keys[pos] is null)
                {
                    pos++;
                }
            }

            internal bool HasNext => pos < outerInstance.keys.Length;

            /// <summary>
            /// Gets the current key as a <see cref="CharArrayCharSequence"/>... do not modify the returned char[]
            /// </summary>
            // LUCENENET specific - quick access to ICharSequence interface
            public virtual ICharSequence CurrentKeyCharSequence
                => outerInstance.keys[lastPos].AsCharSequence();

            /// <summary>
            /// Gets the current key... do not modify the returned char[]
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            [WritableArray]
            public virtual char[] CurrentKey
                => outerInstance.keys[lastPos];

            /// <summary>
            /// Gets the current key as a newly created <see cref="string"/> object.
            /// </summary>
            public virtual string CurrentKeyString
                => new string(outerInstance.keys[lastPos]);

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
            /// Sets the value associated with the current key
            /// </summary>
            /// <returns>Returns the value prior to the update.</returns>
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
                => new KeyValuePair<string, TValue>(CurrentKeyString, CurrentValue);

            object IEnumerator.Current => Current;

            #endregion
        }

        #endregion Nested Class: Enumerator

        // LUCENENET NOTE: The Java Lucene type MapEntry was removed here because it is not possible 
        // to inherit the value type KeyValuePair{TKey, TValue} in .NET.

        // LUCENENET: EntrySet class removed because in .NET we get the entries by calling GetEnumerator() on the dictionary.

        // LUCENENET: Moved UnmodifiableMap static methods to CharArrayDictionary class

        // LUCENENET: Moved Copy static methods to CharArrayDictionary class

        // LUCENENET: Removed EmptyMap() - use Empty instead

        // LUCENENET: Moved UnmodifiableCharArraymap to CharArrayDictionary class

        // LUCENENET: Moved EmptyCharArrayDictionary to CharArrayDictionary class
    }

    /// <summary>
    /// LUCENENET specific interface used so <see cref="CharArraySet"/>
    /// can hold a reference to <see cref="CharArrayDictionary{TValue}"/> without
    /// knowing its generic closing type for TValue.
    /// </summary>
    internal interface ICharArrayDictionary
    {
        void Clear();
        bool ContainsKey(char[] text, int offset, int length);
        bool ContainsKey(char[] text);
        bool ContainsKey(object o);
        bool ContainsKey(string text);
        bool ContainsKey(ICharSequence text);
        int Count { get; }
        bool IgnoreCase { get; }
        bool IsReadOnly { get; }
        LuceneVersion MatchVersion { get; }
        bool Put(char[] text);
        bool Put(ICharSequence text);
        bool Put(object o);
        bool Put(string text);
        void Set(char[] text);
        void Set(ICharSequence text);
        void Set(object o);
        void Set(string text);
        ICharArrayDictionaryEnumerator GetEnumerator();
    }

    /// <summary>
    /// LUCENENET specific interface used so <see cref="CharArraySet"/> can
    /// iterate the keys of <see cref="CharArrayDictionary{TValue}"/> without
    /// knowing its generic closing type for TValue.
    /// </summary>
    internal interface ICharArrayDictionaryEnumerator : IDisposable
    {
        bool MoveNext();
        ICharSequence CurrentKeyCharSequence { get; }
        string CurrentKeyString { get; }
        char[] CurrentKey { get; }
        void Reset();
    }

    public static class CharArrayDictionary // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Returns a copy of the given map as a <see cref="CharArrayDictionary{TValue}"/>. If the given map
        /// is a <see cref="CharArrayDictionary{TValue}"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArrayDictionary{TValue}"/> where
        /// the <see cref="LuceneVersion"/> of the source map differs from its copy
        /// <see cref="CharArrayDictionary{TValue}.CharArrayDictionary(LuceneVersion, IDictionary{string, TValue}, bool)"/> should be used instead.
        /// The <see cref="Copy{TValue}(LuceneVersion, IDictionary{string, TValue})"/> will preserve the <see cref="LuceneVersion"/> of the
        /// source map if it is an instance of <see cref="CharArrayDictionary{TValue}"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. This argument will be ignored if the
        ///          given map is a <see cref="CharArrayDictionary{TValue}"/>. </param>
        /// <param name="map">
        ///          a map to copy </param>
        /// <returns> a copy of the given map as a <see cref="CharArrayDictionary{TValue}"/>. If the given map
        ///         is a <see cref="CharArrayDictionary{TValue}"/> the ignoreCase property as well as the
        ///         <paramref name="matchVersion"/> will be of the given map will be preserved. </returns>
        public static CharArrayDictionary<TValue> Copy<TValue>(LuceneVersion matchVersion, IDictionary<string, TValue> map)
        {
            if (map == CharArrayDictionary<TValue>.Empty)
            {
                return CharArrayDictionary<TValue>.Empty;
            }

            if (map is CharArrayDictionary<TValue> m)
            {
                // use fast path instead of iterating all values
                // this is even on very small sets ~10 times faster than iterating
                var keys = new char[m.keys.Length][];
                Array.Copy(m.keys, 0, keys, 0, keys.Length);
                var values = new CharArrayDictionary<TValue>.MapValue[m.values.Length];
                Array.Copy(m.values, 0, values, 0, values.Length);
                m = new CharArrayDictionary<TValue>(m) { keys = keys, values = values };
                return m;
            }
            return new CharArrayDictionary<TValue>(matchVersion, map, false);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to copy <see cref="CharArrayDictionary{TValue}"/> without knowing 
        /// its generic type.
        /// </summary>
        internal static CharArrayDictionary<TValue> Copy<TValue>(LuceneVersion matchVersion, ICharArrayDictionary map)
        {
            return Copy(matchVersion, map as IDictionary<string, TValue>);
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArrayDictionary{TValue}"/>. This allows to provide
        /// unmodifiable views of internal map for "read-only" use.
        /// </summary>
        /// <param name="map">
        ///          a map for which the unmodifiable map is returned. </param>
        /// <returns> an new unmodifiable <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentException">
        ///           if the given map is <c>null</c>. </exception>
        [Obsolete("Use the CharArrayDictionary<TValue>.AsReadOnly() instance method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArrayDictionary<TValue> UnmodifiableMap<TValue>(CharArrayDictionary<TValue> map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map), "Given map is null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayDictionary<TValue>.Empty || map.Count == 0)
            {
                return CharArrayDictionary<TValue>.Empty;
            }
            if (map is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>)
            {
                return map;
            }
            return new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(map);
        }

        /// <summary>
        /// Used by <see cref="CharArraySet"/> to create an <see cref="UnmodifiableCharArrayDictionary{TValue}"/> instance
        /// without knowing the type of <typeparamref name="TValue"/>.
        /// </summary>
        internal static ICharArrayDictionary UnmodifiableMap<TValue>(ICharArrayDictionary map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map), "Given map is null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (map == CharArrayDictionary<TValue>.Empty || map.Count == 0)
            {
                return CharArrayDictionary<TValue>.Empty;
            }
            if (map is CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>)
            {
                return map;
            }
            return new CharArrayDictionary.UnmodifiableCharArrayDictionary<TValue>(map);
        }

        // package private CharArraySet instanceof check in CharArraySet
        internal class UnmodifiableCharArrayDictionary<TValue> : CharArrayDictionary<TValue>
        {
            public UnmodifiableCharArrayDictionary(CharArrayDictionary<TValue> map)
                : base(map)
            { }

            public UnmodifiableCharArrayDictionary(ICharArrayDictionary map)
                : base(map as CharArrayDictionary<TValue>)
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

            // LUCENENET: Removed CreateEntrySet() method - we use IsReadOnly to control whether it can be written to

            #region Added for better .NET support LUCENENET

            public override bool IsReadOnly => true;

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

            #endregion
        }

        /// <summary>
        /// Empty <see cref="UnmodifiableCharArrayDictionary{V}"/> optimized for speed.
        /// Contains checks will always return <c>false</c> or throw
        /// NPE if necessary.
        /// </summary>
        internal class EmptyCharArrayDictionary<V> : UnmodifiableCharArrayDictionary<V>
        {
            public EmptyCharArrayDictionary()
#pragma warning disable 612, 618
                : base(new CharArrayDictionary<V>(LuceneVersion.LUCENE_CURRENT, 0, false))
#pragma warning restore 612, 618
            {
            }

            public override bool ContainsKey(char[] text, int offset, int length)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey(char[] text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey(ICharSequence text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return false;
            }

            public override bool ContainsKey(object o)
            {
                if (o is null)
                    throw new ArgumentNullException(nameof(o));

                return false;
            }

            public override V Get(char[] text, int offset, int length)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return default;
            }

            public override V Get(char[] text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return default;
            }

            public override V Get(ICharSequence text)
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));

                return default;
            }

            public override V Get(object o)
            {
                if (o is null)
                    throw new ArgumentNullException(nameof(o));

                return default;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific extension methods for <see cref="CharArrayDictionary{TValue}"/>.
    /// </summary>
    public static class CharArrayDictionaryExtensions
    {
        #region ContainsKey

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="bool"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, bool key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString());
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="byte"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, byte key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="char"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, char key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey("" + key);
        }

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="decimal"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, decimal key)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="double"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, double key)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        ///// <summary>
        ///// <c>true</c> if the <paramref name="key"/> <see cref="float"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        ///// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, float key)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        //}

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="int"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, int key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="long"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, long key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="sbyte"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, sbyte key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="short"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, short key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="uint"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, uint key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="ulong"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, ulong key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="key"/> <see cref="ushort"/> is in the <see cref="CharArrayDictionary{TValue}.KeySet"/>; 
        /// otherwise <c>false</c>. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool ContainsKey<TValue>(this CharArrayDictionary<TValue> map, ushort key)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.ContainsKey(key.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Get

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, bool text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString());
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, byte text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, char text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, decimal text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, double text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, float text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, int text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, long text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, sbyte text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, short text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, uint text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, ulong text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the value of the mapping of the chars inside this <paramref name="text"/>.
        /// <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Get<TValue>(this CharArrayDictionary<TValue> map, ushort text)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Get(text.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

        #region Put

        /// <summary>
        /// Add the given mapping.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, bool text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, byte text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, char text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        ///// <summary>
        ///// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, decimal text, TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        ///// <summary>
        ///// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, double text, TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        ///// <summary>
        ///// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, float text, TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        //}

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, int text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, long text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, sbyte text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, short text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, uint text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, ulong text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        /// <summary>
        /// Add the given mapping. <paramref name="text"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static TValue Put<TValue>(this CharArrayDictionary<TValue> map, ushort text, TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.Put(text.ToString(CultureInfo.InvariantCulture), value);
        }

        #endregion

        #region PutAll

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<bool, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="byte"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<byte, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="char"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<char, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="decimal"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<decimal, TValue> dictionary)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (dictionary is null)
        //        throw new ArgumentNullException(nameof(dictionary));

        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="double"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<double, TValue> dictionary)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (dictionary is null)
        //        throw new ArgumentNullException(nameof(dictionary));

        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="float"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<float, TValue> dictionary)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (dictionary is null)
        //        throw new ArgumentNullException(nameof(dictionary));

        //    foreach (var kvp in dictionary)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="int"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<int, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="long"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<long, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="sbyte"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<sbyte, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="short"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<short, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="uint"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<uint, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="ulong"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<ulong, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="dictionary"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="ushort"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="dictionary">A dictionary of values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="dictionary"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IDictionary<ushort, TValue> dictionary)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            foreach (var kvp in dictionary)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }


        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<bool, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="byte"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<byte, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<char, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put("" + kvp.Key, kvp.Value);
            }
        }

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="decimal"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<decimal, TValue>> collection)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (collection is null)
        //        throw new ArgumentNullException(nameof(collection));

        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="double"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<double, TValue>> collection)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (collection is null)
        //        throw new ArgumentNullException(nameof(collection));

        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        ///// <summary>
        ///// This implementation enumerates over the specified <paramref name="collection"/>'s
        ///// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        ///// <see cref="float"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="collection">The values to add/update in the current map.</param>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        //public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<float, TValue>> collection)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));
        //    if (collection is null)
        //        throw new ArgumentNullException(nameof(collection));

        //    foreach (var kvp in collection)
        //    {
        //        map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
        //    }
        //}

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="int"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<int, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="long"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<long, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="sbyte"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<sbyte, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="short"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<short, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="uint"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<uint, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="ulong"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<ulong, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var kvp in collection)
            {
                map.Put(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value);
            }
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this map's <see cref="CharArrayDictionary{TValue}.Put(string, TValue)"/> operation once for each entry.
        /// <see cref="ushort"/> values are converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="collection">The values to add/update in the current map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> or <paramref name="collection"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static void PutAll<TValue>(this CharArrayDictionary<TValue> map, IEnumerable<KeyValuePair<ushort, TValue>> collection)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, bool key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, byte key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

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
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, char key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue("" + key, out value);
        }

        ///// <summary>
        ///// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, decimal key, out TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        ///// <summary>
        ///// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, double key, out TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        ///// <summary>
        ///// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        ///// </summary>
        ///// <param name="map">this map</param>
        ///// <param name="key">The key of the value to get.</param>
        ///// <param name="value">When this method returns, contains the value associated with the specified key, 
        ///// if the key is found; otherwise, the default value for the type of the value parameter. 
        ///// This parameter is passed uninitialized.</param>
        ///// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        ///// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        //public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, float key, out TValue value)
        //{
        //    if (map is null)
        //        throw new ArgumentNullException(nameof(map));

        //    return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        //}

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, int key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, long key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, sbyte key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, short key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, uint key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, ulong key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        /// <summary>
        /// Gets the value associated with the specified key. <paramref name="key"/> is converted using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="map">this map</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified key; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        [CLSCompliant(false)]
        public static bool TryGetValue<TValue>(this CharArrayDictionary<TValue> map, ushort key, out TValue value)
        {
            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map.TryGetValue(key.ToString(CultureInfo.InvariantCulture), out value);
        }

        #endregion
    }

    /// <summary>
    /// Extensions to <see cref="IDictionary{TKey, TValue}"/> for <see cref="CharArrayDictionary{TValue}"/>.
    /// </summary>
    // LUCENENET specific - allow .NET-like syntax for copying CharArrayDictionary
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns a copy of the current <see cref="IDictionary{TKey, TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> value.
        /// </summary>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="dictionary">
        ///          A <see cref="IDictionary{TKey, TValue}"/> to copy. </param>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <returns> A copy of the current dictionary as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        public static CharArrayDictionary<TValue> ToCharArrayDictionary<TValue>(this IDictionary<string, TValue> dictionary, LuceneVersion matchVersion)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return new CharArrayDictionary<TValue>(matchVersion, dictionary.Count, ignoreCase: true);
        }

        /// <summary>
        /// Returns a copy of the current <see cref="IDictionary{TKey, TValue}"/> as a <see cref="CharArrayDictionary{TValue}"/>
        /// using the specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/> values.
        /// </summary>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="dictionary">
        ///          A <see cref="IDictionary{TKey, TValue}"/> to copy. </param>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns> A copy of the current dictionary as a <see cref="CharArrayDictionary{TValue}"/>. </returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <c>null</c>.</exception>
        public static CharArrayDictionary<TValue> ToCharArrayDictionary<TValue>(this IDictionary<string, TValue> dictionary, LuceneVersion matchVersion, bool ignoreCase)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            return new CharArrayDictionary<TValue>(matchVersion, dictionary.Count, ignoreCase);
        }
    }
}