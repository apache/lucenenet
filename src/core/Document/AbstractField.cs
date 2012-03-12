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
using System.IO;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using StringHelper = Lucene.Net.Util.StringHelper;
using PhraseQuery = Lucene.Net.Search.PhraseQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;

namespace Lucene.Net.Documents
{
	/// <summary> 
	/// 
	/// 
	/// </summary>
	[Serializable]
	public abstract class AbstractField : IFieldable
	{
		
		protected internal System.String name = "body";
		protected internal bool storeTermVector = false;
		protected internal bool storeOffsetWithTermVector = false;
		protected internal bool storePositionWithTermVector = false;
		protected internal bool omitNorms = false;
		protected internal bool isStored = false;
		protected internal bool isIndexed = true;
		protected internal bool isTokenized = true;
		protected internal bool isBinary = false;
		protected internal bool lazy = false;
		protected internal bool omitTermFreqAndPositions = false;
		protected internal float boost = 1.0f;
		// the data object for all different kind of field values
		protected internal System.Object fieldsData = null;
		// pre-analyzed tokenStream for indexed fields
		protected internal TokenStream tokenStream;
		// length/offset for all primitive types
		protected internal int binaryLength;
		protected internal int binaryOffset;
		
		protected internal AbstractField()
		{
		}
		
		protected internal AbstractField(System.String name, Field.Store store, Field.Index index, Field.TermVector termVector)
		{
			if (name == null)
				throw new System.NullReferenceException("name cannot be null");
			this.name = StringHelper.Intern(name); // field names are interned

		    this.isStored = store.IsStored();
		    this.isIndexed = index.IsIndexed();
		    this.isTokenized = index.IsAnalyzed();
		    this.omitNorms = index.OmitNorms();
			
			this.isBinary = false;
			
			SetStoreTermVector(termVector);
		}

	    /// <summary>Returns the boost factor for hits for this field.
	    /// 
	    /// <p/>The default value is 1.0.
	    /// 
	    /// <p/>Note: this value is not stored directly with the document in the index.
	    /// Documents returned from <see cref="Lucene.Net.Index.IndexReader.Document(int)" /> and
	    /// <see cref="Lucene.Net.Search.Searcher.Doc(int)" /> may thus not have the same value present as when
	    /// this field was indexed.
	    /// 
	    /// </summary>
	    /// <seealso cref="SetBoost(float)">
	    /// </seealso>
	    public virtual float Boost
	    {
	        get { return boost; }
	        set { this.boost = value; }
	    }

	    /// <summary>Returns the name of the field as an interned string.
	    /// For example "date", "title", "body", ...
	    /// </summary>
	    public virtual string Name
	    {
	        get { return name; }
	    }

	    protected internal virtual void  SetStoreTermVector(Field.TermVector termVector)
		{
		    this.storeTermVector = termVector.IsStored();
		    this.storePositionWithTermVector = termVector.WithPositions();
		    this.storeOffsetWithTermVector = termVector.WithOffsets();
		}

	    /// <summary>True iff the value of the field is to be stored in the index for return
	    /// with search hits.  It is an error for this to be true if a field is
	    /// Reader-valued. 
	    /// </summary>
	    public bool IsStored
	    {
	        get { return isStored; }
	    }

	    /// <summary>True iff the value of the field is to be indexed, so that it may be
	    /// searched on. 
	    /// </summary>
	    public bool IsIndexed
	    {
	        get { return isIndexed; }
	    }

	    /// <summary>True iff the value of the field should be tokenized as text prior to
	    /// indexing.  Un-tokenized fields are indexed as a single word and may not be
	    /// Reader-valued. 
	    /// </summary>
	    public bool IsTokenized
	    {
	        get { return isTokenized; }
	    }

