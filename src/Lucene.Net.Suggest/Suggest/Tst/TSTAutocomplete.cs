using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Tst
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
    /// Ternary Search Trie implementation.
    /// </summary>
    /// <seealso cref="TernaryTreeNode"/>
    public class TSTAutocomplete
    {

        internal TSTAutocomplete()
        {
        }

        /// <summary>
        /// Inserting keys in TST in the order middle,small,big (lexicographic measure)
        /// recursively creates a balanced tree which reduces insertion and search
        /// times significantly.
        /// </summary>
        /// <param name="tokens">
        ///          Sorted list of keys to be inserted in TST. </param>
        /// <param name="lo">
        ///          stores the lower index of current list. </param>
        /// <param name="hi">
        ///          stores the higher index of current list. </param>
        /// <param name="root">
        ///          a reference object to root of TST. </param>
        public virtual void BalancedTree(object[] tokens, object[] vals, int lo, int hi, TernaryTreeNode root)
        {
            if (lo > hi)
            {
                return;
            }
            int mid = (lo + hi) / 2;
            root = Insert(root, (string)tokens[mid], vals[mid], 0);
            BalancedTree(tokens, vals, lo, mid - 1, root);
            BalancedTree(tokens, vals, mid + 1, hi, root);
        }

        /// <summary>
        /// Inserts a key in TST creating a series of Binary Search Trees at each node.
        /// The key is actually stored across the eqKid of each node in a successive
        /// manner.
        /// </summary>
        /// <param name="currentNode">
        ///          a reference node where the insertion will take currently. </param>
        /// <param name="s">
        ///          key to be inserted in TST. </param>
        /// <param name="x">
        ///          index of character in key to be inserted currently. </param>
        /// <returns> The new reference to root node of TST </returns>
        public virtual TernaryTreeNode Insert(TernaryTreeNode currentNode, string s, object val, int x)
        {
            if (s is null || s.Length <= x)
            {
                return currentNode;
            }
            if (currentNode is null)
            {
                TernaryTreeNode newNode = new TernaryTreeNode();
                newNode.splitchar = s[x];
                currentNode = newNode;
                if (x < s.Length - 1)
                {
                    currentNode.eqKid = Insert(currentNode.eqKid, s, val, x + 1);
                }
                else
                {
                    currentNode.token = s.ToString();
                    currentNode.val = val;
                    return currentNode;
                }
            }
            else if (currentNode.splitchar > s[x])
            {
                currentNode.loKid = Insert(currentNode.loKid, s, val, x);
            }
            else if (currentNode.splitchar == s[x])
            {
                if (x < s.Length - 1)
                {
                    currentNode.eqKid = Insert(currentNode.eqKid, s, val, x + 1);
                }
                else
                {
                    currentNode.token = s;
                    currentNode.val = val;
                    return currentNode;
                }
            }
            else
            {
                currentNode.hiKid = Insert(currentNode.hiKid, s, val, x);
            }
            return currentNode;
        }

        /// <summary>
        /// Auto-completes a given prefix query using Depth-First Search with the end
        /// of prefix as source node each time finding a new leaf to get a complete key
        /// to be added in the suggest list.
        /// </summary>
        /// <param name="root">
        ///          a reference to root node of TST. </param>
        /// <param name="s">
        ///          prefix query to be auto-completed. </param>
        /// <param name="x">
        ///          index of current character to be searched while traversing through
        ///          the prefix in TST. </param>
        /// <returns> suggest list of auto-completed keys for the given prefix query. </returns>
        public virtual IList<TernaryTreeNode> PrefixCompletion(TernaryTreeNode root, string s, int x)
        {

            TernaryTreeNode p = root;
            JCG.List<TernaryTreeNode> suggest = new JCG.List<TernaryTreeNode>();

            while (p != null)
            {
                if (s[x] < p.splitchar)
                {
                    p = p.loKid;
                }
                else if (s[x] == p.splitchar)
                {
                    if (x == s.Length - 1)
                    {
                        break;
                    }
                    else
                    {
                        x++;
                    }
                    p = p.eqKid;
                }
                else
                {
                    p = p.hiKid;
                }
            }

            if (p is null)
            {
                return suggest;
            }
            if (p.eqKid is null && p.token is null)
            {
                return suggest;
            }
            if (p.eqKid is null && p.token != null)
            {
                suggest.Add(p);
                return suggest;
            }

            if (p.token != null)
            {
                suggest.Add(p);
            }
            p = p.eqKid;

            var st = new Stack<TernaryTreeNode>();
            st.Push(p);
            while (st.Count > 0)
            {
                TernaryTreeNode top = st.Peek();
                st.Pop();
                if (top.token != null)
                {
                    suggest.Add(top);
                }
                if (top.eqKid != null)
                {
                    st.Push(top.eqKid);
                }
                if (top.loKid != null)
                {
                    st.Push(top.loKid);
                }
                if (top.hiKid != null)
                {
                    st.Push(top.hiKid);
                }
            }
            return suggest;
        }
    }
}