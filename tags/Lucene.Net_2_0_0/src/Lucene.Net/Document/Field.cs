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
using Hits = Lucene.Net.Search.Hits;
using Similarity = Lucene.Net.Search.Similarity;
using Parameter = Lucene.Net.Util.Parameter;

namespace Lucene.Net.Documents
{
	
	/// <summary>A field is a section of a Document.  Each field has two parts, a name and a
	/// value.  Values may be free text, provided as a String or as a Reader, or they
	/// may be atomic keywords, which are not further processed.  Such keywords may
	/// be used to represent dates, urls, etc.  Fields are optionally stored in the
	/// index, so that they may be returned with hits on the document.
	/// </summary>
	
	[Serializable]
	public sealed class Field
	{
		private System.String name = "body";
		
		// the one and only data object for all different kind of field values
		private System.Object fieldsData = null;
		
		private bool storeTermVector = false;
		private bool storeOffsetWithTermVector = false;
		private bool storePositionWithTermVector = false;
		private bool omitNorms = false;
		private bool isStored = false;
		private bool isIndexed = true;
		private bool isTokenized = true;
		private bool isBinary = false;
		private bool isCompressed = false;
		
		private float boost = 1.0f;
		
        /// <summary>Specifies whether and how a field should be stored. </summary>
        [Serializable]
		public sealed class Store : Parameter
		{
			
			internal Store(System.String name) : base(name)
			{
			}
			
			/// <summary>Store the original field value in the index in a compressed form. This is
			/// useful for long documents and for binary valued fields.
			/// </summary>
			public static readonly Store COMPRESS = new Store("COMPRESS");
			
			/// <summary>Store the original field value in the index. This is useful for short texts
			/// like a document's title which should be displayed with the results. The
			/// value is stored in its original form, i.e. no analyzer is used before it is
			/// stored. 
			/// </summary>
			public static readonly Store YES = new Store("YES");
			
			/// <summary>Do not store the field value in the index. </summary>
			public static readonly Store NO = new Store("NO");
		}
		
        /// <summary>Specifies whether and how a field should be indexed. </summary>
        [Serializable]
		public sealed class Index : Parameter
		{
			
			internal Index(System.String name) : base(name)
			{
			}
			
			/// <summary>Do not index the field value. This field can thus not be searched,
			/// but one can still access its contents provided it is 
			/// {@link Field.Store stored}. 
			/// </summary>
			public static readonly Index NO = new Index("NO");
			
			/// <summary>Index the field's value so it can be searched. An Analyzer will be used
			/// to tokenize and possibly further normalize the text before its
			/// terms will be stored in the index. This is useful for common text.
			/// </summary>
			public static readonly Index TOKENIZED = new Index("TOKENIZED");
			
			/// <summary>Index the field's value without using an Analyzer, so it can be searched.
			/// As no analyzer is used the value will be stored as a single term. This is
			/// useful for unique Ids like product numbers.
			/// </summary>
			public static readonly Index UN_TOKENIZED = new Index("UN_TOKENIZED");
			
			/// <summary>Index the field's value without an Analyzer, and disable
			/// the storing of norms.  No norms means that index-time boosting
			/// and field length normalization will be disabled.  The benefit is
			/// less memory usage as norms take up one byte per indexed field
			/// for every document in the index.
			/// </summary>
			public static readonly Index NO_NORMS = new Index("NO_NORMS");
		}
		
        /// <summary>Specifies whether and how a field should have term vectors. </summary>
        [Serializable]
		public sealed class TermVector : Parameter
		{
			
			internal TermVector(System.String name) : base(name)
			{
			}
			
			/// <summary>Do not store term vectors. </summary>
			public static readonly TermVector NO = new TermVector("NO");
			
			/// <summary>Store the term vectors of each document. A term vector is a list
			/// of the document's terms and their number of occurences in that document. 
			/// </summary>
			public static readonly TermVector YES = new TermVector("YES");
			
			/// <summary> Store the term vector + token position information
			/// 
			/// </summary>
			/// <seealso cref="YES">
			/// </seealso>
			public static readonly TermVector WITH_POSITIONS = new TermVector("WITH_POSITIONS");
			
			/// <summary> Store the term vector + Token offset information
			/// 
			/// </summary>
			/// <seealso cref="YES">
			/// </seealso>
			public static readonly TermVector WITH_OFFSETS = new TermVector("WITH_OFFSETS");
			
