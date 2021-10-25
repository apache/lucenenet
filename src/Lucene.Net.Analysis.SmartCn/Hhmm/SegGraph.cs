// lucene version compatibility level: 4.8.1
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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
    /// Graph representing possible tokens at each start offset in the sentence.
    /// <para>
    /// For each start offset, a list of possible tokens is stored.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    internal class SegGraph
    {
        /// <summary>
        /// Map of start offsets to <see cref="T:IList{SegToken}"/> of tokens at that position
        /// </summary>
        private readonly IDictionary<int, IList<SegToken>> tokenListTable = new Dictionary<int, IList<SegToken>>(); // LUCENENET: marked readonly

        private int maxStart = -1;

        /// <summary>
        /// Returns <c>true</c> if a mapping for the specified start offset exists
        /// </summary>
        /// <param name="s">startOffset</param>
        /// <returns><c>true</c> if there are tokens for the startOffset</returns>
        public virtual bool IsStartExist(int s)
        {
            //return tokenListTable.get(s) != null;
            return tokenListTable.TryGetValue(s, out IList<SegToken> result) && result != null;
        }

        /// <summary>
        ///  Get the list of tokens at the specified start offset
        /// </summary>
        /// <param name="s">startOffset</param>
        /// <returns><see cref="T:IList{SegToken}"/> of tokens at the specified start offset.</returns>
        public virtual IList<SegToken> GetStartList(int s)
        {
            tokenListTable.TryGetValue(s, out IList<SegToken> result);
            return result;
        }

        /// <summary>
        /// Get the highest start offset in the map. Returns maximum start offset, or -1 if the map is empty.
        /// </summary>
        public virtual int MaxStart => maxStart;

        /// <summary>
        /// Set the <see cref="SegToken.Index"/> for each token, based upon its order by startOffset. 
        /// </summary>
        /// <returns>a <see cref="T:IList{SegToken}"/> of these ordered tokens.</returns>
        public virtual IList<SegToken> MakeIndex()
        {
            IList<SegToken> result = new JCG.List<SegToken>();
            int s = -1, count = 0, size = tokenListTable.Count;
            IList<SegToken> tokenList;
            int index = 0;
            while (count < size)
            {
                if (IsStartExist(s))
                {
                    tokenList = tokenListTable[s];
                    foreach (SegToken st in tokenList)
                    {
                        st.Index = index;
                        result.Add(st);
                        index++;
                    }
                    count++;
                }
                s++;
            }
            return result;
        }

        /// <summary>
        /// Add a <see cref="SegToken"/> to the mapping, creating a new mapping at the token's startOffset if one does not exist. 
        /// </summary>
        /// <param name="token">token <see cref="SegToken"/>.</param>
        public virtual void AddToken(SegToken token)
        {
            int s = token.StartOffset;
            if (!IsStartExist(s))
            {
                IList<SegToken> newlist = new JCG.List<SegToken>
                {
                    token
                };
                tokenListTable[s] = newlist;
            }
            else
            {
                IList<SegToken> tokenList = tokenListTable[s];
                tokenList.Add(token);
            }
            if (s > maxStart)
            {
                maxStart = s;
            }
        }

        /// <summary>
        /// Return a <see cref="T:IList{SegToken}"/> of all tokens in the map, ordered by startOffset.
        /// </summary>
        /// <returns><see cref="T:IList{SegToken}"/> of all tokens in the map.</returns>
        public virtual IList<SegToken> ToTokenList()
        {
            IList<SegToken> result = new JCG.List<SegToken>();
            int s = -1, count = 0, size = tokenListTable.Count;
            IList<SegToken> tokenList;

            while (count < size)
            {
                if (IsStartExist(s))
                {
                    tokenList = tokenListTable[s];
                    foreach (SegToken st in tokenList)
                    {
                        result.Add(st);
                    }
                    count++;
                }
                s++;
            }
            return result;
        }

        public override string ToString()
        {
            IList<SegToken> tokenList = this.ToTokenList();
            StringBuilder sb = new StringBuilder();
            foreach (SegToken t in tokenList)
            {
                sb.Append(t + "\n");
            }
            return sb.ToString();
        }
    }
}
