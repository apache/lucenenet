// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JCG = J2N.Collections.Generic;
using CollectionMarshal = J2N.Runtime.InteropServices.CollectionMarshal;
#nullable enable

// LUCENENET specific - this class was significantly refactored from its Java counterpart to look and act more like collections in .NET.
// It is backed by a J2N Dictionary<string, TValue> rather than a custom hash table of char[] (see #1478).

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
    /// A simple class that stores text <see cref="string"/>s as keys in a hash table. Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the dictionary, etc.  It is designed to be quick to retrieve items
    /// by <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> keys (such as a slice of a term buffer) without the necessity
    /// of converting to a <see cref="string"/> first.
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
    /// <para>
    /// When the dictionary is case insensitive, keys are lowercased per code point in the invariant culture when
    /// they are added, so enumerating the dictionary returns the lowercased form.
    /// </para>
    /// </summary>
    [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
    [SuppressMessage("CodeQuality", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Following Microsoft's code style for collections")]
    [SuppressMessage("CodeQuality", "S1939:Inheritance list should not be redundant", Justification = "Following Microsoft's code style for collections")]
    public sealed class CharArrayDictionary<TValue> : IDictionary<string, TValue>, IDictionary, IReadOnlyDictionary<string, TValue>
    {
        // LUCENENET: Made public, renamed Empty
        /// <summary>
        /// Returns an empty, read-only dictionary. </summary>
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
#pragma warning disable 612, 618
        public static readonly CharArrayDictionary<TValue> Empty = new CharArrayDictionary<TValue>(LuceneVersion.LUCENE_CURRENT, ignoreCase: false, CreateDictionary(0, comparer: null), isReadOnly: true);
#pragma warning restore 612, 618

        private const int DefaultDictionarySize = 8; // LUCENENET specific

        private readonly JCG.Dictionary<string, TValue> dictionary;
        private readonly JCG.Dictionary<string, TValue>.SpanAlternateLookup<char> lookup;
        private readonly CharArrayComparer? comparer; // null if case sensitive
        private readonly LuceneVersion matchVersion;
        private readonly bool ignoreCase;
        private readonly bool isReadOnly;

        private KeyCollection? keys;
        private ValueCollection? values;

        /// <summary>
        /// Create dictionary with enough capacity to hold <paramref name="capacity"/> terms.
        /// </summary>
        /// <param name="matchVersion">
        ///          lucene compatibility version - see <see cref="CharArrayDictionary{TValue}"/> for details. </param>
        /// <param name="capacity">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, int capacity, bool ignoreCase)
            : this(matchVersion, ignoreCase, CreateDictionary(capacity, GetComparer(matchVersion, ignoreCase)), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a dictionary from the mappings in another dictionary.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="collection">
        ///          a dictionary (<see cref="T:IDictionary{string, V}"/>) whose mappings to be copied. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> or one of its keys is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Two keys in <paramref name="collection"/> are equal according to the
        /// comparison rules of this dictionary.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, IDictionary<string, TValue> collection, bool ignoreCase)
            : this(matchVersion, (IEnumerable<KeyValuePair<string, TValue>>)collection, ignoreCase)
        {
        }

        /// <summary>
        /// Creates a dictionary from the mappings in a collection of <see cref="KeyValuePair{TKey, TValue}"/>s.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <a href="#version">Version
        ///          note</a> above for details. </param>
        /// <param name="collection">
        ///          a collection of mappings to be copied. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive;
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> or one of its keys is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Two keys in <paramref name="collection"/> are equal according to the
        /// comparison rules of this dictionary.</exception>
        public CharArrayDictionary(LuceneVersion matchVersion, IEnumerable<KeyValuePair<string, TValue>> collection, bool ignoreCase)
            : this(matchVersion, ignoreCase, CreateDictionary(collection, GetComparer(matchVersion, ignoreCase)), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a copy of the given dictionary as a new <see cref="CharArrayDictionary{TValue}"/>.
        /// <para/>
        /// If <paramref name="dictionary"/> is a <see cref="CharArrayDictionary{TValue}"/>, its <c>ignoreCase</c> and
        /// match version are preserved and <paramref name="matchVersion"/> is ignored. Otherwise, the
        /// new dictionary is case sensitive.
        /// <para/>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArrayDictionary{TValue}"/> where
        /// the <see cref="LuceneVersion"/> of the source dictionary differs from its copy,
        /// <see cref="CharArrayDictionary(LuceneVersion, IDictionary{string, TValue}, bool)"/> should be used instead.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. </param>
        /// <param name="dictionary">
        ///          a dictionary to copy </param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> or one of its keys is <c>null</c>.</exception>
        // LUCENENET: Replaces the static CharArrayDictionary.Copy() method
        public CharArrayDictionary(LuceneVersion matchVersion, IDictionary<string, TValue> dictionary)
            : this(dictionary is CharArrayDictionary<TValue> source ? source.matchVersion : matchVersion,
                  dictionary is CharArrayDictionary<TValue> source2 && source2.ignoreCase,
                  CreateCopy(dictionary), isReadOnly: false)
        {
        }

        /// <summary>
        /// Creates a copy of the given <see cref="CharArrayDictionary{TValue}"/>. The copy preserves the <c>ignoreCase</c>
        /// and match version of <paramref name="collection"/>, and <paramref name="matchVersion"/> is ignored.
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. </param>
        /// <param name="collection">
        ///          a dictionary to copy </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        // LUCENENET: Replaces the static CharArrayDictionary.Copy() method
        public CharArrayDictionary(LuceneVersion matchVersion, CharArrayDictionary<TValue> collection)
            : this(matchVersion, (IDictionary<string, TValue>)collection)
        {
        }

        /// <summary>
        /// Creates a copy of the given <see cref="CharArrayDictionary{TValue}"/>, preserving its <c>ignoreCase</c>
        /// and match version.
        /// </summary>
        /// <param name="collection">a dictionary to copy</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public CharArrayDictionary(CharArrayDictionary<TValue> collection)
            : this(collection is null ? throw new ArgumentNullException(nameof(collection)) : collection.matchVersion, (IDictionary<string, TValue>)collection)
        {
        }

        private CharArrayDictionary(LuceneVersion matchVersion, bool ignoreCase, JCG.Dictionary<string, TValue> dictionary, bool isReadOnly)
        {
            this.matchVersion = matchVersion;
            this.ignoreCase = ignoreCase;
            this.comparer = GetComparer(matchVersion, ignoreCase);
            this.dictionary = dictionary;
            this.lookup = dictionary.GetSpanAlternateLookup<char>();
            this.isReadOnly = isReadOnly;
        }

        private static CharArrayComparer? GetComparer(LuceneVersion matchVersion, bool ignoreCase)
            => ignoreCase ? CharArrayComparer.GetInstance(matchVersion) : null;

        private static JCG.Dictionary<string, TValue> CreateDictionary(int capacity, CharArrayComparer? comparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);

            return new JCG.Dictionary<string, TValue>(capacity, comparer ?? (IEqualityComparer<string>)StringComparer.Ordinal);
        }

        private static JCG.Dictionary<string, TValue> CreateDictionary(IEnumerable<KeyValuePair<string, TValue>> collection, CharArrayComparer? comparer)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            // Keys of a CharArrayDictionary that uses the same comparer are already folded
            if (collection is CharArrayDictionary<TValue> other && other.comparer == comparer)
                return new JCG.Dictionary<string, TValue>(other.dictionary, comparer ?? (IEqualityComparer<string>)StringComparer.Ordinal);

            int capacity = collection is ICollection<KeyValuePair<string, TValue>> c ? c.Count : DefaultDictionarySize;
            var result = CreateDictionary(capacity, comparer);
            foreach (var pair in collection)
            {
                if (pair.Key is null)
                    throw new ArgumentNullException(nameof(collection), "A given key within the collection is null.");
                if (!result.TryAdd(comparer is null ? pair.Key : comparer.Fold(pair.Key), pair.Value))
                    throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, pair.Key), nameof(collection));
            }
            return result;
        }

        private static JCG.Dictionary<string, TValue> CreateCopy(IDictionary<string, TValue> dictionary)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary is CharArrayDictionary<TValue> source)
                return CreateDictionary(source, source.comparer);
            return CreateDictionary(dictionary, comparer: null);
        }

        /// <summary>
        /// Adds the specified <paramref name="text"/> and <paramref name="value"/> to the dictionary.
        /// If the dictionary is case insensitive, the lowercased form of <paramref name="text"/> is stored.
        /// </summary>
        /// <param name="text">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public void Add(ReadOnlySpan<char> text, TValue value)
        {
            ThrowIfReadOnly();
            if (!lookup.TryAdd(text, value))
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, text.ToString()), nameof(text));
        }

        /// <summary>
        /// Adds the specified <paramref name="text"/> and <paramref name="value"/> to the dictionary.
        /// If the dictionary is case insensitive, the lowercased form of <paramref name="text"/> is stored.
        /// </summary>
        /// <param name="text">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the dictionary.</exception>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public void Add(string text, TValue value)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            ThrowIfReadOnly();
            if (!dictionary.TryAdd(Fold(text), value))
                throw new ArgumentException(string.Format(SR.Argument_AddingDuplicate, text), nameof(text));
        }

        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Returns an unmodifiable view of this <see cref="CharArrayDictionary{TValue}"/>. This allows to provide
        /// unmodifiable views of internal dictionary for "read-only" use.
        /// <para/>
        /// The returned dictionary reflects later changes made to this dictionary.
        /// </summary>
        /// <returns> an new unmodifiable <see cref="CharArrayDictionary{TValue}"/>. </returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public CharArrayDictionary<TValue> AsReadOnly()
        {
            return isReadOnly ? this : new CharArrayDictionary<TValue>(matchVersion, ignoreCase, dictionary, isReadOnly: true);
        }

        /// <summary>
        /// Clears all entries in this dictionary. This method is supported for reusing, but not
        /// <see cref="IDictionary{TKey, TValue}.Remove(TKey)"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public void Clear()
        {
            ThrowIfReadOnly();
            dictionary.Clear();
        }

        /// <summary>
        /// Not supported by <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Copies all items in the current dictionary to <paramref name="array"/> starting at the <paramref name="arrayIndex"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (arrayIndex > array.Length || Count > array.Length - arrayIndex)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> is in the <see cref="Keys"/>;
        /// otherwise <c>false</c>
        /// </summary>
        public bool ContainsKey(ReadOnlySpan<char> text)
        {
            return lookup.ContainsKey(text);
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="text"/> is in the <see cref="Keys"/>;
        /// otherwise <c>false</c>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public bool ContainsKey(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return lookup.ContainsKey(text.AsSpan());
        }

        /// <summary>
        /// Gets the value associated with the specified <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified text,
        /// if the text is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified text; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out TValue value)
        {
            return lookup.TryGetValue(text, out value);
        }

        /// <summary>
        /// Gets the value associated with the specified <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified text,
        /// if the text is found; otherwise, the default value for the type of the value parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> contains an element with the specified text; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
