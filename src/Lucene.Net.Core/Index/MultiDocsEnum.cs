using Lucene.Net.Support;
using System.Diagnostics;

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
    /// Exposes <seealso cref="DocsEnum"/>, merged from <seealso cref="DocsEnum"/>
    /// API of sub-segments.
    ///
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
        /// <param name="parent"> The <seealso cref="MultiTermsEnum"/> that created us. </param>
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
        /// Returns {@code true} if this instance can be reused by
        ///  the provided <seealso cref="MultiTermsEnum"/>.
        /// </summary>
        public bool CanReuse(MultiTermsEnum parent)
        {
            return this.parent == parent;
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        ///  <seealso cref= #getSubs  </seealso>
        public int NumSubs
        {
            get
            {
                return numSubs;
            }
        }

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        public EnumWithSlice[] Subs // LUCENENET TODO: Make method GetSubs() (property returning array)
        {
            get
            {
                return subs;
            }
        }

        public override int Freq
        {
            get { return current.Freq; }
        }

        public override int DocID()
        {
            return doc;
        }

        public override int Advance(int target)
        {
            Debug.Assert(target > doc);
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
                if (current == null)
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

        public override long Cost()
        {
            long cost = 0;
            for (int i = 0; i < numSubs; i++)
            {
                cost += subs[i].DocsEnum.Cost();
            }
            return cost;
        }

        // TODO: implement bulk read more efficiently than super
        /// <summary>
        /// Holds a <seealso cref="DocsEnum"/> along with the
        ///  corresponding <seealso cref="ReaderSlice"/>.
        /// </summary>
        public sealed class EnumWithSlice
        {
            internal EnumWithSlice()
            {
            }

            /// <summary>
            /// <seealso cref="DocsEnum"/> of this sub-reader. </summary>
            public DocsEnum DocsEnum; // LUCENENET TODO: Make property

            /// <summary>
            /// <seealso cref="ReaderSlice"/> describing how this sub-reader
            ///  fits into the composite reader.
            /// </summary>
            public ReaderSlice Slice; // LUCENENET TODO: Make property

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