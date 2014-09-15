using System.Text;

namespace org.apache.lucene.queries
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

	using AtomicReader = org.apache.lucene.index.AtomicReader;
	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using BitsFilteredDocIdSet = org.apache.lucene.search.BitsFilteredDocIdSet;
	using DocIdSet = org.apache.lucene.search.DocIdSet;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using Filter = org.apache.lucene.search.Filter;
	using Bits = org.apache.lucene.util.Bits;
	using FixedBitSet = org.apache.lucene.util.FixedBitSet;

	/// <summary>
	/// <para>
	/// Allows multiple <seealso cref="Filter"/>s to be chained.
	/// Logical operations such as <b>NOT</b> and <b>XOR</b>
	/// are applied between filters. One operation can be used
	/// for all filters, or a specific operation can be declared
	/// for each filter.
	/// </para>
	/// <para>
	/// Order in which filters are called depends on
	/// the position of the filter in the chain. It's probably
	/// more efficient to place the most restrictive filters
	/// /least computationally-intensive filters first.
	/// </para>
	/// </summary>
	public class ChainedFilter : Filter
	{

	  public const int OR = 0;
	  public const int AND = 1;
	  public const int ANDNOT = 2;
	  public const int XOR = 3;
	  /// <summary>
	  /// Logical operation when none is declared. Defaults to OR.
	  /// </summary>
	  public const int DEFAULT = OR;

	  /// <summary>
	  /// The filter chain
	  /// </summary>
	  private Filter[] chain = null;

	  private int[] logicArray;

	  private int logic = -1;

	  /// <summary>
	  /// Ctor.
	  /// </summary>
	  /// <param name="chain"> The chain of filters </param>
	  public ChainedFilter(Filter[] chain)
	  {
		this.chain = chain;
	  }

	  /// <summary>
	  /// Ctor.
	  /// </summary>
	  /// <param name="chain"> The chain of filters </param>
	  /// <param name="logicArray"> Logical operations to apply between filters </param>
	  public ChainedFilter(Filter[] chain, int[] logicArray)
	  {
		this.chain = chain;
		this.logicArray = logicArray;
	  }

	  /// <summary>
	  /// Ctor.
	  /// </summary>
	  /// <param name="chain"> The chain of filters </param>
	  /// <param name="logic"> Logical operation to apply to ALL filters </param>
	  public ChainedFilter(Filter[] chain, int logic)
	  {
		this.chain = chain;
		this.logic = logic;
	  }

	  /// <summary>
	  /// <seealso cref="Filter#getDocIdSet"/>.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.DocIdSet getDocIdSet(org.apache.lucene.index.AtomicReaderContext context, org.apache.lucene.util.Bits acceptDocs) throws java.io.IOException
	  public override DocIdSet getDocIdSet(AtomicReaderContext context, Bits acceptDocs)
	  {
		int[] index = new int[1]; // use array as reference to modifiable int;
		index[0] = 0; // an object attribute would not be thread safe.
		if (logic != -1)
		{
		  return BitsFilteredDocIdSet.wrap(getDocIdSet(context, logic, index), acceptDocs);
		}
		else if (logicArray != null)
		{
		  return BitsFilteredDocIdSet.wrap(getDocIdSet(context, logicArray, index), acceptDocs);
		}

		return BitsFilteredDocIdSet.wrap(getDocIdSet(context, DEFAULT, index), acceptDocs);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.search.DocIdSetIterator getDISI(org.apache.lucene.search.Filter filter, org.apache.lucene.index.AtomicReaderContext context) throws java.io.IOException
	  private DocIdSetIterator getDISI(Filter filter, AtomicReaderContext context)
	  {
		// we dont pass acceptDocs, we will filter at the end using an additional filter
		DocIdSet docIdSet = filter.getDocIdSet(context, null);
		if (docIdSet == null)
		{
		  return DocIdSetIterator.empty();
		}
		else
		{
		  DocIdSetIterator iter = docIdSet.GetEnumerator();
		  if (iter == null)
		  {
			return DocIdSetIterator.empty();
		  }
		  else
		  {
			return iter;
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.util.FixedBitSet initialResult(org.apache.lucene.index.AtomicReaderContext context, int logic, int[] index) throws java.io.IOException
	  private FixedBitSet initialResult(AtomicReaderContext context, int logic, int[] index)
	  {
		AtomicReader reader = context.reader();
		FixedBitSet result = new FixedBitSet(reader.maxDoc());
		if (logic == AND)
		{
		  result.or(getDISI(chain[index[0]], context));
		  ++index[0];
		}
		else if (logic == ANDNOT)
		{
		  result.or(getDISI(chain[index[0]], context));
		  result.flip(0, reader.maxDoc()); // NOTE: may set bits for deleted docs.
		  ++index[0];
		}
		return result;
	  }

	  /// <summary>
	  /// Delegates to each filter in the chain.
	  /// </summary>
	  /// <param name="context"> AtomicReaderContext </param>
	  /// <param name="logic"> Logical operation </param>
	  /// <returns> DocIdSet </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.search.DocIdSet getDocIdSet(org.apache.lucene.index.AtomicReaderContext context, int logic, int[] index) throws java.io.IOException
	  private DocIdSet getDocIdSet(AtomicReaderContext context, int logic, int[] index)
	  {
		FixedBitSet result = initialResult(context, logic, index);
		for (; index[0] < chain.Length; index[0]++)
		{
		  // we dont pass acceptDocs, we will filter at the end using an additional filter
		  doChain(result, logic, chain[index[0]].getDocIdSet(context, null));
		}
		return result;
	  }

	  /// <summary>
	  /// Delegates to each filter in the chain.
	  /// </summary>
	  /// <param name="context"> AtomicReaderContext </param>
	  /// <param name="logic"> Logical operation </param>
	  /// <returns> DocIdSet </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.search.DocIdSet getDocIdSet(org.apache.lucene.index.AtomicReaderContext context, int[] logic, int[] index) throws java.io.IOException
	  private DocIdSet getDocIdSet(AtomicReaderContext context, int[] logic, int[] index)
	  {
		if (logic.Length != chain.Length)
		{
		  throw new System.ArgumentException("Invalid number of elements in logic array");
		}

		FixedBitSet result = initialResult(context, logic[0], index);
		for (; index[0] < chain.Length; index[0]++)
		{
		  // we dont pass acceptDocs, we will filter at the end using an additional filter
		  doChain(result, logic[index[0]], chain[index[0]].getDocIdSet(context, null));
		}
		return result;
	  }

	  public override string ToString()
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append("ChainedFilter: [");
		foreach (Filter aChain in chain)
		{
		  sb.Append(aChain);
		  sb.Append(' ');
		}
		sb.Append(']');
		return sb.ToString();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void doChain(org.apache.lucene.util.FixedBitSet result, int logic, org.apache.lucene.search.DocIdSet dis) throws java.io.IOException
	  private void doChain(FixedBitSet result, int logic, DocIdSet dis)
	  {
		if (dis is FixedBitSet)
		{
		  // optimized case for FixedBitSets
		  switch (logic)
		  {
			case OR:
			  result.or((FixedBitSet) dis);
			  break;
			case AND:
			  result.and((FixedBitSet) dis);
			  break;
			case ANDNOT:
			  result.andNot((FixedBitSet) dis);
			  break;
			case XOR:
			  result.xor((FixedBitSet) dis);
			  break;
			default:
			  doChain(result, DEFAULT, dis);
			  break;
		  }
		}
		else
		{
		  DocIdSetIterator disi;
		  if (dis == null)
		  {
			disi = DocIdSetIterator.empty();
		  }
		  else
		  {
			disi = dis.GetEnumerator();
			if (disi == null)
			{
			  disi = DocIdSetIterator.empty();
			}
		  }

		  switch (logic)
		  {
			case OR:
			  result.or(disi);
			  break;
			case AND:
			  result.and(disi);
			  break;
			case ANDNOT:
			  result.andNot(disi);
			  break;
			case XOR:
			  result.xor(disi);
			  break;
			default:
			  doChain(result, DEFAULT, dis);
			  break;
		  }
		}
	  }

	}

}