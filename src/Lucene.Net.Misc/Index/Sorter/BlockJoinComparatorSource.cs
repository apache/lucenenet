using System;

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

	using DocIdSet = org.apache.lucene.search.DocIdSet;
	using FieldComparator = org.apache.lucene.search.FieldComparator;
	using FieldComparatorSource = org.apache.lucene.search.FieldComparatorSource;
	using Filter = org.apache.lucene.search.Filter;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher; // javadocs
	using Query = org.apache.lucene.search.Query; // javadocs
	using ScoreDoc = org.apache.lucene.search.ScoreDoc; // javadocs
	using Scorer = org.apache.lucene.search.Scorer;
	using Sort = org.apache.lucene.search.Sort;
	using SortField = org.apache.lucene.search.SortField;
	using FixedBitSet = org.apache.lucene.util.FixedBitSet;

	/// <summary>
	/// Helper class to sort readers that contain blocks of documents.
	/// <para>
	/// Note that this class is intended to used with <seealso cref="SortingMergePolicy"/>,
	/// and for other purposes has some limitations:
	/// <ul>
	///    <li>Cannot yet be used with <seealso cref="IndexSearcher#searchAfter(ScoreDoc, Query, int, Sort) IndexSearcher.searchAfter"/>
	///    <li>Filling sort field values is not yet supported.
	/// </ul>
	/// @lucene.experimental
	/// </para>
	/// </summary>
	// TODO: can/should we clean this thing up (e.g. return a proper sort value)
	// and move to the join/ module?
	public class BlockJoinComparatorSource : FieldComparatorSource
	{
	  internal readonly Filter parentsFilter;
	  internal readonly Sort parentSort;
	  internal readonly Sort childSort;

	  /// <summary>
	  /// Create a new BlockJoinComparatorSource, sorting only blocks of documents
	  /// with {@code parentSort} and not reordering children with a block.
	  /// </summary>
	  /// <param name="parentsFilter"> Filter identifying parent documents </param>
	  /// <param name="parentSort"> Sort for parent documents </param>
	  public BlockJoinComparatorSource(Filter parentsFilter, Sort parentSort) : this(parentsFilter, parentSort, new Sort(SortField.FIELD_DOC))
	  {
	  }

	  /// <summary>
	  /// Create a new BlockJoinComparatorSource, specifying the sort order for both
	  /// blocks of documents and children within a block.
	  /// </summary>
	  /// <param name="parentsFilter"> Filter identifying parent documents </param>
	  /// <param name="parentSort"> Sort for parent documents </param>
	  /// <param name="childSort"> Sort for child documents in the same block </param>
	  public BlockJoinComparatorSource(Filter parentsFilter, Sort parentSort, Sort childSort)
	  {
		this.parentsFilter = parentsFilter;
		this.parentSort = parentSort;
		this.childSort = childSort;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.FieldComparator<Integer> newComparator(String fieldname, int numHits, int sortPos, boolean reversed) throws java.io.IOException
	  public override FieldComparator<int?> newComparator(string fieldname, int numHits, int sortPos, bool reversed)
	  {
		// we keep parallel slots: the parent ids and the child ids
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int parentSlots[] = new int[numHits];
		int[] parentSlots = new int[numHits];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int childSlots[] = new int[numHits];
		int[] childSlots = new int[numHits];

		SortField[] parentFields = parentSort.Sort;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int parentReverseMul[] = new int[parentFields.length];
		int[] parentReverseMul = new int[parentFields.Length];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldComparator<?> parentComparators[] = new org.apache.lucene.search.FieldComparator[parentFields.length];
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		FieldComparator<?>[] parentComparators = new FieldComparator[parentFields.Length];
		for (int i = 0; i < parentFields.Length; i++)
		{
		  parentReverseMul[i] = parentFields[i].Reverse ? - 1 : 1;
		  parentComparators[i] = parentFields[i].getComparator(1, i);
		}

		SortField[] childFields = childSort.Sort;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int childReverseMul[] = new int[childFields.length];
		int[] childReverseMul = new int[childFields.Length];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldComparator<?> childComparators[] = new org.apache.lucene.search.FieldComparator[childFields.length];
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		FieldComparator<?>[] childComparators = new FieldComparator[childFields.Length];
		for (int i = 0; i < childFields.Length; i++)
		{
		  childReverseMul[i] = childFields[i].Reverse ? - 1 : 1;
		  childComparators[i] = childFields[i].getComparator(1, i);
		}

		// NOTE: we could return parent ID as value but really our sort "value" is more complex...
		// So we throw UOE for now. At the moment you really should only use this at indexing time.
		return new FieldComparatorAnonymousInnerClassHelper(this, parentSlots, childSlots, parentReverseMul, parentComparators, childReverseMul, childComparators);
	  }

	  private class FieldComparatorAnonymousInnerClassHelper : FieldComparator<int?>
	  {
		  private readonly BlockJoinComparatorSource outerInstance;

		  private int[] parentSlots;
		  private int[] childSlots;
		  private int[] parentReverseMul;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.search.FieldComparator<JavaToDotNetGenericWildcard>[] parentComparators;
		  private FieldComparator<?>[] parentComparators;
		  private int[] childReverseMul;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.search.FieldComparator<JavaToDotNetGenericWildcard>[] childComparators;
		  private FieldComparator<?>[] childComparators;

		  public FieldComparatorAnonymousInnerClassHelper<T1, T2>(BlockJoinComparatorSource outerInstance, int[] parentSlots, int[] childSlots, int[] parentReverseMul, FieldComparator<T1>[] parentComparators, int[] childReverseMul, FieldComparator<T2>[] childComparators)
		  {
			  this.outerInstance = outerInstance;
			  this.parentSlots = parentSlots;
			  this.childSlots = childSlots;
			  this.parentReverseMul = parentReverseMul;
			  this.parentComparators = parentComparators;
			  this.childReverseMul = childReverseMul;
			  this.childComparators = childComparators;
		  }

		  internal int bottomParent;
		  internal int bottomChild;
		  internal FixedBitSet parentBits;

		  public override int compare(int slot1, int slot2)
		  {
			try
			{
			  return compare(childSlots[slot1], parentSlots[slot1], childSlots[slot2], parentSlots[slot2]);
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
		  }

		  public override int Bottom
		  {
			  set
			  {
				bottomParent = parentSlots[value];
				bottomChild = childSlots[value];
			  }
		  }

		  public override int? TopValue
		  {
			  set
			  {
				// we dont have enough information (the docid is needed)
				throw new System.NotSupportedException("this comparator cannot be used with deep paging");
			  }
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int compareBottom(int doc) throws java.io.IOException
		  public override int compareBottom(int doc)
		  {
			return compare(bottomChild, bottomParent, doc, parent(doc));
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int compareTop(int doc) throws java.io.IOException
		  public override int compareTop(int doc)
		  {
			// we dont have enough information (the docid is needed)
			throw new System.NotSupportedException("this comparator cannot be used with deep paging");
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void copy(int slot, int doc) throws java.io.IOException
		  public override void copy(int slot, int doc)
		  {
			childSlots[slot] = doc;
			parentSlots[slot] = parent(doc);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.FieldComparator<Integer> setNextReader(org.apache.lucene.index.AtomicReaderContext context) throws java.io.IOException
		  public override FieldComparator<int?> setNextReader(AtomicReaderContext context)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.DocIdSet parents = parentsFilter.getDocIdSet(context, null);
			DocIdSet parents = outerInstance.parentsFilter.getDocIdSet(context, null);
			if (parents == null)
			{
			  throw new IllegalStateException("AtomicReader " + context.reader() + " contains no parents!");
			}
			if (!(parents is FixedBitSet))
			{
			  throw new IllegalStateException("parentFilter must return FixedBitSet; got " + parents);
			}
			parentBits = (FixedBitSet) parents;
			for (int i = 0; i < parentComparators.Length; i++)
			{
			  parentComparators[i] = parentComparators[i].setNextReader(context);
			}
			for (int i = 0; i < childComparators.Length; i++)
			{
			  childComparators[i] = childComparators[i].setNextReader(context);
			}
			return this;
		  }

		  public override int? value(int slot)
		  {
			// really our sort "value" is more complex...
			throw new System.NotSupportedException("filling sort field values is not yet supported");
		  }

		  public override Scorer Scorer
		  {
			  set
			  {
				base.Scorer = value;
	//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
	//ORIGINAL LINE: for (org.apache.lucene.search.FieldComparator<?> comp : parentComparators)
				foreach (FieldComparator<?> comp in parentComparators)
				{
				  comp.Scorer = value;
				}
	//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
	//ORIGINAL LINE: for (org.apache.lucene.search.FieldComparator<?> comp : childComparators)
				foreach (FieldComparator<?> comp in childComparators)
				{
				  comp.Scorer = value;
				}
			  }
		  }

		  internal virtual int parent(int doc)
		  {
			return parentBits.nextSetBit(doc);
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int compare(int docID1, int parent1, int docID2, int parent2) throws java.io.IOException
		  internal virtual int compare(int docID1, int parent1, int docID2, int parent2)
		  {
			if (parent1 == parent2) // both are in the same block
			{
			  if (docID1 == parent1 || docID2 == parent2)
			  {
				// keep parents at the end of blocks
				return docID1 - docID2;
			  }
			  else
			  {
				return compare(docID1, docID2, childComparators, childReverseMul);
			  }
			}
			else
			{
			  int cmp = compare(parent1, parent2, parentComparators, parentReverseMul);
			  if (cmp == 0)
			  {
				return parent1 - parent2;
			  }
			  else
			  {
				return cmp;
			  }
			}
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int compare(int docID1, int docID2, org.apache.lucene.search.FieldComparator<?> comparators[] , int reverseMul[]) throws java.io.IOException
		  internal virtual int compare<T1>(int docID1, int docID2, FieldComparator<T1>[] comparators, int[] reverseMul)
		  {
			for (int i = 0; i < comparators.Length; i++)
			{
			  // TODO: would be better if copy() didnt cause a term lookup in TermOrdVal & co,
			  // the segments are always the same here...
			  comparators[i].copy(0, docID1);
			  comparators[i].Bottom = 0;
			  int comp = reverseMul[i] * comparators[i].compareBottom(docID2);
			  if (comp != 0)
			  {
				return comp;
			  }
			}
			return 0; // no need to docid tiebreak
		  }
	  }

	  public override string ToString()
	  {
		return "blockJoin(parentSort=" + parentSort + ",childSort=" + childSort + ")";
	  }
	}

}