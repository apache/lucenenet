using System.Diagnostics;

namespace Lucene.Net.Search
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

	using Lucene.Net.Util;

	/// <summary>
	/// Expert: A hit queue for sorting by hits by terms in more than one field.
	/// Uses <code>FieldCache.DEFAULT</code> for maintaining
	/// internal term lookup tables.
	/// 
	/// @lucene.experimental
	/// @since 2.9 </summary>
	/// <seealso cref= IndexSearcher#search(Query,Filter,int,Sort) </seealso>
	/// <seealso cref= FieldCache </seealso>
	public abstract class FieldValueHitQueue<T> : PriorityQueue<T> where T : FieldValueHitQueue.Entry
	{

	  /// <summary>
	  /// Extension of ScoreDoc to also store the 
	  /// <seealso cref="FieldComparator"/> slot.
	  /// </summary>
	  public class Entry : ScoreDoc
	  {
		public int Slot;

		public Entry(int slot, int doc, float score) : base(doc, score)
		{
		  this.Slot = slot;
		}

		public override string ToString()
		{
		  return "slot:" + Slot + " " + base.ToString();
		}
	  }

	  /// <summary>
	  /// An implementation of <seealso cref="FieldValueHitQueue"/> which is optimized in case
	  /// there is just one comparator.
	  /// </summary>
	  private sealed class OneComparatorFieldValueHitQueue<T> : FieldValueHitQueue<T> where T : FieldValueHitQueue.Entry
	  {
		internal readonly int OneReverseMul;

		public OneComparatorFieldValueHitQueue(SortField[] fields, int size) : base(fields, size)
		{

		  SortField field = fields[0];
		  outerInstance.SetComparator(0,field.GetComparator(size, 0));
		  OneReverseMul = field.Reverse_Renamed ? - 1 : 1;

		  outerInstance.ReverseMul_Renamed[0] = OneReverseMul;
		}

		/// <summary>
		/// Returns whether <code>hitA</code> is less relevant than <code>hitB</code>. </summary>
		/// <param name="hitA"> Entry </param>
		/// <param name="hitB"> Entry </param>
		/// <returns> <code>true</code> if document <code>hitA</code> should be sorted after document <code>hitB</code>. </returns>
		protected internal override bool LessThan(Entry hitA, Entry hitB)
		{

		  Debug.Assert(hitA != hitB);
		  Debug.Assert(hitA.Slot != hitB.Slot);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int c = oneReverseMul * firstComparator.compare(hitA.slot, hitB.slot);
		  int c = OneReverseMul * outerInstance.FirstComparator.compare(hitA.Slot, hitB.Slot);
		  if (c != 0)
		  {
			return c > 0;
		  }

		  // avoid random sort order that could lead to duplicates (bug #31241):
		  return hitA.Doc > hitB.Doc;
		}

	  }

	  /// <summary>
	  /// An implementation of <seealso cref="FieldValueHitQueue"/> which is optimized in case
	  /// there is more than one comparator.
	  /// </summary>
	  private sealed class MultiComparatorsFieldValueHitQueue<T> : FieldValueHitQueue<T> where T : FieldValueHitQueue.Entry
	  {

		public MultiComparatorsFieldValueHitQueue(SortField[] fields, int size) : base(fields, size)
		{

		  int numComparators = outerInstance.Comparators_Renamed.Length;
		  for (int i = 0; i < numComparators; ++i)
		  {
			SortField field = fields[i];

			outerInstance.ReverseMul_Renamed[i] = field.Reverse_Renamed ? - 1 : 1;
			outerInstance.SetComparator(i, field.GetComparator(size, i));
		  }
		}

		protected internal override bool LessThan(Entry hitA, Entry hitB)
		{

		  Debug.Assert(hitA != hitB);
		  Debug.Assert(hitA.Slot != hitB.Slot);

		  int numComparators = outerInstance.Comparators_Renamed.Length;
		  for (int i = 0; i < numComparators; ++i)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int c = reverseMul[i] * comparators[i].compare(hitA.slot, hitB.slot);
			int c = outerInstance.ReverseMul_Renamed[i] * outerInstance.Comparators_Renamed[i].compare(hitA.Slot, hitB.Slot);
			if (c != 0)
			{
			  // Short circuit
			  return c > 0;
			}
		  }

		  // avoid random sort order that could lead to duplicates (bug #31241):
		  return hitA.Doc > hitB.Doc;
		}

	  }

	  // prevent instantiation and extension.
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) private FieldValueHitQueue(SortField[] fields, int size)
	  private FieldValueHitQueue(SortField[] fields, int size) : base(size)
	  {
		// When we get here, fields.length is guaranteed to be > 0, therefore no
		// need to check it again.

		// All these are required by this class's API - need to return arrays.
		// Therefore even in the case of a single comparator, create an array
		// anyway.
		this.Fields_Renamed = fields;
		int numComparators = fields.Length;
		Comparators_Renamed = new FieldComparator[numComparators];
		ReverseMul_Renamed = new int[numComparators];
	  }

	  /// <summary>
	  /// Creates a hit queue sorted by the given list of fields.
	  /// 
	  /// <p><b>NOTE</b>: The instances returned by this method
	  /// pre-allocate a full array of length <code>numHits</code>.
	  /// </summary>
	  /// <param name="fields">
	  ///          SortField array we are sorting by in priority order (highest
	  ///          priority first); cannot be <code>null</code> or empty </param>
	  /// <param name="size">
	  ///          The number of hits to retain. Must be greater than zero. </param>
	  /// <exception cref="IOException"> if there is a low-level IO error </exception>
	  public static FieldValueHitQueue<T> create<T>(SortField[] fields, int size) where T : FieldValueHitQueue.Entry
	  {

		if (fields.Length == 0)
		{
		  throw new System.ArgumentException("Sort must contain at least one field");
		}

		if (fields.Length == 1)
		{
		  return new OneComparatorFieldValueHitQueue<>(fields, size);
		}
		else
		{
		  return new MultiComparatorsFieldValueHitQueue<>(fields, size);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: public FieldComparator<?>[] getComparators()
	  public virtual FieldComparator<?>[] Comparators
	  {
		  get
		  {
			return Comparators_Renamed;
		  }
	  }

	  public virtual int[] ReverseMul
	  {
		  get
		  {
			return ReverseMul_Renamed;
		  }
	  }

	  public virtual void setComparator<T1>(int pos, FieldComparator<T1> comparator)
	  {
		if (pos == 0)
		{
			FirstComparator = comparator;
		}
		Comparators_Renamed[pos] = comparator;
	  }

	  /// <summary>
	  /// Stores the sort criteria being used. </summary>
	  protected internal readonly SortField[] Fields_Renamed;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: protected final FieldComparator<?>[] comparators;
	  protected internal readonly FieldComparator<?>[] Comparators_Renamed; // use setComparator to change this array
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: protected FieldComparator<?> firstComparator;
	  protected internal FieldComparator<?> FirstComparator; // this must always be equal to comparators[0]
	  protected internal readonly int[] ReverseMul_Renamed;

	  protected internal override abstract bool LessThan(Entry a, Entry b);

	  /// <summary>
	  /// Given a queue Entry, creates a corresponding FieldDoc
	  /// that contains the values used to sort the given document.
	  /// These values are not the raw values out of the index, but the internal
	  /// representation of them. this is so the given search hit can be collated by
	  /// a MultiSearcher with other search hits.
	  /// </summary>
	  /// <param name="entry"> The Entry used to create a FieldDoc </param>
	  /// <returns> The newly created FieldDoc </returns>
	  /// <seealso cref= IndexSearcher#search(Query,Filter,int,Sort) </seealso>
	  internal virtual FieldDoc FillFields(Entry entry)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int n = comparators.length;
		int n = Comparators_Renamed.Length;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object[] fields = new Object[n];
		object[] fields = new object[n];
		for (int i = 0; i < n; ++i)
		{
		  fields[i] = Comparators_Renamed[i].Value(entry.Slot);
		}
		//if (maxscore > 1.0f) doc.score /= maxscore;   // normalize scores
		return new FieldDoc(entry.Doc, entry.Score, fields);
	  }

	  /// <summary>
	  /// Returns the SortFields being used by this hit queue. </summary>
	  internal virtual SortField[] Fields
	  {
		  get
		  {
			return Fields_Renamed;
		  }
	  }
	}

}