			/// <summary> Store the term vector + Token position and offset information
			/// 
			/// </summary>
			/// <seealso cref="YES">
			/// </seealso>
			/// <seealso cref="WITH_POSITIONS">
			/// </seealso>
			/// <seealso cref="WITH_OFFSETS">
			/// </seealso>
			public static readonly TermVector WITH_POSITIONS_OFFSETS = new TermVector("WITH_POSITIONS_OFFSETS");
		}
		
		/// <summary>Sets the boost factor hits on this field.  This value will be
		/// multiplied into the score of all hits on this this field of this
		/// document.
		/// 
		/// <p>The boost is multiplied by {@link Document#GetBoost()} of the document
		/// containing this field.  If a document has multiple fields with the same
		/// name, all such values are multiplied together.  This product is then
		/// multipled by the value {@link Similarity#LengthNorm(String,int)}, and
		/// rounded by {@link Similarity#EncodeNorm(float)} before it is stored in the
		/// index.  One should attempt to ensure that this product does not overflow
		/// the range of that encoding.
		/// 
		/// </summary>
		/// <seealso cref="Document.SetBoost(float)">
		/// </seealso>
		/// <seealso cref="Similarity.LengthNorm(String, int)">
		/// </seealso>
		/// <seealso cref="Similarity.EncodeNorm(float)">
		/// </seealso>
		public void  SetBoost(float boost)
		{
			this.boost = boost;
		}
		
		/// <summary>Returns the boost factor for hits for this field.
		/// 
		/// <p>The default value is 1.0.
		/// 
		/// <p>Note: this value is not stored directly with the document in the index.
		/// Documents returned from {@link IndexReader#Document(int)} and
		/// {@link Hits#Doc(int)} may thus not have the same value present as when
		/// this field was indexed.
		/// 
		/// </summary>
		/// <seealso cref="SetBoost(float)">
		/// </seealso>
		public float GetBoost()
		{
			return boost;
		}
		
		/// <summary>Returns the name of the field as an interned string.
		/// For example "date", "title", "body", ...
		/// </summary>
		public System.String Name()
		{
			return name;
		}
		
		/// <summary>The value of the field as a String, or null.  If null, the Reader value
		/// or binary value is used.  Exactly one of stringValue(), readerValue(), and
		/// binaryValue() must be set. 
		/// </summary>
		public System.String StringValue()
		{
			return fieldsData is System.String ? (System.String) fieldsData : null;
		}
		
		/// <summary>The value of the field as a Reader, or null.  If null, the String value
		/// or binary value is  used.  Exactly one of stringValue(), readerValue(),
		/// and binaryValue() must be set. 
		/// </summary>
		public System.IO.TextReader ReaderValue()
		{
			return fieldsData is System.IO.TextReader ? (System.IO.TextReader) fieldsData : null;
		}
		
		/// <summary>The value of the field in Binary, or null.  If null, the Reader or
		/// String value is used.  Exactly one of stringValue(), readerValue() and
		/// binaryValue() must be set. 
		/// </summary>
		public byte[] BinaryValue()
		{
			return fieldsData is byte[] ? (byte[]) fieldsData : null;
		}
		
		/// <summary> Create a field by specifying its name, value and how it will
		/// be saved in the index. Term vectors will not be stored in the index.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="value">The string to process
		/// </param>
		/// <param name="store">Whether <code>value</code> should be stored in the index
		/// </param>
		/// <param name="index">Whether the field should be indexed, and if so, if it should
		/// be tokenized before indexing 
		/// </param>
		/// <throws>  NullPointerException if name or value is <code>null</code> </throws>
		/// <throws>  IllegalArgumentException if the field is neither stored nor indexed  </throws>
		public Field(System.String name, System.String value_Renamed, Store store, Index index) : this(name, value_Renamed, store, index, TermVector.NO)
		{
		}
		
