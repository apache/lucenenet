using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Spell
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
    /// Interface for enumerating term,weight pairs.
    /// </summary>
    public interface ITermFreqEnumerator : IBytesRefEnumerator
    {

        /// <summary>
        /// Gets the term's weight, higher numbers mean better suggestions.
        /// </summary>
        long Weight { get; }
    }

    /// <summary>
    /// Wraps a <see cref="BytesRefEnumerator"/> as a <see cref="ITermFreqEnumerator"/>, with all weights
    /// set to <c>1</c>.
    /// </summary>
    public class TermFreqEnumeratorWrapper : ITermFreqEnumerator
    {
        internal IBytesRefEnumerator wrapped;

        /// <summary>
        /// Creates a new wrapper, wrapping the specified iterator and 
        /// specifying a weight value of <code>1</code> for all terms.
        /// </summary>
        public TermFreqEnumeratorWrapper(IBytesRefEnumerator wrapped)
        {
            this.wrapped = wrapped;
        }

        public virtual long Weight => 1;

        public BytesRef Current => wrapped.Current;

        public bool MoveNext() => wrapped.MoveNext();

        public virtual IComparer<BytesRef> Comparer => wrapped.Comparer;
    }
}