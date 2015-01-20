/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Index.Sorter;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Index.Sorter
{
	/// <summary>
	/// A
	/// <see cref="Org.Apache.Lucene.Search.Collector">Org.Apache.Lucene.Search.Collector
	/// 	</see>
	/// that early terminates collection of documents on a
	/// per-segment basis, if the segment was sorted according to the given
	/// <see cref="Org.Apache.Lucene.Search.Sort">Org.Apache.Lucene.Search.Sort</see>
	/// .
	/// <p>
	/// <b>NOTE:</b> the
	/// <code>Collector</code>
	/// detects sorted segments according to
	/// <see cref="SortingMergePolicy">SortingMergePolicy</see>
	/// , so it's best used in conjunction with it. Also,
	/// it collects up to a specified
	/// <code>numDocsToCollect</code>
	/// from each segment,
	/// and therefore is mostly suitable for use in conjunction with collectors such as
	/// <see cref="Org.Apache.Lucene.Search.TopDocsCollector{T}">Org.Apache.Lucene.Search.TopDocsCollector&lt;T&gt;
	/// 	</see>
	/// , and not e.g.
	/// <see cref="Org.Apache.Lucene.Search.TotalHitCountCollector">Org.Apache.Lucene.Search.TotalHitCountCollector
	/// 	</see>
	/// .
	/// <p>
	/// <b>NOTE</b>: If you wrap a
	/// <code>TopDocsCollector</code>
	/// that sorts in the same
	/// order as the index order, the returned
	/// <see cref="Org.Apache.Lucene.Search.TopDocsCollector{T}.TopDocs()">TopDocs</see>
	/// will be correct. However the total of
	/// <see cref="Org.Apache.Lucene.Search.TopDocsCollector{T}.GetTotalHits()">hit count
	/// 	</see>
	/// will be underestimated since not all matching documents will have
	/// been collected.
	/// <p>
	/// <b>NOTE</b>: This
	/// <code>Collector</code>
	/// uses
	/// <see cref="Org.Apache.Lucene.Search.Sort.ToString()">Org.Apache.Lucene.Search.Sort.ToString()
	/// 	</see>
	/// to detect
	/// whether a segment was sorted with the same
	/// <code>Sort</code>
	/// . This has
	/// two implications:
	/// <ul>
	/// <li>if a custom comparator is not implemented correctly and returns
	/// different identifiers for equivalent instances, this collector will not
	/// detect sorted segments,</li>
	/// <li>if you suddenly change the
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter">Org.Apache.Lucene.Index.IndexWriter
	/// 	</see>
	/// 's
	/// <code>SortingMergePolicy</code>
	/// to sort according to another criterion and if both
	/// the old and the new
	/// <code>Sort</code>
	/// s have the same identifier, this
	/// <code>Collector</code>
	/// will incorrectly detect sorted segments.</li>
	/// </ul>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class EarlyTerminatingSortingCollector : Collector
	{
		/// <summary>The wrapped Collector</summary>
		protected internal readonly Collector @in;

		/// <summary>Sort used to sort the search results</summary>
		protected internal readonly Sort sort;

		/// <summary>Number of documents to collect in each segment</summary>
		protected internal readonly int numDocsToCollect;

		/// <summary>Number of documents to collect in the current segment being processed</summary>
		protected internal int segmentTotalCollect;

		/// <summary>
		/// True if the current segment being processed is sorted by
		/// <see cref="sort">sort</see>
		/// 
		/// </summary>
		protected internal bool segmentSorted;

		private int numCollected;

		/// <summary>
		/// Create a new
		/// <see cref="EarlyTerminatingSortingCollector">EarlyTerminatingSortingCollector</see>
		/// instance.
		/// </summary>
		/// <param name="in">the collector to wrap</param>
		/// <param name="sort">the sort you are sorting the search results on</param>
		/// <param name="numDocsToCollect">
		/// the number of documents to collect on each segment. When wrapping
		/// a
		/// <see cref="Org.Apache.Lucene.Search.TopDocsCollector{T}">Org.Apache.Lucene.Search.TopDocsCollector&lt;T&gt;
		/// 	</see>
		/// , this number should be the number of
		/// hits.
		/// </param>
		public EarlyTerminatingSortingCollector(Collector @in, Sort sort, int numDocsToCollect
			)
		{
			if (numDocsToCollect <= 0)
			{
				throw new InvalidOperationException("numDocsToCollect must always be > 0, got " +
					 segmentTotalCollect);
			}
			this.@in = @in;
			this.sort = sort;
			this.numDocsToCollect = numDocsToCollect;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			@in.SetScorer(scorer);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			@in.Collect(doc);
			if (++numCollected >= segmentTotalCollect)
			{
				throw new CollectionTerminatedException();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			@in.SetNextReader(context);
			segmentSorted = SortingMergePolicy.IsSorted(((AtomicReader)context.Reader()), sort
				);
			segmentTotalCollect = segmentSorted ? numDocsToCollect : int.MaxValue;
			numCollected = 0;
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return !segmentSorted && @in.AcceptsDocsOutOfOrder();
		}
	}
}
