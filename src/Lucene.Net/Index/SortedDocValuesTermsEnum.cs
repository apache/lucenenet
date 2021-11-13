using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Implements a <see cref="TermsEnum"/> wrapping a provided
    /// <see cref="SortedDocValues"/>.
    /// </summary>

    internal class SortedDocValuesTermsEnum : TermsEnum
    {
        private readonly SortedDocValues values;
        private int currentOrd = -1;
        private readonly BytesRef term = new BytesRef();

        /// <summary>
        /// Creates a new <see cref="TermsEnum"/> over the provided values </summary>
        public SortedDocValuesTermsEnum(SortedDocValues values)
        {
            this.values = values;
        }

        public override SeekStatus SeekCeil(BytesRef text)
        {
            int ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                currentOrd = ord;
                term.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.Bytes = new byte[text.Length];
                term.CopyBytes(text);
                return SeekStatus.FOUND;
            }
            else
            {
                currentOrd = -ord - 1;
                if (currentOrd == values.ValueCount)
                {
                    return SeekStatus.END;
                }
                else
                {
                    // TODO: hmm can we avoid this "extra" lookup?:
                    values.LookupOrd(currentOrd, term);
                    return SeekStatus.NOT_FOUND;
                }
            }
        }

        public override bool SeekExact(BytesRef text)
        {
            int ord = values.LookupTerm(text);
            if (ord >= 0)
            {
                term.Offset = 0;
                // TODO: is there a cleaner way?
                // term.bytes may be pointing to codec-private byte[]
                // storage, so we must force new byte[] allocation:
                term.Bytes = new byte[text.Length];
                term.CopyBytes(text);
                currentOrd = ord;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void SeekExact(long ord)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(ord >= 0 && ord < values.ValueCount);
            currentOrd = (int)ord;
            values.LookupOrd(currentOrd, term);
        }

        public override bool MoveNext()
        {
            currentOrd++;
            if (currentOrd >= values.ValueCount)
            {
                return false;
            }
            values.LookupOrd(currentOrd, term);
            return term != null;
        }

        [Obsolete("Use MoveNext() and Term instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override BytesRef Next()
        {
            if (MoveNext())
                return term;
            return null;
        }

        public override BytesRef Term => term;

        public override long Ord => currentOrd;

        public override int DocFreq => throw UnsupportedOperationException.Create();

        public override long TotalTermFreq => -1;

        public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, DocsFlags flags)
        {
            throw UnsupportedOperationException.Create();
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            throw UnsupportedOperationException.Create();
        }

        public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

        public override void SeekExact(BytesRef term, TermState state)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(state != null && state is OrdTermState);
            this.SeekExact(((OrdTermState)state).Ord);
        }

        public override TermState GetTermState()
        {
            OrdTermState state = new OrdTermState();
            state.Ord = currentOrd;
            return state;
        }
    }
}