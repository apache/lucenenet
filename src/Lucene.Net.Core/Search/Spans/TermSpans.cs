using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search.Spans
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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Expert:
    /// Public for extension only
    /// </summary>
    public class TermSpans : Spans
    {
        protected readonly DocsAndPositionsEnum Postings_Renamed; // LUCENENET TODO: rename
        protected readonly Term Term; // LUCENENET TODO: rename
        protected int Doc_Renamed; // LUCENENET TODO: rename
        protected int Freq; // LUCENENET TODO: rename
        protected int Count; // LUCENENET TODO: rename
        protected int Position; // LUCENENET TODO: rename
        protected bool ReadPayload; // LUCENENET TODO: rename

        public TermSpans(DocsAndPositionsEnum postings, Term term)
        {
            this.Postings_Renamed = postings;
            this.Term = term;
            Doc_Renamed = -1;
        }

        // only for EmptyTermSpans (below)
        internal TermSpans()
        {
            Term = null;
            Postings_Renamed = null;
        }

        public override bool Next()
        {
            if (Count == Freq)
            {
                if (Postings_Renamed == null)
                {
                    return false;
                }
                Doc_Renamed = Postings_Renamed.NextDoc();
                if (Doc_Renamed == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return false;
                }
                Freq = Postings_Renamed.Freq;
                Count = 0;
            }
            Position = Postings_Renamed.NextPosition();
            Count++;
            ReadPayload = false;
            return true;
        }

        public override bool SkipTo(int target)
        {
            Debug.Assert(target > Doc_Renamed);
            Doc_Renamed = Postings_Renamed.Advance(target);
            if (Doc_Renamed == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }

            Freq = Postings_Renamed.Freq;
            Count = 0;
            Position = Postings_Renamed.NextPosition();
            Count++;
            ReadPayload = false;
            return true;
        }

        public override int Doc()
        {
            return Doc_Renamed;
        }

        public override int Start()
        {
            return Position;
        }

        public override int End()
        {
            return Position + 1;
        }

        public override long Cost()
        {
            return Postings_Renamed.Cost();
        }

        // TODO: Remove warning after API has been finalized
        public override ICollection<byte[]> Payload
        {
            get
            {
                var payload = Postings_Renamed.Payload;
                ReadPayload = true;
                byte[] bytes;
                if (payload != null)
                {
                    bytes = new byte[payload.Length];
                    Array.Copy(payload.Bytes, payload.Offset, bytes, 0, payload.Length);
                }
                else
                {
                    bytes = null;
                }
                //LUCENE TO-DO
                return new[] { bytes };
                //return Collections.singletonList(bytes);
            }
        }

        // TODO: Remove warning after API has been finalized
        public override bool PayloadAvailable
        {
            get
            {
                return ReadPayload == false && Postings_Renamed.Payload != null;
            }
        }

        public override string ToString()
        {
            return "spans(" + Term.ToString() + ")@" + (Doc_Renamed == -1 ? "START" : (Doc_Renamed == int.MaxValue) ? "END" : Doc_Renamed + "-" + Position);
        }

        public virtual DocsAndPositionsEnum Postings
        {
            get
            {
                return Postings_Renamed;
            }
        }

        private sealed class EmptyTermSpans : TermSpans
        {
            public override bool Next()
            {
                return false;
            }

            public override bool SkipTo(int target)
            {
                return false;
            }

            public override int Doc()
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Start()
            {
                return -1;
            }

            public override int End()
            {
                return -1;
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    return null;
                }
            }

            public override bool PayloadAvailable
            {
                get
                {
                    return false;
                }
            }

            public override long Cost()
            {
                return 0;
            }
        }

        public static readonly TermSpans EMPTY_TERM_SPANS = new EmptyTermSpans();
    }
}