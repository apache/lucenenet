using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;

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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments.
    ///
    /// @lucene.experimental
    /// </summary>

    public sealed class MultiDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private readonly MultiTermsEnum Parent;
        internal readonly DocsAndPositionsEnum[] SubDocsAndPositionsEnum;
        private EnumWithSlice[] Subs_Renamed;
        internal int NumSubs_Renamed;
        internal int Upto;
        internal DocsAndPositionsEnum Current;
        internal int CurrentBase;
        internal int Doc = -1;

        /// <summary>
        /// Sole constructor. </summary>
        public MultiDocsAndPositionsEnum(MultiTermsEnum parent, int subReaderCount)
        {
            this.Parent = parent;
            SubDocsAndPositionsEnum = new DocsAndPositionsEnum[subReaderCount];
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
        /// Rre-use and reset this instance on the provided slices. </summary>
        public MultiDocsAndPositionsEnum Reset(EnumWithSlice[] subs, int numSubs)
        {
            this.NumSubs_Renamed = numSubs;
            this.Subs_Renamed = new EnumWithSlice[subs.Length];
            for (int i = 0; i < subs.Length; i++)
            {
                this.Subs_Renamed[i] = new EnumWithSlice();
                this.Subs_Renamed[i].DocsAndPositionsEnum = subs[i].DocsAndPositionsEnum;
                this.Subs_Renamed[i].Slice = subs[i].Slice;
            }
            Upto = -1;
            Doc = -1;
            Current = null;
            return this;
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
            Debug.Assert(Current != null);
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
                    Current = Subs_Renamed[Upto].DocsAndPositionsEnum;
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
                        Current = Subs_Renamed[Upto].DocsAndPositionsEnum;
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

        public override int NextPosition()
        {
            return Current.NextPosition();
        }

        public override int StartOffset()
        {
            return Current.StartOffset();
        }

        public override int EndOffset()
        {
            return Current.EndOffset();
        }

        public override BytesRef Payload
        {
            get
            {
                return Current.Payload;
            }
        }

        // TODO: implement bulk read more efficiently than super
        /// <summary>
        /// Holds a <seealso cref="DocsAndPositionsEnum"/> along with the
        ///  corresponding <seealso cref="ReaderSlice"/>.
        /// </summary>
        public sealed class EnumWithSlice
        {
            internal EnumWithSlice()
            {
            }

            /// <summary>
            /// <seealso cref="DocsAndPositionsEnum"/> for this sub-reader. </summary>
            public DocsAndPositionsEnum DocsAndPositionsEnum;

            /// <summary>
            /// <seealso cref="ReaderSlice"/> describing how this sub-reader
            ///  fits into the composite reader.
            /// </summary>
            public ReaderSlice Slice;

            public override string ToString()
            {
                return Slice.ToString() + ":" + DocsAndPositionsEnum;
            }
        }

        public override long Cost()
        {
            long cost = 0;
            for (int i = 0; i < NumSubs_Renamed; i++)
            {
                cost += Subs_Renamed[i].DocsAndPositionsEnum.Cost();
            }
            return cost;
        }

        public override string ToString()
        {
            return "MultiDocsAndPositionsEnum(" + Arrays.ToString(Subs) + ")";
        }
    }
}