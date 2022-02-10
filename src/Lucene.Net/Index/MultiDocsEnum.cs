using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Index
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
    /// Exposes <see cref="DocsEnum"/>, merged from <see cref="DocsEnum"/>
    /// API of sub-segments.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public sealed class MultiDocsEnum : DocsEnum
    {
        private readonly MultiTermsEnum parent;
        internal readonly DocsEnum[] subDocsEnum;
        private EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal DocsEnum current;
        internal int currentBase;
        internal int doc = -1;

        /// <summary>
        /// Sole constructor </summary>
        /// <param name="parent"> The <see cref="MultiTermsEnum"/> that created us. </param>
        /// <param name="subReaderCount"> How many sub-readers are being merged.  </param>
        public MultiDocsEnum(MultiTermsEnum parent, int subReaderCount)
        {
            this.parent = parent;
            subDocsEnum = new DocsEnum[subReaderCount];
        }

        internal MultiDocsEnum Reset(EnumWithSlice[] subs, int numSubs)
        {
            this.numSubs = numSubs;

            this.subs = new EnumWithSlice[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                this.subs[i] = new EnumWithSlice();
                this.subs[i].DocsEnum = subs[i].DocsEnum;
                this.subs[i].Slice = subs[i].Slice;
            }
            upto = -1;
            doc = -1;
            current = null;
            return this;
        }

        /// <summary>
        /// Returns <c>true</c> if this instance can be reused by
        /// the provided <see cref="MultiTermsEnum"/>.
        /// </summary>
        public bool CanReuse(MultiTermsEnum parent)
        {
            return this.parent == parent;
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        /// <seealso cref="Subs"/>
        public int NumSubs => numSubs;

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public EnumWithSlice[] Subs => subs;

        public override int Freq => current.Freq;

        public override int DocID => doc;

        public override int Advance(int target)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(target > doc);
            while (true)
            {
                if (current != null)
                {
                    int doc;
                    if (target < currentBase)
                    {
                        // target was in the previous slice but there was no matching doc after it
                        doc = current.NextDoc();
                    }
                    else
                    {
                        doc = current.Advance(target - currentBase);
                    }
                    if (doc == NO_MORE_DOCS)
                    {
                        current = null;
                    }
                    else
                    {
                        return this.doc = doc + currentBase;
                    }
                }
                else if (upto == numSubs - 1)
                {
                    return this.doc = NO_MORE_DOCS;
                }
                else
                {
                    upto++;
                    current = subs[upto].DocsEnum;
                    currentBase = subs[upto].Slice.Start;
                }
            }
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (current is null)
                {
                    if (upto == numSubs - 1)
                    {
                        return this.doc = NO_MORE_DOCS;
                    }
                    else
                    {
                        upto++;
                        current = subs[upto].DocsEnum;
                        currentBase = subs[upto].Slice.Start;
                    }
                }

                int doc = current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    return this.doc = currentBase + doc;
                }
                else
                {
                    current = null;
                }
            }
        }

        public override long GetCost()
        {
            long cost = 0;
            for (int i = 0; i < numSubs; i++)
            {
                cost += subs[i].DocsEnum.GetCost();
            }
            return cost;
        }

        // TODO: implement bulk read more efficiently than super
        /// <summary>
        /// Holds a <see cref="Index.DocsEnum"/> along with the
        /// corresponding <see cref="ReaderSlice"/>.
        /// </summary>
        public sealed class EnumWithSlice
        {
            internal EnumWithSlice()
            {
            }

            /// <summary>
            /// <see cref="Index.DocsEnum"/> of this sub-reader. </summary>
            public DocsEnum DocsEnum { get; internal set; } // LUCENENET NOTE: Made setter internal because ctor is internal

            /// <summary>
            /// <see cref="ReaderSlice"/> describing how this sub-reader
            /// fits into the composite reader.
            /// </summary>
            public ReaderSlice Slice { get; internal set; } // LUCENENET NOTE: Made setter internal because ctor is internal

            public override string ToString()
            {
                return Slice.ToString() + ":" + DocsEnum;
            }
        }

        public override string ToString()
        {
            return "MultiDocsEnum(" + Arrays.ToString(Subs) + ")";
        }
    }
}