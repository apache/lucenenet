using System.Collections.Generic;
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

    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Implements a <seealso cref="TermsEnum"/> wrapping a provided
    /// <seealso cref="SortedDocValues"/>.
    /// </summary>

    internal class SortedDocValuesTermsEnum : TermsEnum
    {
        private readonly SortedDocValues Values;
        private int CurrentOrd = -1;
        private readonly BytesRef Term_Renamed = new BytesRef();

        /// <summary>
        /// Creates a new TermsEnum over the provided values </summary>
        public SortedDocValuesTermsEnum(SortedDocValues values)
        {
            this.Values = values;
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            int ord = Values.LookupTerm(text);
            if (ord >= 0)
            {
                CurrentOrd = ord;
                Term_Renamed.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                Term_Renamed.Bytes = new byte[text.Length];
                Term_Renamed.CopyBytes(text);
                return SeekStatus.FOUND;
            }
            else
            {
                CurrentOrd = -ord - 1;
                if (CurrentOrd == Values.ValueCount)
                {
                    return SeekStatus.END;
                }
                else
                {
                    // TODO: hmm can we avoid this "extra" lookup?:
                    Values.LookupOrd(CurrentOrd, Term_Renamed);
                    return SeekStatus.NOT_FOUND;
                }
            }
        }

        public override bool SeekExact(BytesRef text)
        {
            int ord = Values.LookupTerm(text);
            if (ord >= 0)
            {
                Term_Renamed.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                Term_Renamed.Bytes = new byte[text.Length];
                Term_Renamed.CopyBytes(text);
                CurrentOrd = ord;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void SeekExact(long ord)
        {
            Debug.Assert(ord >= 0 && ord < Values.ValueCount);
            CurrentOrd = (int)ord;
            Values.LookupOrd(CurrentOrd, Term_Renamed);
        }

        public override BytesRef Next()
        {
            CurrentOrd++;
            if (CurrentOrd >= Values.ValueCount)
            {
                return null;
            }
            Values.LookupOrd(CurrentOrd, Term_Renamed);
            return Term_Renamed;
        }

        public override BytesRef Term()
        {
            return Term_Renamed;
        }

        public override long Ord()
        {
            return CurrentOrd;
        }

        public override int DocFreq()
        {
            throw new System.NotSupportedException();
        }

        public override long TotalTermFreq()
        {
            return -1;
        }

        public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
        {
            throw new System.NotSupportedException();
        }

        public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
        {
            throw new System.NotSupportedException();
        }

        public override IComparer<BytesRef> Comparator
        {
            get
            {
                return BytesRef.UTF8SortedAsUnicodeComparer;
            }
        }

        public override void SeekExact(BytesRef term, TermState state)
        {
            Debug.Assert(state != null && state is OrdTermState);
            this.SeekExact(((OrdTermState)state).ord);
        }

        public override TermState TermState()
        {
            OrdTermState state = new OrdTermState();
            state.ord = CurrentOrd;
            return state;
        }
    }
}