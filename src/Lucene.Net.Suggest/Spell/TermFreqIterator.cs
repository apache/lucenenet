using Lucene.Net.Util;
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
    public interface ITermFreqIterator : IBytesRefIterator
    {

        /// <summary>
        /// Gets the term's weight, higher numbers mean better suggestions.
        /// </summary>
        long Weight { get; }
    }

    /// <summary>
    /// Wraps a BytesRefIterator as a TermFreqIterator, with all weights
    /// set to <code>1</code>
    /// </summary>
    public class TermFreqIteratorWrapper : ITermFreqIterator
    {
        internal IBytesRefIterator wrapped;

        /// <summary>
        /// Creates a new wrapper, wrapping the specified iterator and 
        /// specifying a weight value of <code>1</code> for all terms.
        /// </summary>
        public TermFreqIteratorWrapper(IBytesRefIterator wrapped)
        {
            this.wrapped = wrapped;
        }

        public virtual long Weight
        {
            get { return 1; }
        }

        public virtual BytesRef Next()
        {
            return wrapped.Next();
        }

        public virtual IComparer<BytesRef> Comparer
        {
            get { return wrapped.Comparer; }
        }
    }
}