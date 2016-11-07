using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping
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
    /// This collector specializes in collecting the most relevant document (group head) for each group that match the query.
    /// 
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="GH"></typeparam>
    public abstract class AbstractAllGroupHeadsCollector<GH> : AbstractAllGroupHeadsCollector
        where GH : AbstractAllGroupHeadsCollector_GroupHead
    {
        protected readonly int[] reversed;
        protected readonly int compIDXEnd;
        protected readonly TemporalResult temporalResult;

        protected AbstractAllGroupHeadsCollector(int numberOfSorts)
        {
            this.reversed = new int[numberOfSorts];
            this.compIDXEnd = numberOfSorts - 1;
            temporalResult = new TemporalResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxDoc">The maxDoc of the top level <see cref="Index.IndexReader"/></param>
        /// <returns>a <see cref="FixedBitSet"/> containing all group heads.</returns>
        public override FixedBitSet RetrieveGroupHeads(int maxDoc)
        {
            FixedBitSet bitSet = new FixedBitSet(maxDoc);

            ICollection<GH> groupHeads = CollectedGroupHeads;
            foreach (GH groupHead in groupHeads)
            {
                bitSet.Set(groupHead.Doc);
            }

            return bitSet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>an int array containing all group heads. The size of the array is equal to number of collected unique groups.</returns>
        public override int[] RetrieveGroupHeads()
        {
            ICollection<GH> groupHeads = CollectedGroupHeads;
            int[] docHeads = new int[groupHeads.Count];

            int i = 0;
            foreach (GH groupHead in groupHeads)
            {
                docHeads[i++] = groupHead.Doc;
            }

            return docHeads;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the number of group heads found for a query.</returns>
        public override int GroupHeadsSize
        {
            get
            {
                return CollectedGroupHeads.Count;
            }
        }

        /// <summary>
        /// Returns the group head and puts it into <see cref="TemporalResult"/>.
        /// If the group head wasn't encountered before then it will be added to the collected group heads.
        /// <para>
        /// The <see cref="TemporalResult.stop"/> property will be <c>true</c> if the group head wasn't encountered before
        /// otherwise <c>false</c>.
        /// </para>
        /// </summary>
        /// <param name="doc">The document to retrieve the group head for.</param>
        /// <exception cref="IOException">If I/O related errors occur</exception>
        protected override abstract void RetrieveGroupHeadAndAddIfNotExist(int doc);

        /// <summary>
        /// Returns the collected group heads.
        /// Subsequent calls should return the same group heads.
        /// </summary>
        /// <returns>the collected group heads</returns>
        protected abstract ICollection<GH> CollectedGroupHeads { get; }

        public override void Collect(int doc)
        {
            RetrieveGroupHeadAndAddIfNotExist(doc);
            if (temporalResult.stop)
            {
                return;
            }
            GH groupHead = temporalResult.groupHead;

            // Ok now we need to check if the current doc is more relevant then current doc for this group
            for (int compIDX = 0; ; compIDX++)
            {
                int c = reversed[compIDX] * groupHead.Compare(compIDX, doc);
                if (c < 0)
                {
                    // Definitely not competitive. So don't even bother to continue
                    return;
                }
                else if (c > 0)
                {
                    // Definitely competitive.
                    break;
                }
                else if (compIDX == compIDXEnd)
                {
                    // Here c=0. If we're at the last comparator, this doc is not
                    // competitive, since docs are visited in doc Id order, which means
                    // this doc cannot compete with any other document in the queue.
                    return;
                }
            }
            groupHead.UpdateDocHead(doc);
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return false;
        }

        /// <summary>
        /// Contains the result of group head retrieval.
        /// To prevent new object creations of this class for every collect.
        /// </summary>
        protected class TemporalResult
        {
            public GH groupHead;
            public bool stop;
        }
    }

    /// <summary>
    /// Represents a group head. A group head is the most relevant document for a particular group.
    /// The relevancy is based is usually based on the sort.
    /// <para>
    /// The group head contains a group value with its associated most relevant document id.
    /// </para>
    /// </summary>
    /// <remarks>
    /// LUCENENET: moved this class from being a nested class of <see cref="AbstractAllGroupHeadsCollector{TGroupValue}"/>,
    /// made it non-generic so the generic closing type doesn't need to be specified in classes that
    /// use <see cref="AbstractAllGroupHeadsCollector_GroupHead"/> as a generic closing type, and renamed 
    /// it from GroupHead to <see cref="AbstractAllGroupHeadsCollector_GroupHead"/> to avoid naming conflicts with nested classes 
    /// named GroupHead in derived classes of <see cref="AbstractAllGroupHeadsCollector"/>.
    /// </remarks>
    public abstract class AbstractAllGroupHeadsCollector_GroupHead /*<TGroupValue>*/
    {

        //public readonly TGroupValue groupValue;
        public int Doc { get; protected set; }

        protected AbstractAllGroupHeadsCollector_GroupHead(/*TGroupValue groupValue,*/ int doc)
        {
            //this.groupValue = groupValue;
            this.Doc = doc;
        }

        /// <summary>
        /// Compares the specified document for a specified comparator against the current most relevant document.
        /// </summary>
        /// <param name="compIDX">The comparator index of the specified comparator.</param>
        /// <param name="doc">The specified document.</param>
        /// <returns>
        /// -1 if the specified document wasn't competitive against the current most relevant document, 1 if the
        /// specified document was competitive against the current most relevant document. Otherwise 0.
        /// </returns>
        /// <exception cref="IOException">If I/O related errors occur</exception>
        public abstract int Compare(int compIDX, int doc);

        /// <summary>
        /// Updates the current most relevant document with the specified document.
        /// </summary>
        /// <param name="doc">The specified document</param>
        /// <exception cref="IOException">If I/O related errors occur</exception>
        public abstract void UpdateDocHead(int doc);
    }

    /// <summary>
    /// LUCENENET specific class used to reference an 
    /// <see cref="AbstractAllGroupHeadsCollector{GH}"/> subclass
    /// without refering to its generic closing type.
    /// </summary>
    public abstract class AbstractAllGroupHeadsCollector : Collector
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxDoc">The maxDoc of the top level <see cref="Index.IndexReader"/></param>
        /// <returns>a <see cref="FixedBitSet"/> containing all group heads.</returns>
        public abstract FixedBitSet RetrieveGroupHeads(int maxDoc);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>an int array containing all group heads. The size of the array is equal to number of collected unique groups.</returns>
        public abstract int[] RetrieveGroupHeads();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the number of group heads found for a query.</returns>
        public abstract int GroupHeadsSize { get; }

        /// <summary>
        /// Returns the group head and puts it into <see cref="TemporalResult"/>.
        /// If the group head wasn't encountered before then it will be added to the collected group heads.
        /// <para>
        /// The <see cref="TemporalResult.stop"/> property will be <c>true</c> if the group head wasn't encountered before
        /// otherwise <c>false</c>.
        /// </para>
        /// </summary>
        /// <param name="doc">The document to retrieve the group head for.</param>
        /// <exception cref="IOException">If I/O related errors occur</exception>
        protected abstract void RetrieveGroupHeadAndAddIfNotExist(int doc);
    }
}
