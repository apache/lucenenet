/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{
	
	/// <summary> Expert: Maintains caches of term values.
	/// 
	/// <p>Created: May 19, 2004 11:13:14 AM
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: FieldCache.java 544546 2007-06-05 16:29:35Z doronc $
	/// </version>
	/// <summary>Expert: Stores term text values and document ordering data. </summary>
	public class StringIndex
	{
		
		/// <summary>All the term values, in natural order. </summary>
		public System.String[] Lookup;
		
		/// <summary>For each document, an index into the lookup array. </summary>
		public int[] Order;
		
		/// <summary>Creates one of these objects </summary>
		public StringIndex(int[] values, System.String[] lookup)
		{
			this.Order = values;
			this.Lookup = lookup;
		}
	}
	public struct FieldCache_Fields{
		/// <summary>Indicator for StringIndex values in the cache. </summary>
		// NOTE: the value assigned to this constant must not be
		// the same as any of those in SortField!!
		public readonly static int STRING_INDEX = - 1;
		/// <summary>Expert: The cache used internally by sorting and range query classes. </summary>
		public readonly static FieldCache DEFAULT;
		static FieldCache_Fields()
		{
			DEFAULT = new FieldCacheImpl();
		}
	}
	public interface FieldCache
	{
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is
		/// found, reads the terms in <code>field</code> as a single byte and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the single byte values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		byte[] GetBytes(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is found,
		/// reads the terms in <code>field</code> as bytes and returns an array of
		/// size <code>reader.maxDoc()</code> of the value each document has in the
		/// given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the bytes.
		/// </param>
		/// <param name="parser"> Computes byte for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		byte[] GetBytes(IndexReader reader, System.String field, ByteParser parser);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is
		/// found, reads the terms in <code>field</code> as shorts and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the shorts.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		short[] GetShorts(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is found,
		/// reads the terms in <code>field</code> as shorts and returns an array of
		/// size <code>reader.maxDoc()</code> of the value each document has in the
		/// given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the shorts.
		/// </param>
		/// <param name="parser"> Computes short for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		short[] GetShorts(IndexReader reader, System.String field, ShortParser parser);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is
		/// found, reads the terms in <code>field</code> as integers and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the integers.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		int[] GetInts(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none is found,
		/// reads the terms in <code>field</code> as integers and returns an array of
		/// size <code>reader.maxDoc()</code> of the value each document has in the
		/// given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the integers.
		/// </param>
		/// <param name="parser"> Computes integer for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		int[] GetInts(IndexReader reader, System.String field, IntParser parser);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if
		/// none is found, reads the terms in <code>field</code> as floats and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the floats.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		float[] GetFloats(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if
		/// none is found, reads the terms in <code>field</code> as floats and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the floats.
		/// </param>
		/// <param name="parser"> Computes float for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		float[] GetFloats(IndexReader reader, System.String field, FloatParser parser);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none
		/// is found, reads the term values in <code>field</code> and returns an array
		/// of size <code>reader.maxDoc()</code> containing the value each document
		/// has in the given field.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the strings.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		System.String[] GetStrings(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none
		/// is found reads the term values in <code>field</code> and returns
		/// an array of them in natural order, along with an array telling
		/// which element in the term array each document uses.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the strings.
		/// </param>
		/// <returns> Array of terms and index into the array for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		StringIndex GetStringIndex(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if
		/// none is found reads <code>field</code> to see if it contains integers, floats
		/// or strings, and then calls one of the other methods in this class to get the
		/// values.  For string values, a StringIndex is returned.  After
		/// calling this method, there is an entry in the cache for both
		/// type <code>AUTO</code> and the actual found type.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the values.
		/// </param>
		/// <returns> int[], float[] or StringIndex.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		System.Object GetAuto(IndexReader reader, System.String field);
		
		/// <summary>Checks the internal cache for an appropriate entry, and if none
		/// is found reads the terms out of <code>field</code> and calls the given SortComparator
		/// to get the sort values.  A hit in the cache will happen if <code>reader</code>,
		/// <code>field</code>, and <code>comparator</code> are the same (using <code>equals()</code>)
		/// as a previous call to this method.
		/// </summary>
		/// <param name="reader"> Used to get field values.
		/// </param>
		/// <param name="field">  Which field contains the values.
		/// </param>
		/// <param name="comparator">Used to convert terms into something to sort by.
		/// </param>
		/// <returns> Array of sort objects, one for each document.
		/// </returns>
		/// <throws>  IOException  If any error occurs. </throws>
		System.IComparable[] GetCustom(IndexReader reader, System.String field, SortComparator comparator);
	}
	
	/// <summary>Interface to parse bytes from document fields.</summary>
	/// <seealso cref="FieldCache.GetBytes(IndexReader, String, FieldCache.ByteParser)">
	/// </seealso>
	public interface ByteParser
	{
		/// <summary>Return a single Byte representation of this field's value. </summary>
		byte ParseByte(System.String string_Renamed);
	}
	
	/// <summary>Interface to parse shorts from document fields.</summary>
	/// <seealso cref="FieldCache.GetShorts(IndexReader, String, FieldCache.ShortParser)">
	/// </seealso>
	public interface ShortParser
	{
		/// <summary>Return a short representation of this field's value. </summary>
		short ParseShort(System.String string_Renamed);
	}
	
	/// <summary>Interface to parse ints from document fields.</summary>
	/// <seealso cref="FieldCache.GetInts(IndexReader, String, FieldCache.IntParser)">
	/// </seealso>
	public interface IntParser
	{
		/// <summary>Return an integer representation of this field's value. </summary>
		int ParseInt(System.String string_Renamed);
	}
	
	/// <summary>Interface to parse floats from document fields.</summary>
	/// <seealso cref="FieldCache.GetFloats(IndexReader, String, FieldCache.FloatParser)">
	/// </seealso>
	public interface FloatParser
	{
		/// <summary>Return an float representation of this field's value. </summary>
		float ParseFloat(System.String string_Renamed);
	}
}