		/// <summary> Create a field by specifying its name, value and how it will
		/// be saved in the index.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="value">The string to process
		/// </param>
		/// <param name="store">Whether <code>value</code> should be stored in the index
		/// </param>
		/// <param name="index">Whether the field should be indexed, and if so, if it should
		/// be tokenized before indexing 
		/// </param>
		/// <param name="termVector">Whether term vector should be stored
		/// </param>
		/// <throws>  NullPointerException if name or value is <code>null</code> </throws>
		/// <throws>  IllegalArgumentException in any of the following situations: </throws>
		/// <summary> <ul> 
		/// <li>the field is neither stored nor indexed</li> 
		/// <li>the field is not indexed but termVector is <code>TermVector.YES</code></li>
		/// </ul> 
		/// </summary>
		public Field(System.String name, System.String value_Renamed, Store store, Index index, TermVector termVector)
		{
			if (name == null)
				throw new System.NullReferenceException("name cannot be null");
			if (value_Renamed == null)
				throw new System.NullReferenceException("value cannot be null");
            if (name.Length == 0 && value_Renamed.Length == 0)
                throw new System.ArgumentException("name and value cannot both be empty");
            if (index == Index.NO && store == Store.NO)
				throw new System.ArgumentException("it doesn't make sense to have a field that " + "is neither indexed nor stored");
			if (index == Index.NO && termVector != TermVector.NO)
				throw new System.ArgumentException("cannot store term vector information " + "for a field that is not indexed");
			
			this.name = String.Intern(name); // field names are interned
			this.fieldsData = value_Renamed;
			
			if (store == Store.YES)
			{
				this.isStored = true;
				this.isCompressed = false;
			}
			else if (store == Store.COMPRESS)
			{
				this.isStored = true;
				this.isCompressed = true;
			}
			else if (store == Store.NO)
			{
				this.isStored = false;
				this.isCompressed = false;
			}
			else
			{
				throw new System.ArgumentException("unknown store parameter " + store);
			}
			
			if (index == Index.NO)
			{
				this.isIndexed = false;
				this.isTokenized = false;
			}
			else if (index == Index.TOKENIZED)
			{
				this.isIndexed = true;
				this.isTokenized = true;
			}
			else if (index == Index.UN_TOKENIZED)
			{
				this.isIndexed = true;
				this.isTokenized = false;
			}
			else if (index == Index.NO_NORMS)
			{
				this.isIndexed = true;
				this.isTokenized = false;
				this.omitNorms = true;
			}
			else
			{
				throw new System.ArgumentException("unknown index parameter " + index);
			}
			
			this.isBinary = false;
			
			SetStoreTermVector(termVector);
		}
		
		/// <summary> Create a tokenized and indexed field that is not stored. Term vectors will
		/// not be stored.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="reader">The reader with the content
		/// </param>
		/// <throws>  NullPointerException if name or reader is <code>null</code> </throws>
		public Field(System.String name, System.IO.TextReader reader) : this(name, reader, TermVector.NO)
		{
		}
		
		/// <summary> Create a tokenized and indexed field that is not stored, optionally with 
		/// storing term vectors.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="reader">The reader with the content
		/// </param>
		/// <param name="termVector">Whether term vector should be stored
		/// </param>
		/// <throws>  NullPointerException if name or reader is <code>null</code> </throws>
		public Field(System.String name, System.IO.TextReader reader, TermVector termVector)
		{
			if (name == null)
				throw new System.NullReferenceException("name cannot be null");
			if (reader == null)
				throw new System.NullReferenceException("reader cannot be null");
			
			this.name = String.Intern(name); // field names are interned
			this.fieldsData = reader;
			
			this.isStored = false;
			this.isCompressed = false;
			
			this.isIndexed = true;
			this.isTokenized = true;
			
			this.isBinary = false;
			
			SetStoreTermVector(termVector);
		}
		
		
		
        /// <summary> Create a stored field with binary value. Optionally the value may be compressed.
		/// 
		/// </summary>
		/// <param name="name">The name of the field
		/// </param>
		/// <param name="value">The binary value
		/// </param>
		/// <param name="store">How <code>value</code> should be stored (compressed or not.)
		/// </param>
        /// <throws>  IllegalArgumentException if store is <code>Store.NO</code>  </throws>
        public Field(System.String name, byte[] value_Renamed, Store store)
		{
			if (name == null)
				throw new System.ArgumentException("name cannot be null");
			if (value_Renamed == null)
				throw new System.ArgumentException("value cannot be null");
			
			this.name = String.Intern(name);
			this.fieldsData = value_Renamed;
			
			if (store == Store.YES)
			{
				this.isStored = true;
				this.isCompressed = false;
			}
			else if (store == Store.COMPRESS)
			{
				this.isStored = true;
				this.isCompressed = true;
			}
			else if (store == Store.NO)
				throw new System.ArgumentException("binary values can't be unstored");
			else
			{
				throw new System.ArgumentException("unknown store parameter " + store);
			}
			
			this.isIndexed = false;
			this.isTokenized = false;
			
			this.isBinary = true;
			
			SetStoreTermVector(TermVector.NO);
		}
		