	    /// <summary>True iff the term or terms used to index this field are stored as a term
	    /// vector, available from <see cref="Lucene.Net.Index.IndexReader.GetTermFreqVector(int,String)" />.
	    /// These methods do not provide access to the original content of the field,
	    /// only to terms used to index it. If the original content must be
	    /// preserved, use the <c>stored</c> attribute instead.
	    /// 
	    /// </summary>
	    /// <seealso cref="Lucene.Net.Index.IndexReader.GetTermFreqVector(int, String)">
	    /// </seealso>
	    public bool IsTermVectorStored
	    {
	        get { return storeTermVector; }
	    }

	    /// <summary> True iff terms are stored as term vector together with their offsets 
	    /// (start and end position in source text).
	    /// </summary>
	    public virtual bool IsStoreOffsetWithTermVector
	    {
	        get { return storeOffsetWithTermVector; }
	    }

	    /// <summary> True iff terms are stored as term vector together with their token positions.</summary>
	    public virtual bool IsStorePositionWithTermVector
	    {
	        get { return storePositionWithTermVector; }
	    }

	    /// <summary>True iff the value of the filed is stored as binary </summary>
	    public bool IsBinary
	    {
	        get { return isBinary; }
	    }


	    /// <summary> Return the raw byte[] for the binary field.  Note that
	    /// you must also call <see cref="GetBinaryLength" /> and <see cref="GetBinaryOffset" />
	    /// to know which range of bytes in this
	    /// returned array belong to the field.
	    /// </summary>
	    /// <value> reference to the Field value as byte[]. </value>
	    public virtual byte[] BinaryValue
	    {
	        get { return GetBinaryValue(null); }
	    }

	    public virtual byte[] GetBinaryValue(byte[] result)
		{
			if (isBinary || fieldsData is byte[])
				return (byte[]) fieldsData;
			else
				return null;
		}

	    /// <summary> Returns length of byte[] segment that is used as value, if Field is not binary
	    /// returned value is undefined
	    /// </summary>
	    /// <value> length of byte[] segment that represents this Field value </value>
	    public virtual int BinaryLength
	    {
	        get
	        {
	            if (isBinary)
	            {
	                return binaryLength;
	            }
	            else if (fieldsData is byte[])
	                return ((byte[]) fieldsData).Length;
	            else
	                return 0;
	        }
	    }

	    /// <summary> Returns offset into byte[] segment that is used as value, if Field is not binary
	    /// returned value is undefined
	    /// </summary>
	    /// <value> index of the first character in byte[] segment that represents this Field value </value>
	    public virtual int BinaryOffset
	    {
	        get { return binaryOffset; }
	    }

	    /// <summary>True if norms are omitted for this indexed field </summary>
	    public virtual bool OmitNorms
	    {
	        get { return omitNorms; }
	        set { this.omitNorms = value; }
	    }

	    /// <summary>Expert:
	    /// 
	    /// If set, omit term freq, positions and payloads from
	    /// postings for this field.
	    /// 
	    /// <p/><b>NOTE</b>: While this option reduces storage space
	    /// required in the index, it also means any query
	    /// requiring positional information, such as <see cref="PhraseQuery" />
	    /// or <see cref="SpanQuery" /> subclasses will
	    /// silently fail to find results.
	    /// </summary>
	    public virtual bool OmitTermFreqAndPositions
	    {
	        set { this.omitTermFreqAndPositions = value; }
	        get { return omitTermFreqAndPositions; }
	    }

	    public virtual bool IsLazy
	    {
	        get { return lazy; }
	    }

	    /// <summary>Prints a Field for human consumption. </summary>
		public override System.String ToString()
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			if (isStored)
			{
				result.Append("stored");
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
			if (omitTermFreqAndPositions)
			{
				result.Append(",omitTermFreqAndPositions");
			}
			if (lazy)
			{
				result.Append(",lazy");
			}
			result.Append('<');
			result.Append(name);
			result.Append(':');
			
			if (fieldsData != null && lazy == false)
			{
				result.Append(fieldsData);
			}
			
			result.Append('>');
			return result.ToString();
		}

	    public abstract TokenStream TokenStreamValue { get; }
	    public abstract TextReader ReaderValue { get; }
	    public abstract string StringValue { get; }
	}
}