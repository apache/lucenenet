/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lucene.Net.Index;

namespace Lucene.Net.Search.Spans
{

    /// <summary> Expert:
    /// Public for extension only
    /// </summary>
    public class TermSpans : Spans
    {
        protected readonly DocsAndPositionsEnum postings;
        protected readonly Term term;
        protected int doc;
        protected int freq;
        protected int count;
        protected int position;
        protected bool readPayload;


        public TermSpans(DocsAndPositionsEnum postings, Term term)
        {
            this.postings = postings;
            this.term = term;
            doc = -1;
        }

        public TermSpans()
        {
            term = null;
            postings = null;
        }

        public override bool Next()
        {
            if (count == freq)
            {
                if (postings == null)
                {
                    return false;
                }
                doc = postings.NextDoc();
                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return false;
                }
                freq = postings.Freq;
                count = 0;
            }
            position = postings.NextPosition();
            count++;
            readPayload = false;
            return true;
        }

        public override bool SkipTo(int target)
        {
            doc = postings.Advance(target);
            if (doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }

            freq = postings.NextPosition();
            count++;
            readPayload = false;
            return true;
        }

        public override int Doc
        {
            get { return doc; }
        }

        public override int Start
        {
            get { return position; }
        }

        public override int End
        {
            get { return position + 1; }
        }

        public override long Cost
        {
            get { return postings.Cost; }
        }

        // TODO: Remove warning after API has been finalized

        public override ICollection<sbyte[]> GetPayload()
        {
            var payload = postings.Payload;
            readPayload = true;
            sbyte[] bytes;
            if (payload != null)
            {
                bytes = new sbyte[payload.length];
                Array.Copy(payload.bytes, payload.offset, bytes, 0, payload.length);
            }
            else
            {
                bytes = null;
            }
            return new ReadOnlyCollection<sbyte[]>(new List<sbyte[]> { bytes });
        }

        // TODO: Remove warning after API has been finalized

        public override bool IsPayloadAvailable()
        {
            return readPayload == false && postings.Payload != null;
        }

        public override string ToString()
        {
            return "spans(" + term.ToString() + ")@" + (doc == -1 ? "START" : ((doc == int.MaxValue) ? "END" : doc + "-" + position));
        }

        public virtual DocsAndPositionsEnum Postings
        {
            get { return postings; }
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

            public override int Doc
            {
                get { return DocIdSetIterator.NO_MORE_DOCS; }
            }

            public override int Start
            {
                get { return -1; }
            }

            public override int End
            {
                get { return -1; }
            }

            public override ICollection<sbyte[]> GetPayload()
            {
                return null;
            }

            public override bool IsPayloadAvailable()
            {
                return false;
            }

            public override long Cost
            {
                get { return 0; }
            }

            private static readonly TermSpans EMPTY_TERM_SPANS = new EmptyTermSpans();
        }
    }
}