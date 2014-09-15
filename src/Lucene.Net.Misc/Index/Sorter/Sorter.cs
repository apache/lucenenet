using System;
using System.Diagnostics;

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


	using FieldComparator = org.apache.lucene.search.FieldComparator;
	using Scorer = org.apache.lucene.search.Scorer;
	using Sort = org.apache.lucene.search.Sort;
	using SortField = org.apache.lucene.search.SortField;
	using TimSorter = org.apache.lucene.util.TimSorter;
	using MonotonicAppendingLongBuffer = org.apache.lucene.util.packed.MonotonicAppendingLongBuffer;

	/// <summary>
	/// Sorts documents of a given index by returning a permutation on the document
	/// IDs.
	/// @lucene.experimental
	/// </summary>
	internal sealed class Sorter
	{
	  internal readonly Sort sort_Renamed;

	  /// <summary>
	  /// Creates a new Sorter to sort the index with {@code sort} </summary>
	  internal Sorter(Sort sort)
	  {
		if (sort.needsScores())
		{
		  throw new System.ArgumentException("Cannot sort an index with a Sort that refers to the relevance score");
		}
		this.sort_Renamed = sort;
	  }

	  /// <summary>
	  /// A permutation of doc IDs. For every document ID between <tt>0</tt> and
	  /// <seealso cref="IndexReader#maxDoc()"/>, <code>oldToNew(newToOld(docID))</code> must
	  /// return <code>docID</code>.
	  /// </summary>
	  internal abstract class DocMap
	  {

		/// <summary>
		/// Given a doc ID from the original index, return its ordinal in the
		///  sorted index. 
		/// </summary>
		internal abstract int oldToNew(int docID);

		/// <summary>
		/// Given the ordinal of a doc ID, return its doc ID in the original index. </summary>
		internal abstract int newToOld(int docID);

		/// <summary>
		/// Return the number of documents in this map. This must be equal to the
		///  <seealso cref="AtomicReader#maxDoc() number of documents"/> of the
		///  <seealso cref="AtomicReader"/> which is sorted. 
		/// </summary>
		internal abstract int size();
	  }

	  /// <summary>
	  /// Check consistency of a <seealso cref="DocMap"/>, useful for assertions. </summary>
	  internal static bool isConsistent(DocMap docMap)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = docMap.size();
		int maxDoc = docMap.size();
		for (int i = 0; i < maxDoc; ++i)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newID = docMap.oldToNew(i);
		  int newID = docMap.oldToNew(i);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int oldID = docMap.newToOld(newID);
		  int oldID = docMap.newToOld(newID);
		  Debug.Assert(newID >= 0 && newID < maxDoc, "doc IDs must be in [0-" + maxDoc + "[, got " + newID);
		  Debug.Assert(i == oldID, "mapping is inconsistent: " + i + " --oldToNew--> " + newID + " --newToOld--> " + oldID);
		  if (i != oldID || newID < 0 || newID >= maxDoc)
		  {
			return false;
		  }
		}
		return true;
	  }

	  /// <summary>
	  /// A comparator of doc IDs. </summary>
	  internal abstract class DocComparator
	  {

		/// <summary>
		/// Compare docID1 against docID2. The contract for the return value is the
		///  same as <seealso cref="Comparator#compare(Object, Object)"/>. 
		/// </summary>
		public abstract int compare(int docID1, int docID2);

	  }

	  private sealed class DocValueSorter : TimSorter
	  {

		internal readonly int[] docs;
		internal readonly Sorter.DocComparator comparator;
		internal readonly int[] tmp;

		internal DocValueSorter(int[] docs, Sorter.DocComparator comparator) : base(docs.Length / 64)
		{
		  this.docs = docs;
		  this.comparator = comparator;
		  tmp = new int[docs.Length / 64];
		}

		protected internal override int compare(int i, int j)
		{
		  return comparator.compare(docs[i], docs[j]);
		}

		protected internal override void swap(int i, int j)
		{
		  int tmpDoc = docs[i];
		  docs[i] = docs[j];
		  docs[j] = tmpDoc;
		}

		protected internal override void copy(int src, int dest)
		{
		  docs[dest] = docs[src];
		}

		protected internal override void save(int i, int len)
		{
		  Array.Copy(docs, i, tmp, 0, len);
		}

		protected internal override void restore(int i, int j)
		{
		  docs[j] = tmp[i];
		}

		protected internal override int compareSaved(int i, int j)
		{
		  return comparator.compare(tmp[i], docs[j]);
		}
	  }

	  /// <summary>
	  /// Computes the old-to-new permutation over the given comparator. </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private static Sorter.DocMap sort(final int maxDoc, DocComparator comparator)
	  private static Sorter.DocMap sort(int maxDoc, DocComparator comparator)
	  {
		// check if the index is sorted
		bool sorted = true;
		for (int i = 1; i < maxDoc; ++i)
		{
		  if (comparator.compare(i - 1, i) > 0)
		  {
			sorted = false;
			break;
		  }
		}
		if (sorted)
		{
		  return null;
		}

		// sort doc IDs
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] docs = new int[maxDoc];
		int[] docs = new int[maxDoc];
		for (int i = 0; i < maxDoc; i++)
		{
		  docs[i] = i;
		}

		DocValueSorter sorter = new DocValueSorter(docs, comparator);
		// It can be common to sort a reader, add docs, sort it again, ... and in
		// that case timSort can save a lot of time
		sorter.sort(0, docs.Length); // docs is now the newToOld mapping

		// The reason why we use MonotonicAppendingLongBuffer here is that it
		// wastes very little memory if the index is in random order but can save
		// a lot of memory if the index is already "almost" sorted
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.MonotonicAppendingLongBuffer newToOld = new org.apache.lucene.util.packed.MonotonicAppendingLongBuffer();
		MonotonicAppendingLongBuffer newToOld = new MonotonicAppendingLongBuffer();
		for (int i = 0; i < maxDoc; ++i)
		{
		  newToOld.add(docs[i]);
		}
		newToOld.freeze();

		for (int i = 0; i < maxDoc; ++i)
		{
		  docs[(int) newToOld.get(i)] = i;
		} // docs is now the oldToNew mapping

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.packed.MonotonicAppendingLongBuffer oldToNew = new org.apache.lucene.util.packed.MonotonicAppendingLongBuffer();
		MonotonicAppendingLongBuffer oldToNew = new MonotonicAppendingLongBuffer();
		for (int i = 0; i < maxDoc; ++i)
		{
		  oldToNew.add(docs[i]);
		}
		oldToNew.freeze();

		return new DocMapAnonymousInnerClassHelper(maxDoc, newToOld, oldToNew);
	  }

	  private class DocMapAnonymousInnerClassHelper : Sorter.DocMap
	  {
		  private int maxDoc;
		  private MonotonicAppendingLongBuffer newToOld;
		  private MonotonicAppendingLongBuffer oldToNew;

		  public DocMapAnonymousInnerClassHelper(int maxDoc, MonotonicAppendingLongBuffer newToOld, MonotonicAppendingLongBuffer oldToNew)
		  {
			  this.maxDoc = maxDoc;
			  this.newToOld = newToOld;
			  this.oldToNew = oldToNew;
		  }


		  public override int oldToNew(int docID)
		  {
			return (int) oldToNew.get(docID);
		  }

		  public override int newToOld(int docID)
		  {
			return (int) newToOld.get(docID);
		  }

		  public override int size()
		  {
			return maxDoc;
		  }
	  }

	  /// <summary>
	  /// Returns a mapping from the old document ID to its new location in the
	  /// sorted index. Implementations can use the auxiliary
	  /// <seealso cref="#sort(int, DocComparator)"/> to compute the old-to-new permutation
	  /// given a list of documents and their corresponding values.
	  /// <para>
	  /// A return value of <tt>null</tt> is allowed and means that
	  /// <code>reader</code> is already sorted.
	  /// </para>
	  /// <para>
	  /// <b>NOTE:</b> deleted documents are expected to appear in the mapping as
	  /// well, they will however be marked as deleted in the sorted view.
	  /// </para>
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: DocMap sort(org.apache.lucene.index.AtomicReader reader) throws java.io.IOException
	  internal DocMap sort(AtomicReader reader)
	  {
		SortField[] fields = sort_Renamed.Sort;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int reverseMul[] = new int[fields.length];
		int[] reverseMul = new int[fields.Length];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldComparator<?> comparators[] = new org.apache.lucene.search.FieldComparator[fields.length];
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		FieldComparator<?>[] comparators = new FieldComparator[fields.Length];

		for (int i = 0; i < fields.Length; i++)
		{
		  reverseMul[i] = fields[i].Reverse ? - 1 : 1;
		  comparators[i] = fields[i].getComparator(1, i);
		  comparators[i].NextReader = reader.Context;
		  comparators[i].Scorer = FAKESCORER;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final DocComparator comparator = new DocComparator()
		DocComparator comparator = new DocComparatorAnonymousInnerClassHelper(this, reverseMul, comparators);
		return sort(reader.maxDoc(), comparator);
	  }

	  private class DocComparatorAnonymousInnerClassHelper : DocComparator
	  {
		  private readonly Sorter outerInstance;

		  private int[] reverseMul;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private org.apache.lucene.search.FieldComparator<JavaToDotNetGenericWildcard>[] comparators;
		  private FieldComparator<?>[] comparators;

		  public DocComparatorAnonymousInnerClassHelper<T1>(Sorter outerInstance, int[] reverseMul, FieldComparator<T1>[] comparators)
		  {
			  this.outerInstance = outerInstance;
			  this.reverseMul = reverseMul;
			  this.comparators = comparators;
		  }

		  public override int compare(int docID1, int docID2)
		  {
			try
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
			  return int.compare(docID1, docID2); // docid order tiebreak
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	  /// <summary>
	  /// Returns the identifier of this <seealso cref="Sorter"/>.
	  /// <para>This identifier is similar to <seealso cref="Object#hashCode()"/> and should be
	  /// chosen so that two instances of this class that sort documents likewise
	  /// will have the same identifier. On the contrary, this identifier should be
	  /// different on different <seealso cref="Sort sorts"/>.
	  /// </para>
	  /// </summary>
	  public string ID
	  {
		  get
		  {
			return sort_Renamed.ToString();
		  }
	  }

	  public override string ToString()
	  {
		return ID;
	  }

	  internal static readonly Scorer FAKESCORER = new ScorerAnonymousInnerClassHelper();

	  private class ScorerAnonymousInnerClassHelper : Scorer
	  {
		  public ScorerAnonymousInnerClassHelper() : base(null)
		  {
		  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public float score() throws java.io.IOException
		  public override float score()
		  {
			  throw new System.NotSupportedException();
		  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		  public override int freq()
		  {
			  throw new System.NotSupportedException();
		  }
		  public override int docID()
		  {
			  throw new System.NotSupportedException();
		  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
		  public override int nextDoc()
		  {
			  throw new System.NotSupportedException();
		  }
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		  public override int advance(int target)
		  {
			  throw new System.NotSupportedException();
		  }
		  public override long cost()
		  {
			  throw new System.NotSupportedException();
		  }
	  }

	}

}