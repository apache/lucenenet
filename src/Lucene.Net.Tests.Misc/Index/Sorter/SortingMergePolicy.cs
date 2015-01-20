/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Index.Sorter;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Packed;
using Sharpen;

namespace Org.Apache.Lucene.Index.Sorter
{
	/// <summary>
	/// A
	/// <see cref="Org.Apache.Lucene.Index.MergePolicy">Org.Apache.Lucene.Index.MergePolicy
	/// 	</see>
	/// that reorders documents according to a
	/// <see cref="Org.Apache.Lucene.Search.Sort">Org.Apache.Lucene.Search.Sort</see>
	/// before merging them. As a consequence, all segments resulting from a merge
	/// will be sorted while segments resulting from a flush will be in the order
	/// in which documents have been added.
	/// <p><b>NOTE</b>: Never use this policy if you rely on
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter.AddDocuments(Sharpen.Iterable{T}, Org.Apache.Lucene.Analysis.Analyzer)
	/// 	">IndexWriter.addDocuments</see>
	/// to have sequentially-assigned doc IDs, this policy will scatter doc IDs.
	/// <p><b>NOTE</b>: This policy should only be used with idempotent
	/// <code>Sort</code>
	/// s
	/// so that the order of segments is predictable. For example, using
	/// <see cref="Org.Apache.Lucene.Search.Sort.INDEXORDER">Org.Apache.Lucene.Search.Sort.INDEXORDER
	/// 	</see>
	/// in reverse (which is not idempotent) will make
	/// the order of documents in a segment depend on the number of times the segment
	/// has been merged.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SortingMergePolicy : MergePolicy
	{
		/// <summary>
		/// Put in the
		/// <see cref="Org.Apache.Lucene.Index.SegmentInfo.GetDiagnostics()">diagnostics</see>
		/// to denote that
		/// this segment is sorted.
		/// </summary>
		public static readonly string SORTER_ID_PROP = "sorter";

		internal class SortingOneMerge : MergePolicy.OneMerge
		{
			internal IList<AtomicReader> unsortedReaders;

			internal Sorter.DocMap docMap;

			internal AtomicReader sortedView;

			public SortingOneMerge(SortingMergePolicy _enclosing, IList<SegmentCommitInfo> segments
				) : base(segments)
			{
				this._enclosing = _enclosing;
			}

			// javadocs
			/// <exception cref="System.IO.IOException"></exception>
			public override IList<AtomicReader> GetMergeReaders()
			{
				if (this.unsortedReaders == null)
				{
					this.unsortedReaders = base.GetMergeReaders();
					AtomicReader atomicView;
					if (this.unsortedReaders.Count == 1)
					{
						atomicView = this.unsortedReaders[0];
					}
					else
					{
						IndexReader multiReader = new MultiReader(Sharpen.Collections.ToArray(this.unsortedReaders
							, new AtomicReader[this.unsortedReaders.Count]));
						atomicView = SlowCompositeReaderWrapper.Wrap(multiReader);
					}
					this.docMap = this._enclosing.sorter.Sort(atomicView);
					this.sortedView = SortingAtomicReader.Wrap(atomicView, this.docMap);
				}
				// a null doc map means that the readers are already sorted
				return this.docMap == null ? this.unsortedReaders : Sharpen.Collections.SingletonList
					(this.sortedView);
			}

			public override void SetInfo(SegmentCommitInfo info)
			{
				IDictionary<string, string> diagnostics = info.info.GetDiagnostics();
				diagnostics.Put(SortingMergePolicy.SORTER_ID_PROP, this._enclosing.sorter.GetID()
					);
				base.SetInfo(info);
			}

			private MonotonicAppendingLongBuffer GetDeletes(IList<AtomicReader> readers)
			{
				MonotonicAppendingLongBuffer deletes = new MonotonicAppendingLongBuffer();
				int deleteCount = 0;
				foreach (AtomicReader reader in readers)
				{
					int maxDoc = reader.MaxDoc();
					Bits liveDocs = reader.GetLiveDocs();
					for (int i = 0; i < maxDoc; ++i)
					{
						if (liveDocs != null && !liveDocs.Get(i))
						{
							++deleteCount;
						}
						else
						{
							deletes.Add(deleteCount);
						}
					}
				}
				deletes.Freeze();
				return deletes;
			}

			public override MergePolicy.DocMap GetDocMap(MergeState mergeState)
			{
				if (this.unsortedReaders == null)
				{
					throw new InvalidOperationException();
				}
				if (this.docMap == null)
				{
					return base.GetDocMap(mergeState);
				}
				//HM:revisit
				//assert mergeState.docMaps.length == 1; // we returned a singleton reader
				MonotonicAppendingLongBuffer deletes = this.GetDeletes(this.unsortedReaders);
				return new _DocMap_128(this, deletes, mergeState);
			}

			private sealed class _DocMap_128 : MergePolicy.DocMap
			{
				public _DocMap_128(SortingOneMerge _enclosing, MonotonicAppendingLongBuffer deletes
					, MergeState mergeState)
				{
					this._enclosing = _enclosing;
					this.deletes = deletes;
					this.mergeState = mergeState;
				}

				public override int Map(int old)
				{
					int oldWithDeletes = old + (int)deletes.Get(old);
					int newWithDeletes = this._enclosing.docMap.OldToNew(oldWithDeletes);
					return mergeState.docMaps[0].Get(newWithDeletes);
				}

				private readonly SortingOneMerge _enclosing;

				private readonly MonotonicAppendingLongBuffer deletes;

				private readonly MergeState mergeState;
			}

			private readonly SortingMergePolicy _enclosing;
		}

		internal class SortingMergeSpecification : MergePolicy.MergeSpecification
		{
			public override void Add(MergePolicy.OneMerge merge)
			{
				base.Add(new SortingMergePolicy.SortingOneMerge(this, merge.segments));
			}

			public override string SegString(Directory dir)
			{
				return "SortingMergeSpec(" + base.SegString(dir) + ", sorter=" + this._enclosing.
					sorter + ")";
			}

			internal SortingMergeSpecification(SortingMergePolicy _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly SortingMergePolicy _enclosing;
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if the given
		/// <code>reader</code>
		/// is sorted by the specified
		/// <code>sort</code>
		/// .
		/// </summary>
		public static bool IsSorted(AtomicReader reader, Sort sort)
		{
			if (reader is SegmentReader)
			{
				SegmentReader segReader = (SegmentReader)reader;
				IDictionary<string, string> diagnostics = segReader.GetSegmentInfo().info.GetDiagnostics
					();
				if (diagnostics != null && sort.ToString().Equals(diagnostics.Get(SORTER_ID_PROP)
					))
				{
					return true;
				}
			}
			return false;
		}

		private MergePolicy.MergeSpecification SortedMergeSpecification(MergePolicy.MergeSpecification
			 specification)
		{
			if (specification == null)
			{
				return null;
			}
			MergePolicy.MergeSpecification sortingSpec = new SortingMergePolicy.SortingMergeSpecification
				(this);
			foreach (MergePolicy.OneMerge merge in specification.merges)
			{
				sortingSpec.Add(merge);
			}
			return sortingSpec;
		}

		internal readonly MergePolicy @in;

		internal readonly Org.Apache.Lucene.Index.Sorter.Sorter sorter;

		internal readonly Sort sort;

		/// <summary>
		/// Create a new
		/// <code>MergePolicy</code>
		/// that sorts documents with the given
		/// <code>sort</code>
		/// .
		/// </summary>
		public SortingMergePolicy(MergePolicy @in, Sort sort)
		{
			this.@in = @in;
			this.sorter = new Org.Apache.Lucene.Index.Sorter.Sorter(sort);
			this.sort = sort;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MergePolicy.MergeSpecification FindMerges(MergeTrigger mergeTrigger
			, SegmentInfos segmentInfos)
		{
			return SortedMergeSpecification(@in.FindMerges(mergeTrigger, segmentInfos));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MergePolicy.MergeSpecification FindForcedMerges(SegmentInfos segmentInfos
			, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
		{
			return SortedMergeSpecification(@in.FindForcedMerges(segmentInfos, maxSegmentCount
				, segmentsToMerge));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MergePolicy.MergeSpecification FindForcedDeletesMerges(SegmentInfos
			 segmentInfos)
		{
			return SortedMergeSpecification(@in.FindForcedDeletesMerges(segmentInfos));
		}

		public override MergePolicy Clone()
		{
			return new SortingMergePolicy(@in.Clone(), sort);
		}

		public override void Close()
		{
			@in.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment
			)
		{
			return @in.UseCompoundFile(segments, newSegment);
		}

		public override void SetIndexWriter(IndexWriter writer)
		{
			@in.SetIndexWriter(writer);
		}

		public override string ToString()
		{
			return "SortingMergePolicy(" + @in + ", sorter=" + sorter + ")";
		}
	}
}
