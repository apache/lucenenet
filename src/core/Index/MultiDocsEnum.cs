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
        private readonly MultiTermsEnum Parent;
        internal readonly DocsEnum[] SubDocsEnum;
        private EnumWithSlice[] Subs_Renamed;
        internal int NumSubs_Renamed;
        internal int Upto;
        internal DocsEnum Current;
        internal int CurrentBase;
        internal int Doc = -1;

        /// <summary>
        /// Sole constructor </summary>
        /// <param name="parent"> The <seealso cref="MultiTermsEnum"/> that created us. </param>
        /// <param name="subReaderCount"> How many sub-readers are being merged.  </param>
        public MultiDocsEnum(MultiTermsEnum parent, int subReaderCount)
        {
            this.Parent = parent;
            SubDocsEnum = new DocsEnum[subReaderCount];
        }

        internal MultiDocsEnum Reset(EnumWithSlice[] subs, int numSubs)
        {
            this.NumSubs_Renamed = numSubs;

            this.Subs_Renamed = new EnumWithSlice[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                this.Subs_Renamed[i] = new EnumWithSlice();
                this.Subs_Renamed[i].DocsEnum = subs[i].DocsEnum;
                this.Subs_Renamed[i].Slice = subs[i].Slice;
            }
            Upto = -1;
            Doc = -1;
            Current = null;
            return this;
        }

        /// <summary>
        /// Returns {@code true} if this instance can be reused by
        ///  the provided <seealso cref="MultiTermsEnum"/>.
        /// </summary>
        public bool CanReuse(MultiTermsEnum parent)
        {
            return this.Parent == parent;
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        ///  <seealso cref= #getSubs  </seealso>
        public int NumSubs
        {
            get
            {
                return NumSubs_Renamed;
            }
        }

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        public EnumWithSlice[] Subs
        {
            get
            {
                return Subs_Renamed;
            }
        }

        public override int Freq()
        {
            return Current.Freq();
        }

        public override int DocID()
        {
            return Doc;
        }

        public override int Advance(int target)
        {
            Debug.Assert(target > Doc);
            while (true)
            {
                if (Current != null)
                {
                    int doc;
                    if (target < CurrentBase)
                    {
                        // target was in the previous slice but there was no matching doc after it
                        doc = Current.NextDoc();
                    }
                    else
                    {
                        doc = Current.Advance(target - CurrentBase);
                    }
                    if (doc == NO_MORE_DOCS)
                    {
                        Current = null;
                    }
                    else
                    {
                        return this.Doc = doc + CurrentBase;
                    }
                }
                else if (Upto == NumSubs_Renamed - 1)
                {
                    return this.Doc = NO_MORE_DOCS;
                }
                else
                {
                    Upto++;
                    Current = Subs_Renamed[Upto].DocsEnum;
                    CurrentBase = Subs_Renamed[Upto].Slice.Start;
                }
            }
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (Current == null)
                {
                    if (Upto == NumSubs_Renamed - 1)
                    {
                        return this.Doc = NO_MORE_DOCS;
                    }
                    else
                    {
                        Upto++;
                        Current = Subs_Renamed[Upto].DocsEnum;
                        CurrentBase = Subs_Renamed[Upto].Slice.Start;
                    }
                }

                int doc = Current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    return this.Doc = CurrentBase + doc;
                }
                else
                {
                    Current = null;
                }
            }
        }

        public override long Cost()
        {
            long cost = 0;
            for (int i = 0; i < NumSubs_Renamed; i++)
            {
                cost += Subs_Renamed[i].DocsEnum.Cost();
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
            public DocsEnum DocsEnum;

            /// <summary>
            /// <seealso cref="ReaderSlice"/> describing how this sub-reader
            ///  fits into the composite reader.
            /// </summary>
            public ReaderSlice Slice;

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