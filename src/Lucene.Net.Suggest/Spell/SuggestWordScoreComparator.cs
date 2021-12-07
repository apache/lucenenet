using J2N.Text;
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
    /// Score first, then frequency
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class SuggestWordScoreComparer : IComparer<SuggestWord>
    {

        /// <summary>
        /// Creates a new comparer that will compare by <see cref="SuggestWord.Score"/>,
        /// then by <see cref="SuggestWord.Freq"/>, then by <see cref="SuggestWord.String"/>.
        /// </summary>
        public SuggestWordScoreComparer()
        {
        }

        public virtual int Compare(SuggestWord first, SuggestWord second)
        {
            // first criteria: the distance
            if (first.Score > second.Score)
            {
                return 1;
            }
            if (first.Score < second.Score)
            {
                return -1;
            }

            // second criteria (if first criteria is equal): the popularity
            if (first.Freq > second.Freq)
            {
                return 1;
            }

            if (first.Freq < second.Freq)
            {
                return -1;
            }
            // third criteria: term text
            return second.String.CompareToOrdinal(first.String);
        }
    }
}