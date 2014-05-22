using System;
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


	using NumericTokenStream = Lucene.Net.Analysis.NumericTokenStream;
	using DoubleField = Lucene.Net.Document.DoubleField;
	using FloatField = Lucene.Net.Document.FloatField;
	using IntField = Lucene.Net.Document.IntField;
	using LongField = Lucene.Net.Document.LongField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
	using DocTermOrds = Lucene.Net.Index.DocTermOrds;
	using IndexReader = Lucene.Net.Index.IndexReader; // javadocs
	using SortedDocValues = Lucene.Net.Index.SortedDocValues;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using NumericUtils = Lucene.Net.Util.NumericUtils;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

	/// <summary>
	/// Expert: Maintains caches of term values.
	/// 
	/// <p>Created: May 19, 2004 11:13:14 AM
	/// 
	/// @since   lucene 1.4 </summary>
	/// <seealso cref= Lucene.Net.Util.FieldCacheSanityChecker
	/// 
	/// @lucene.internal </seealso>
	public interface FieldCache
	{

	  /// <summary>
	  /// Field values as 8-bit signed bytes </summary>

	  /// <summary>
	  /// Field values as 16-bit signed shorts </summary>

	  /// <summary>
	  /// Field values as 32-bit signed integers </summary>

	  /// <summary>
	  /// Field values as 64-bit signed long integers </summary>

	  /// <summary>
	  /// Field values as 32-bit floats </summary>

	  /// <summary>
	  /// Field values as 64-bit doubles </summary>

	  /// <summary>
	  /// Placeholder indicating creation of this cache is currently in-progress.
	  /// </summary>

	  /// <summary>
	  /// Marker interface as super-interface to all parsers. It
	  /// is used to specify a custom parser to {@link
	  /// SortField#SortField(String, FieldCache.Parser)}.
	  /// </summary>

	  /// <summary>
	  /// Interface to parse bytes from document fields. </summary>
	  /// <seealso cref= FieldCache#getBytes(AtomicReader, String, FieldCache.ByteParser, boolean) </seealso>

	  /// <summary>
	  /// Interface to parse shorts from document fields. </summary>
	  /// <seealso cref= FieldCache#getShorts(AtomicReader, String, FieldCache.ShortParser, boolean) </seealso>

	  /// <summary>
	  /// Interface to parse ints from document fields. </summary>
	  /// <seealso cref= FieldCache#getInts(AtomicReader, String, FieldCache.IntParser, boolean) </seealso>

	  /// <summary>
	  /// Interface to parse floats from document fields. </summary>
	  /// <seealso cref= FieldCache#getFloats(AtomicReader, String, FieldCache.FloatParser, boolean) </seealso>

	  /// <summary>
	  /// Interface to parse long from document fields. </summary>
	  /// <seealso cref= FieldCache#getLongs(AtomicReader, String, FieldCache.LongParser, boolean) </seealso>

	  /// <summary>
	  /// Interface to parse doubles from document fields. </summary>
	  /// <seealso cref= FieldCache#getDoubles(AtomicReader, String, FieldCache.DoubleParser, boolean) </seealso>

	  /// <summary>
	  /// Expert: The cache used internally by sorting and range query classes. </summary>

	  /// <summary>
	  /// The default parser for byte values, which are encoded by <seealso cref="Byte#toString(byte)"/> </summary>
[Obsolete]
//	  public static final FieldCache_ByteParser DEFAULT_BYTE_PARSER = new FieldCache_ByteParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// The default parser for short values, which are encoded by <seealso cref="Short#toString(short)"/> </summary>
[Obsolete]
//	  public static final FieldCache_ShortParser DEFAULT_SHORT_PARSER = new FieldCache_ShortParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// The default parser for int values, which are encoded by <seealso cref="Integer#toString(int)"/> </summary>
[Obsolete]
//	  public static final FieldCache_IntParser DEFAULT_INT_PARSER = new FieldCache_IntParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// The default parser for float values, which are encoded by <seealso cref="Float#toString(float)"/> </summary>
[Obsolete]
//	  public static final FieldCache_FloatParser DEFAULT_FLOAT_PARSER = new FieldCache_FloatParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// The default parser for long values, which are encoded by <seealso cref="Long#toString(long)"/> </summary>
[Obsolete]
//	  public static final FieldCache_LongParser DEFAULT_LONG_PARSER = new FieldCache_LongParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// The default parser for double values, which are encoded by <seealso cref="Double#toString(double)"/> </summary>
[Obsolete]
//	  public static final FieldCache_DoubleParser DEFAULT_DOUBLE_PARSER = new FieldCache_DoubleParserAnonymousInnerClassHelper();

	  /// <summary>
	  /// A parser instance for int values encoded by <seealso cref="NumericUtils"/>, e.g. when indexed
	  /// via <seealso cref="IntField"/>/<seealso cref="NumericTokenStream"/>.
	  /// </summary>
//	  public static final FieldCache_IntParser NUMERIC_UTILS_INT_PARSER = new FieldCache_IntParserAnonymousInnerClassHelper2();

	  /// <summary>
	  /// A parser instance for float values encoded with <seealso cref="NumericUtils"/>, e.g. when indexed
	  /// via <seealso cref="FloatField"/>/<seealso cref="NumericTokenStream"/>.
	  /// </summary>
//	  public static final FieldCache_FloatParser NUMERIC_UTILS_FLOAT_PARSER = new FieldCache_FloatParserAnonymousInnerClassHelper2();

	  /// <summary>
	  /// A parser instance for long values encoded by <seealso cref="NumericUtils"/>, e.g. when indexed
	  /// via <seealso cref="LongField"/>/<seealso cref="NumericTokenStream"/>.
	  /// </summary>
//	  public static final FieldCache_LongParser NUMERIC_UTILS_LONG_PARSER = new FieldCache_LongParserAnonymousInnerClassHelper2();

	  /// <summary>
	  /// A parser instance for double values encoded with <seealso cref="NumericUtils"/>, e.g. when indexed
	  /// via <seealso cref="DoubleField"/>/<seealso cref="NumericTokenStream"/>.
	  /// </summary>
//	  public static final FieldCache_DoubleParser NUMERIC_UTILS_DOUBLE_PARSER = new FieldCache_DoubleParserAnonymousInnerClassHelper2();

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is found,
	  ///  reads the terms in <code>field</code> and returns a bit set at the size of
	  ///  <code>reader.maxDoc()</code>, with turned on bits for each docid that 
	  ///  does have a value for this field.
	  /// </summary>
	  Bits GetDocsWithField(AtomicReader reader, string field);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is
	  /// found, reads the terms in <code>field</code> as a single byte and returns an array
	  /// of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the single byte values. </param>
	  /// <param name="setDocsWithField">  If true then <seealso cref="#getDocsWithField"/> will
	  ///        also be computed and stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  /// @deprecated (4.4) Index as a numeric field using <seealso cref="IntField"/> and then use <seealso cref="#getInts(AtomicReader, String, boolean)"/> instead. 
	  [Obsolete("(4.4) Index as a numeric field using <seealso cref="Lucene.Net.Document.IntField"/> and then use <seealso cref="#getInts(Lucene.Net.Index.AtomicReader, String, boolean)"/> instead.")]
	  FieldCache_Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is found,
	  /// reads the terms in <code>field</code> as bytes and returns an array of
	  /// size <code>reader.maxDoc()</code> of the value each document has in the
	  /// given field. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the bytes. </param>
	  /// <param name="parser">  Computes byte for string values. </param>
	  /// <param name="setDocsWithField">  If true then <seealso cref="#getDocsWithField"/> will
	  ///        also be computed and stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  /// @deprecated (4.4) Index as a numeric field using <seealso cref="IntField"/> and then use <seealso cref="#getInts(AtomicReader, String, boolean)"/> instead. 
	  [Obsolete("(4.4) Index as a numeric field using <seealso cref="Lucene.Net.Document.IntField"/> and then use <seealso cref="#getInts(Lucene.Net.Index.AtomicReader, String, boolean)"/> instead.")]
	  FieldCache_Bytes GetBytes(AtomicReader reader, string field, FieldCache_ByteParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is
	  /// found, reads the terms in <code>field</code> as shorts and returns an array
	  /// of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the shorts. </param>
	  /// <param name="setDocsWithField">  If true then <seealso cref="#getDocsWithField"/> will
	  ///        also be computed and stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  /// @deprecated (4.4) Index as a numeric field using <seealso cref="IntField"/> and then use <seealso cref="#getInts(AtomicReader, String, boolean)"/> instead. 
	  [Obsolete("(4.4) Index as a numeric field using <seealso cref="Lucene.Net.Document.IntField"/> and then use <seealso cref="#getInts(Lucene.Net.Index.AtomicReader, String, boolean)"/> instead.")]
	  FieldCache_Shorts GetShorts(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is found,
	  /// reads the terms in <code>field</code> as shorts and returns an array of
	  /// size <code>reader.maxDoc()</code> of the value each document has in the
	  /// given field. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the shorts. </param>
	  /// <param name="parser">  Computes short for string values. </param>
	  /// <param name="setDocsWithField">  If true then <seealso cref="#getDocsWithField"/> will
	  ///        also be computed and stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  /// @deprecated (4.4) Index as a numeric field using <seealso cref="IntField"/> and then use <seealso cref="#getInts(AtomicReader, String, boolean)"/> instead. 
	  [Obsolete("(4.4) Index as a numeric field using <seealso cref="Lucene.Net.Document.IntField"/> and then use <seealso cref="#getInts(Lucene.Net.Index.AtomicReader, String, boolean)"/> instead.")]
	  FieldCache_Shorts GetShorts(AtomicReader reader, string field, FieldCache_ShortParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Returns an <seealso cref="Ints"/> over the values found in documents in the given
	  /// field.
	  /// </summary>
	  /// <seealso cref= #getInts(AtomicReader, String, IntParser, boolean) </seealso>
	  FieldCache_Ints GetInts(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Returns an <seealso cref="Ints"/> over the values found in documents in the given
	  /// field. If the field was indexed as <seealso cref="NumericDocValuesField"/>, it simply
	  /// uses <seealso cref="AtomicReader#getNumericDocValues(String)"/> to read the values.
	  /// Otherwise, it checks the internal cache for an appropriate entry, and if
	  /// none is found, reads the terms in <code>field</code> as ints and returns
	  /// an array of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field.
	  /// </summary>
	  /// <param name="reader">
	  ///          Used to get field values. </param>
	  /// <param name="field">
	  ///          Which field contains the longs. </param>
	  /// <param name="parser">
	  ///          Computes int for string values. May be {@code null} if the
	  ///          requested field was indexed as <seealso cref="NumericDocValuesField"/> or
	  ///          <seealso cref="IntField"/>. </param>
	  /// <param name="setDocsWithField">
	  ///          If true then <seealso cref="#getDocsWithField"/> will also be computed and
	  ///          stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">
	  ///           If any error occurs. </exception>
	  FieldCache_Ints GetInts(AtomicReader reader, string field, FieldCache_IntParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Floats"/> over the values found in documents in the given
	  /// field.
	  /// </summary>
	  /// <seealso cref= #getFloats(AtomicReader, String, FloatParser, boolean) </seealso>
	  FieldCache_Floats GetFloats(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Floats"/> over the values found in documents in the given
	  /// field. If the field was indexed as <seealso cref="NumericDocValuesField"/>, it simply
	  /// uses <seealso cref="AtomicReader#getNumericDocValues(String)"/> to read the values.
	  /// Otherwise, it checks the internal cache for an appropriate entry, and if
	  /// none is found, reads the terms in <code>field</code> as floats and returns
	  /// an array of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field.
	  /// </summary>
	  /// <param name="reader">
	  ///          Used to get field values. </param>
	  /// <param name="field">
	  ///          Which field contains the floats. </param>
	  /// <param name="parser">
	  ///          Computes float for string values. May be {@code null} if the
	  ///          requested field was indexed as <seealso cref="NumericDocValuesField"/> or
	  ///          <seealso cref="FloatField"/>. </param>
	  /// <param name="setDocsWithField">
	  ///          If true then <seealso cref="#getDocsWithField"/> will also be computed and
	  ///          stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">
	  ///           If any error occurs. </exception>
	  FieldCache_Floats GetFloats(AtomicReader reader, string field, FieldCache_FloatParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Longs"/> over the values found in documents in the given
	  /// field.
	  /// </summary>
	  /// <seealso cref= #getLongs(AtomicReader, String, LongParser, boolean) </seealso>
	  FieldCache_Longs GetLongs(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Longs"/> over the values found in documents in the given
	  /// field. If the field was indexed as <seealso cref="NumericDocValuesField"/>, it simply
	  /// uses <seealso cref="AtomicReader#getNumericDocValues(String)"/> to read the values.
	  /// Otherwise, it checks the internal cache for an appropriate entry, and if
	  /// none is found, reads the terms in <code>field</code> as longs and returns
	  /// an array of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field.
	  /// </summary>
	  /// <param name="reader">
	  ///          Used to get field values. </param>
	  /// <param name="field">
	  ///          Which field contains the longs. </param>
	  /// <param name="parser">
	  ///          Computes long for string values. May be {@code null} if the
	  ///          requested field was indexed as <seealso cref="NumericDocValuesField"/> or
	  ///          <seealso cref="LongField"/>. </param>
	  /// <param name="setDocsWithField">
	  ///          If true then <seealso cref="#getDocsWithField"/> will also be computed and
	  ///          stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">
	  ///           If any error occurs. </exception>
	  FieldCache_Longs GetLongs(AtomicReader reader, string field, FieldCache_LongParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Doubles"/> over the values found in documents in the given
	  /// field.
	  /// </summary>
	  /// <seealso cref= #getDoubles(AtomicReader, String, DoubleParser, boolean) </seealso>
	  FieldCache_Doubles GetDoubles(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Returns a <seealso cref="Doubles"/> over the values found in documents in the given
	  /// field. If the field was indexed as <seealso cref="NumericDocValuesField"/>, it simply
	  /// uses <seealso cref="AtomicReader#getNumericDocValues(String)"/> to read the values.
	  /// Otherwise, it checks the internal cache for an appropriate entry, and if
	  /// none is found, reads the terms in <code>field</code> as doubles and returns
	  /// an array of size <code>reader.maxDoc()</code> of the value each document
	  /// has in the given field.
	  /// </summary>
	  /// <param name="reader">
	  ///          Used to get field values. </param>
	  /// <param name="field">
	  ///          Which field contains the longs. </param>
	  /// <param name="parser">
	  ///          Computes double for string values. May be {@code null} if the
	  ///          requested field was indexed as <seealso cref="NumericDocValuesField"/> or
	  ///          <seealso cref="DoubleField"/>. </param>
	  /// <param name="setDocsWithField">
	  ///          If true then <seealso cref="#getDocsWithField"/> will also be computed and
	  ///          stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">
	  ///           If any error occurs. </exception>
	  FieldCache_Doubles GetDoubles(AtomicReader reader, string field, FieldCache_DoubleParser parser, bool setDocsWithField);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none
	  /// is found, reads the term values in <code>field</code>
	  /// and returns a <seealso cref="BinaryDocValues"/> instance, providing a
	  /// method to retrieve the term (as a BytesRef) per document. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the strings. </param>
	  /// <param name="setDocsWithField">  If true then <seealso cref="#getDocsWithField"/> will
	  ///        also be computed and stored in the FieldCache. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField);

	  /// <summary>
	  /// Expert: just like <seealso cref="#getTerms(AtomicReader,String,boolean)"/>,
	  ///  but you can specify whether more RAM should be consumed in exchange for
	  ///  faster lookups (default is "true").  Note that the
	  ///  first call for a given reader and field "wins",
	  ///  subsequent calls will share the same cache entry. 
	  /// </summary>
	  BinaryDocValues GetTerms(AtomicReader reader, string field, bool setDocsWithField, float acceptableOverheadRatio);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none
	  /// is found, reads the term values in <code>field</code>
	  /// and returns a <seealso cref="SortedDocValues"/> instance,
	  /// providing methods to retrieve sort ordinals and terms
	  /// (as a ByteRef) per document. </summary>
	  /// <param name="reader">  Used to get field values. </param>
	  /// <param name="field">   Which field contains the strings. </param>
	  /// <returns> The values in the given field for each document. </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  SortedDocValues GetTermsIndex(AtomicReader reader, string field);

	  /// <summary>
	  /// Expert: just like {@link
	  ///  #getTermsIndex(AtomicReader,String)}, but you can specify
	  ///  whether more RAM should be consumed in exchange for
	  ///  faster lookups (default is "true").  Note that the
	  ///  first call for a given reader and field "wins",
	  ///  subsequent calls will share the same cache entry. 
	  /// </summary>
	  SortedDocValues GetTermsIndex(AtomicReader reader, string field, float acceptableOverheadRatio);

	  /// <summary>
	  /// Checks the internal cache for an appropriate entry, and if none is found, reads the term values
	  /// in <code>field</code> and returns a <seealso cref="DocTermOrds"/> instance, providing a method to retrieve
	  /// the terms (as ords) per document.
	  /// </summary>
	  /// <param name="reader">  Used to build a <seealso cref="DocTermOrds"/> instance </param>
	  /// <param name="field">   Which field contains the strings. </param>
	  /// <returns> a <seealso cref="DocTermOrds"/> instance </returns>
	  /// <exception cref="IOException">  If any error occurs. </exception>
	  SortedSetDocValues GetDocTermOrds(AtomicReader reader, string field);

	  /// <summary>
	  /// EXPERT: A unique Identifier/Description for each item in the FieldCache. 
	  /// Can be useful for logging/debugging.
	  /// @lucene.experimental
	  /// </summary>

	  /// <summary>
	  /// EXPERT: Generates an array of CacheEntry objects representing all items 
	  /// currently in the FieldCache.
	  /// <p>
	  /// NOTE: These CacheEntry objects maintain a strong reference to the 
	  /// Cached Values.  Maintaining references to a CacheEntry the AtomicIndexReader 
	  /// associated with it has garbage collected will prevent the Value itself
	  /// from being garbage collected when the Cache drops the WeakReference.
	  /// </p>
	  /// @lucene.experimental
	  /// </summary>
	  FieldCache_CacheEntry[] CacheEntries {get;}

	  /// <summary>
	  /// <p>
	  /// EXPERT: Instructs the FieldCache to forcibly expunge all entries 
	  /// from the underlying caches.  this is intended only to be used for 
	  /// test methods as a way to ensure a known base state of the Cache 
	  /// (with out needing to rely on GC to free WeakReferences).  
	  /// It should not be relied on for "Cache maintenance" in general 
	  /// application code.
	  /// </p>
	  /// @lucene.experimental
	  /// </summary>
	  void PurgeAllCaches();

	  /// <summary>
	  /// Expert: drops all cache entries associated with this
	  /// reader <seealso cref="IndexReader#getCoreCacheKey"/>.  NOTE: this cache key must
	  /// precisely match the reader that the cache entry is
	  /// keyed on. If you pass a top-level reader, it usually
	  /// will have no effect as Lucene now caches at the segment
	  /// reader level.
	  /// </summary>
	  void PurgeByCacheKey(object coreCacheKey);

	  /// <summary>
	  /// If non-null, FieldCacheImpl will warn whenever
	  /// entries are created that are not sane according to
	  /// <seealso cref="Lucene.Net.Util.FieldCacheSanityChecker"/>.
	  /// </summary>
	  PrintStream InfoStream {set;get;}

	}

	public static class FieldCache_Fields
	{
	  public static readonly FieldCache DEFAULT = new FieldCacheImpl();

	  private class FieldCache_ByteParserAnonymousInnerClassHelper : FieldCache_ByteParser
	  {
		  public FieldCache_ByteParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual sbyte ParseByte(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // IntField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToByte(term.Utf8ToString());
		  }
		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_BYTE_PARSER";
		}
		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}
	  }

	  private class FieldCache_ShortParserAnonymousInnerClassHelper : FieldCache_ShortParser
	  {
		  public FieldCache_ShortParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual short ParseShort(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // IntField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToInt16(term.Utf8ToString());
		  }
		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_SHORT_PARSER";
		}

		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}
	  }

	  private class FieldCache_IntParserAnonymousInnerClassHelper : FieldCache_IntParser
	  {
		  public FieldCache_IntParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int ParseInt(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // IntField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToInt32(term.Utf8ToString());
		  }

		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}

		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_INT_PARSER";
		}
	  }

	  private class FieldCache_FloatParserAnonymousInnerClassHelper : FieldCache_FloatParser
	  {
		  public FieldCache_FloatParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual float ParseFloat(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // FloatField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToSingle(term.Utf8ToString());
		  }

		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}

		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_FLOAT_PARSER";
		}
	  }

	  private class FieldCache_LongParserAnonymousInnerClassHelper : FieldCache_LongParser
	  {
		  public FieldCache_LongParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual long ParseLong(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // LongField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToInt64(term.Utf8ToString());
		  }

		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}

		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_LONG_PARSER";
		}
	  }

	  private class FieldCache_DoubleParserAnonymousInnerClassHelper : FieldCache_DoubleParser
	  {
		  public FieldCache_DoubleParserAnonymousInnerClassHelper()
		  {
		  }

		  public virtual double ParseDouble(BytesRef term)
		  {
		  // TODO: would be far better to directly parse from
		  // UTF8 bytes... but really users should use
		  // DoubleField, instead, which already decodes
		  // directly from byte[]
		  return Convert.ToDouble(term.Utf8ToString());
		  }

		public virtual TermsEnum TermsEnum(Terms terms)
		{
		  return terms.Iterator(null);
		}

		public override string ToString()
		{
		  return typeof(FieldCache).Name + ".DEFAULT_DOUBLE_PARSER";
		}
	  }

	  private class FieldCache_IntParserAnonymousInnerClassHelper2 : FieldCache_IntParser
	  {
		  public FieldCache_IntParserAnonymousInnerClassHelper2()
		  {
		  }

		  public override int ParseInt(BytesRef term)
		  {
			return NumericUtils.PrefixCodedToInt(term);
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return NumericUtils.FilterPrefixCodedInts(terms.Iterator(null));
		  }

		  public override string ToString()
		  {
			return typeof(FieldCache).Name + ".NUMERIC_UTILS_INT_PARSER";
		  }
	  }

	  private class FieldCache_FloatParserAnonymousInnerClassHelper2 : FieldCache_FloatParser
	  {
		  public FieldCache_FloatParserAnonymousInnerClassHelper2()
		  {
		  }

		  public override float ParseFloat(BytesRef term)
		  {
			return NumericUtils.SortableIntToFloat(NumericUtils.PrefixCodedToInt(term));
		  }
		  public override string ToString()
		  {
			return typeof(FieldCache).Name + ".NUMERIC_UTILS_FLOAT_PARSER";
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return NumericUtils.FilterPrefixCodedInts(terms.Iterator(null));
		  }
	  }

	  private class FieldCache_LongParserAnonymousInnerClassHelper2 : FieldCache_LongParser
	  {
		  public FieldCache_LongParserAnonymousInnerClassHelper2()
		  {
		  }

		  public override long ParseLong(BytesRef term)
		  {
			return NumericUtils.PrefixCodedToLong(term);
		  }
		  public override string ToString()
		  {
			return typeof(FieldCache).Name + ".NUMERIC_UTILS_LONG_PARSER";
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return NumericUtils.FilterPrefixCodedLongs(terms.Iterator(null));
		  }
	  }

	  private class FieldCache_DoubleParserAnonymousInnerClassHelper2 : FieldCache_DoubleParser
	  {
		  public FieldCache_DoubleParserAnonymousInnerClassHelper2()
		  {
		  }

		  public override double ParseDouble(BytesRef term)
		  {
			return NumericUtils.SortableLongToDouble(NumericUtils.PrefixCodedToLong(term));
		  }
		  public override string ToString()
		  {
			return typeof(FieldCache).Name + ".NUMERIC_UTILS_DOUBLE_PARSER";
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return NumericUtils.FilterPrefixCodedLongs(terms.Iterator(null));
		  }
	  }
	}

	  public abstract class FieldCache_Bytes
	  {
	/// <summary>
	/// Return a single Byte representation of this field's value. </summary>
	public abstract sbyte Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Bytes EMPTY = new FieldCache_BytesAnonymousInnerClassHelper();

	private class FieldCache_BytesAnonymousInnerClassHelper : FieldCache_Bytes
	{
		public FieldCache_BytesAnonymousInnerClassHelper()
		{
		}

	  public override sbyte Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public abstract class FieldCache_Shorts
	  {
	/// <summary>
	/// Return a short representation of this field's value. </summary>
	public abstract short Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Shorts EMPTY = new FieldCache_ShortsAnonymousInnerClassHelper();

	private class FieldCache_ShortsAnonymousInnerClassHelper : FieldCache_Shorts
	{
		public FieldCache_ShortsAnonymousInnerClassHelper()
		{
		}

	  public override short Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public abstract class FieldCache_Ints
	  {
	/// <summary>
	/// Return an integer representation of this field's value. </summary>
	public abstract int Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Ints EMPTY = new FieldCache_IntsAnonymousInnerClassHelper();

	private class FieldCache_IntsAnonymousInnerClassHelper : FieldCache_Ints
	{
		public FieldCache_IntsAnonymousInnerClassHelper()
		{
		}

	  public override int Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public abstract class FieldCache_Longs
	  {
	/// <summary>
	/// Return an long representation of this field's value. </summary>
	public abstract long Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Longs EMPTY = new FieldCache_LongsAnonymousInnerClassHelper();

	private class FieldCache_LongsAnonymousInnerClassHelper : FieldCache_Longs
	{
		public FieldCache_LongsAnonymousInnerClassHelper()
		{
		}

	  public override long Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public abstract class FieldCache_Floats
	  {
	/// <summary>
	/// Return an float representation of this field's value. </summary>
	public abstract float Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Floats EMPTY = new FieldCache_FloatsAnonymousInnerClassHelper();

	private class FieldCache_FloatsAnonymousInnerClassHelper : FieldCache_Floats
	{
		public FieldCache_FloatsAnonymousInnerClassHelper()
		{
		}

	  public override float Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public abstract class FieldCache_Doubles
	  {
	/// <summary>
	/// Return an double representation of this field's value. </summary>
	public abstract double Get(int docID);

	/// <summary>
	/// Zero value for every document </summary>
	public static readonly FieldCache_Doubles EMPTY = new FieldCache_DoublesAnonymousInnerClassHelper();

	private class FieldCache_DoublesAnonymousInnerClassHelper : FieldCache_Doubles
	{
		public FieldCache_DoublesAnonymousInnerClassHelper()
		{
		}

	  public override double Get(int docID)
	  {
		return 0;
	  }
	}
	  }

	  public sealed class FieldCache_CreationPlaceholder
	  {
	internal object Value;
	  }

	  public interface FieldCache_Parser
	  {

	/// <summary>
	/// Pulls a <seealso cref="TermsEnum"/> from the given <seealso cref="Terms"/>. this method allows certain parsers
	/// to filter the actual TermsEnum before the field cache is filled.
	/// </summary>
	/// <param name="terms"> the <seealso cref="Terms"/> instance to create the <seealso cref="TermsEnum"/> from. </param>
	/// <returns> a possibly filtered <seealso cref="TermsEnum"/> instance, this method must not return <code>null</code>. </returns>
	/// <exception cref="IOException"> if an <seealso cref="IOException"/> occurs </exception>
	TermsEnum TermsEnum(Terms terms);
	  }

	  [Obsolete]
	  public interface FieldCache_ByteParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return a single Byte representation of this field's value. </summary>
	sbyte ParseByte(BytesRef term);
	  }

	  [Obsolete]
	  public interface FieldCache_ShortParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return a short representation of this field's value. </summary>
	short ParseShort(BytesRef term);
	  }

	  public interface FieldCache_IntParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return an integer representation of this field's value. </summary>
	int ParseInt(BytesRef term);
	  }

	  public interface FieldCache_FloatParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return an float representation of this field's value. </summary>
	float ParseFloat(BytesRef term);
	  }

	  public interface FieldCache_LongParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return an long representation of this field's value. </summary>
	long ParseLong(BytesRef term);
	  }

	  public interface FieldCache_DoubleParser : FieldCache_Parser
	  {
	/// <summary>
	/// Return an double representation of this field's value. </summary>
	double ParseDouble(BytesRef term);
	  }

	  public sealed class FieldCache_CacheEntry
	  {

	private readonly object ReaderKey_Renamed;
	private readonly string FieldName_Renamed;
	private readonly Type CacheType_Renamed;
	private readonly object Custom_Renamed;
	private readonly object Value_Renamed;
	private string Size;

	public FieldCache_CacheEntry(object readerKey, string fieldName, Type cacheType, object custom, object value)
	{
	  this.ReaderKey_Renamed = readerKey;
	  this.FieldName_Renamed = fieldName;
	  this.CacheType_Renamed = cacheType;
	  this.Custom_Renamed = custom;
	  this.Value_Renamed = value;
	}

	public object ReaderKey
	{
		get
		{
		  return ReaderKey_Renamed;
		}
	}

	public string FieldName
	{
		get
		{
		  return FieldName_Renamed;
		}
	}

	public Type CacheType
	{
		get
		{
		  return CacheType_Renamed;
		}
	}

	public object Custom
	{
		get
		{
		  return Custom_Renamed;
		}
	}

	public object Value
	{
		get
		{
		  return Value_Renamed;
		}
	}

	/// <summary>
	/// Computes (and stores) the estimated size of the cache Value </summary>
	/// <seealso cref= #getEstimatedSize </seealso>
	public void EstimateSize()
	{
	  long bytesUsed = RamUsageEstimator.SizeOf(Value);
	  Size = RamUsageEstimator.HumanReadableUnits(bytesUsed);
	}

	/// <summary>
	/// The most recently estimated size of the value, null unless 
	/// estimateSize has been called.
	/// </summary>
	public string EstimatedSize
	{
		get
		{
		  return Size;
		}
	}

	public override string ToString()
	{
	  StringBuilder b = new StringBuilder();
	  b.Append("'").Append(ReaderKey).Append("'=>");
	  b.Append("'").Append(FieldName).Append("',");
	  b.Append(CacheType).Append(",").Append(Custom);
	  b.Append("=>").Append(Value.GetType().Name).Append("#");
	  b.Append(System.identityHashCode(Value));

	  string s = EstimatedSize;
	  if (null != s)
	  {
		b.Append(" (size =~ ").Append(s).Append(')');
	  }

	  return b.ToString();
	}
	  }

}