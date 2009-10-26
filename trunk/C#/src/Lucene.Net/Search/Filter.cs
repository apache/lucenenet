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
using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;

namespace Lucene.Net.Search
{
	
	/// <summary>Abstract base class providing a mechanism to use a subset of an index
	/// for restriction or permission of index search results.
	/// <p>
	/// <b>Note:</b> In Lucene 3.0 {@link #Bits(IndexReader)} will be removed
	/// and {@link #GetDocIdSet(IndexReader)} will be defined as abstract.
	/// All implementing classes must therefore implement {@link #GetDocIdSet(IndexReader)}
	/// in order to work with Lucene 3.0.
	/// </summary>
	[Serializable]
	public abstract class Filter
	{
		/// <returns> A BitSet with true for documents which should be permitted in
		/// search results, and false for those that should not.
		/// </returns>
		/// <deprecated> Use {@link #GetDocIdSet(IndexReader)} instead.
		/// </deprecated>
		public virtual System.Collections.BitArray Bits(IndexReader reader)
		{
			throw new System.NotSupportedException();
		}
		
		/// <returns> a DocIdSet that provides the documents which should be permitted or
		/// prohibited in search results. <b>NOTE:</b> null can be returned if
		/// no documents will be accepted by this Filter.
		/// 
		/// </returns>
		/// <seealso cref="DocIdBitSet">
		/// </seealso>
		public virtual DocIdSet GetDocIdSet(IndexReader reader)
		{
			return new DocIdBitSet(Bits(reader));
		}
	}
}