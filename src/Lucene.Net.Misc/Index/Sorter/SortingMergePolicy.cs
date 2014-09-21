using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.index.sorter
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


	using Analyzer = org.apache.lucene.analysis.Analyzer; // javadocs
	using Sort = org.apache.lucene.search.Sort;
	using Directory = org.apache.lucene.store.Directory;
	using Bits = org.apache.lucene.util.Bits;
	using MonotonicAppendingLongBuffer = org.apache.lucene.util.packed.MonotonicAppendingLongBuffer;

	/// <summary>
	/// A <seealso cref="MergePolicy"/> that reorders documents according to a <seealso cref="Sort"/>
	///  before merging them. As a consequence, all segments resulting from a merge
	///  will be sorted while segments resulting from a flush will be in the order
	///  in which documents have been added.
	///  <para><b>NOTE</b>: Never use this policy if you rely on
	///  <seealso cref="IndexWriter#addDocuments(Iterable, Analyzer) IndexWriter.addDocuments"/>
	///  to have sequentially-assigned doc IDs, this policy will scatter doc IDs.
	/// </para>
	///  <para><b>NOTE</b>: This policy should only be used with idempotent {@code Sort}s 
	///  so that the order of segments is predictable. For example, using 
	///  <seealso cref="Sort#INDEXORDER"/> in reverse (which is not idempotent) will make 
	///  the order of documents in a segment depend on the number of times the segment 
	///  has been merged.
	///  @lucene.experimental 
	/// </para>
	/// </summary>
	public sealed class SortingMergePolicy : MergePolicy
	{

	  /// <summary>
	  /// Put in the <seealso cref="SegmentInfo#getDiagnostics() diagnostics"/> to denote that
	  /// this segment is sorted.
	  /// </summary>
	  public const string SORTER_ID_PROP = "sorter";

	  internal class SortingOneMerge : OneMerge
	  {
		  private readonly SortingMergePolicy outerInstance;


		internal IList<AtomicReader> unsortedReaders;
		internal Sorter.DocMap docMap;
		internal AtomicReader sortedView;

		internal SortingOneMerge(SortingMergePolicy outerInstance, IList<SegmentCommitInfo> segments) : base(segments)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public java.util.List<org.apache.lucene.index.AtomicReader> getMergeReaders() throws java.io.IOException
		public override IList<AtomicReader> MergeReaders
		{
			get
			{
			  if (unsortedReaders == null)
			  {
				unsortedReaders = base.MergeReaders;
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final org.apache.lucene.index.AtomicReader atomicView;
				AtomicReader atomicView;
				if (unsortedReaders.Count == 1)
				{
				  atomicView = unsortedReaders[0];
				}
				else
				{
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final org.apache.lucene.index.IndexReader multiReader = new org.apache.lucene.index.MultiReader(unsortedReaders.toArray(new org.apache.lucene.index.AtomicReader[unsortedReaders.size()]));
				  IndexReader multiReader = new MultiReader(unsortedReaders.ToArray());
				  atomicView = SlowCompositeReaderWrapper.wrap(multiReader);
				}
				docMap = outerInstance.sorter.sort(atomicView);
				sortedView = SortingAtomicReader.wrap(atomicView, docMap);
			  }
			  // a null doc map means that the readers are already sorted
			  return docMap == null ? unsortedReaders : Collections.singletonList(sortedView);
			}
		}

		public override SegmentCommitInfo Info
		{
			set
			{
			  IDictionary<string, string> diagnostics = value.info.Diagnostics;
			  diagnostics[SORTER_ID_PROP] = outerInstance.sorter.ID;
			  base.Info = value;
			}
		}

		internal virtual MonotonicAppendingLongBuffer getDeletes(IList<AtomicReader> readers)
		{
		  MonotonicAppendingLongBuffer deletes = new MonotonicAppendingLongBuffer();
		  int deleteCount = 0;
		  foreach (AtomicReader reader in readers)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = reader.maxDoc();
			int maxDoc = reader.maxDoc();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits liveDocs = reader.getLiveDocs();
			Bits liveDocs = reader.LiveDocs;
			for (int i = 0; i < maxDoc; ++i)
			{
			  if (liveDocs != null && !liveDocs.get(i))
			  {
				++deleteCount;
			  }
			  else
			  {
				deletes.add(deleteCount);
			  }
			}
		  }
		  deletes.freeze();
		  return deletes;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.MergePolicy.DocMap getDocMap(final org.apache.lucene.index.MergeState mergeState)
		public override MergePolicy.DocMap getDocMap(MergeState mergeState)
		{
		  if (unsortedReaders == null)
		  {
			throw new IllegalStateException();
		  }
		  if (docMap == null)
		  {
			return base.getDocMap(mergeState);
		  }
		  Debug.Assert(mergeState.docMaps.length == 1); // we returned a singleton reader
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.MonotonicAppendingLongBuffer deletes = getDeletes(unsortedReaders);
		  MonotonicAppendingLongBuffer deletes = getDeletes(unsortedReaders);
		  return new DocMapAnonymousInnerClassHelper(this, mergeState, deletes);
		}

		private class DocMapAnonymousInnerClassHelper : MergePolicy.DocMap
		{
			private readonly SortingOneMerge outerInstance;

			private MergeState mergeState;
			private MonotonicAppendingLongBuffer deletes;

			public DocMapAnonymousInnerClassHelper(SortingOneMerge outerInstance, MergeState mergeState, MonotonicAppendingLongBuffer deletes)
			{
				this.outerInstance = outerInstance;
				this.mergeState = mergeState;
				this.deletes = deletes;
			}

			public override int map(int old)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int oldWithDeletes = old + (int) deletes.get(old);
			  int oldWithDeletes = old + (int) deletes.get(old);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newWithDeletes = docMap.oldToNew(oldWithDeletes);
			  int newWithDeletes = outerInstance.docMap.oldToNew(oldWithDeletes);
			  return mergeState.docMaps[0].get(newWithDeletes);
			}
		}

	  }

	  internal class SortingMergeSpecification : MergeSpecification
	  {
		  private readonly SortingMergePolicy outerInstance;

		  public SortingMergeSpecification(SortingMergePolicy outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }


		public override void add(OneMerge merge)
		{
		  base.add(new SortingOneMerge(outerInstance, merge.segments));
		}

		public override string segString(Directory dir)
		{
		  return "SortingMergeSpec(" + base.segString(dir) + ", sorter=" + outerInstance.sorter + ")";
		}

	  }

	  /// <summary>
	  /// Returns {@code true} if the given {@code reader} is sorted by the specified {@code sort}. </summary>
	  public static bool isSorted(AtomicReader reader, Sort sort)
	  {
		if (reader is SegmentReader)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.SegmentReader segReader = (org.apache.lucene.index.SegmentReader) reader;
		  SegmentReader segReader = (SegmentReader) reader;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, String> diagnostics = segReader.getSegmentInfo().info.getDiagnostics();
		  IDictionary<string, string> diagnostics = segReader.SegmentInfo.info.Diagnostics;
		  if (diagnostics != null && sort.ToString().Equals(diagnostics[SORTER_ID_PROP]))
		  {
			return true;
		  }
		}
		return false;
	  }

	  private MergeSpecification sortedMergeSpecification(MergeSpecification specification)
	  {
		if (specification == null)
		{
		  return null;
		}
		MergeSpecification sortingSpec = new SortingMergeSpecification(this);
		foreach (OneMerge merge in specification.merges)
		{
		  sortingSpec.add(merge);
		}
		return sortingSpec;
	  }

	  internal readonly MergePolicy @in;
	  internal readonly Sorter sorter;
	  internal readonly Sort sort;

	  /// <summary>
	  /// Create a new {@code MergePolicy} that sorts documents with the given {@code sort}. </summary>
	  public SortingMergePolicy(MergePolicy @in, Sort sort)
	  {
		this.@in = @in;
		this.sorter = new Sorter(sort);
		this.sort = sort;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public MergeSpecification findMerges(org.apache.lucene.index.MergeTrigger mergeTrigger, org.apache.lucene.index.SegmentInfos segmentInfos) throws java.io.IOException
	  public override MergeSpecification findMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
	  {
		return sortedMergeSpecification(@in.findMerges(mergeTrigger, segmentInfos));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public MergeSpecification findForcedMerges(org.apache.lucene.index.SegmentInfos segmentInfos, int maxSegmentCount, java.util.Map<org.apache.lucene.index.SegmentCommitInfo,Boolean> segmentsToMerge) throws java.io.IOException
	  public override MergeSpecification findForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
	  {
		return sortedMergeSpecification(@in.findForcedMerges(segmentInfos, maxSegmentCount, segmentsToMerge));
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public MergeSpecification findForcedDeletesMerges(org.apache.lucene.index.SegmentInfos segmentInfos) throws java.io.IOException
	  public override MergeSpecification findForcedDeletesMerges(SegmentInfos segmentInfos)
	  {
		return sortedMergeSpecification(@in.findForcedDeletesMerges(segmentInfos));
	  }

	  public override MergePolicy clone()
	  {
		return new SortingMergePolicy(@in.clone(), sort);
	  }

	  public override void close()
	  {
		@in.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean useCompoundFile(org.apache.lucene.index.SegmentInfos segments, org.apache.lucene.index.SegmentCommitInfo newSegment) throws java.io.IOException
	  public override bool useCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
	  {
		return @in.useCompoundFile(segments, newSegment);
	  }

	  public override IndexWriter IndexWriter
	  {
		  set
		  {
			@in.IndexWriter = value;
		  }
	  }

	  public override string ToString()
	  {
		return "SortingMergePolicy(" + @in + ", sorter=" + sorter + ")";
	  }

	}

}