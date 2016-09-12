using Lucene.Net.Support;
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
    /// A <seealso cref="InputIterator"/> over a sequence of <seealso cref="Input"/>s.
    /// </summary>
    public sealed class InputArrayIterator : InputIterator
    {
        private readonly IEnumerator<Input> i;
        private readonly bool hasPayloads;
        private readonly bool hasContexts;
        private bool first;
        private Input current;
        private readonly BytesRef spare = new BytesRef();

        public InputArrayIterator(IEnumerator<Input> i)
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

        public InputArrayIterator(Input[] i)
            : this(Arrays.AsList(i))
        {
        }
        public InputArrayIterator(IEnumerable<Input> i)
            : this(i.GetEnumerator())
        {
        }

        public long Weight
        {
            get { return current.v; }
        }

        public BytesRef Next()
        {
            if (i.MoveNext() || (first && current != null))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    current = i.Current;
                }
                spare.CopyBytes(current.term);
                return spare;
            }

            //if (i.hasNext() || (first && current != null))
            //{
            //    if (first)
            //    {
            //        first = false;
            //    }
            //    else
            //    {
            //        current = i.next();
            //    }
            //    spare.copyBytes(current.term);
            //    return spare;
            //}
            return null;
        }

        public BytesRef Payload
        {
            get { return current.payload; }
        }

        public bool HasPayloads
        {
            get { return hasPayloads; }
        }

        public IComparer<BytesRef> Comparator
        {
            get { return null; }
        }

        public IEnumerable<BytesRef> Contexts
        {
            get { return current.contexts; }
        }

        public bool HasContexts
        {
            get { return hasContexts; }
        }
    }
}
