/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Index.Sorter;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Packed;
using Sharpen;

namespace Org.Apache.Lucene.Index.Sorter
{
	/// <summary>
	/// Sorts documents of a given index by returning a permutation on the document
	/// IDs.
	/// </summary>
	/// <remarks>
	/// Sorts documents of a given index by returning a permutation on the document
	/// IDs.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal sealed class Sorter
	{
		internal readonly Org.Apache.Lucene.Search.Sort sort;

		/// <summary>
		/// Creates a new Sorter to sort the index with
		/// <code>sort</code>
		/// 
		/// </summary>
		internal Sorter(Org.Apache.Lucene.Search.Sort sort)
		{
			if (sort.NeedsScores())
			{
				throw new ArgumentException("Cannot sort an index with a Sort that refers to the relevance score"
					);
			}
			this.sort = sort;
		}

		/// <summary>A permutation of doc IDs.</summary>
		/// <remarks>
		/// A permutation of doc IDs. For every document ID between <tt>0</tt> and
		/// <see cref="Org.Apache.Lucene.Index.IndexReader.MaxDoc()">Org.Apache.Lucene.Index.IndexReader.MaxDoc()
		/// 	</see>
		/// , <code>oldToNew(newToOld(docID))</code> must
		/// return <code>docID</code>.
		/// </remarks>
		internal abstract class DocMap
		{
			/// <summary>
			/// Given a doc ID from the original index, return its ordinal in the
			/// sorted index.
			/// </summary>
			/// <remarks>
			/// Given a doc ID from the original index, return its ordinal in the
			/// sorted index.
			/// </remarks>
			internal abstract int OldToNew(int docID);

			/// <summary>Given the ordinal of a doc ID, return its doc ID in the original index.</summary>
			/// <remarks>Given the ordinal of a doc ID, return its doc ID in the original index.</remarks>
			internal abstract int NewToOld(int docID);

			/// <summary>Return the number of documents in this map.</summary>
			/// <remarks>
			/// Return the number of documents in this map. This must be equal to the
			/// <see cref="Org.Apache.Lucene.Index.IndexReader.MaxDoc()">number of documents</see>
			/// of the
			/// <see cref="Org.Apache.Lucene.Index.AtomicReader">Org.Apache.Lucene.Index.AtomicReader
			/// 	</see>
			/// which is sorted.
			/// </remarks>
			internal abstract int Size();
		}

		/// <summary>
		/// Check consistency of a
		/// <see cref="DocMap">DocMap</see>
		/// , useful for assertions.
		/// </summary>
		internal static bool IsConsistent(Sorter.DocMap docMap)
		{
			int maxDoc = docMap.Size();
			for (int i = 0; i < maxDoc; ++i)
			{
				int newID = docMap.OldToNew(i);
				int oldID = docMap.NewToOld(newID);
				//HM:revisit
				if (i != oldID || newID < 0 || newID >= maxDoc)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>A comparator of doc IDs.</summary>
		/// <remarks>A comparator of doc IDs.</remarks>
		internal abstract class DocComparator
		{
			/// <summary>Compare docID1 against docID2.</summary>
			/// <remarks>
			/// Compare docID1 against docID2. The contract for the return value is the
			/// same as
			/// <see cref="System.Collections.IEnumerator{T}.Compare(object, object)">System.Collections.IEnumerator&lt;T&gt;.Compare(object, object)
			/// 	</see>
			/// .
			/// </remarks>
			public abstract int Compare(int docID1, int docID2);
		}

		private sealed class DocValueSorter : TimSorter
		{
			private readonly int[] docs;

			private readonly Sorter.DocComparator comparator;

			private readonly int[] tmp;

			internal DocValueSorter(int[] docs, Sorter.DocComparator comparator) : base(docs.
				Length / 64)
			{
				this.docs = docs;
				this.comparator = comparator;
				tmp = new int[docs.Length / 64];
			}

			protected override int Compare(int i, int j)
			{
				return comparator.Compare(docs[i], docs[j]);
			}

			protected override void Swap(int i, int j)
			{
				int tmpDoc = docs[i];
				docs[i] = docs[j];
				docs[j] = tmpDoc;
			}

			protected override void Copy(int src, int dest)
			{
				docs[dest] = docs[src];
			}

			protected override void Save(int i, int len)
			{
				System.Array.Copy(docs, i, tmp, 0, len);
			}

			protected override void Restore(int i, int j)
			{
				docs[j] = tmp[i];
			}

			protected override int CompareSaved(int i, int j)
			{
				return comparator.Compare(tmp[i], docs[j]);
			}
		}

		/// <summary>Computes the old-to-new permutation over the given comparator.</summary>
		/// <remarks>Computes the old-to-new permutation over the given comparator.</remarks>
		private static Sorter.DocMap Sort(int maxDoc, Sorter.DocComparator comparator)
		{
			// check if the index is sorted
			bool sorted = true;
			for (int i = 1; i < maxDoc; ++i)
			{
				if (comparator.Compare(i - 1, i) > 0)
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
			int[] docs = new int[maxDoc];
			for (int i_1 = 0; i_1 < maxDoc; i_1++)
			{
				docs[i_1] = i_1;
			}
			Sorter.DocValueSorter sorter = new Sorter.DocValueSorter(docs, comparator);
			// It can be common to sort a reader, add docs, sort it again, ... and in
			// that case timSort can save a lot of time
			sorter.Sort(0, docs.Length);
			// docs is now the newToOld mapping
			// The reason why we use MonotonicAppendingLongBuffer here is that it
			// wastes very little memory if the index is in random order but can save
			// a lot of memory if the index is already "almost" sorted
			MonotonicAppendingLongBuffer newToOld = new MonotonicAppendingLongBuffer();
			for (int i_2 = 0; i_2 < maxDoc; ++i_2)
			{
				newToOld.Add(docs[i_2]);
			}
			newToOld.Freeze();
			for (int i_3 = 0; i_3 < maxDoc; ++i_3)
			{
				docs[(int)newToOld.Get(i_3)] = i_3;
			}
			// docs is now the oldToNew mapping
			MonotonicAppendingLongBuffer oldToNew = new MonotonicAppendingLongBuffer();
			for (int i_4 = 0; i_4 < maxDoc; ++i_4)
			{
				oldToNew.Add(docs[i_4]);
			}
			oldToNew.Freeze();
			return new _DocMap_183(oldToNew, newToOld, maxDoc);
		}

		private sealed class _DocMap_183 : Sorter.DocMap
		{
			public _DocMap_183(MonotonicAppendingLongBuffer oldToNew, MonotonicAppendingLongBuffer
				 newToOld, int maxDoc)
			{
				this.oldToNew = oldToNew;
				this.newToOld = newToOld;
				this.maxDoc = maxDoc;
			}

			internal override int OldToNew(int docID)
			{
				return (int)oldToNew.Get(docID);
			}

			internal override int NewToOld(int docID)
			{
				return (int)newToOld.Get(docID);
			}

			internal override int Size()
			{
				return maxDoc;
			}

			private readonly MonotonicAppendingLongBuffer oldToNew;

			private readonly MonotonicAppendingLongBuffer newToOld;

			private readonly int maxDoc;
		}

		/// <summary>
		/// Returns a mapping from the old document ID to its new location in the
		/// sorted index.
		/// </summary>
		/// <remarks>
		/// Returns a mapping from the old document ID to its new location in the
		/// sorted index. Implementations can use the auxiliary
		/// <see cref="Sort(int, DocComparator)">Sort(int, DocComparator)</see>
		/// to compute the old-to-new permutation
		/// given a list of documents and their corresponding values.
		/// <p>
		/// A return value of <tt>null</tt> is allowed and means that
		/// <code>reader</code> is already sorted.
		/// <p>
		/// <b>NOTE:</b> deleted documents are expected to appear in the mapping as
		/// well, they will however be marked as deleted in the sorted view.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		internal Sorter.DocMap Sort(AtomicReader reader)
		{
			SortField[] fields = sort.GetSort();
			int[] reverseMul = new int[fields.Length];
			FieldComparator<object>[] comparators = new FieldComparator[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				reverseMul[i] = fields[i].GetReverse() ? -1 : 1;
				comparators[i] = fields[i].GetComparator(1, i);
				comparators[i].SetNextReader(((AtomicReaderContext)reader.GetContext()));
				comparators[i].SetScorer(FAKESCORER);
			}
			Sorter.DocComparator comparator = new _DocComparator_225(comparators, reverseMul);
			// TODO: would be better if copy() didnt cause a term lookup in TermOrdVal & co,
			// the segments are always the same here...
			// docid order tiebreak
			return Sort(reader.MaxDoc(), comparator);
		}

		private sealed class _DocComparator_225 : Sorter.DocComparator
		{
			public _DocComparator_225(FieldComparator<object>[] comparators, int[] reverseMul
				)
			{
				this.comparators = comparators;
				this.reverseMul = reverseMul;
			}

			public override int Compare(int docID1, int docID2)
			{
				try
				{
					for (int i = 0; i < comparators.Length; i++)
					{
						comparators[i].Copy(0, docID1);
						comparators[i].SetBottom(0);
						int comp = reverseMul[i] * comparators[i].CompareBottom(docID2);
						if (comp != 0)
						{
							return comp;
						}
					}
					return int.Compare(docID1, docID2);
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly FieldComparator<object>[] comparators;

			private readonly int[] reverseMul;
		}

		/// <summary>
		/// Returns the identifier of this
		/// <see cref="Sorter">Sorter</see>
		/// .
		/// <p>This identifier is similar to
		/// <see cref="object.GetHashCode()">object.GetHashCode()</see>
		/// and should be
		/// chosen so that two instances of this class that sort documents likewise
		/// will have the same identifier. On the contrary, this identifier should be
		/// different on different
		/// <see cref="Org.Apache.Lucene.Search.Sort">sorts</see>
		/// .
		/// </summary>
		public string GetID()
		{
			return sort.ToString();
		}

		public override string ToString()
		{
			return GetID();
		}

		private sealed class _Scorer_264 : Scorer
		{
			public _Scorer_264(Weight baseArg1) : base(baseArg1)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				throw new NotSupportedException();
			}

			public override int DocID()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				throw new NotSupportedException();
			}

			public override long Cost()
			{
				throw new NotSupportedException();
			}
		}

		internal static readonly Scorer FAKESCORER = new _Scorer_264(null);
	}
}
