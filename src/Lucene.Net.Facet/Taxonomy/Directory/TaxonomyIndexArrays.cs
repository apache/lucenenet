// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Lucene.Net.Facet.Taxonomy.Directory
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MultiFields = Lucene.Net.Index.MultiFields;

    /// <summary>
    /// A <see cref="ParallelTaxonomyArrays"/> that are initialized from the taxonomy
    /// index.
    /// 
    /// @lucene.experimental
    /// </summary>
    internal class TaxonomyIndexArrays : ParallelTaxonomyArrays
    {
        private readonly int[] parents;

        // the following two arrays are lazily intialized. note that we only keep a
        // single boolean member as volatile, instead of declaring the arrays
        // volatile. the code guarantees that only after the boolean is set to true,
        // the arrays are returned.
        private /*volatile*/ bool initializedChildren = false;
        private int[] children, siblings;
        private object syncLock = new object();

        /// <summary>
        /// Used by <see cref="Add(int, int)"/> after the array grew.
        /// </summary>
        private TaxonomyIndexArrays(int[] parents)
        {
            this.parents = parents;
        }

        public TaxonomyIndexArrays(IndexReader reader)
        {
            parents = new int[reader.MaxDoc];
            if (parents.Length > 0)
            {
                InitParents(reader, 0);
                // Starting Lucene 2.9, following the change LUCENE-1542, we can
                // no longer reliably read the parent "-1" (see comment in
                // LuceneTaxonomyWriter.SinglePositionTokenStream). We have no way
                // to fix this in indexing without breaking backward-compatibility
                // with existing indexes, so what we'll do instead is just
                // hard-code the parent of ordinal 0 to be -1, and assume (as is
                // indeed the case) that no other parent can be -1.
                parents[0] = TaxonomyReader.INVALID_ORDINAL;
            }
        }

        public TaxonomyIndexArrays(IndexReader reader, TaxonomyIndexArrays copyFrom)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(copyFrom != null);

            // note that copyParents.length may be equal to reader.maxDoc(). this is not a bug
            // it may be caused if e.g. the taxonomy segments were merged, and so an updated
            // NRT reader was obtained, even though nothing was changed. this is not very likely
            // to happen.
            int[] copyParents = copyFrom.Parents;
            this.parents = new int[reader.MaxDoc];
            Arrays.Copy(copyParents, 0, parents, 0, copyParents.Length);
            InitParents(reader, copyParents.Length);

            if (copyFrom.initializedChildren)
            {
                InitChildrenSiblings(copyFrom);
            }
        }

        private void InitChildrenSiblings(TaxonomyIndexArrays copyFrom)
        {
            if (!initializedChildren) // must do this check !
            {
                LazyInitializer.EnsureInitialized(ref children, ref initializedChildren, ref syncLock, () =>
                {
                    children = new int[parents.Length];
                    siblings = new int[parents.Length];
                    if (copyFrom != null)
                    {
                        // called from the ctor, after we know copyFrom has initialized children/siblings
                        Arrays.Copy(copyFrom.Children, 0, children, 0, copyFrom.Children.Length);
                        Arrays.Copy(copyFrom.Siblings, 0, siblings, 0, copyFrom.Siblings.Length);
                        ComputeChildrenSiblings(copyFrom.parents.Length);
                    }
                    else
                    {
                        ComputeChildrenSiblings(0);
                    }
                    return children;
                });
            }
        }

        private void ComputeChildrenSiblings(int first)
        {
            // reset the youngest child of all ordinals. while this should be done only
            // for the leaves, we don't know up front which are the leaves, so we reset
            // all of them.
            for (int i = first; i < parents.Length; i++)
            {
                children[i] = TaxonomyReader.INVALID_ORDINAL;
            }

            // the root category has no parent, and therefore no siblings
            if (first == 0)
            {
                first = 1;
                siblings[0] = TaxonomyReader.INVALID_ORDINAL;
            }

            for (int i = first; i < parents.Length; i++)
            {
                // note that parents[i] is always < i, so the right-hand-side of
                // the following line is already set when we get here
                siblings[i] = children[parents[i]];
                children[parents[i]] = i;
            }
        }

        /// <summary>
        /// Read the parents of the new categories
        /// </summary>
        private void InitParents(IndexReader reader, int first)
        {
            if (reader.MaxDoc == first)
            {
                return;
            }

            // it's ok to use MultiFields because we only iterate on one posting list.
            // breaking it to loop over the leaves() only complicates code for no
            // apparent gain.
            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, null, Consts.FIELD_PAYLOADS, Consts.PAYLOAD_PARENT_BYTES_REF, DocsAndPositionsFlags.PAYLOADS);

            // shouldn't really happen, if it does, something's wrong
            if (positions is null || positions.Advance(first) == DocIdSetIterator.NO_MORE_DOCS)
            {
                throw new CorruptIndexException("Missing parent data for category " + first);
            }

            int num = reader.MaxDoc;
            for (int i = first; i < num; i++)
            {
                if (positions.DocID == i)
                {
                    if (positions.Freq == 0) // shouldn't happen
                    {
                        throw new CorruptIndexException("Missing parent data for category " + i);
                    }

                    parents[i] = positions.NextPosition();

                    if (positions.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i + 1 < num)
                        {
                            throw new CorruptIndexException("Missing parent data for category " + (i + 1));
                        }
                        break;
                    }
                } // this shouldn't happen
                else
                {
                    throw new CorruptIndexException("Missing parent data for category " + i);
                }
            }
        }

        /// <summary>
        /// Adds the given ordinal/parent info and returns either a new instance if the
        /// underlying array had to grow, or this instance otherwise.
        /// <para>
        /// <b>NOTE:</b> you should call this method from a thread-safe code.
        /// </para>
        /// </summary>
        internal virtual TaxonomyIndexArrays Add(int ordinal, int parentOrdinal)
        {
            if (ordinal >= parents.Length)
            {
                int[] newarray = ArrayUtil.Grow(parents, ordinal + 1);
                newarray[ordinal] = parentOrdinal;
                return new TaxonomyIndexArrays(newarray);
            }
            parents[ordinal] = parentOrdinal;
            return this;
        }

        /// <summary>
        /// Returns the parents array, where <c>Parents[i]</c> denotes the parent of
        /// category ordinal <c>i</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public override int[] Parents => parents;

        /// <summary>
        /// Returns the children array, where <c>Children[i]</c> denotes the youngest
        /// child of category ordinal <c>i</c>. The youngest child is defined as the
        /// category that was added last to the taxonomy as an immediate child of
        /// <c>i</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public override int[] Children
        {
            get
            {
                if (!initializedChildren)
                {
                    InitChildrenSiblings(null);
                }

                // the array is guaranteed to be populated
                return children;
            }
        }

        /// <summary>
        /// Returns the siblings array, where <c>Siblings[i]</c> denotes the sibling
        /// of category ordinal <c>i</c>. The sibling is defined as the previous
        /// youngest child of <c>Parents[i]</c>.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public override int[] Siblings
        {
            get
            {
                if (!initializedChildren)
                {
                    InitChildrenSiblings(null);
                }

                // the array is guaranteed to be populated
                return siblings;
            }
        }
    }
}