		private void  SetStoreTermVector(TermVector termVector)
		{
			if (termVector == TermVector.NO)
			{
				this.storeTermVector = false;
				this.storePositionWithTermVector = false;
				this.storeOffsetWithTermVector = false;
			}
			else if (termVector == TermVector.YES)
			{
				this.storeTermVector = true;
				this.storePositionWithTermVector = false;
				this.storeOffsetWithTermVector = false;
			}
			else if (termVector == TermVector.WITH_POSITIONS)
			{
				this.storeTermVector = true;
				this.storePositionWithTermVector = true;
				this.storeOffsetWithTermVector = false;
			}
			else if (termVector == TermVector.WITH_OFFSETS)
			{
				this.storeTermVector = true;
				this.storePositionWithTermVector = false;
				this.storeOffsetWithTermVector = true;
			}
			else if (termVector == TermVector.WITH_POSITIONS_OFFSETS)
			{
				this.storeTermVector = true;
				this.storePositionWithTermVector = true;
				this.storeOffsetWithTermVector = true;
			}
			else
			{
				throw new System.ArgumentException("unknown termVector parameter " + termVector);
			}
		}
		
		/// <summary>True iff the value of the field is to be stored in the index for return
		/// with search hits.  It is an error for this to be true if a field is
		/// Reader-valued. 
		/// </summary>
		public bool IsStored()
		{
			return isStored;
		}
		
		/// <summary>True iff the value of the field is to be indexed, so that it may be
		/// searched on. 
		/// </summary>
		public bool IsIndexed()
		{
			return isIndexed;
		}
		
		/// <summary>True iff the value of the field should be tokenized as text prior to
		/// indexing.  Un-tokenized fields are indexed as a single word and may not be
		/// Reader-valued. 
		/// </summary>
		public bool IsTokenized()
		{
			return isTokenized;
		}
		
		/// <summary>True if the value of the field is stored and compressed within the index </summary>
		public bool IsCompressed()
		{
			return isCompressed;
		}
		
		/// <summary>True iff the term or terms used to index this field are stored as a term
		/// vector, available from {@link IndexReader#GetTermFreqVector(int,String)}.
		/// These methods do not provide access to the original content of the field,
		/// only to terms used to index it. If the original content must be
		/// preserved, use the <code>stored</code> attribute instead.
		/// 
		/// </summary>
		/// <seealso cref="IndexReader.GetTermFreqVector(int, String)">
		/// </seealso>
		public bool IsTermVectorStored()
		{
			return storeTermVector;
		}
		
		/// <summary> True iff terms are stored as term vector together with their offsets 
		/// (start and end positon in source text).
		/// </summary>
		public bool IsStoreOffsetWithTermVector()
		{
			return storeOffsetWithTermVector;
		}
		
		/// <summary> True iff terms are stored as term vector together with their token positions.</summary>
		public bool IsStorePositionWithTermVector()
		{
			return storePositionWithTermVector;
		}
		
		/// <summary>True iff the value of the filed is stored as binary </summary>
		public bool IsBinary()
		{
			return isBinary;
		}
		
		/// <summary>True if norms are omitted for this indexed field </summary>
		public bool GetOmitNorms()
		{
			return omitNorms;
		}
		
		/// <summary>Expert:
		/// 
		/// If set, omit normalization factors associated with this indexed field.
		/// This effectively disables indexing boosts and length normalization for this field.
		/// </summary>
		public void  SetOmitNorms(bool omitNorms)
		{
			this.omitNorms = omitNorms;
		}
		
		/// <summary>Prints a Field for human consumption. </summary>
		public override System.String ToString()
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			if (isStored)
			{
				result.Append("stored");
				if (isCompressed)
					result.Append("/compressed");
				else
					result.Append("/uncompressed");
			}
			if (isIndexed)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("indexed");
			}
			if (isTokenized)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("tokenized");
			}
			if (storeTermVector)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("termVector");
			}
			if (storeOffsetWithTermVector)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("termVectorOffsets");
			}
			if (storePositionWithTermVector)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("termVectorPosition");
			}
			if (isBinary)
			{
				if (result.Length > 0)
					result.Append(",");
				result.Append("binary");
			}
			if (omitNorms)
			{
				result.Append(",omitNorms");
			}
			result.Append('<');
			result.Append(name);
			result.Append(':');
			
			if (fieldsData != null)
			{
				result.Append(fieldsData);
			}
			
			result.Append('>');
			return result.ToString();
		}
	}
}