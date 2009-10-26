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
	
	/// <summary>
    /// Abstract base class providing a mechanism to limit index search results
    /// to a subset of an index.
	/// <para>
    /// Note: In Lucene 3.0, Bits(IndexReader) will be removed and GetDocIdSet(IndexReader)
    /// will be made abstract.  All implementin classes must therefore implement
    /// GetDocIdSet(IndexReader) in order to work with Lucene 3.0.
    /// </para>
	/// </summary>
	[Serializable]
	public abstract class Filter
	{
		/// <summary>
        /// Returns a BitSet with true for documents which should be permitted in
		/// search results, and false for those that should not. 
		/// </summary>
		[System.Obsolete("Use GetDocIdSet(IndexReader) instead.")]
        public abstract System.Collections.BitArray Bits(IndexReader reader);

        /// <summary>
        /// Return a DocIdSet that provides the documents which are permitted
        /// or prohibited in search results.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <see cref="DocIdBitSet"/>
        public virtual DocIdSet GetDocIdSet(IndexReader reader)
        {
            return new DocIdBitSet(Bits(reader));
        }
	}
}