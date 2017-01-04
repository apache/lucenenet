using System.Collections.Generic;

namespace Lucene.Net.Analysis
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// this class can be used if the token attributes of a TokenStream
    /// are intended to be consumed more than once. It caches
    /// all token attribute states locally in a List.
    ///
    /// <P>CachingTokenFilter implements the optional method
    /// <seealso cref="TokenStream#reset()"/>, which repositions the
    /// stream to the first Token.
    /// </summary>
    public sealed class CachingTokenFilter : TokenFilter
    {
        private LinkedList<AttributeSource.State> cache = null;
        private IEnumerator<AttributeSource.State> iterator = null;
        private AttributeSource.State finalState;

        /// <summary>
        /// Create a new CachingTokenFilter around <code>input</code>,
        /// caching its token attributes, which can be replayed again
        /// after a call to <seealso cref="#reset()"/>.
        /// </summary>
        public CachingTokenFilter(TokenStream input)
            : base(input)
        {
        }

        public override bool IncrementToken()
        {
            if (cache == null)
            {
                // fill cache lazily
                cache = new LinkedList<AttributeSource.State>();
                FillCache();
                iterator = cache.GetEnumerator();
            }

            if (!iterator.MoveNext())
            {
                // the cache is exhausted, return false
                return false;
            }
            // Since the TokenFilter can be reset, the tokens need to be preserved as immutable.
            RestoreState(iterator.Current);
            return true;
        }

        public override void End()
        {
            if (finalState != null)
            {
                RestoreState(finalState);
            }
        }

        /// <summary>
        /// Rewinds the iterator to the beginning of the cached list.
        /// <p>
        /// Note that this does not call reset() on the wrapped tokenstream ever, even
        /// the first time. You should reset() the inner tokenstream before wrapping
        /// it with CachingTokenFilter.
        /// </summary>
        public override void Reset()
        {
            if (cache != null)
            {
                iterator = cache.GetEnumerator();
            }
        }

        private void FillCache()
        {
            while (m_input.IncrementToken())
            {
                cache.AddLast(CaptureState());
            }
            // capture final state
            m_input.End();
            finalState = CaptureState();
        }
    }
}