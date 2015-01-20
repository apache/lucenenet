/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>
	/// <p>
	/// Allows multiple
	/// <see cref="Org.Apache.Lucene.Search.Filter">Org.Apache.Lucene.Search.Filter</see>
	/// s to be chained.
	/// Logical operations such as <b>NOT</b> and <b>XOR</b>
	/// are applied between filters. One operation can be used
	/// for all filters, or a specific operation can be declared
	/// for each filter.
	/// </p>
	/// <p>
	/// Order in which filters are called depends on
	/// the position of the filter in the chain. It's probably
	/// more efficient to place the most restrictive filters
	/// /least computationally-intensive filters first.
	/// </p>
	/// </summary>
	public class ChainedFilter : Filter
	{
		public const int OR = 0;

		public const int AND = 1;

		public const int ANDNOT = 2;

		public const int XOR = 3;

		/// <summary>Logical operation when none is declared.</summary>
		/// <remarks>Logical operation when none is declared. Defaults to OR.</remarks>
		public const int DEFAULT = OR;

		/// <summary>The filter chain</summary>
		private Filter[] chain = null;

		private int[] logicArray;

		private int logic = -1;

		/// <summary>Ctor.</summary>
		/// <remarks>Ctor.</remarks>
		/// <param name="chain">The chain of filters</param>
		public ChainedFilter(Filter[] chain)
		{
			this.chain = chain;
		}

		/// <summary>Ctor.</summary>
		/// <remarks>Ctor.</remarks>
		/// <param name="chain">The chain of filters</param>
		/// <param name="logicArray">Logical operations to apply between filters</param>
		public ChainedFilter(Filter[] chain, int[] logicArray)
		{
			this.chain = chain;
			this.logicArray = logicArray;
		}

		/// <summary>Ctor.</summary>
		/// <remarks>Ctor.</remarks>
		/// <param name="chain">The chain of filters</param>
		/// <param name="logic">Logical operation to apply to ALL filters</param>
		public ChainedFilter(Filter[] chain, int logic)
		{
			this.chain = chain;
			this.logic = logic;
		}

		/// <summary>
		/// <see cref="Org.Apache.Lucene.Search.Filter.GetDocIdSet(Org.Apache.Lucene.Index.AtomicReaderContext, Org.Apache.Lucene.Util.Bits)
		/// 	">Org.Apache.Lucene.Search.Filter.GetDocIdSet(Org.Apache.Lucene.Index.AtomicReaderContext, Org.Apache.Lucene.Util.Bits)
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			int[] index = new int[1];
			// use array as reference to modifiable int;
			index[0] = 0;
			// an object attribute would not be thread safe.
			if (logic != -1)
			{
				return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, logic, index), acceptDocs);
			}
			else
			{
				if (logicArray != null)
				{
					return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, logicArray, index), acceptDocs
						);
				}
			}
			return BitsFilteredDocIdSet.Wrap(GetDocIdSet(context, DEFAULT, index), acceptDocs
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DocIdSetIterator GetDISI(Filter filter, AtomicReaderContext context)
		{
			// we dont pass acceptDocs, we will filter at the end using an additional filter
			DocIdSet docIdSet = filter.GetDocIdSet(context, null);
			if (docIdSet == null)
			{
				return DocIdSetIterator.Empty();
			}
			else
			{
				DocIdSetIterator iter = docIdSet.Iterator();
				if (iter == null)
				{
					return DocIdSetIterator.Empty();
				}
				else
				{
					return iter;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FixedBitSet InitialResult(AtomicReaderContext context, int logic, int[] index
			)
		{
			AtomicReader reader = ((AtomicReader)context.Reader());
			FixedBitSet result = new FixedBitSet(reader.MaxDoc());
			if (logic == AND)
			{
				result.Or(GetDISI(chain[index[0]], context));
				++index[0];
			}
			else
			{
				if (logic == ANDNOT)
				{
					result.Or(GetDISI(chain[index[0]], context));
					result.Flip(0, reader.MaxDoc());
					// NOTE: may set bits for deleted docs.
					++index[0];
				}
			}
			return result;
		}

		/// <summary>Delegates to each filter in the chain.</summary>
		/// <remarks>Delegates to each filter in the chain.</remarks>
		/// <param name="context">AtomicReaderContext</param>
		/// <param name="logic">Logical operation</param>
		/// <returns>DocIdSet</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private DocIdSet GetDocIdSet(AtomicReaderContext context, int logic, int[] index)
		{
			FixedBitSet result = InitialResult(context, logic, index);
			for (; index[0] < chain.Length; index[0]++)
			{
				// we dont pass acceptDocs, we will filter at the end using an additional filter
				DoChain(result, logic, chain[index[0]].GetDocIdSet(context, null));
			}
			return result;
		}

		/// <summary>Delegates to each filter in the chain.</summary>
		/// <remarks>Delegates to each filter in the chain.</remarks>
		/// <param name="context">AtomicReaderContext</param>
		/// <param name="logic">Logical operation</param>
		/// <returns>DocIdSet</returns>
		/// <exception cref="System.IO.IOException"></exception>
		private DocIdSet GetDocIdSet(AtomicReaderContext context, int[] logic, int[] index
			)
		{
			if (logic.Length != chain.Length)
			{
				throw new ArgumentException("Invalid number of elements in logic array");
			}
			FixedBitSet result = InitialResult(context, logic[0], index);
			for (; index[0] < chain.Length; index[0]++)
			{
				// we dont pass acceptDocs, we will filter at the end using an additional filter
				DoChain(result, logic[index[0]], chain[index[0]].GetDocIdSet(context, null));
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

		/// <exception cref="System.IO.IOException"></exception>
		private void DoChain(FixedBitSet result, int logic, DocIdSet dis)
		{
			if (dis is FixedBitSet)
			{
				switch (logic)
				{
					case OR:
					{
						// optimized case for FixedBitSets
						result.Or((FixedBitSet)dis);
						break;
					}

					case AND:
					{
						result.And((FixedBitSet)dis);
						break;
					}

					case ANDNOT:
					{
						result.AndNot((FixedBitSet)dis);
						break;
					}

					case XOR:
					{
						result.Xor((FixedBitSet)dis);
						break;
					}

					default:
					{
						DoChain(result, DEFAULT, dis);
						break;
						break;
					}
				}
			}
			else
			{
				DocIdSetIterator disi;
				if (dis == null)
				{
					disi = DocIdSetIterator.Empty();
				}
				else
				{
					disi = dis.Iterator();
					if (disi == null)
					{
						disi = DocIdSetIterator.Empty();
					}
				}
				switch (logic)
				{
					case OR:
					{
						result.Or(disi);
						break;
					}

					case AND:
					{
						result.And(disi);
						break;
					}

					case ANDNOT:
					{
						result.AndNot(disi);
						break;
					}

					case XOR:
					{
						result.Xor(disi);
						break;
					}

					default:
					{
						DoChain(result, DEFAULT, dis);
						break;
						break;
					}
				}
			}
		}
	}
}
