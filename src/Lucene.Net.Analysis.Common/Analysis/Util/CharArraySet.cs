// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using JCG = J2N.Collections.Generic;
#nullable enable

// LUCENENET specific - this class was significantly refactored from its Java counterpart to look and act more like collections in .NET.

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
    /// A simple class that stores <see cref="string"/>s as <see cref="T:char[]"/>'s in a
    /// hash table.  Note that this is not a general purpose
    /// class.  For example, it cannot remove items from the
    /// set, nor does it resize its hash table to be smaller,
    /// etc.  It is designed to be quick to test if a <see cref="T:char[]"/>
    /// is in the set without the necessity of converting it
    /// to a <see cref="string"/> first.
    /// 
    /// <a name="version"></a>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="CharArraySet"/>:
    /// <ul>
    ///   <li> As of 3.1, supplementary characters are
    ///       properly lowercased.</li>
    /// </ul>
    /// Before 3.1 supplementary characters could not be
    /// lowercased correctly due to the lack of Unicode 4
    /// support in JDK 1.4. To use instances of
    /// <see cref="CharArraySet"/> with the behavior before Lucene
    /// 3.1 pass a <see cref="LuceneVersion"/> to the constructors.
    /// </para>
    /// <para>
    /// <em>Please note:</em> This class implements <see cref="ISet{T}"/> but
    /// does not behave like it should in all cases. The generic type is
    /// <see cref="string"/>, because you can add any object to it,
    /// that has a string representation (which is converted to a string). The add methods will use
    /// <see cref="object.ToString()"/> and store the result using a <see cref="T:char[]"/>
    /// buffer. The same behavior have the <see cref="Contains(string)"/> methods.
    /// The <see cref="GetEnumerator()"/> returns an <see cref="T:IEnumerator{char[]}"/>
    /// </para>
    /// </summary>
    [DebuggerDisplay("Count = {Count}, Values = {ToString()}")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
    [SuppressMessage("CodeQuality", "S3218:Inner class members should not shadow outer class \"static\" or type members", Justification = "Following Microsoft's code style for collections")]
    [SuppressMessage("CodeQuality", "S1939:Inheritance list should not be redundant", Justification = "Following Microsoft's code style for collections")]
    public class CharArraySet : ISet<string>, ICollection<string>, ICollection, IReadOnlyCollection<string>
#if FEATURE_READONLYSET
        , IReadOnlySet<string>
#endif
    {
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly CharArraySet Empty = new CharArraySet(CharArrayDictionary<object>.Empty);

        [Obsolete("Use Empty instead. This field will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArraySet EMPTY_SET => Empty;

        // LUCENENET: PLACEHOLDER moved to CharArrayDictionary

        internal readonly ICharArrayDictionary map;

        private const int DefaultSetSize = 8; // LUCENENET specific

        /// <summary>
        /// Create set with enough capacity to hold <paramref name="capacity"/> terms
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="capacity">
        ///          the initial capacity </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public CharArraySet(LuceneVersion matchVersion, int capacity, bool ignoreCase)
            : this(new CharArrayDictionary<object>(matchVersion, capacity, ignoreCase))
        {
        }

        /// <summary>
        /// Creates a set from a collection of <see cref="string"/>s. 
        /// </summary>
        /// <param name="matchVersion">
        ///          Compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="collection">
        ///          A collection whose elements to be placed into the set. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<string> collection, bool ignoreCase)
            : this(matchVersion, collection is ICollection<string> c ? c.Count : DefaultSetSize, ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (string text in collection)
            {
                // LUCENENET: S1699: Don't call call protected members in the constructor
                map.Set(text);
            }
        }

        /// <summary>
        /// Creates a set from a collection of <see cref="T:char[]"/>s.
        /// <para/>
        /// <b>NOTE:</b> If <paramref name="ignoreCase"/> is <c>true</c>, the text arrays will be directly modified.
        /// The user should never modify these text arrays after calling this method.
        /// </summary>
        /// <param name="matchVersion">
        ///          Compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="collection">
        ///          A collection whose elements to be placed into the set. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// </exception>
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<char[]> collection, bool ignoreCase)
            : this(matchVersion, collection is ICollection<char[]> c ? c.Count : DefaultSetSize, ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (char[] text in collection)
            {
                // LUCENENET: S1699: Don't call call protected members in the constructor
                map.Set(text);
            }
        }

        /// <summary>
        /// Creates a set from a collection of <see cref="ICharSequence"/>s. 
        /// </summary>
        /// <param name="matchVersion">
        ///          Compatibility match version see <see cref="CharArraySet"/> for details. </param>
        /// <param name="collection">
        ///          A collection whose elements to be placed into the set. </param>
        /// <param name="ignoreCase">
        ///          <c>false</c> if and only if the set should be case sensitive
        ///          otherwise <c>true</c>. </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <see cref="ICharSequence.HasValue"/> property for a given element in the <paramref name="collection"/> returns <c>false</c>.
        /// </exception>
        public CharArraySet(LuceneVersion matchVersion, IEnumerable<ICharSequence> collection, bool ignoreCase)
            : this(matchVersion, collection is ICollection<ICharSequence> c ? c.Count : DefaultSetSize, ignoreCase)
        {
            // LUCENENET: Added guard clause
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            foreach (ICharSequence text in collection)
            {
                // LUCENENET: S1699: Don't call call protected members in the constructor
                map.Set(text);
            }
        }

        /// <summary>
        /// Create set from the specified map (internal only), used also by <see cref="CharArrayDictionary{TValue}.Keys"/>
        /// </summary>
        internal CharArraySet(ICharArrayDictionary map)
        {
            this.map = map;
        }

        /// <summary>
        /// Clears all entries in this set. This method is supported for reusing, but not <see cref="M:ICollection{string}.Remove(string)"/>.
        /// </summary>
        public virtual void Clear()
        {
            map.Clear();
        }

        /// <summary>
        /// <c>true</c> if the <paramref name="length"/> chars of <paramref name="text"/> starting at <paramref name="startIndex"/>
        /// are in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="startIndex"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        public virtual bool Contains(char[] text, int startIndex, int length)
        {
            return map.ContainsKey(text, startIndex, length);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="T:char[]"/>s 
        /// are in the set 
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Contains(char[] text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            return map.ContainsKey(text, 0, text.Length);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="ICharSequence"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="text"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <paramref name="text"/>'s <see cref="ICharSequence.HasValue"/> property returns <c>false</c>.
        /// </exception>
        public virtual bool Contains(ICharSequence text)
        {
            return map.ContainsKey(text);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="string"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Contains(string text)
        {
            return map.ContainsKey(text);
        }

        /// <summary>
        /// <c>true</c> if the <see cref="object.ToString()"/> representation of <paramref name="text"/> is in the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Contains<T>(T text)
        {
            return map.ContainsKey(text);
        }

        /// <summary>
        /// Adds the <see cref="object.ToString()"/> representation of <paramref name="text"/> into the set.
        /// The <see cref="object.ToString()"/> method is called after setting the thread to <see cref="CultureInfo.InvariantCulture"/>.
        /// If the type of <paramref name="text"/> is a value type, it will be converted using the 
        /// <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        /// <param name="text">A string-able object.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Add<T>(T text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Adds a <see cref="ICharSequence"/> into the set
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Add(ICharSequence text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Adds a <see cref="string"/> into the set
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Add(string text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Adds a <see cref="T:char[]"/> directly to the set.
        /// <para/>
        /// <b>NOTE:</b> If <c>ignoreCase</c> is <c>true</c> for this <see cref="CharArraySet"/>, the text array will be directly modified.
        /// The user should never modify this text array after calling this method.
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public virtual bool Add(char[] text)
        {
            return map.Put(text);
        }

        /// <summary>
        /// Adds a <see cref="T:char[]"/> to the set using the specified <paramref name="startIndex"/> and <paramref name="length"/>.
        /// <para/>
        /// <b>NOTE:</b> If <c>ignoreCase</c> is <c>true</c> for this <see cref="CharArraySet"/>, the text array will be directly modified.
        /// </summary>
        /// <param name="text">The text to be added to the set.</param>
        /// <param name="startIndex">The position of the <paramref name="text"/> where the target text begins.</param>
        /// <param name="length">The total length of the <paramref name="text"/>.</param>
        /// <returns><c>true</c> if <paramref name="text"/> was added to the set; <c>false</c> if it already existed prior to this call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> or <paramref name="length"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="startIndex"/> and <paramref name="length"/> refer to a position outside of <paramref name="text"/>.</exception>
        public virtual bool Add(char[] text, int startIndex, int length)
        {
            return map.Put(text, startIndex, length);
        }

        /// <summary>
        /// LUCENENET specific for supporting <see cref="ICollection{T}"/>.
        /// </summary>
        void ICollection<string>.Add(string item) => Add(item);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CharArraySet"/>.
        /// </summary>
        public virtual int Count => map.Count;

        /// <summary>
        /// <c>true</c> if the <see cref="CharArraySet"/> is read-only; otherwise <c>false</c>.
        /// </summary>
        public virtual bool IsReadOnly => map.IsReadOnly;

        bool ICollection<string>.IsReadOnly => map.IsReadOnly;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// </summary>
        /// <param name="set">
        ///          a set for which the unmodifiable set is returned. </param>
        /// <returns> an new unmodifiable <see cref="CharArraySet"/>. </returns>
        /// <exception cref="ArgumentNullException">
        ///           if the given set is <c>null</c>. </exception>
        [Obsolete("Use the AsReadOnly() instance method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public static CharArraySet UnmodifiableSet(CharArraySet set)
        {
            if (set is null)
            {
                throw new ArgumentNullException(nameof(set)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (set == Empty)
            {
                return Empty;
            }
            if (set.map is CharArrayDictionary.ReadOnlyCharArrayDictionary<object>)
            {
                return set;
            }
            return new CharArraySet(CharArrayDictionary.UnmodifiableMap<object>(set.map));
        }

        /// <summary>
        /// Returns an unmodifiable <see cref="CharArraySet"/>. This allows to provide
        /// unmodifiable views of internal sets for "read-only" use.
        /// </summary>
        /// <returns>A new unmodifiable <see cref="CharArraySet"/>.</returns>
        // LUCENENET specific - allow .NET-like syntax for creating immutable collections
        public virtual CharArraySet AsReadOnly()
        {
            if (this == Empty)
            {
                return Empty;
            }
            if (this.map is CharArrayDictionary.ReadOnlyCharArrayDictionary<object>)
            {
                return this;
            }
            return new CharArraySet(CharArrayDictionary.UnmodifiableMap<object>(this.map));
        }

        /// <summary>
        /// Returns a copy of this set as a new instance <see cref="CharArraySet"/>.
        /// The <see cref="LuceneVersion"/> and <c>ignoreCase</c> property will be preserved.
        /// </summary>
        /// <returns>A copy of this set as a new instance of <see cref="CharArraySet"/>.
        ///         The <see cref="CharArrayDictionary{TValue}.ignoreCase"/> field as well as the
        ///         <see cref="CharArrayDictionary{TValue}.MatchVersion"/> will be preserved.</returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArraySet
        public virtual CharArraySet ToCharArraySet()
        {
            if (this == Empty)
            {
                return Empty;
            }

            return new CharArraySet(CharArrayDictionary.Copy<object>(this.map.MatchVersion, this.map));
        }

        /// <summary>
        /// Returns a copy of this set as a new instance <see cref="CharArraySet"/>
        /// with the provided <paramref name="matchVersion"/>.
        /// The <c>ignoreCase</c> property will be preserved from this <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns>A copy of this set as a new instance of <see cref="CharArraySet"/>.
        ///         The <see cref="CharArrayDictionary{TValue}.ignoreCase"/> field will be preserved.</returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArraySet
        public virtual CharArraySet ToCharArraySet(LuceneVersion matchVersion)
        {
            if (this == Empty)
            {
                return Empty;
            }

            return new CharArraySet(new CharArrayDictionary<object>(matchVersion, (IDictionary<string, object>)this.map, this.map.IgnoreCase));
        }

        /// <summary>
        /// Returns a copy of this set as a new instance <see cref="CharArraySet"/>
        /// with the provided <paramref name="matchVersion"/> and <paramref name="ignoreCase"/> values.
        /// </summary>
        /// <returns>A copy of this set as a new instance of <see cref="CharArraySet"/>.</returns>
        // LUCENENET specific - allow .NET-like syntax for copying CharArraySet
        public virtual CharArraySet ToCharArraySet(LuceneVersion matchVersion, bool ignoreCase)
        {
            if (this == Empty)
            {
                return Empty;
            }

            return new CharArraySet(new CharArrayDictionary<object>(matchVersion, (IDictionary<string, object>)this.map, ignoreCase));
        }

        /// <summary>
        /// Returns a copy of the given set as a <see cref="CharArraySet"/>. If the given set
        /// is a <see cref="CharArraySet"/> the ignoreCase property will be preserved.
        /// <para>
        /// <b>Note:</b> If you intend to create a copy of another <see cref="CharArraySet"/> where
        /// the <see cref="LuceneVersion"/> of the source set differs from its copy
        /// <see cref="CharArraySet.CharArraySet(LuceneVersion, IEnumerable{string}, bool)"/> should be used instead.
        /// The <see cref="Copy{T}(LuceneVersion, IEnumerable{T})"/> method will preserve the <see cref="LuceneVersion"/> of the
        /// source set it is an instance of <see cref="CharArraySet"/>.
        /// </para>
        /// </summary>
        /// <param name="matchVersion">
        ///          compatibility match version. This argument will be ignored if the
        ///          given set is a <see cref="CharArraySet"/>. </param>
        /// <param name="collection">
        ///          a set to copy </param>
        /// <returns> A copy of the given set as a <see cref="CharArraySet"/>. If the given set
        ///         is a <see cref="CharArraySet"/> the <see cref="CharArrayDictionary{TValue}.ignoreCase"/> field as well as the
        ///         <see cref="CharArrayDictionary{TValue}.MatchVersion"/> will be preserved. </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the <paramref name="collection"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <see cref="ICharSequence.HasValue"/> property for a given element in the <paramref name="collection"/> returns <c>false</c>.
        /// </exception>
        public static CharArraySet Copy<T>(LuceneVersion matchVersion, IEnumerable<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (collection == Empty)
            {
                return Empty;
            }

            // LUCENENET NOTE: Testing for *is* is at least 10x faster
            // than casting using *as* and then checking for null.
            // http://stackoverflow.com/q/1583050/181087
            if (collection is CharArraySet source)
            {
                return new CharArraySet(CharArrayDictionary.Copy<object>(source.map.MatchVersion, source.map));
            }

            return CopySet(matchVersion, collection, ignoreCase: false);
        }

        internal static CharArraySet CopySet<T>(LuceneVersion matchVersion, IEnumerable<T> collection, bool ignoreCase)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (collection is IEnumerable<string> stringCollection)
            {
                return new CharArraySet(matchVersion, stringCollection, ignoreCase);
            }
            else if (collection is IEnumerable<char[]> charArrayCollection)
            {
                return new CharArraySet(matchVersion, charArrayCollection, ignoreCase);
            }
            else if (collection is IEnumerable<ICharSequence> charSequenceCollection)
            {
                return new CharArraySet(matchVersion, charSequenceCollection, ignoreCase);
            }

            return new CharArraySet(matchVersion, collection.Select(text =>
            {
                // We cannot capture Span<T> from outside of the lambda, so we just re-alocate the
                // stack on every loop.
                var returnType = CharArrayDictionary.ConvertObjectToChars(text, out char[] chars, out string s);
                if (returnType == CharArrayDictionary.CharReturnType.String)
                    return s.ToCharArray();
                else
                    return chars;
            }), ignoreCase);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="CharArraySet"/>.
        /// </summary>
        /// <returns>An enumerator that iterates through the <see cref="CharArraySet"/>.</returns>
        /// <remarks>
        /// An enumerator remains valid as long as the collection remains unchanged. If changes are made to
        /// the collection, such as adding, modifying, or deleting elements, the enumerator is irrecoverably
        /// invalidated and the next call to <see cref="Enumerator.MoveNext()"/> or <see cref="IEnumerator.Reset()"/>
        /// throws an <see cref="InvalidOperationException"/>.
        /// <para/>
        /// This method is an <c>O(log n)</c> operation.
        /// </remarks>
        public Enumerator GetEnumerator()
        {
            // LUCENENET specific - Use custom Enumerator to prevent endless recursion
            return new Enumerator(map);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        #region Nested Struct: Enumerator

        /// <summary>
        /// Enumerates the elements of a <see cref="CharArraySet"/> object.
        /// <para/>
        /// This implementation provides direct access to the <see cref="T:char[]"/> array of the underlying collection
        /// as well as convenience properties for converting to <see cref="string"/> and <see cref="ICharSequence"/>.
        /// </summary>
        /// <remarks>
        /// The <c>foreach</c> statement of the C# language (<c>for each</c> in C++, <c>For Each</c> in Visual Basic)
        /// hides the complexity of enumerators. Therefore, using <c>foreach</c> is recommended instead of directly manipulating the enumerator.
        /// <para/>
        /// Enumerators can be used to read the data in the collection, but they cannot be used to modify the underlying collection.
        /// <para/>
        /// Initially, the enumerator is positioned before the first element in the collection. At this position, the
        /// <see cref="Current"/> property is undefined. Therefore, you must call the
        /// <see cref="MoveNext()"/> method to advance the enumerator to the first element
        /// of the collection before reading the value of <see cref="Current"/>.
        /// <para/>
        /// The <see cref="Current"/> property returns the same object until
        /// <see cref="MoveNext()"/> is called. <see cref="MoveNext()"/>
        /// sets <see cref="Current"/> to the next element.
        /// <para/>
        /// If <see cref="MoveNext()"/> passes the end of the collection, the enumerator is
        /// positioned after the last element in the collection and <see cref="MoveNext()"/>
        /// returns <c>false</c>. When the enumerator is at this position, subsequent calls to <see cref="MoveNext()"/>
        /// also return <c>false</c>. If the last call to <see cref="MoveNext()"/> returned false,
        /// <see cref="Current"/> is undefined. You cannot set <see cref="Current"/>
        /// to the first element of the collection again; you must create a new enumerator object instead.
        /// <para/>
        /// An enumerator remains valid as long as the collection remains unchanged. If changes are made to the collection,
        /// such as adding, modifying, or deleting elements, the enumerator is irrecoverably invalidated and the next call
        /// to <see cref="MoveNext()"/> or <see cref="IEnumerator.Reset()"/> throws an
        /// <see cref="InvalidOperationException"/>.
        /// <para/>
        /// The enumerator does not have exclusive access to the collection; therefore, enumerating through a collection is
        /// intrinsically not a thread-safe procedure. To guarantee thread safety during enumeration, you can lock the
        /// collection during the entire enumeration. To allow the collection to be accessed by multiple threads for
        /// reading and writing, you must implement your own synchronization.
        /// <para/>
        /// This method is an O(1) operation.
        /// </remarks>
        //  LUCENENET specific.
        public readonly struct Enumerator : IEnumerator<string>, IEnumerator
        {
            private readonly ICharArrayDictionaryEnumerator enumerator;

            internal Enumerator(ICharArrayDictionary map)
            {
                this.enumerator = map.GetEnumerator();
            }

            /// <summary>
            /// Gets the current value as a <see cref="CharArrayCharSequence"/>.
            /// </summary>
            // LUCENENET specific - quick access to ICharSequence interface
            public ICharSequence CurrentValueCharSequence
                => enumerator.CurrentKeyCharSequence;

            /// <summary>
            /// Gets the current value... do not modify the returned char[].
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            [WritableArray]
            public char[] CurrentValue => enumerator.CurrentKey;

            /// <summary>
            /// Gets the current value as a newly created <see cref="string"/> object.
            /// </summary>
            public string Current => enumerator.CurrentKeyString;

            object IEnumerator.Current
            {
                get
                {
                    if (enumerator.NotStartedOrEnded)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

                    return Current;
                }
            }

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose()
            {
                enumerator.Dispose();
            }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="CharArraySet"/>.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
            /// <c>false</c> if the enumerator has passed the end of the collection.</returns>
            /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created.</exception>
            /// <remarks>
            /// After an enumerator is created, the enumerator is positioned before the first element in the collection,
            /// and the first call to the <see cref="MoveNext()"/> method advances the enumerator to the first element
            /// of the collection.
            /// <para/>
            /// If <see cref="MoveNext()"/> passes the end of the collection, the enumerator is positioned after the last element in the
            /// collection and <see cref="MoveNext()"/> returns <c>false</c>. When the enumerator is at this position,
            /// subsequent calls to <see cref="MoveNext()"/> also return <c>false</c>.
            /// <para/>
            /// An enumerator remains valid as long as the collection remains unchanged. If changes are made to the
            /// collection, such as adding, modifying, or deleting elements, the enumerator is irrecoverably invalidated
            /// and the next call to <see cref="MoveNext()"/> or <see cref="IEnumerator.Reset()"/> throws an
            /// <see cref="InvalidOperationException"/>.
            /// </remarks>
            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            void IEnumerator.Reset() => enumerator.Reset();
        }

        #endregion Nested Struct: Enumerator

        /// <summary>
        /// Returns a string that represents the current collection.
        /// <para/>
        /// The presentation has a specific format. It is enclosed by curly
        /// brackets ("{}"). Keys and values are separated by '=',
        /// KeyValuePairs are separated by ', ' (comma and space).
        /// <c>null</c> values are represented as the string "null".
        /// </summary>
        /// <returns>A string that represents the current collection.</returns>
        public override string ToString()
        {
            if (Count == 0)
                return "[]";

            var sb = new StringBuilder("[");
            using var iter = GetEnumerator();
            while (iter.MoveNext())
            {
                if (sb.Length > 1)
                {
                    sb.Append(", ");
                }
                var currentValue = iter.CurrentValue; // LUCENENET specific - avoid string allocations by using iter.CurrentValue instead of iter.Current
                if (currentValue is not null)
                    sb.Append(currentValue);
                else
                    sb.Append("null");
            }
            return sb.Append(']').ToString();
        }

        #region LUCENENET specific members

        /// <summary>
        /// Compares the specified object with this set for equality. Returns <c>true</c> if the 
        /// given object is also a set, the two sets have the same size, and every member of the 
        /// given set is contained in this set. This ensures that the equals method works properly 
        /// across different implementations of the <see cref="T:ISet{string}"/> interface.
        /// <para/>
        /// This implementation first checks if the specified object is this set; if so it 
        /// returns <c>true</c>. Then, it checks if the specified object is a set whose 
        /// size is identical to the size of this set; if not, it returns <c>false</c>. If so, 
        /// it uses the enumerator of this set and the specified object to determine if all of the
        /// contained values are present (using <see cref="string.Equals(string)"/>).
        /// </summary>
        /// <param name="obj">object to be compared for equality with this set</param>
        /// <returns><c>true</c> if the specified object is equal to this set</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (obj is ISet<string> other)
                return JCG.SetEqualityComparer<string>.Default.Equals(this, other);
            return false;
        }

        /// <summary>
        /// Returns the hash code value for this set. The hash code of a set 
        /// is defined to be the sum of the hash codes of the elements in the 
        /// set, where the hash code of a <c>null</c> element is defined to be zero. 
        /// This ensures that <c>s1.Equals(s2)</c> implies that 
        /// <c>s1.GetHashCode()==s2.GetHashCode()</c> for any two sets s1 and s2.
        /// This implementation iterates over the set, calling the GetHashCode() 
        /// method on each element in the set, and adding up the results.
        /// </summary>
        /// <returns>the hash code value for this set</returns>
        public override int GetHashCode()
        {
            return JCG.SetEqualityComparer<string>.Default.GetHashCode(this);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space in the destination array.</exception>
        public void CopyTo(string[] array)
        {
            CopyTo(array, 0, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public void CopyTo(string[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:string[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:string[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> or <paramref name="count"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="arrayIndex"/> is greater than the length of the destination <paramref name="array"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="count"/> is greater than the available space from the <paramref name="arrayIndex"/>
        /// to the end of the destination <paramref name="array"/>.
        /// </exception>
        internal void CopyTo(string[] array, int arrayIndex, int count)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = arrayIndex, numCopied = 0; numCopied < count && iter.MoveNext(); i++, numCopied++)
            {
                array[i] = iter.Current;
            }
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[],
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[] that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space in the destination array.</exception>
        public void CopyTo(IList<char[]> array)
        {
            CopyTo(array, 0, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[]
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[] that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="index"/> to the end of the destination array.</exception>
        public void CopyTo(IList<char[]> array, int index)
        {
            CopyTo(array, index, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[]
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The jagged <see cref="T:char[][]"/> array or <see cref="IList{T}"/> of type char[] that is the destination of the
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> or <paramref name="count"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="arrayIndex"/> is greater than the length of the destination <paramref name="array"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="count"/> is greater than the available space from the <paramref name="arrayIndex"/>
        /// to the end of the destination <paramref name="array"/>.
        /// </exception>
        internal void CopyTo(IList<char[]> array, int arrayIndex, int count)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (arrayIndex > array.Count || count > array.Count - arrayIndex)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = arrayIndex, numCopied = 0; numCopied < count && iter.MoveNext(); i++, numCopied++)
            {
                array[i] = (char[])iter.CurrentValue.Clone();
            }
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:ICharSequence[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:ICharSequence[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space in the destination array.</exception>
        public void CopyTo(ICharSequence[] array)
        {
            CopyTo(array, 0, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:ICharSequence[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:ICharSequence[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source is greater
        /// than the available space from <paramref name="arrayIndex"/> to the end of the destination array.</exception>
        public void CopyTo(ICharSequence[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex, map.Count);
        }

        /// <summary>
        /// Copies the entire <see cref="CharArraySet"/> to a one-dimensional <see cref="T:ICharSequence[]"/> array, 
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:ICharSequence[]"/> Array that is the destination of the 
        /// elements copied from <see cref="CharArraySet"/>. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> or <paramref name="count"/> is less than zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="arrayIndex"/> is greater than the length of the destination <paramref name="array"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="count"/> is greater than the available space from the <paramref name="arrayIndex"/>
        /// to the end of the destination <paramref name="array"/>.
        /// </exception>
        internal void CopyTo(ICharSequence[] array, int arrayIndex, int count)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);

            using var iter = GetEnumerator();
            for (int i = arrayIndex, numCopied = 0; numCopied < count && iter.MoveNext(); i++, numCopied++)
            {
                array[i] = ((char[])iter.CurrentValue.Clone()).AsCharSequence();
            }
        }

        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            if (array is string[] strings)
            {
                CopyTo(strings, index);
            }
            else if (array is IList<char[]> chars)
            {
                CopyTo(chars, index);
            }
            else if (array is ICharSequence[] charSequences)
            {
                CopyTo(charSequences, index);
            }
            else
            {
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {

                    foreach (var entry in this)
                        objects[index++] = entry;
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        bool ICollection<string>.Remove(string item)
        {
            // LUCENENET NOTE: According to the documentation header, Remove should not be supported
            throw UnsupportedOperationException.Create();
        }

        // LUCENENET - Added to ensure equality checking works in tests
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool SetEquals(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is CharArraySet charArraySet)
                return this.map.Equals(charArraySet.map);

            if (other is ICollection<string> otherAsCollection)
            {
                if (this.Count != otherAsCollection.Count)
                    return false;

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsCollection);
            }

            int otherCount = 0;
            foreach (var local in other)
            {
                if (local is not null && !this.Contains(local))
                {
                    return false;
                }
                otherCount++;
            }
            return this.Count == otherCount;
        }

        // LUCENENET - Added to ensure equality checking works in tests
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool SetEquals(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is CharArraySet charArraySet)
                return this.map.Equals(charArraySet.map);

            if (other is ICollection<char[]> otherAsCollection)
            {
                if (this.Count != otherAsCollection.Count)
                    return false;

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsCollection);
            }

            int otherCount = 0;
            foreach (var local in other)
            {
                if (local is not null && !this.Contains(local))
                {
                    return false;
                }
                otherCount++;
            }
            return this.Count == otherCount;
        }

        // LUCENENET - Added to ensure equality checking works in tests
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool SetEquals(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is CharArraySet charArraySet)
                return this.map.Equals(charArraySet.map);

            if (other is ICollection<ICharSequence> otherAsCollection)
            {
                if (this.Count != otherAsCollection.Count)
                    return false;

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsCollection);
            }

            int otherCount = 0;
            foreach (var local in other)
            {
                if (local is null || !local.HasValue || !this.Contains(local))
                {
                    return false;
                }
                otherCount++;
            }
            return this.Count == otherCount;
        }

        // LUCENENET - Added to ensure equality checking works in tests
        /// <summary>
        /// Determines whether the current set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><c>true</c> if the current set is equal to other; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool SetEquals<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is CharArraySet charArraySet)
                return this.map.Equals(charArraySet.map);

            if (other is ICollection<T> otherAsCollection)
            {
                if (this.Count != otherAsCollection.Count)
                    return false;

                // already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal
                return ContainsAllElements(otherAsCollection);
            }

            int otherCount = 0;
            foreach (var local in other)
            {
                if (local is null || (local is ICharSequence charSequence && !charSequence.HasValue) || !this.Contains(local))
                {
                    return false;
                }
                otherCount++;
            }
            return this.Count == otherCount;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// <para/>
        /// <b>NOTE:</b> If <c>ignoreCase</c> is <c>true</c> for this <see cref="CharArraySet"/>, the text arrays will be directly modified.
        /// The user should never modify these text arrays after calling this method.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">This set instance is read-only.</exception>
        public virtual bool UnionWith(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (IsReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);

            bool modified = false;
            foreach (var item in other)
            {
                modified |= Add(item);
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// A given element within the collection is <c>null</c>.
        /// <para/>
        /// -or-
        /// <para/>
        /// The <see cref="ICharSequence.HasValue"/> property for a given element in the collection returns <c>false</c>.
        /// </exception>
        /// <exception cref="NotSupportedException">This set instance is read-only.</exception>
        public virtual bool UnionWith(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (IsReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);

            bool modified = false;
            foreach (var item in other)
            {
                modified |= Add(item);
            }
            return modified;
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">This set instance is read-only.</exception>
        public virtual bool UnionWith(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (IsReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);

            bool modified = false;
            foreach (var item in other)
            {
                modified |= Add(item);
            }
            return modified;
        }

        void ISet<string>.UnionWith(IEnumerable<string> other)
        {
            UnionWith(other);
        }

        /// <summary>
        /// Modifies the current <see cref="CharArraySet"/> to contain all elements that are present 
        /// in itself, the specified collection, or both.
        /// </summary>
        /// <param name="other">The collection whose elements should be merged into the <see cref="CharArraySet"/>.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> changed as a result of the call.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">This set instance is read-only.</exception>
        public virtual bool UnionWith<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));
            if (IsReadOnly)
                throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);

#if FEATURE_SPANFORMATTABLE
            Span<char> buffer = stackalloc char[256];
#else
            Span<char> buffer = stackalloc char[1];
#endif

            bool modified = false;
            foreach (var item in other)
            {
                if (item is char[] charArray)
                {
                    modified |= Add(charArray);
                    continue;
                }

                // Convert the item to chars in the invariant culture
                var returnType = CharArrayDictionary.ConvertObjectToChars(item, out char[] chars, out string s, buffer);
                if (returnType == CharArrayDictionary.CharReturnType.String)
                    modified |= Add(s);
                else
                    modified |= Add(chars);
            }
            return modified;
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        void ISet<string>.IntersectWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        void ISet<string>.ExceptWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
        }

        // LUCENENET - no modifications should be made outside of original
        // Java implmentation's methods.
        void ISet<string>.SymmetricExceptWith(IEnumerable<string> other)
        {
            throw UnsupportedOperationException.Create(SR.NotSupported_ReadOnlyCollection);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return true;
            }
            CharArraySet? set = other as CharArraySet;
            if (set != null)
            {
                if (this.Count > set.Count)
                {
                    return false;
                }
                return this.IsSubsetOfCharArraySet(set);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSubsetOf(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return true;
            }
            CharArraySet? set = other as CharArraySet;
            if (set != null)
            {
                if (this.Count > set.Count)
                {
                    return false;
                }
                return this.IsSubsetOfCharArraySet(set);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSubsetOf(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return true;
            }
            CharArraySet? set = other as CharArraySet;
            if (set != null)
            {
                if (this.Count > set.Count)
                {
                    return false;
                }
                return this.IsSubsetOfCharArraySet(set);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool IsSubsetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return true;
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int _);
            return foundCount == this.Count;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<string>? is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if ((set != null) && (set.Count > this.Count))
                {
                    return false;
                }
            }
            return this.ContainsAllElements(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSupersetOf(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<char[]>? is2 = other as ICollection<char[]>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if ((set != null) && (set.Count > this.Count))
                {
                    return false;
                }
            }
            return this.ContainsAllElements(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSupersetOf(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<ICharSequence>? is2 = other as ICollection<ICharSequence>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if ((set != null) && (set.Count > this.Count))
                {
                    return false;
                }
            }
            return this.ContainsAllElements(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsSupersetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<T>? is2 = other as ICollection<T>;
            if (is2 != null && is2.Count == 0)
            {
                return true;
            }
            return this.ContainsAllElements(other);
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSubsetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<string>? is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (this.Count == 0)
                {
                    return (is2.Count > 0);
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (this.Count >= set.Count)
                    {
                        return false;
                    }
                    return this.IsSubsetOfCharArraySet(set);
                }
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSubsetOf(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<char[]>? is2 = other as ICollection<char[]>;
            if (is2 != null)
            {
                if (this.Count == 0)
                {
                    return (is2.Count > 0);
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (this.Count >= set.Count)
                    {
                        return false;
                    }
                    return this.IsSubsetOfCharArraySet(set);
                }
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSubsetOf(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<ICharSequence>? is2 = other as ICollection<ICharSequence>;
            if (is2 != null)
            {
                if (this.Count == 0)
                {
                    return (is2.Count > 0);
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (this.Count >= set.Count)
                    {
                        return false;
                    }
                    return this.IsSubsetOfCharArraySet(set);
                }
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper subset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSubsetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            ICollection<T>? is2 = other as ICollection<T>;
            if (is2 != null && this.Count == 0)
            {
                return (is2.Count > 0);
            }
            // we just need to return true if the other set
            // contains all of the elements of the this set plus at least one more,
            // but we need to use the comparison rules of the current set.
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount == this.Count && unfoundCount > 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSupersetOf(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return false;
            }
            ICollection<string>? is2 = other as ICollection<string>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (set.Count >= this.Count)
                    {
                        return false;
                    }
                    return this.ContainsAllElements(set);
                }
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSupersetOf(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return false;
            }
            ICollection<char[]>? is2 = other as ICollection<char[]>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (set.Count >= this.Count)
                    {
                        return false;
                    }
                    return this.ContainsAllElements(set);
                }
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSupersetOf(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return false;
            }
            ICollection<ICharSequence>? is2 = other as ICollection<ICharSequence>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                CharArraySet? set = other as CharArraySet;
                if (set != null)
                {
                    if (set.Count >= this.Count)
                    {
                        return false;
                    }
                    return this.ContainsAllElements(set);
                }
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether a <see cref="CharArraySet"/> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object is a proper superset of <paramref name="other"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "Following Microsoft's coding style")]
        public virtual bool IsProperSupersetOf<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count == 0)
            {
                return false;
            }
            ICollection<T>? is2 = other as ICollection<T>;
            if (is2 != null && is2.Count == 0)
            {
                return true;
            }
            this.GetFoundAndUnfoundCounts(other, out int foundCount, out int unfoundCount);
            return foundCount < this.Count && unfoundCount == 0;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if the <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool Overlaps(IEnumerable<string> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (local is not null && this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if the <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool Overlaps(IEnumerable<char[]> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (local is not null && this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if the <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool Overlaps(IEnumerable<ICharSequence> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (local is not null && local.HasValue && this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the current <see cref="CharArraySet"/> object and a specified collection share common elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="CharArraySet"/> object.</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> object and <paramref name="other"/> share at least one common element; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        public virtual bool Overlaps<T>(IEnumerable<T> other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (this.Count != 0)
            {
                foreach (var local in other)
                {
                    if (local is not null && this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        [Obsolete("Use the IsSupersetOf() method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ContainsAll(IEnumerable<string> other) => IsSupersetOf(other);

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        [Obsolete("Use the IsSupersetOf() method instead. This method will be removed in 4.8.0 release candidate."), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual bool ContainsAll<T>(IEnumerable<T> other) => IsSupersetOf(other);

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        private bool ContainsAllElements(IEnumerable<string> other)
        {
            foreach (var local in other)
            {
                if (local is null || !this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        private bool ContainsAllElements(IEnumerable<char[]> other)
        {
            foreach (var local in other)
            {
                if (local is null || !this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        private bool ContainsAllElements(IEnumerable<ICharSequence> other)
        {
            foreach (var local in other)
            {
                if (local is null || !local.HasValue || !this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this collection contains all of the elements
        /// in the specified collection.
        /// </summary>
        /// <param name="other">collection to be checked for containment in this collection</param>
        /// <returns><c>true</c> if this <see cref="CharArraySet"/> contains all of the elements in the specified collection; otherwise, <c>false</c>.</returns>
        private bool ContainsAllElements<T>(IEnumerable<T> other)
        {
            foreach (var local in other)
            {
                if (local is null || (local is ICharSequence charSequence && !charSequence.HasValue) || !this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsSubsetOfCharArraySet(CharArraySet other)
        {
            foreach (var local in this)
            {
                if (local is null || !other.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        private void GetFoundAndUnfoundCounts(IEnumerable<string> other, out int foundCount, out int unfoundCount)
        {
            foundCount = 0;
            unfoundCount = 0;
            foreach (var item in other)
            {
                if (item is not null && this.Contains(item))
                {
                    foundCount++;
                }
                else
                {
                    unfoundCount++;
                }
            }
        }

        private void GetFoundAndUnfoundCounts(IEnumerable<char[]> other, out int foundCount, out int unfoundCount)
        {
            foundCount = 0;
            unfoundCount = 0;
            foreach (var item in other)
            {
                if (item is not null && this.Contains(item))
                {
                    foundCount++;
                }
                else
                {
                    unfoundCount++;
                }
            }
        }

        private void GetFoundAndUnfoundCounts(IEnumerable<ICharSequence> other, out int foundCount, out int unfoundCount)
        {
            foundCount = 0;
            unfoundCount = 0;
            foreach (var item in other)
            {
                if (item is not null && item.HasValue && this.Contains(item))
                {
                    foundCount++;
                }
                else
                {
                    unfoundCount++;
                }
            }
        }

        private void GetFoundAndUnfoundCounts<T>(IEnumerable<T> other, out int foundCount, out int unfoundCount)
        {
            foundCount = 0;
            unfoundCount = 0;
            foreach (var item in other)
            {
                if (item is not null && this.Contains(item))
                {
                    foundCount++;
                }
                else
                {
                    unfoundCount++;
                }
            }
        }

#endregion
    }

    /// <summary>
    /// Extensions to <see cref="IEnumerable{T}"/> for <see cref="CharArraySet"/>.
    /// </summary>
    // LUCENENET specific
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns a copy of this <see cref="IEnumerable{T}"/> as a new instance of <see cref="CharArraySet"/> with the
        /// specified <paramref name="matchVersion"/> and ignoreCase set to <c>false</c>.
        /// </summary>
        /// <typeparam name="T">The type of collection. Typically a <see cref="string"/> or <see cref="T:char[]"/>.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="matchVersion">Compatibility match version.</param>
        /// <returns>A copy of this <see cref="IEnumerable{T}"/> as a <see cref="CharArraySet"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public static CharArraySet ToCharArraySet<T>(this IEnumerable<T> collection, LuceneVersion matchVersion)
        {
            return CharArraySet.CopySet(matchVersion, collection, ignoreCase: false);
        }

        /// <summary>
        /// Returns a copy of this <see cref="IEnumerable{T}"/> as a new instance of <see cref="CharArraySet"/> with the
        /// specified <paramref name="matchVersion"/> and <paramref name="ignoreCase"/>.
        /// </summary>
        /// <typeparam name="T">The type of collection. Typically a <see cref="string"/> or <see cref="T:char[]"/>.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="matchVersion">Compatibility match version.</param>
        /// <param name="ignoreCase"><c>false</c> if and only if the set should be case sensitive otherwise <c>true</c>.</param>
        /// <returns>A copy of this <see cref="IEnumerable{T}"/> as a <see cref="CharArraySet"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public static CharArraySet ToCharArraySet<T>(this IEnumerable<T> collection, LuceneVersion matchVersion, bool ignoreCase)
        {
            return CharArraySet.CopySet(matchVersion, collection, ignoreCase);
        }
    }
}