#pragma warning disable CS8767 // The BCL interfaces are not annotated with [MaybeNullWhen(false)] on older target frameworks
        public bool TryGetValue(string text, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return lookup.TryGetValue(text.AsSpan(), out value);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="text"/>.
        /// <para/>
        /// If the dictionary is case insensitive, setting a new key stores its lowercased form.
        /// </summary>
        /// <param name="text">The text of the value to get or set.</param>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="text"/> is not found.</exception>
        /// <exception cref="NotSupportedException">The property is set and the dictionary is read-only.</exception>
        public TValue this[ReadOnlySpan<char> text]
        {
            get
            {
                if (lookup.TryGetValue(text, out TValue? value))
                    return value;
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text.ToString()));
            }
            set
            {
                ThrowIfReadOnly();
                lookup[text] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified <paramref name="text"/>.
        /// <para/>
        /// If the dictionary is case insensitive, setting a new key stores its lowercased form.
        /// </summary>
        /// <param name="text">The text of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="text"/> is not found.</exception>
        /// <exception cref="NotSupportedException">The property is set and the dictionary is read-only.</exception>
        public TValue this[string text]
        {
            get
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));
                if (lookup.TryGetValue(text.AsSpan(), out TValue? value))
                    return value;
                throw new KeyNotFoundException(string.Format(SR.Arg_KeyNotFoundWithKey, text));
            }
            set
            {
                if (text is null)
                    throw new ArgumentNullException(nameof(text));
                ThrowIfReadOnly();
                dictionary[Fold(text)] = value;
            }
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="text"/>, replacing
        /// any existing value.
        /// </summary>
        /// <param name="text">A text with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <param name="previousValue">The previous value associated with the text, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if <paramref name="text"/> did not previously exist in the dictionary (a new entry was added);
        /// <c>false</c> if an existing value was replaced.</returns>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public bool Put(ReadOnlySpan<char> text, TValue value, [MaybeNullWhen(true)] out TValue previousValue)
        {
            ThrowIfReadOnly();
            ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(lookup, text, out bool exists);
            previousValue = exists ? slot! : default!;
            slot = value;
            return !exists;
        }

        /// <summary>
        /// Adds the <paramref name="value"/> for the passed in <paramref name="text"/>, replacing
        /// any existing value.
        /// </summary>
        /// <param name="text">A text with which the specified <paramref name="value"/> is associated.</param>
        /// <param name="value">The value to be associated with the specified <paramref name="text"/>.</param>
        /// <param name="previousValue">The previous value associated with the text, or the default for the type of <paramref name="value"/>
        /// parameter if there was no mapping for <paramref name="text"/>.</param>
        /// <returns><c>true</c> if <paramref name="text"/> did not previously exist in the dictionary (a new entry was added);
        /// <c>false</c> if an existing value was replaced.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public bool Put(string text, TValue value, [MaybeNullWhen(true)] out TValue previousValue)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            ThrowIfReadOnly();
            ref TValue? slot = ref CollectionMarshal.GetValueRefOrAddDefault(dictionary, Fold(text), out bool exists);
            previousValue = exists ? slot! : default!;
            slot = value;
            return !exists;
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this dictionary's <see cref="this[string]"/> setter once
        /// for each entry.
        /// </summary>
        /// <param name="collection">A dictionary of values to add/update in the current dictionary.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection has a <c>null</c> key.
        /// </exception>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public void PutAll(IDictionary<string, TValue> collection)
        {
            PutAll((IEnumerable<KeyValuePair<string, TValue>>)collection);
        }

        /// <summary>
        /// This implementation enumerates over the specified <paramref name="collection"/>'s
        /// entries, and calls this dictionary's <see cref="this[string]"/> setter once
        /// for each entry.
        /// </summary>
        /// <param name="collection">The values to add/update in the current dictionary.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// An element in the collection has a <c>null</c> key.
        /// </exception>
        /// <exception cref="NotSupportedException">The dictionary is read-only.</exception>
        public void PutAll(IEnumerable<KeyValuePair<string, TValue>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));
            ThrowIfReadOnly();

            foreach (var pair in collection)
            {
                this[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// <c>true</c> if the <see cref="CharArrayDictionary{TValue}"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public bool IsReadOnly => isReadOnly;

        /// <summary>
        /// The Lucene version corresponding to the compatibility behavior
        /// that this instance emulates
        /// </summary>
        public LuceneVersion MatchVersion => matchVersion;

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        // LUCENENET: In Lucene, keySet() returned a CharArraySet. In .NET, IDictionary<TKey, TValue>.Keys
        // is an ICollection<TKey>, so a read-only KeyCollection is returned instead.
        public KeyCollection Keys => keys ??= new KeyCollection(this);

        /// <summary>
        /// Gets a collection containing the values in the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public ValueCollection Values => values ??= new ValueCollection(this);

        ICollection<string> IDictionary<string, TValue>.Keys => Keys;

        ICollection<TValue> IDictionary<string, TValue>.Values => Values;

        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Values;

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(dictionary.GetEnumerator());

        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Not supported by <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        void IDictionary.Remove(object key)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported by <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        bool IDictionary<string, TValue>.Remove(string key)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported by <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">Always.</exception>
        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
        {
            throw UnsupportedOperationException.Create();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (array.Length - index < Count)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            if (array is KeyValuePair<string, TValue>[] pairs)
            {
                dictionary.CopyTo(pairs, index);
            }
            else if (array is DictionaryEntry[] dictEntryArray)
            {
                foreach (var pair in dictionary)
                    dictEntryArray[index++] = new DictionaryEntry(pair.Key, pair.Value);
            }
            else
            {
                if (array is not object?[] objects)
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));

                try
                {
                    foreach (var pair in dictionary)
                        objects[index++] = new KeyValuePair<string, TValue>(pair.Key, pair.Value);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        bool IDictionary.IsFixedSize => isReadOnly;

        ICollection IDictionary.Keys => Keys;

        ICollection IDictionary.Values => Values;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object? IDictionary.this[object key]
        {
            get
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));
                if (key is string text && TryGetValue(text, out TValue? value))
                    return value;
                return null;
            }
            set
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));
                if (key is not string text)
                    throw new ArgumentException(string.Format(SR.Arg_WrongType, key, typeof(string)), nameof(key));
                this[text] = CastValue(value);
            }
        }

        void IDictionary.Add(object key, object? value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key is not string text)
                throw new ArgumentException(string.Format(SR.Arg_WrongType, key, typeof(string)), nameof(key));
            Add(text, CastValue(value));
        }

        bool IDictionary.Contains(object key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            return key is string text && ContainsKey(text);
        }

        private static TValue CastValue(object? value)
        {
            if (value is null)
            {
                if (default(TValue) is null)
                    return default!;
                throw new ArgumentNullException(nameof(value));
            }
            if (value is TValue result)
                return result;
            throw new ArgumentException(string.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
        }

        /// <summary>
        /// Compares the specified object with this dictionary for equality. Returns <c>true</c> if the
        /// given object is also a dictionary and the two dictionaries represent the same mappings. In other words,
        /// two dictionaries <c>m1</c> and <c>m2</c> represent the same mappings if <c>m1.Keys</c> has the same
        /// elements as <c>m2.Keys</c> and the values for each key are equal. This ensures that the
        /// <see cref="Equals(object)"/> method works properly across different implementations of the
        /// <see cref="IDictionary{TKey, TValue}"/> interface. Keys are looked up using the comparison rules
        /// of this dictionary.
        /// </summary>
        /// <param name="obj">object to be compared for equality with this dictionary</param>
        /// <returns><c>true</c> if the specified object is equal to this dictionary</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj is IDictionary<string, TValue> other)
                return JCG.DictionaryEqualityComparer<string, TValue>.Default.Equals(this, other);
            return false;
        }

        /// <summary>
        /// Returns the hash code value for this dictionary. The hash code of a dictionary is
        /// defined to be the sum of the hash codes of each entry in the dictionary, so it does
        /// not depend on the order of the entries. This ensures that <c>m1.Equals(m2)</c>
        /// implies that <c>m1.GetHashCode()==m2.GetHashCode()</c> for any two dictionaries
        /// <c>m1</c> and <c>m2</c>.
        /// </summary>
        /// <returns>the hash code value for this dictionary</returns>
        public override int GetHashCode()
        {
            return JCG.DictionaryEqualityComparer<string, TValue>.Default.GetHashCode(this);
        }

        /// <summary>
        /// Returns a string that represents the current dictionary, in the form <c>{key1=value1, key2=value2}</c>.
        /// </summary>
        public override string ToString()
        {
            return dictionary.ToString(StringFormatter.InvariantCulture);
        }

        private string Fold(string text) => comparer is null ? text : comparer.Fold(text);

        private void ThrowIfReadOnly()
        {
            if (isReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
        }

        private static string FormatCollection<T>(IEnumerable<T> collection)
        {
            using var iter = collection.GetEnumerator();
            if (!iter.MoveNext())
                return "[]";

            var sb = new StringBuilder("[");
            while (true)
            {
                T current = iter.Current;
                sb.Append(current is null ? "null" : string.Format(StringFormatter.InvariantCulture, "{0}", current));
                if (!iter.MoveNext())
                    return sb.Append(']').ToString();
                sb.Append(", ");
            }
        }

        #region Nested Struct: Enumerator

        /// <summary>
        /// Enumerates the elements of a <see cref="CharArrayDictionary{TValue}"/>.
        /// </summary>
        // LUCENENET: J2N's Dictionary<TKey, TValue>.GetEnumerator() currently returns IEnumerator<T>, so this wraps it.
        public readonly struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>, IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<string, TValue>> enumerator;

            internal Enumerator(IEnumerator<KeyValuePair<string, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public KeyValuePair<string, TValue> Current => enumerator.Current;

            object IEnumerator.Current => ((IEnumerator)enumerator).Current!;

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    var current = enumerator.Current;
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            object IDictionaryEnumerator.Key => enumerator.Current.Key;

            object? IDictionaryEnumerator.Value => enumerator.Current.Value;

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose() => enumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="CharArrayDictionary{TValue}"/>.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
            /// <c>false</c> if the enumerator has passed the end of the collection.</returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            public bool MoveNext() => enumerator.MoveNext();

            void IEnumerator.Reset() => enumerator.Reset();
        }

        #endregion

        #region Nested Class: KeyCollection

        /// <summary>
        /// Class that represents the keys in the <see cref="CharArrayDictionary{TValue}"/>.
        /// This collection is read-only.
        /// </summary>
        // LUCENENET specific
        [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
        public sealed class KeyCollection : ICollection<string>, ICollection, IReadOnlyCollection<string>
        {
            private readonly CharArrayDictionary<TValue> dictionary;
            private readonly JCG.Dictionary<string, TValue>.KeyCollection keys;

            internal KeyCollection(CharArrayDictionary<TValue> dictionary)
            {
                this.dictionary = dictionary;
                this.keys = new JCG.Dictionary<string, TValue>.KeyCollection(dictionary.dictionary);
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="KeyCollection"/>.
            /// </summary>
            public int Count => keys.Count;

            bool ICollection<string>.IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;

            /// <summary>
            /// Determines whether the <see cref="KeyCollection"/> contains <paramref name="item"/>,
            /// using the comparison rules of the dictionary.
            /// </summary>
            public bool Contains(string item) => item is not null && dictionary.ContainsKey(item);

            void ICollection<string>.Add(string item) => throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);

            void ICollection<string>.Clear() => throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);

            bool ICollection<string>.Remove(string item) => throw UnsupportedOperationException.Create(SR.NotSupported_KeyCollectionSet);

            /// <summary>
            /// Copies the <see cref="KeyCollection"/> elements to an existing one-dimensional
            /// array, starting at the specified array index.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
            /// <exception cref="ArgumentException">The number of elements in the source is greater
            /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
            public void CopyTo(string[] array, int index) => keys.CopyTo(array, index);

            void ICollection.CopyTo(Array array, int index) => ((ICollection)keys).CopyTo(array, index);

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="KeyCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new Enumerator(keys.GetEnumerator());

            IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Returns a string that represents the current collection, in the form <c>[a, b, c]</c>.
            /// </summary>
            public override string ToString() => FormatCollection(this);

            /// <summary>
            /// Enumerates the elements of a <see cref="KeyCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<string>, IEnumerator
            {
                private JCG.Dictionary<string, TValue>.KeyCollection.Enumerator enumerator;

                internal Enumerator(JCG.Dictionary<string, TValue>.KeyCollection.Enumerator enumerator)
                {
                    this.enumerator = enumerator;
                }

                /// <summary>
                /// Gets the element at the current position of the enumerator.
                /// </summary>
                public readonly string Current => enumerator.Current;

                readonly object IEnumerator.Current => ((IEnumerator)enumerator).Current!;

                /// <summary>
                /// Releases all resources used by the <see cref="Enumerator"/>.
                /// </summary>
                public void Dispose() => enumerator.Dispose();

                /// <summary>
                /// Advances the enumerator to the next element of the <see cref="KeyCollection"/>.
                /// </summary>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
                public bool MoveNext() => enumerator.MoveNext();

                void IEnumerator.Reset() => ((IEnumerator)enumerator).Reset();
            }
        }

        #endregion

        #region Nested Class: ValueCollection

        /// <summary>
        /// Class that represents the values in the <see cref="CharArrayDictionary{TValue}"/>.
        /// This collection is read-only.
        /// </summary>
        // LUCENENET specific
        [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
        public sealed class ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly CharArrayDictionary<TValue> dictionary;
            private readonly JCG.Dictionary<string, TValue>.ValueCollection values;

            internal ValueCollection(CharArrayDictionary<TValue> dictionary)
            {
                this.dictionary = dictionary;
                this.values = new JCG.Dictionary<string, TValue>.ValueCollection(dictionary.dictionary);
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="ValueCollection"/>.
            /// </summary>
            public int Count => values.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;

            /// <summary>
            /// Determines whether the <see cref="ValueCollection"/> contains <paramref name="item"/>.
            /// </summary>
            public bool Contains(TValue item) => dictionary.dictionary.ContainsValue(item);

            void ICollection<TValue>.Add(TValue item) => throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);

            void ICollection<TValue>.Clear() => throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);

            bool ICollection<TValue>.Remove(TValue item) => throw UnsupportedOperationException.Create(SR.NotSupported_ValueCollectionSet);

            /// <summary>
            /// Copies the <see cref="ValueCollection"/> elements to an existing one-dimensional
            /// array, starting at the specified array index.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0.</exception>
            /// <exception cref="ArgumentException">The number of elements in the source is greater
            /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
            public void CopyTo(TValue[] array, int index) => values.CopyTo(array, index);

            void ICollection.CopyTo(Array array, int index) => ((ICollection)values).CopyTo(array, index);

            /// <summary>
            /// Returns an enumerator that iterates through the <see cref="ValueCollection"/>.
            /// </summary>
            public Enumerator GetEnumerator() => new Enumerator(values.GetEnumerator());

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// Returns a string that represents the current collection, in the form <c>[a, b, c]</c>.
            /// </summary>
            public override string ToString() => FormatCollection(this);

            /// <summary>
            /// Enumerates the elements of a <see cref="ValueCollection"/>.
            /// </summary>
            public struct Enumerator : IEnumerator<TValue>, IEnumerator
            {
                private JCG.Dictionary<string, TValue>.ValueCollection.Enumerator enumerator;

                internal Enumerator(JCG.Dictionary<string, TValue>.ValueCollection.Enumerator enumerator)
                {
                    this.enumerator = enumerator;
                }

                /// <summary>
                /// Gets the element at the current position of the enumerator.
                /// </summary>
                public readonly TValue Current => enumerator.Current;

                readonly object? IEnumerator.Current => ((IEnumerator)enumerator).Current;

                /// <summary>
                /// Releases all resources used by the <see cref="Enumerator"/>.
                /// </summary>
                public void Dispose() => enumerator.Dispose();

                /// <summary>
                /// Advances the enumerator to the next element of the <see cref="ValueCollection"/>.
                /// </summary>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
                public bool MoveNext() => enumerator.MoveNext();

                void IEnumerator.Reset() => ((IEnumerator)enumerator).Reset();
            }
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
        /// using the specified <paramref name="matchVersion"/> value. If <paramref name="dictionary"/> is a
        /// <see cref="CharArrayDictionary{TValue}"/>, its <c>ignoreCase</c> and match version are preserved;
        /// otherwise, the new dictionary is case sensitive.
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
            return new CharArrayDictionary<TValue>(matchVersion, dictionary);
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
            return new CharArrayDictionary<TValue>(matchVersion, dictionary, ignoreCase);
        }
    }

    /// <summary>
    /// LUCENENET specific. Just a class to make error messages easier to manage in one place.
    /// Ideally, these would be in resources so they can be localized (eventually), but at least
    /// this half-measure will make that somewhat easier to do and is guaranteed not to cause
    /// performance issues.
    /// </summary>
    internal static class SR
    {
        public const string Arg_ArrayPlusOffTooSmall = "Destination array is not long enough to copy all the items in the collection. Check array index and length.";
        public const string Arg_KeyNotFoundWithKey = "The given text '{0}' was not present in the dictionary.";
        public const string Arg_NonZeroLowerBound = "The lower bound of target array must be zero.";
        public const string Arg_RankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
        public const string Arg_WrongType = "The value '{0}' is not of type '{1}' and cannot be used in this generic collection.";

        public const string Argument_AddingDuplicate = "An item with the same text has already been added. Key: {0}";
        public const string Argument_InvalidArrayType = "Target array type is not compatible with the type of items in the collection.";

        public const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

        public const string NotSupported_KeyCollectionSet = "Mutating a text collection derived from a dictionary is not allowed.";
        public const string NotSupported_ReadOnlyCollection = "Collection is read-only.";
        public const string NotSupported_ValueCollectionSet = "Mutating a value collection derived from a dictionary is not allowed.";
    }
}
