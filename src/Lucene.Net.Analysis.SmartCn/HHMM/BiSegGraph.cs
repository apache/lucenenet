// lucene version compatibility level: 4.8.1
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart.HHMM
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
    /// Graph representing possible token pairs (bigrams) at each start offset in the sentence.
    /// <para>
    /// For each start offset, a list of possible token pairs is stored.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    internal class BiSegGraph
    {
        private IDictionary<int, IList<SegTokenPair>> tokenPairListTable = new Dictionary<int, IList<SegTokenPair>>();

        private IList<SegToken> segTokenList;

        private static BigramDictionary bigramDict = BigramDictionary.GetInstance();

        public BiSegGraph(SegGraph segGraph)
        {
            segTokenList = segGraph.MakeIndex();
            GenerateBiSegGraph(segGraph);
        }

        /// <summary>
        /// Generate a <see cref="BiSegGraph"/> based upon a <see cref="SegGraph"/>
        /// </summary>
        private void GenerateBiSegGraph(SegGraph segGraph)
        {
            double smooth = 0.1;
            int wordPairFreq = 0;
            int maxStart = segGraph.MaxStart;
            double oneWordFreq, weight, tinyDouble = 1.0 / Utility.MAX_FREQUENCE;

            int next;
            char[] idBuffer;
            // get the list of tokens ordered and indexed
            segTokenList = segGraph.MakeIndex();
            // Because the beginning position of startToken is -1, therefore startToken can be obtained when key = -1
            int key = -1;
            IList<SegToken> nextTokens = null;
            while (key < maxStart)
            {
                if (segGraph.IsStartExist(key))
                {

                    IList<SegToken> tokenList = segGraph.GetStartList(key);

                    // Calculate all tokens for a given key.
                    foreach (SegToken t1 in tokenList)
                    {
                        oneWordFreq = t1.Weight;
                        next = t1.EndOffset;
                        nextTokens = null;
                        // Find the next corresponding Token.
                        // For example: "Sunny seashore", the present Token is "sunny", next one should be "sea" or "seashore".
                        // If we cannot find the next Token, then go to the end and repeat the same cycle.
                        while (next <= maxStart)
                        {
                            // Because the beginning position of endToken is sentenceLen, so equal to sentenceLen can find endToken.
                            if (segGraph.IsStartExist(next))
                            {
                                nextTokens = segGraph.GetStartList(next);
                                break;
                            }
                            next++;
                        }
                        if (nextTokens == null)
                        {
                            break;
                        }
                        foreach (SegToken t2 in nextTokens)
                        {
                            idBuffer = new char[t1.CharArray.Length + t2.CharArray.Length + 1];
                            System.Array.Copy(t1.CharArray, 0, idBuffer, 0, t1.CharArray.Length);
                            idBuffer[t1.CharArray.Length] = BigramDictionary.WORD_SEGMENT_CHAR;
                            System.Array.Copy(t2.CharArray, 0, idBuffer,
                                t1.CharArray.Length + 1, t2.CharArray.Length);

                            // Two linked Words frequency
                            wordPairFreq = bigramDict.GetFrequency(idBuffer);

                            // Smoothing

                            // -log{a*P(Ci-1)+(1-a)P(Ci|Ci-1)} Note 0<a<1
                            weight = -Math
                                .Log(smooth
                                    * (1.0 + oneWordFreq)
                                    / (Utility.MAX_FREQUENCE + 0.0)
                                    + (1.0 - smooth)
                                    * ((1.0 - tinyDouble) * wordPairFreq / (1.0 + oneWordFreq) + tinyDouble));

                            SegTokenPair tokenPair = new SegTokenPair(idBuffer, t1.Index,
                                t2.Index, weight);
                            this.AddSegTokenPair(tokenPair);
                        }
                    }
                }
                key++;
            }

        }

        /// <summary>
        /// Returns <c>true</c> if their is a list of token pairs at this offset (index of the second token)
        /// </summary>
        /// <param name="to">index of the second token in the token pair</param>
        /// <returns><c>true</c> if a token pair exists</returns>
        public virtual bool IsToExist(int to)
        {
            //return tokenPairListTable.get(Integer.valueOf(to)) != null;
            //return tokenPairListTable.ContainsKey(to) && tokenPairListTable[to] != null;
            IList<SegTokenPair> result;
            return tokenPairListTable.TryGetValue(to, out result) && result != null;
        }

        /// <summary>
        /// Return a <see cref="T:IList{SegTokenPair}"/> of all token pairs at this offset (index of the second token)
        /// </summary>
        /// <param name="to">index of the second token in the token pair</param>
        /// <returns><see cref="T:IList{SegTokenPair}"/> of token pairs. </returns>
        public virtual IList<SegTokenPair> GetToList(int to)
        {
            IList<SegTokenPair> result;
            tokenPairListTable.TryGetValue(to, out result);
            return result;
        }

        /// <summary>
        /// Add a <see cref="SegTokenPair"/>
        /// </summary>
        /// <param name="tokenPair"><see cref="SegTokenPair"/></param>
        public virtual void AddSegTokenPair(SegTokenPair tokenPair)
        {
            int to = tokenPair.To;
            if (!IsToExist(to))
            {
                List<SegTokenPair> newlist = new List<SegTokenPair>();
                newlist.Add(tokenPair);
                tokenPairListTable[to] = newlist;
            }
            else
            {
                IList<SegTokenPair> tokenPairList = tokenPairListTable[to];
                tokenPairList.Add(tokenPair);
            }
        }

        /// <summary>
        /// Get the number of <see cref="SegTokenPair"/> entries in the table.
        /// </summary>
        /// <returns>number of <see cref="SegTokenPair"/> entries</returns>
        public virtual int ToCount
        {
            get { return tokenPairListTable.Count; }
        }

        /// <summary>
        /// Find the shortest path with the Viterbi algorithm.
        /// </summary>
        /// <returns><see cref="T:IList{SegToken}"/></returns>
        [ExceptionToNetNumericConvention]
        public virtual IList<SegToken> GetShortPath()
        {
            int current;
            int nodeCount = ToCount;
            IList<PathNode> path = new List<PathNode>();
            PathNode zeroPath = new PathNode();
            zeroPath.Weight = 0;
            zeroPath.PreNode = 0;
            path.Add(zeroPath);
            for (current = 1; current <= nodeCount; current++)
            {
                double weight;
                IList<SegTokenPair> edges = GetToList(current);

                double minWeight = double.MaxValue;
                SegTokenPair minEdge = null;
                foreach (SegTokenPair edge in edges)
                {
                    weight = edge.Weight;
                    PathNode preNode2 = path[edge.From];
                    if (preNode2.Weight + weight < minWeight)
                    {
                        minWeight = preNode2.Weight + weight;
                        minEdge = edge;
                    }
                }
                PathNode newNode = new PathNode();
                newNode.Weight = minWeight;
                newNode.PreNode = minEdge.From;
                path.Add(newNode);
            }

            // Calculate PathNodes
            int preNode, lastNode;
            lastNode = path.Count - 1;
            current = lastNode;
            IList<int> rpath = new List<int>();
            IList<SegToken> resultPath = new List<SegToken>();

            rpath.Add(current);
            while (current != 0)
            {
                PathNode currentPathNode = path[current];
                preNode = currentPathNode.PreNode;
                rpath.Add(preNode);
                current = preNode;
            }
            for (int j = rpath.Count - 1; j >= 0; j--)
            {
                //int idInteger = rpath.get(j);
                //int id = idInteger.intValue();
                int id = rpath[j];
                SegToken t = segTokenList[id];
                resultPath.Add(t);
            }
            return resultPath;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ICollection<IList<SegTokenPair>> values = tokenPairListTable.Values;
            foreach (IList<SegTokenPair> segList in values)
            {
                foreach (SegTokenPair pair in segList)
                {
                    sb.Append(pair + "\n");
                }
            }
            return sb.ToString();
        }
    }
}
