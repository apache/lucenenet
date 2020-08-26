using Lucene.Net.Util;
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
    /// corresponds to <seealso cref="InputEnumerator"/>'s entries
    /// </summary>
    public sealed class Input
    {
        public readonly BytesRef term;
        public readonly long v;
        public readonly BytesRef payload;
        public readonly bool hasPayloads;
        public readonly ICollection<BytesRef> contexts;
        public readonly bool hasContexts;

        public Input(BytesRef term, long v, BytesRef payload)
                  : this(term, v, payload, true, null, false)
        {
        }

        public Input(string term, long v, BytesRef payload)
            : this(new BytesRef(term), v, payload)
        {
        }

        public Input(BytesRef term, long v, ICollection<BytesRef> contexts)
            : this(term, v, null, false, contexts, true)
        {
        }

        public Input(string term, long v, ICollection<BytesRef> contexts)
            : this(new BytesRef(term), v, null, false, contexts, true)
        {
        }

        public Input(BytesRef term, long v)
            : this(term, v, null, false, null, false)
        {
        }

        public Input(string term, long v)
            : this(new BytesRef(term), v, null, false, null, false)
        {
        }

        public Input(string term, int v, BytesRef payload, ICollection<BytesRef> contexts)
            : this(new BytesRef(term), v, payload, true, contexts, true)
        {
        }

        public Input(BytesRef term, long v, BytesRef payload, ICollection<BytesRef> contexts)
            : this(term, v, payload, true, contexts, true)
        {
        }



        public Input(BytesRef term, long v, BytesRef payload, bool hasPayloads, ICollection<BytesRef> contexts,
            bool hasContexts)
        {
            this.term = term;
            this.v = v;
            this.payload = payload;
            this.hasPayloads = hasPayloads;
            this.contexts = contexts;
            this.hasContexts = hasContexts;
        }

        public bool HasContexts => hasContexts;

        public bool HasPayloads => hasPayloads;
    }
}
