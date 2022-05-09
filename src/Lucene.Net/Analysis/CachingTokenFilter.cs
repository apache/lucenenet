using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// This class can be used if the token attributes of a <see cref="TokenStream"/>
    /// are intended to be consumed more than once. It caches
    /// all token attribute states locally in a List.
    ///
    /// <para/><see cref="CachingTokenFilter"/> implements the optional method
    /// <see cref="TokenStream.Reset()"/>, which repositions the
    /// stream to the first <see cref="Token"/>.
    /// </summary>
    public sealed class CachingTokenFilter : TokenFilter
    {
        private IList<AttributeSource.State> cache = null;
        private IEnumerator<AttributeSource.State> iterator = null;
        private AttributeSource.State finalState;

        /// <summary>
        /// Create a new <see cref="CachingTokenFilter"/> around <paramref name="input"/>,
        /// caching its token attributes, which can be replayed again
        /// after a call to <see cref="Reset()"/>.
        /// </summary>
        public CachingTokenFilter(TokenStream input)
            : base(input)
        {
        }

        public override bool IncrementToken()
        {
            if (cache is null)
            {
                // fill cache lazily
                cache = new JCG.List<AttributeSource.State>();
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
        /// <para/>
        /// Note that this does not call <see cref="Reset()"/> on the wrapped tokenstream ever, even
        /// the first time. You should <see cref="Reset()"/> the inner tokenstream before wrapping
        /// it with <see cref="CachingTokenFilter"/>.
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
                cache.Add(CaptureState());
            }
            // capture final state
            m_input.End();
            finalState = CaptureState();
        }

        /// <summary>
        /// Releases resources used by the <see cref="CachingTokenFilter"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    iterator?.Dispose();
                    iterator = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}