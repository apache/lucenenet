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
    /// Sorts SuggestWord instances
    /// </summary>
    /// <seealso cref="SuggestWordScoreComparer"/>
    /// <seealso cref="SuggestWordFrequencyComparer"/>
    public sealed class SuggestWordQueue : PriorityQueue<SuggestWord>
    {
        /// <summary>
        /// Default comparer: score then frequency. </summary>
        /// <seealso cref="SuggestWordScoreComparer"/>
        public static readonly IComparer<SuggestWord> DEFAULT_COMPARER = new SuggestWordScoreComparer();


        private readonly IComparer<SuggestWord> comparer;

        /// <summary>
        /// Use the <see cref="DEFAULT_COMPARER"/> </summary>
        /// <param name="size"> The size of the queue </param>
        public SuggestWordQueue(int size)
            : base(size)
        {
            comparer = DEFAULT_COMPARER;
        }

        /// <summary>
        /// Specify the size of the queue and the comparer to use for sorting. </summary>
        /// <param name="size"> The size </param>
        /// <param name="comparer"> The comparer. </param>
        public SuggestWordQueue(int size, IComparer<SuggestWord> comparer)
            : base(size)
        {
            this.comparer = comparer;
        }

        protected internal override bool LessThan(SuggestWord wa, SuggestWord wb)
        {
            int val = comparer.Compare(wa, wb);
            return val < 0;
        }
    }
}