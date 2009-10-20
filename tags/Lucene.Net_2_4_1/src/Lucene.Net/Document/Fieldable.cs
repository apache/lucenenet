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

using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Documents
{
	
	/// <summary>
    /// Synonymous with {@link Field}.
    /// <p><bold>WARNING</bold>: This interface may change within minor versions, despite Lucene's backward compatibility requirements.
    /// This means new methods may be added from version to version.  This change only affects the Fieldable API; other backwards
    /// compatibility promises remain intact. For example, Lucene can still
    /// read and write indices created within the same major version.
    /// </p>
 	/// </summary>
	public interface Fieldable
	{
		/// <summary>Sets the boost factor hits on this field.  This value will be
		/// multiplied into the score of all hits on this this field of this
		/// document.
		/// 
		/// <p>The boost is multiplied by {@link Lucene.Net.Documents.Document#GetBoost()} of the document
		/// containing this field.  If a document has multiple fields with the same
		/// name, all such values are multiplied together.  This product is then
		/// multipled by the value {@link Lucene.Net.Search.Similarity#LengthNorm(String,int)}, and
		/// rounded by {@link Lucene.Net.Search.Similarity#EncodeNorm(float)} before it is stored in the
		/// index.  One should attempt to ensure that this product does not overflow
		/// the range of that encoding.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Documents.Document.SetBoost(float)">
		/// </seealso>
		/// <seealso cref="Lucene.Net.Search.Similarity.LengthNorm(String, int)">
		/// </seealso>
		/// <seealso cref="Lucene.Net.Search.Similarity.EncodeNorm(float)">
		/// </seealso>
		void  SetBoost(float boost);
		
		/// <summary>Returns the boost factor for hits for this field.
		/// 
		/// <p>The default value is 1.0.
		/// 
		/// <p>Note: this value is not stored directly with the document in the index.
		/// Documents returned from {@link Lucene.Net.Index.IndexReader#Document(int)} and
		/// {@link Lucene.Net.Search.Hits#Doc(int)} may thus not have the same value present as when
		/// this field was indexed.
		/// 
		/// </summary>
		/// <seealso cref="#SetBoost(float)">
		/// </seealso>
		float GetBoost();
		
		/// <summary>Returns the name of the field as an interned string.
		/// For example "date", "title", "body", ...
		/// </summary>
		System.String Name();
		
		/// <summary>The value of the field as a String, or null.  If null, the Reader value,
		/// binary value, or TokenStream value is used.  Exactly one of stringValue(), 
		/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
		/// </summary>
		System.String StringValue();
		
		/// <summary>The value of the field as a Reader, or null.  If null, the String value,
		/// binary value, or TokenStream value is used.  Exactly one of stringValue(), 
		/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
		/// </summary>
		System.IO.TextReader ReaderValue();
		
		/// <summary>The value of the field in Binary, or null.  If null, the Reader value,
		/// String value, or TokenStream value is used. Exactly one of stringValue(), 
		/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
		/// </summary>
		byte[] BinaryValue();
		
		/// <summary>The value of the field as a TokenStream, or null.  If null, the Reader value,
		/// String value, or binary value is used. Exactly one of stringValue(), 
		/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
		/// </summary>
		TokenStream TokenStreamValue();
		
		/// <summary>True iff the value of the field is to be stored in the index for return
		/// with search hits.  It is an error for this to be true if a field is
		/// Reader-valued. 
		/// </summary>
		bool IsStored();
		
		/// <summary>True iff the value of the field is to be indexed, so that it may be
		/// searched on. 
		/// </summary>
		bool IsIndexed();
		
		/// <summary>True iff the value of the field should be tokenized as text prior to
		/// indexing.  Un-tokenized fields are indexed as a single word and may not be
		/// Reader-valued. 
		/// </summary>
		bool IsTokenized();
		
		/// <summary>True if the value of the field is stored and compressed within the index </summary>
		bool IsCompressed();
		
		/// <summary>True iff the term or terms used to index this field are stored as a term
		/// vector, available from {@link Lucene.Net.Index.IndexReader#GetTermFreqVector(int,String)}.
		/// These methods do not provide access to the original content of the field,
		/// only to terms used to index it. If the original content must be
		/// preserved, use the <code>stored</code> attribute instead.
		/// 
		/// </summary>
		/// <seealso cref="Lucene.Net.Index.IndexReader.GetTermFreqVector(int, String)">
		/// </seealso>
		bool IsTermVectorStored();
		
		/// <summary> True iff terms are stored as term vector together with their offsets 
		/// (start and end positon in source text).
		/// </summary>
		bool IsStoreOffsetWithTermVector();
		
		/// <summary> True iff terms are stored as term vector together with their token positions.</summary>
		bool IsStorePositionWithTermVector();
		
		/// <summary>True iff the value of the filed is stored as binary </summary>
		bool IsBinary();
		
		/// <summary>True if norms are omitted for this indexed field </summary>
		bool GetOmitNorms();
		
		/// <summary>Expert:
		/// 
		/// If set, omit normalization factors associated with this indexed field.
		/// This effectively disables indexing boosts and length normalization for this field.
		/// </summary>
		void  SetOmitNorms(bool omitNorms);

        /// <summary>
        /// Expert:
        /// If set, omit term freq, positions, and payloads from postings for this field for this field.
        /// </summary>
        void SetOmitTf(bool omitTf);

        /// <summary>
        /// True if tf is omitted for this indexed field.
        /// </summary>
        /// <returns></returns>
        bool GetOmitTf();

		/// <summary> Indicates whether a Field is Lazy or not.  The semantics of Lazy loading are such that if a Field is lazily loaded, retrieving
		/// it's values via {@link #StringValue()} or {@link #BinaryValue()} is only valid as long as the {@link Lucene.Net.Index.IndexReader} that
		/// retrieved the {@link Document} is still open.
		/// 
		/// </summary>
		/// <returns> true if this field can be loaded lazily
		/// </returns>
		bool IsLazy();

        /// <summary>
        /// Returns the offset into the byte[] segment that is used as value.  If Fields is not binary returned value is undefined.
        /// </summary>
        /// <returns>index of the first byte in segment that represents this Field value</returns>
        int GetBinaryOffset();

        /// <summary>
        /// Returns the of byte][ segment that is used as value.  If Fields is not binarythe returned value is undefined.
        /// </summary>
        /// <returns>length of byte[] segment that represents this Field value</returns>
        int GetBinaryLength();

        /// <summary>
        /// Return the raw byte[] for the vinary field.  Note that you must also call GetBinaryLength() and GetBinaryOffset()
        /// to know which range of bytes in the returned array belong to this Field.
        /// </summary>
        /// <returns>refererence to the Field value as byte</returns>
        byte[] GetBinaryValue();

        /// <summary>
        /// Return the raw byte[] for the vinary field.  Note that you must also call GetBinaryLength() and GetBinaryOffset()
        /// to know which range of bytes in the returned array belong to this Field.
        /// About reuse: if you pass in the result byte[] and it is used, it is likely the underlying implementation
        /// will hold onto this byte[] and return it in future calls to BinaryValue() of GetBinaryValue().
        /// So if you subsequently re-use the same byte[] elsewhere it will alter this Fieldable's value.
        /// </summary>
        /// <param name="result">user defined buffer that will be used if non-null and large enough to contain the Field value</param>
        /// <returns></returns>
        byte[] GetBinaryValue(byte[] result);
    }
}