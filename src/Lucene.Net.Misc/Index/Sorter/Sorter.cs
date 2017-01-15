using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Index.Sorter
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
    /// Sorts documents of a given index by returning a permutation on the document
    /// IDs.
    /// @lucene.experimental
    /// </summary>
    internal sealed class Sorter
    {
        internal readonly Sort sort_Renamed;

        /// <summary>
        /// Creates a new Sorter to sort the index with <paramref name="sort"/>.
        /// </summary>
        internal Sorter(Sort sort)
        {
            if (sort.NeedsScores)
            {
                throw new System.ArgumentException("Cannot sort an index with a Sort that refers to the relevance score");
            }
            this.sort_Renamed = sort;
        }

        /// <summary>
        /// A permutation of doc IDs. For every document ID between <c>0</c> and
        /// <see cref="IndexReader.MaxDoc"/>, <code>OldToNew(NewToOld(docID))</code> must
        /// return <code>docID</code>.
        /// </summary>
        internal abstract class DocMap
        {

            /// <summary>
            /// Given a doc ID from the original index, return its ordinal in the
            ///  sorted index. 
            /// </summary>
            public abstract int OldToNew(int docID);

            /// <summary>
            /// Given the ordinal of a doc ID, return its doc ID in the original index. 
            /// </summary>
            public abstract int NewToOld(int docID);

            /// <summary>
            /// Return the number of documents in this map. This must be equal to the
            /// <see cref="AtomicReader.LiveDocs">number of documents</see> of the
            /// <see cref="AtomicReader"/> which is sorted. 
            /// </summary>
            public abstract int Count { get; }
        }

        /// <summary>
        /// Check consistency of a <see cref="DocMap"/>, useful for assertions.
        /// </summary>
        internal static bool IsConsistent(DocMap docMap)
        {
            int maxDoc = docMap.Count;
            for (int i = 0; i < maxDoc; ++i)
            {
                int newID = docMap.OldToNew(i);
                int oldID = docMap.NewToOld(newID);
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
        /// A comparer of doc IDs.
        /// </summary>
        internal abstract class DocComparer : IComparer<int>
        {

            /// <summary>
            /// Compare docID1 against docID2. The contract for the return value is the
            /// same as <see cref="IComparer{T}.Compare(T, T)"/>. 
            /// </summary>
            public abstract int Compare(int docID1, int docID2);

        }

        private sealed class DocValueSorter : TimSorter
        {

            internal readonly int[] docs;
            internal readonly Sorter.DocComparer comparer;
            internal readonly int[] tmp;

            internal DocValueSorter(int[] docs, Sorter.DocComparer comparer) : base(docs.Length / 64)
            {
                this.docs = docs;
                this.comparer = comparer;
                tmp = new int[docs.Length / 64];
            }

            protected override int Compare(int i, int j)
            {
                return comparer.Compare(docs[i], docs[j]);
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
                Array.Copy(docs, i, tmp, 0, len);
            }

            protected override void Restore(int i, int j)
            {
                docs[j] = tmp[i];
            }

            protected override int CompareSaved(int i, int j)
            {
                return comparer.Compare(tmp[i], docs[j]);
            }
        }

        /// <summary>
        /// Computes the old-to-new permutation over the given comparer.
        /// </summary>
        private static Sorter.DocMap Sort(int maxDoc, DocComparer comparer)
        {
            // check if the index is sorted
            bool sorted = true;
            for (int i = 1; i < maxDoc; ++i)
            {
                if (comparer.Compare(i - 1, i) > 0)
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
            for (int i = 0; i < maxDoc; i++)
            {
                docs[i] = i;
            }

            DocValueSorter sorter = new DocValueSorter(docs, comparer);
            // It can be common to sort a reader, add docs, sort it again, ... and in
            // that case timSort can save a lot of time
            sorter.Sort(0, docs.Length); // docs is now the newToOld mapping

            // The reason why we use MonotonicAppendingLongBuffer here is that it
            // wastes very little memory if the index is in random order but can save
            // a lot of memory if the index is already "almost" sorted
            MonotonicAppendingLongBuffer newToOld = new MonotonicAppendingLongBuffer();
            for (int i = 0; i < maxDoc; ++i)
            {
                newToOld.Add(docs[i]);
            }
            newToOld.Freeze();

            for (int i = 0; i < maxDoc; ++i)
            {
                docs[(int)newToOld.Get(i)] = i;
            } // docs is now the oldToNew mapping

            MonotonicAppendingLongBuffer oldToNew = new MonotonicAppendingLongBuffer();
            for (int i = 0; i < maxDoc; ++i)
            {
                oldToNew.Add(docs[i]);
            }
            oldToNew.Freeze();

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


            public override int OldToNew(int docID)
            {
                return (int)oldToNew.Get(docID);
            }

            public override int NewToOld(int docID)
            {
                return (int)newToOld.Get(docID);
            }

            public override int Count
            {
                get { return maxDoc; }
            }
        }

        /// <summary>
        /// Returns a mapping from the old document ID to its new location in the
        /// sorted index. Implementations can use the auxiliary
        /// <see cref="Sort(int, DocComparer)"/> to compute the old-to-new permutation
        /// given a list of documents and their corresponding values.
        /// <para>
        /// A return value of <c>null</c> is allowed and means that
        /// <c>reader</c> is already sorted.
        /// </para>
        /// <para>
        /// <b>NOTE:</b> deleted documents are expected to appear in the mapping as
        /// well, they will however be marked as deleted in the sorted view.
        /// </para>
        /// </summary>
        internal DocMap Sort(AtomicReader reader)
        {
            SortField[] fields = sort_Renamed.GetSort();
            int[] reverseMul = new int[fields.Length];

            FieldComparer[] comparers = new FieldComparer[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                reverseMul[i] = fields[i].IsReverse ? -1 : 1;
                comparers[i] = fields[i].GetComparer(1, i);
                comparers[i].SetNextReader(reader.AtomicContext);
                comparers[i].SetScorer(FAKESCORER);
            }
            DocComparer comparer = new DocComparerAnonymousInnerClassHelper(this, reverseMul, comparers);
            return Sort(reader.MaxDoc, comparer);
        }

        private class DocComparerAnonymousInnerClassHelper : DocComparer
        {
            private readonly Sorter outerInstance;

            private int[] reverseMul;
            private FieldComparer[] comparers;

            public DocComparerAnonymousInnerClassHelper(Sorter outerInstance, int[] reverseMul, FieldComparer[] comparers)
            {
                this.outerInstance = outerInstance;
                this.reverseMul = reverseMul;
                this.comparers = comparers;
            }

            public override int Compare(int docID1, int docID2)
            {
                try
                {
                    for (int i = 0; i < comparers.Length; i++)
                    {
                        // TODO: would be better if copy() didnt cause a term lookup in TermOrdVal & co,
                        // the segments are always the same here...
                        comparers[i].Copy(0, docID1);
                        comparers[i].SetBottom(0);
                        int comp = reverseMul[i] * comparers[i].CompareBottom(docID2);
                        if (comp != 0)
                        {
                            return comp;
                        }
                    }
                    return docID1.CompareTo(docID2); // docid order tiebreak
                }
                catch (IOException e)
                {
                    throw new Exception(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Returns the identifier of this <see cref="Sorter"/>.
        /// <para>This identifier is similar to <see cref="object.GetHashCode()"/> and should be
        /// chosen so that two instances of this class that sort documents likewise
        /// will have the same identifier. On the contrary, this identifier should be
        /// different on different <see cref="Sort">sorts</see>.
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

            public override float GetScore()
            {
                throw new System.NotSupportedException();
            }

            public override int Freq
            {
                get { throw new System.NotSupportedException(); }
            }
            public override int DocID
            {
                get { throw new System.NotSupportedException(); }
            }

            public override int NextDoc()
            {
                throw new System.NotSupportedException();
            }

            public override int Advance(int target)
            {
                throw new System.NotSupportedException();
            }
            public override long Cost()
            {
                throw new System.NotSupportedException();
            }
        }
    }
}