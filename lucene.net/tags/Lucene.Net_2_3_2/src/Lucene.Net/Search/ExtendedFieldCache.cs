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
	
	
	/// <summary> 
	/// 
	/// 
	/// </summary>
	public struct ExtendedFieldCache_Fields{
		public readonly static ExtendedFieldCache EXT_DEFAULT;
		static ExtendedFieldCache_Fields()
		{
			EXT_DEFAULT = new ExtendedFieldCacheImpl();
		}
	}
	public interface ExtendedFieldCache : FieldCache
	{
		/// <summary> Checks the internal cache for an appropriate entry, and if none is
		/// found, reads the terms in <code>field</code> as longs and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// 
		/// </summary>
		/// <param name="reader">Used to get field values.
		/// </param>
		/// <param name="field"> Which field contains the longs.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  java.io.IOException If any error occurs. </throws>
		long[] GetLongs(IndexReader reader, System.String field);
		
		/// <summary> Checks the internal cache for an appropriate entry, and if none is found,
		/// reads the terms in <code>field</code> as longs and returns an array of
		/// size <code>reader.maxDoc()</code> of the value each document has in the
		/// given field.
		/// 
		/// </summary>
		/// <param name="reader">Used to get field values.
		/// </param>
		/// <param name="field"> Which field contains the longs.
		/// </param>
		/// <param name="parser">Computes integer for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException If any error occurs. </throws>
		long[] GetLongs(IndexReader reader, System.String field, LongParser parser);
		
		
		/// <summary> Checks the internal cache for an appropriate entry, and if none is
		/// found, reads the terms in <code>field</code> as integers and returns an array
		/// of size <code>reader.maxDoc()</code> of the value each document
		/// has in the given field.
		/// 
		/// </summary>
		/// <param name="reader">Used to get field values.
		/// </param>
		/// <param name="field"> Which field contains the doubles.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException If any error occurs. </throws>
		double[] GetDoubles(IndexReader reader, System.String field);
		
		/// <summary> Checks the internal cache for an appropriate entry, and if none is found,
		/// reads the terms in <code>field</code> as doubles and returns an array of
		/// size <code>reader.maxDoc()</code> of the value each document has in the
		/// given field.
		/// 
		/// </summary>
		/// <param name="reader">Used to get field values.
		/// </param>
		/// <param name="field"> Which field contains the doubles.
		/// </param>
		/// <param name="parser">Computes integer for string values.
		/// </param>
		/// <returns> The values in the given field for each document.
		/// </returns>
		/// <throws>  IOException If any error occurs. </throws>
		double[] GetDoubles(IndexReader reader, System.String field, DoubleParser parser);
	}
	
	public interface LongParser
	{
		/// <summary> Return an long representation of this field's value.</summary>
		long ParseLong(System.String string_Renamed);
	}
	
	public interface DoubleParser
	{
		/// <summary> Return an long representation of this field's value.</summary>
		double ParseDouble(System.String string_Renamed);
	}
}