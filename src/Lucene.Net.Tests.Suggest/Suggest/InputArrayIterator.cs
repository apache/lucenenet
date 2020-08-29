using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Suggest
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
    /// A <seealso cref="InputEnumerator"/> over a sequence of <seealso cref="Input"/>s.
    /// </summary>
    public sealed class InputArrayEnumerator : IInputEnumerator
    {
        private readonly IEnumerator<Input> i;
        private readonly bool hasPayloads;
        private readonly bool hasContexts;
        private bool first;
        private Input current;
        private readonly BytesRef spare = new BytesRef();

        public InputArrayEnumerator(IEnumerator<Input> i)
        {
            this.i = i;
            if (i.MoveNext())
            {
                current = i.Current;
                first = true;
                this.hasPayloads = current.hasPayloads;
                this.hasContexts = current.hasContexts;
            }
            else
            {
                this.hasPayloads = false;
                this.hasContexts = false;
            }
        }

        public InputArrayEnumerator(Input[] i)
            : this((IEnumerable<Input>)i)
        {
        }
        public InputArrayEnumerator(IEnumerable<Input> i)
            : this(i.GetEnumerator())
        {
        }

        public long Weight => current.v;

        public BytesRef Current { get; private set; }

        public bool MoveNext()
        {
            // LUCENENET NOTE: We moved the cursor when 
            // the instance was created. Make sure we don't
            // move it again until the second call to Next().
            if (first && current != null)
            {
                first = false;
            }
            else if (i.MoveNext())
            {
                current = i.Current;
            }
            else
            {
                Current = null;
                return false;
            }

            spare.CopyBytes(current.term);
            Current = spare;
            return true;
        }

        public BytesRef Payload => current.payload;

        public bool HasPayloads => hasPayloads;

        public IComparer<BytesRef> Comparer => null;

        public ICollection<BytesRef> Contexts => current.contexts;

        public bool HasContexts => hasContexts;
    }
}
