using System.Collections.Generic;

namespace Lucene.Net.Util
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



	/// <summary>
	/// Methods for manipulating (sorting) collections.
	/// Sort methods work directly on the supplied lists and don't copy to/from arrays
	/// before/after. For medium size collections as used in the Lucene indexer that is
	/// much more efficient.
	/// 
	/// @lucene.internal
	/// </summary>

	public sealed class CollectionUtil
	{

	  private CollectionUtil() // no instance
	  {
	  }
	  private sealed class ListIntroSorter<T> : IntroSorter
	  {

		internal T Pivot_Renamed;
		internal readonly IList<T> List;
//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
//ORIGINAL LINE: final java.util.Comparator<? base T> comp;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		internal readonly IComparer<?> Comp;

//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
//ORIGINAL LINE: ListIntroSorter(java.util.List<T> list, java.util.Comparator<? base T> comp)
		internal ListIntroSorter<T1>(IList<T> list, IComparer<T1> comp) : base()
		{
		  if (!(list is RandomAccess))
		  {
			throw new System.ArgumentException("CollectionUtil can only sort random access lists in-place.");
		  }
		  this.List = list;
		  this.Comp = comp;
		}

		protected internal override int Pivot
		{
			set
			{
			  Pivot_Renamed = List[value];
			}
		}

		protected internal override void Swap(int i, int j)
		{
		  Collections.swap(List, i, j);
		}

		protected internal override int Compare(int i, int j)
		{
		  return Comp.Compare(List[i], List[j]);
		}

		protected internal override int ComparePivot(int j)
		{
		  return Comp.Compare(Pivot_Renamed, List[j]);
		}

	  }

	  private sealed class ListTimSorter<T> : TimSorter
	  {

		internal readonly IList<T> List;
//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
//ORIGINAL LINE: final java.util.Comparator<? base T> comp;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		internal readonly IComparer<?> Comp;
		internal readonly T[] Tmp;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") ListTimSorter(java.util.List<T> list, java.util.Comparator<? base T> comp, int maxTempSlots)
//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
		internal ListTimSorter<T1>(IList<T> list, IComparer<T1> comp, int maxTempSlots) : base(maxTempSlots)
		{
		  if (!(list is RandomAccess))
		  {
			throw new System.ArgumentException("CollectionUtil can only sort random access lists in-place.");
		  }
		  this.List = list;
		  this.Comp = comp;
		  if (maxTempSlots > 0)
		  {
			this.Tmp = (T[]) new object[maxTempSlots];
		  }
		  else
		  {
			this.Tmp = null;
		  }
		}

		protected internal override void Swap(int i, int j)
		{
		  Collections.swap(List, i, j);
		}

		protected internal override void Copy(int src, int dest)
		{
		  List[dest] = List[src];
		}

		protected internal override void Save(int i, int len)
		{
		  for (int j = 0; j < len; ++j)
		  {
			Tmp[j] = List[i + j];
		  }
		}

		protected internal override void Restore(int i, int j)
		{
		  List[j] = Tmp[i];
		}

		protected internal override int Compare(int i, int j)
		{
		  return Comp.Compare(List[i], List[j]);
		}

		protected internal override int CompareSaved(int i, int j)
		{
		  return Comp.Compare(Tmp[i], List[j]);
		}

	  }

	  /// <summary>
	  /// Sorts the given random access <seealso cref="List"/> using the <seealso cref="Comparator"/>.
	  /// The list must implement <seealso cref="RandomAccess"/>. this method uses the intro sort
	  /// algorithm, but falls back to insertion sort for small lists. </summary>
	  /// <exception cref="IllegalArgumentException"> if list is e.g. a linked list without random access. </exception>
//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
//ORIGINAL LINE: public static <T> void introSort(java.util.List<T> list, java.util.Comparator<? base T> comp)
	  public static void introSort<T, T1>(IList<T> list, IComparer<T1> comp)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = list.size();
		int size = list.Count;
		if (size <= 1)
		{
			return;
		}
		(new ListIntroSorter<>(list, comp)).Sort(0, size);
	  }

	  /// <summary>
	  /// Sorts the given random access <seealso cref="List"/> in natural order.
	  /// The list must implement <seealso cref="RandomAccess"/>. this method uses the intro sort
	  /// algorithm, but falls back to insertion sort for small lists. </summary>
	  /// <exception cref="IllegalArgumentException"> if list is e.g. a linked list without random access. </exception>
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: public static <T extends Comparable<? base T>> void introSort(java.util.List<T> list)
	  public static void introSort<T>(IList<T> list) where T : Comparable<? base T>
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = list.size();
		int size = list.Count;
		if (size <= 1)
		{
			return;
		}
		IntroSort(list, ArrayUtil.NaturalComparator<T>());
	  }

	  // Tim sorts:

	  /// <summary>
	  /// Sorts the given random access <seealso cref="List"/> using the <seealso cref="Comparator"/>.
	  /// The list must implement <seealso cref="RandomAccess"/>. this method uses the Tim sort
	  /// algorithm, but falls back to binary sort for small lists. </summary>
	  /// <exception cref="IllegalArgumentException"> if list is e.g. a linked list without random access. </exception>
//JAVA TO C# CONVERTER TODO TASK: There is no .NET equivalent to the Java 'super' constraint:
//ORIGINAL LINE: public static <T> void timSort(java.util.List<T> list, java.util.Comparator<? base T> comp)
	  public static void timSort<T, T1>(IList<T> list, IComparer<T1> comp)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = list.size();
		int size = list.Count;
		if (size <= 1)
		{
			return;
		}
		(new ListTimSorter<>(list, comp, list.Count / 64)).Sort(0, size);
	  }

	  /// <summary>
	  /// Sorts the given random access <seealso cref="List"/> in natural order.
	  /// The list must implement <seealso cref="RandomAccess"/>. this method uses the Tim sort
	  /// algorithm, but falls back to binary sort for small lists. </summary>
	  /// <exception cref="IllegalArgumentException"> if list is e.g. a linked list without random access. </exception>
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: public static <T extends Comparable<? base T>> void timSort(java.util.List<T> list)
	  public static void timSort<T>(IList<T> list) where T : Comparable<? base T>
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = list.size();
		int size = list.Count;
		if (size <= 1)
		{
			return;
		}
		TimSort(list, ArrayUtil.NaturalComparator<T>());
	  }

	}

}