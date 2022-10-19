using J2N.Globalization;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lucene.Net.Search
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Expert: Maintains caches of term values.
    ///
    /// <para/>Created: May 19, 2004 11:13:14 AM
    /// <para/>
    /// @lucene.internal
    /// <para/>
    /// @since   lucene 1.4 </summary>
    /// <seealso cref="Lucene.Net.Util.FieldCacheSanityChecker"/>
    public interface IFieldCache
    {
        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <paramref name="field"/> and returns a bit set at the size of
        /// <c>reader.MaxDoc</c>, with turned on bits for each docid that
        /// does have a value for this field.
        /// </summary>
        IBits GetDocsWithField(AtomicReader reader, string field);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <paramref name="field"/> as a single <see cref="byte"/> and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field. </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the single <see cref="byte"/> values. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        FieldCache.Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <paramref name="field"/> as bytes and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field. </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the <see cref="byte"/>s. </param>
        /// <param name="parser">  Computes <see cref="byte"/> for string values. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        FieldCache.Bytes GetBytes(AtomicReader reader, string field, FieldCache.IByteParser parser, bool setDocsWithField);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <paramref name="field"/> as <see cref="short"/>s and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field. 
        /// <para/>
        /// NOTE: this was getShorts() in Lucene
        /// </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the <see cref="short"/>s. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        FieldCache.Int16s GetInt16s(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <paramref name="field"/> as shorts and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field. 
        /// <para/>
        /// NOTE: this was getShorts() in Lucene
        /// </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the <see cref="short"/>s. </param>
        /// <param name="parser">  Computes <see cref="short"/> for string values. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        [Obsolete("(4.4) Index as a numeric field using Int32Field and then use GetInt32s(AtomicReader, string, bool) instead.")]
        FieldCache.Int16s GetInt16s(AtomicReader reader, string field, FieldCache.IInt16Parser parser, bool setDocsWithField);

        /// <summary>
        /// Returns an <see cref="FieldCache.Int32s"/> over the values found in documents in the given
        /// field.
        /// <para/>
        /// NOTE: this was getInts() in Lucene
        /// </summary>
        /// <seealso cref="GetInt32s(AtomicReader, string, FieldCache.IInt32Parser, bool)"/>
        FieldCache.Int32s GetInt32s(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Returns an <see cref="FieldCache.Int32s"/> over the values found in documents in the given
        /// field. If the field was indexed as <see cref="Documents.NumericDocValuesField"/>, it simply
        /// uses <see cref="AtomicReader.GetNumericDocValues(string)"/> to read the values.
        /// Otherwise, it checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <paramref name="field"/> as <see cref="int"/>s and returns
        /// an array of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// <para/>
        /// NOTE: this was getInts() in Lucene
        /// </summary>
        /// <param name="reader">
        ///          Used to get field values. </param>
        /// <param name="field">
        ///          Which field contains the <see cref="int"/>s. </param>
        /// <param name="parser">
        ///          Computes <see cref="int"/> for string values. May be <c>null</c> if the
        ///          requested field was indexed as <see cref="Documents.NumericDocValuesField"/> or
        ///          <see cref="Documents.Int32Field"/>. </param>
        /// <param name="setDocsWithField">
        ///          If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will also be computed and
        ///          stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">
        ///           If any error occurs. </exception>
        FieldCache.Int32s GetInt32s(AtomicReader reader, string field, FieldCache.IInt32Parser parser, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Singles"/> over the values found in documents in the given
        /// field.
        /// <para/>
        /// NOTE: this was getFloats() in Lucene
        /// </summary>
        /// <seealso cref="GetSingles(AtomicReader, string, FieldCache.ISingleParser, bool)"/>
        FieldCache.Singles GetSingles(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Singles"/> over the values found in documents in the given
        /// field. If the field was indexed as <see cref="Documents.NumericDocValuesField"/>, it simply
        /// uses <see cref="AtomicReader.GetNumericDocValues(string)"/> to read the values.
        /// Otherwise, it checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <paramref name="field"/> as <see cref="float"/>s and returns
        /// an array of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// <para/>
        /// NOTE: this was getFloats() in Lucene
        /// </summary>
        /// <param name="reader">
        ///          Used to get field values. </param>
        /// <param name="field">
        ///          Which field contains the <see cref="float"/>s. </param>
        /// <param name="parser">
        ///          Computes <see cref="float"/> for string values. May be <c>null</c> if the
        ///          requested field was indexed as <see cref="Documents.NumericDocValuesField"/> or
        ///          <see cref="Documents.SingleField"/>. </param>
        /// <param name="setDocsWithField">
        ///          If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will also be computed and
        ///          stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">
        ///           If any error occurs. </exception>
        FieldCache.Singles GetSingles(AtomicReader reader, string field, FieldCache.ISingleParser parser, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Int64s"/> over the values found in documents in the given
        /// field.
        /// <para/>
        /// NOTE: this was getLongs() in Lucene
        /// </summary>
        /// <seealso cref="GetInt64s(AtomicReader, string, FieldCache.IInt64Parser, bool)"/>
        FieldCache.Int64s GetInt64s(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Int64s"/> over the values found in documents in the given
        /// field. If the field was indexed as <see cref="Documents.NumericDocValuesField"/>, it simply
        /// uses <see cref="AtomicReader.GetNumericDocValues(string)"/> to read the values.
        /// Otherwise, it checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <paramref name="field"/> as <see cref="long"/>s and returns
        /// an array of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// <para/>
        /// NOTE: this was getLongs() in Lucene
        /// </summary>
        /// <param name="reader">
        ///          Used to get field values. </param>
        /// <param name="field">
        ///          Which field contains the <see cref="long"/>s. </param>
        /// <param name="parser">
        ///          Computes <see cref="long"/> for string values. May be <c>null</c> if the
        ///          requested field was indexed as <see cref="Documents.NumericDocValuesField"/> or
        ///          <see cref="Documents.Int64Field"/>. </param>
        /// <param name="setDocsWithField">
        ///          If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will also be computed and
        ///          stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">
        ///           If any error occurs. </exception>
        FieldCache.Int64s GetInt64s(AtomicReader reader, string field, FieldCache.IInt64Parser parser, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Doubles"/> over the values found in documents in the given
        /// field.
        /// </summary>
        /// <seealso cref="GetDoubles(AtomicReader, string, FieldCache.IDoubleParser, bool)"/>
        FieldCache.Doubles GetDoubles(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Returns a <see cref="FieldCache.Doubles"/> over the values found in documents in the given
        /// field. If the field was indexed as <see cref="Documents.NumericDocValuesField"/>, it simply
        /// uses <see cref="AtomicReader.GetNumericDocValues(string)"/> to read the values.
        /// Otherwise, it checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <paramref name="field"/> as <see cref="double"/>s and returns
        /// an array of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader">
        ///          Used to get field values. </param>
        /// <param name="field">
        ///          Which field contains the <see cref="double"/>s. </param>
        /// <param name="parser">
        ///          Computes <see cref="double"/> for string values. May be <c>null</c> if the
        ///          requested field was indexed as <see cref="Documents.NumericDocValuesField"/> or
        ///          <see cref="Documents.DoubleField"/>. </param>
        /// <param name="setDocsWithField">
        ///          If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will also be computed and
        ///          stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">
        ///           If any error occurs. </exception>
        FieldCache.Doubles GetDoubles(AtomicReader reader, string field, FieldCache.IDoubleParser parser, bool setDocsWithField);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none
        /// is found, reads the term values in <paramref name="field"/>
        /// and returns a <see cref="BinaryDocValues"/> instance, providing a
        /// method to retrieve the term (as a <see cref="BytesRef"/>) per document. </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the strings. </param>
        /// <param name="setDocsWithField">  If true then <see cref="GetDocsWithField(AtomicReader, string)"/> will
        ///        also be computed and stored in the <see cref="IFieldCache"/>. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>
        /// Expert: just like <see cref="GetTerms(AtomicReader, string, bool)"/>,
        /// but you can specify whether more RAM should be consumed in exchange for
        /// faster lookups (default is "true").  Note that the
        /// first call for a given reader and field "wins",
        /// subsequent calls will share the same cache entry.
        /// </summary>
        BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField, float acceptableOverheadRatio);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none
        /// is found, reads the term values in <paramref name="field"/>
        /// and returns a <see cref="SortedDocValues"/> instance,
        /// providing methods to retrieve sort ordinals and terms
        /// (as a <see cref="BytesRef"/>) per document. </summary>
        /// <param name="reader">  Used to get field values. </param>
        /// <param name="field">   Which field contains the strings. </param>
        /// <returns> The values in the given field for each document. </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        SortedDocValues GetTermsIndex(AtomicReader reader, string field);

        /// <summary>
        /// Expert: just like 
        /// <see cref="GetTermsIndex(AtomicReader, string)"/>, but you can specify
        /// whether more RAM should be consumed in exchange for
        /// faster lookups (default is "true").  Note that the
        /// first call for a given reader and field "wins",
        /// subsequent calls will share the same cache entry.
        /// </summary>
        SortedDocValues GetTermsIndex(AtomicReader reader, string field, float acceptableOverheadRatio);

        /// <summary>
        /// Checks the internal cache for an appropriate entry, and if none is found, reads the term values
        /// in <paramref name="field"/> and returns a <see cref="SortedSetDocValues"/> instance, providing a method to retrieve
        /// the terms (as ords) per document.
        /// </summary>
        /// <param name="reader">  Used to build a <see cref="SortedSetDocValues"/> instance </param>
        /// <param name="field">   Which field contains the strings. </param>
        /// <returns> a <see cref="SortedSetDocValues"/> instance </returns>
        /// <exception cref="IOException">  If any error occurs. </exception>
        SortedSetDocValues GetDocTermOrds(AtomicReader reader, string field);

        // LUCENENET specific CacheEntry moved to FieldCache static class

        /// <summary>
        /// EXPERT: Generates an array of <see cref="FieldCache.CacheEntry"/> objects representing all items
        /// currently in the <see cref="IFieldCache"/>.
        /// <para>
        /// NOTE: These <see cref="FieldCache.CacheEntry"/> objects maintain a strong reference to the
        /// Cached Values.  Maintaining references to a <see cref="FieldCache.CacheEntry"/> the <see cref="AtomicReader"/>
        /// associated with it has garbage collected will prevent the Value itself
        /// from being garbage collected when the Cache drops the <see cref="WeakReference"/>.
        /// </para>
        /// @lucene.experimental
        /// </summary>
        FieldCache.CacheEntry[] GetCacheEntries();

        /// <summary>
        /// <para>
        /// EXPERT: Instructs the FieldCache to forcibly expunge all entries
        /// from the underlying caches.  This is intended only to be used for
        /// test methods as a way to ensure a known base state of the Cache
        /// (with out needing to rely on GC to free <see cref="WeakReference"/>s).
        /// It should not be relied on for "Cache maintenance" in general
        /// application code.
        /// </para>
        /// @lucene.experimental
        /// </summary>
        void PurgeAllCaches();

        /// <summary>
        /// Expert: drops all cache entries associated with this
        /// reader <see cref="Index.IndexReader.CoreCacheKey"/>.  NOTE: this cache key must
        /// precisely match the reader that the cache entry is
        /// keyed on. If you pass a top-level reader, it usually
        /// will have no effect as Lucene now caches at the segment
        /// reader level.
        /// </summary>
        void PurgeByCacheKey(object coreCacheKey);

        /// <summary>
        /// If non-null, <see cref="FieldCacheImpl"/> will warn whenever
        /// entries are created that are not sane according to
        /// <see cref="Lucene.Net.Util.FieldCacheSanityChecker"/>.
        /// </summary>
        TextWriter InfoStream { set; get; }
    }

    public static class FieldCache 
    {
        /// <summary>
        /// Field values as 8-bit signed bytes
        /// </summary>
        public class Bytes // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, byte> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Bytes"/>.
            /// </summary>
            protected Bytes() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Bytes"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Bytes(Func<int, byte> get) // LUCENENET specific - Added constructor overload to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return a single <see cref="byte"/> representation of this field's value.
            /// </summary>
            public virtual byte Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Bytes EMPTY = new Bytes((docID) => 0);
        }

        /// <summary>
        /// Field values as 16-bit signed shorts
        /// <para/>
        /// NOTE: This was Shorts in Lucene
        /// </summary>
        public class Int16s // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, short> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Int16s"/>.
            /// </summary>
            protected Int16s() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Int16s"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Int16s(Func<int, short> get) // LUCENENET specific - Added constructor overload to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return a <see cref="short"/> representation of this field's value.
            /// </summary>
            public virtual short Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Int16s EMPTY = new Int16s((docID) => 0);
        }

        /// <summary>
        /// Field values as 32-bit signed integers
        /// <para/>
        /// NOTE: This was Ints in Lucene
        /// </summary>
        public class Int32s // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, int> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Int32s"/>.
            /// </summary>
            protected Int32s() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Int32s"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Int32s(Func<int, int> get) // LUCENENET specific - Added constructor overload for to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return an <see cref="int"/> representation of this field's value.
            /// </summary>
            public virtual int Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Int32s EMPTY = new Int32s((docID) => 0);
        }

        /// <summary>
        /// Field values as 64-bit signed long integers
        /// <para/>
        /// NOTE: This was Longs in Lucene
        /// </summary>
        public class Int64s // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, long> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Int64s"/>.
            /// </summary>
            protected Int64s() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Int64s"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Int64s(Func<int, long> get) // LUCENENET: Added constructor overload to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return an <see cref="long"/> representation of this field's value.
            /// </summary>
            public virtual long Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Int64s EMPTY = new Int64s((docID) => 0);
        }

        /// <summary>
        /// Field values as 32-bit floats
        /// <para/>
        /// NOTE: This was Floats in Lucene
        /// </summary>
        public class Singles // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, float> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Singles"/>.
            /// </summary>
            protected Singles() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Singles"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Singles(Func<int, float> get) // LUCENENET: Added constructor overload to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return an <see cref="float"/> representation of this field's value.
            /// </summary>
            public virtual float Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Singles EMPTY = new Singles((docID) => 0);
        }

        /// <summary>
        /// Field values as 64-bit doubles
        /// </summary>
        public class Doubles // LUCENENET specific - removed abstract so we can pass a delegate to the constructor
        {
            private readonly Func<int, double> get;
            private readonly bool hasGet;

            /// <summary>
            /// Initialize an instance of <see cref="Doubles"/>.
            /// </summary>
            protected Doubles() { } // LUCENENET specific - Added default constructor for subclasses

            /// <summary>
            /// Initialize an instance of <see cref="Doubles"/> with the specified
            /// <paramref name="get"/> delegate method.
            /// </summary>
            /// <param name="get">A <see cref="Func{T, TResult}"/> that implements the <see cref="Get(int)"/> method body.</param>
            public Doubles(Func<int, double> get) // LUCENENET: Added constructor overload to simulate anonymous classes
            {
                this.get = get ?? throw new ArgumentNullException(nameof(get));
                this.hasGet = true;
            }

            /// <summary>
            /// Return a <see cref="double"/> representation of this field's value.
            /// </summary>
            public virtual double Get(int docID) => hasGet ? get(docID) : default; // LUCENENET specific - implemented with delegate by default

            /// <summary>
            /// Zero value for every document
            /// </summary>
            public static readonly Doubles EMPTY = new Doubles((docID) => 0);
        }

        /// <summary>
        /// Interface used to identify a <see cref="CreationPlaceholder{TValue}"/> without
        /// referencing its generic closing type.
        /// </summary>
        public interface ICreationPlaceholder { }

        /// <summary>
        /// Placeholder indicating creation of this cache is currently in-progress.
        /// </summary>
        public sealed class CreationPlaceholder<TValue> : ICreationPlaceholder
        {
            internal TValue Value { get; set; }
        }

        /// <summary>
        /// Marker interface as super-interface to all parsers. It
        /// is used to specify a custom parser to
        /// <see cref="SortField.SortField(string, IParser)"/>.
        /// </summary>
        public interface IParser
        {
            /// <summary>
            /// Pulls a <see cref="Index.TermsEnum"/> from the given <see cref="Index.Terms"/>. This method allows certain parsers
            /// to filter the actual <see cref="Index.TermsEnum"/> before the field cache is filled.
            /// </summary>
            /// <param name="terms">The <see cref="Index.Terms"/> instance to create the <see cref="Index.TermsEnum"/> from.</param>
            /// <returns>A possibly filtered <see cref="Index.TermsEnum"/> instance, this method must not return <c>null</c>.</returns>
            /// <exception cref="IOException">If an <see cref="IOException"/> occurs</exception>
            TermsEnum GetTermsEnum(Terms terms);
        }

        /// <summary>
        /// Interface to parse bytes from document fields.
        /// </summary>
        /// <seealso cref="IFieldCache.GetBytes(AtomicReader, string, IByteParser, bool)"/>
        [Obsolete]
        public interface IByteParser : IParser
        {
            /// <summary>
            /// Return a single Byte representation of this field's value.
            /// </summary>
            byte ParseByte(BytesRef term);
        }

        /// <summary>
        /// Interface to parse <see cref="short"/>s from document fields.
        /// <para/>
        /// NOTE: This was ShortParser in Lucene
        /// </summary>
        /// <seealso cref="IFieldCache.GetInt16s(AtomicReader, string, IInt16Parser, bool)"/>
        [Obsolete]
        public interface IInt16Parser : IParser
        {
            /// <summary>
            /// Return a <see cref="short"/> representation of this field's value.
            /// <para/>
            /// NOTE: This was parseShort() in Lucene
            /// </summary>
            short ParseInt16(BytesRef term);
        }

        /// <summary>
        /// Interface to parse <see cref="int"/>s from document fields.
        /// <para/>
        /// NOTE: This was IntParser in Lucene
        /// </summary>
        /// <seealso cref="IFieldCache.GetInt32s(AtomicReader, string, IInt32Parser, bool)"/>
        public interface IInt32Parser : IParser
        {
            /// <summary>
            /// Return an <see cref="int"/> representation of this field's value.
            /// <para/>
            /// NOTE: This was parseInt() in Lucene
            /// </summary>
            int ParseInt32(BytesRef term);
        }

        /// <summary>
        /// Interface to parse <see cref="float"/>s from document fields.
        /// <para/>
        /// NOTE: This was FloatParser in Lucene
        /// </summary>
        public interface ISingleParser : IParser
        {
            /// <summary>
            /// Return an <see cref="float"/> representation of this field's value.
            /// <para/>
            /// NOTE: This was parseFloat() in Lucene
            /// </summary>
            float ParseSingle(BytesRef term);
        }

        /// <summary>
        /// Interface to parse <see cref="long"/> from document fields.
        /// <para/>
        /// NOTE: This was LongParser in Lucene
        /// </summary>
        /// <seealso cref="IFieldCache.GetInt64s(AtomicReader, string, IInt64Parser, bool)"/>
        public interface IInt64Parser : IParser
        {
            /// <summary>
            /// Return a <see cref="long"/> representation of this field's value.
            /// <para/>
            /// NOTE: This was parseLong() in Lucene
            /// </summary>
            long ParseInt64(BytesRef term);
        }

        /// <summary>
        /// Interface to parse <see cref="double"/>s from document fields.
        /// </summary>
        /// <seealso cref="IFieldCache.GetDoubles(AtomicReader, string, IDoubleParser, bool)"/>
        public interface IDoubleParser : IParser
        {
            /// <summary>
            /// Return an <see cref="double"/> representation of this field's value.
            /// </summary>
            double ParseDouble(BytesRef term);
        }

        /// <summary>
        /// Expert: The cache used internally by sorting and range query classes.
        /// </summary>
        public static IFieldCache DEFAULT = new FieldCacheImpl();

        /// <summary>
        /// The default parser for byte values, which are encoded by <see cref="sbyte.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        [Obsolete]
        public static readonly IByteParser DEFAULT_BYTE_PARSER = new ByteParser();

        [Obsolete]
        private sealed class ByteParser : IByteParser
        {
            public byte ParseByte(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return (byte)sbyte.Parse(term.Utf8ToString(), CultureInfo.InvariantCulture);
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_BYTE_PARSER";
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }
        }

        /// <summary>
        /// The default parser for <see cref="short"/> values, which are encoded by <see cref="short.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// NOTE: This was DEFAULT_SHORT_PARSER in Lucene
        /// </summary>
        [Obsolete]
        public static readonly IInt16Parser DEFAULT_INT16_PARSER = new Int16Parser();

        [Obsolete]
        private sealed class Int16Parser : IInt16Parser
        {
            /// <summary>
            /// NOTE: This was parseShort() in Lucene
            /// </summary>
            public short ParseInt16(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return short.Parse(term.Utf8ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_INT16_PARSER";
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }
        }

        /// <summary>
        /// The default parser for <see cref="int"/> values, which are encoded by <see cref="int.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// NOTE: This was DEFAULT_INT_PARSER in Lucene
        /// </summary>
        [Obsolete]
        public static readonly IInt32Parser DEFAULT_INT32_PARSER = new Int32Parser();

        [Obsolete]
        private sealed class Int32Parser : IInt32Parser
        {
            /// <summary>
            /// NOTE: This was parseInt() in Lucene
            /// </summary>
            public int ParseInt32(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return int.Parse(term.Utf8ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_INT32_PARSER";
            }
        }

        /// <summary>
        /// The default parser for <see cref="float"/> values, which are encoded by <see cref="float.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// NOTE: This was DEFAULT_FLOAT_PARSER in Lucene
        /// </summary>
        [Obsolete]
        public static readonly ISingleParser DEFAULT_SINGLE_PARSER = new SingleParser();

        [Obsolete]
        private sealed class SingleParser : ISingleParser
        {
            /// <summary>
            /// NOTE: This was parseFloat() in Lucene
            /// </summary>
            public float ParseSingle(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // FloatField, instead, which already decodes
                // directly from byte[]

                string text = term.Utf8ToString();
                return J2N.Numerics.Single.Parse(text, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_SINGLE_PARSER";
            }
        }

        /// <summary>
        /// The default parser for <see cref="long"/> values, which are encoded by <see cref="long.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// <para/>
        /// NOTE: This was DEFAULT_LONG_PARSER in Lucene
        /// </summary>
        [Obsolete]
        public static readonly IInt64Parser DEFAULT_INT64_PARSER = new Int64Parser();

        [Obsolete]
        private sealed class Int64Parser : IInt64Parser
        {
            /// <summary>
            /// NOTE: This was parseLong() in Lucene
            /// </summary>
            public long ParseInt64(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // LongField, instead, which already decodes
                // directly from byte[]
                return long.Parse(term.Utf8ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_INT64_PARSER";
            }
        }

        /// <summary>
        /// The default parser for <see cref="double"/> values, which are encoded by <see cref="double.ToString(string, IFormatProvider)"/>
        /// using <see cref="CultureInfo.InvariantCulture"/>.
        /// </summary>
        [Obsolete]
        public static readonly IDoubleParser DEFAULT_DOUBLE_PARSER = new DoubleParser();

        [Obsolete]
        private sealed class DoubleParser : IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // DoubleField, instead, which already decodes
                // directly from byte[]
                string text = term.Utf8ToString();
                return J2N.Numerics.Double.Parse(text, NumberStyle.Float, NumberFormatInfo.InvariantInfo);
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return terms.GetEnumerator();
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".DEFAULT_DOUBLE_PARSER";
            }
        }

        /// <summary>
        /// A parser instance for <see cref="int"/> values encoded by <see cref="NumericUtils"/>, e.g. when indexed
        /// via <see cref="Documents.Int32Field"/>/<see cref="Analysis.NumericTokenStream"/>.
        /// <para/>
        /// NOTE: This was NUMERIC_UTILS_INT_PARSER in Lucene
        /// </summary>
        public static readonly IInt32Parser NUMERIC_UTILS_INT32_PARSER = new NumericUtilsInt32Parser();

        private sealed class NumericUtilsInt32Parser : IInt32Parser
        {
            /// <summary>
            /// NOTE: This was parseInt() in Lucene
            /// </summary>
            public int ParseInt32(BytesRef term)
            {
                return NumericUtils.PrefixCodedToInt32(term);
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInt32s(terms.GetEnumerator());
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".NUMERIC_UTILS_INT32_PARSER";
            }
        }

        /// <summary>
        /// A parser instance for <see cref="float"/> values encoded with <see cref="NumericUtils"/>, e.g. when indexed
        /// via <see cref="Documents.SingleField"/>/<see cref="Analysis.NumericTokenStream"/>.
        /// <para/>
        /// NOTE: This was NUMERIC_UTILS_FLOAT_PARSER in Lucene
        /// </summary>
        public static readonly ISingleParser NUMERIC_UTILS_SINGLE_PARSER = new NumericUtilsSingleParser();

        private sealed class NumericUtilsSingleParser : ISingleParser
        {
            /// <summary>
            /// NOTE: This was parseFloat() in Lucene
            /// </summary>
            public float ParseSingle(BytesRef term)
            {
                return NumericUtils.SortableInt32ToSingle(NumericUtils.PrefixCodedToInt32(term));
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".NUMERIC_UTILS_SINGLE_PARSER";
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInt32s(terms.GetEnumerator());
            }
        }

        /// <summary>
        /// A parser instance for <see cref="long"/> values encoded by <see cref="NumericUtils"/>, e.g. when indexed
        /// via <see cref="Documents.Int64Field"/>/<see cref="Analysis.NumericTokenStream"/>.
        /// <para/>
        /// NOTE: This was NUMERIC_UTILS_LONG_PARSER in Lucene
        /// </summary>
        public static readonly IInt64Parser NUMERIC_UTILS_INT64_PARSER = new NumericUtilsInt64Parser();

        private sealed class NumericUtilsInt64Parser : IInt64Parser
        {
            /// <summary>
            /// NOTE: This was parseLong() in Lucene
            /// </summary>
            public long ParseInt64(BytesRef term)
            {
                return NumericUtils.PrefixCodedToInt64(term);
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".NUMERIC_UTILS_INT64_PARSER";
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInt64s(terms.GetEnumerator());
            }
        }

        /// <summary>
        /// A parser instance for <see cref="double"/> values encoded with <see cref="NumericUtils"/>, e.g. when indexed
        /// via <see cref="Documents.DoubleField"/>/<see cref="Analysis.NumericTokenStream"/>.
        /// </summary>
        public static readonly IDoubleParser NUMERIC_UTILS_DOUBLE_PARSER = new NumericUtilsDoubleParser();

        private sealed class NumericUtilsDoubleParser : IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                return NumericUtils.SortableInt64ToDouble(NumericUtils.PrefixCodedToInt64(term));
            }

            public override string ToString()
            {
                return typeof(IFieldCache).FullName + ".NUMERIC_UTILS_DOUBLE_PARSER";
            }

            public TermsEnum GetTermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInt64s(terms.GetEnumerator());
            }
        }

        // .NET Port: skipping down to about line 681 of java version. The actual interface methods of FieldCache are in IFieldCache below.
        /// <summary>
        /// EXPERT: A unique Identifier/Description for each item in the <see cref="IFieldCache"/>. 
        /// Can be useful for logging/debugging.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public sealed class CacheEntry
        {
            private readonly object readerKey;
            private readonly string fieldName;
            private readonly Type cacheType;
            private readonly object custom;
            private readonly object value;
            private string size;

            public CacheEntry(object readerKey, string fieldName,
                      Type cacheType,
                      object custom,
                      object value)
            {
                this.readerKey = readerKey;
                this.fieldName = fieldName;
                this.cacheType = cacheType;
                this.custom = custom;
                this.value = value;
            }

            public object ReaderKey => readerKey;

            public string FieldName => fieldName;

            public Type CacheType => cacheType;

            [Obsolete("Use Parser and AcceptableOverheadRatio instead to eliminate boxing. This property will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public object Custom => custom;

            // LUCENENET specific - Added property for when custom is an IParser
            public IParser Parser => custom as IParser;

            // LUCENENET specific - Added AcceptableOverheadRatio to eliminate boxing/unboxing
            public float? AcceptableOverheadRatio => custom is AcceptableOverheadRatio ratio ? ratio.Value : (float?)null;

            public object Value => value;

            /// <summary>
            /// Computes (and stores) the estimated size of the cache <see cref="Value"/>
            /// </summary>
            /// <seealso cref="EstimatedSize"/>
            public void EstimateSize()
            {
                long bytesUsed = RamUsageEstimator.SizeOf(Value);
                size = RamUsageEstimator.HumanReadableUnits(bytesUsed);
            }

            /// <summary>
            /// The most recently estimated size of the value, <c>null</c> unless 
            /// <see cref="EstimateSize()"/> has been called.
            /// </summary>
            public string EstimatedSize => size;

            public override string ToString()
            {
                StringBuilder b = new StringBuilder();
                b.Append('\'').Append(ReaderKey).Append("'=>");
                b.Append('\'').Append(FieldName).Append("',");
                b.Append(CacheType).Append(',').Append(custom is null ? "null" : custom.ToString()); // LUCENENET specific: use field instead of property
                b.Append("=>").Append(Value.GetType().FullName).Append('#');
                b.Append(RuntimeHelpers.GetHashCode(Value));

                string s = EstimatedSize;
                if (null != s)
                {
                    b.Append(" (size =~ ").Append(s).Append(')');
                }

                return b.ToString();
            }
        }

        // LUCENENET specific reference type to be used to store float value in the field cache
        // to avoid boxing/unboxing.
        internal class AcceptableOverheadRatio
        {
            public AcceptableOverheadRatio(float value)
            {
                Value = value;
            }

            public float Value { get; }

            public override bool Equals(object obj)
            {
                if (obj is AcceptableOverheadRatio other)
                {
                    // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                    return NumericUtils.SingleToSortableInt32(Value) == NumericUtils.SingleToSortableInt32(other.Value);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }
    }
}