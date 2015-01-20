/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Search.CachingWrapperFilter">Lucene.Net.Search.CachingWrapperFilter
	/// 	</see>
	/// that caches sets using a
	/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
	/// 	</see>
	/// ,
	/// as required for joins.
	/// </summary>
	public sealed class FixedBitSetCachingWrapperFilter : CachingWrapperFilter
	{
		/// <summary>
		/// Sole constructor, see
		/// <see cref="Lucene.Net.Search.CachingWrapperFilter.CachingWrapperFilter(Lucene.Net.Search.Filter)
		/// 	">Lucene.Net.Search.CachingWrapperFilter.CachingWrapperFilter(Lucene.Net.Search.Filter)
		/// 	</see>
		/// .
		/// </summary>
		public FixedBitSetCachingWrapperFilter(Filter filter) : base(filter)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override DocIdSet DocIdSetToCache(DocIdSet docIdSet, AtomicReader reader
			)
		{
			if (docIdSet == null)
			{
				return EMPTY_DOCIDSET;
			}
			else
			{
				if (docIdSet is FixedBitSet)
				{
					// this is different from CachingWrapperFilter: even when the DocIdSet is
					// cacheable, we convert it to a FixedBitSet since we require all the
					// cached filters to be FixedBitSets
					return docIdSet;
				}
				else
				{
					DocIdSetIterator it = docIdSet.Iterator();
					if (it == null)
					{
						return EMPTY_DOCIDSET;
					}
					else
					{
						FixedBitSet copy = new FixedBitSet(reader.MaxDoc());
						copy.Or(it);
						return copy;
					}
				}
			}
		}
	}
}
