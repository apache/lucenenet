using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Suggest.Jaspell
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
    /// Suggest implementation based on 
    /// <a href="http://jaspell.sourceforge.net/">JaSpell</a>.
    /// </summary>
    /// <seealso cref="JaspellTernarySearchTrie"/>
    public class JaspellLookup : Lookup
    {
        internal JaspellTernarySearchTrie trie = new JaspellTernarySearchTrie();
        private bool usePrefix = true;
        private int editDistance = 2;

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// Creates a new empty trie
        /// </summary>
        /// <seealso cref="Build(IInputIterator)"/>
        public JaspellLookup()
        {
        }

        public override void Build(IInputIterator tfit)
        {
            if (tfit.HasPayloads)
            {
                throw new ArgumentException("this suggester doesn't support payloads");
            }
            if (tfit.Comparer != null)
            {
                // make sure it's unsorted
                // WTF - this could result in yet another sorted iteration....
                tfit = new UnsortedInputIterator(tfit);
            }
            if (tfit.HasContexts)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            count = 0;
            trie = new JaspellTernarySearchTrie { MatchAlmostDiff = editDistance };
            BytesRef spare;

            var charsSpare = new CharsRef();

            while ((spare = tfit.Next()) != null)
            {

                long weight = tfit.Weight;
                if (spare.Length == 0)
                {
                    continue;
                }
                charsSpare.Grow(spare.Length);
                UnicodeUtil.UTF8toUTF16(spare.Bytes, spare.Offset, spare.Length, charsSpare);
                trie.Put(charsSpare.ToString(), Convert.ToInt64(weight));
            }
        }

        /// <summary>
        /// Adds a new node if <code>key</code> already exists,
        /// otherwise replaces its value.
        /// <para>
        /// This method always returns false.
        /// </para>
        /// </summary>
        /// <param name="key"> A <see cref="string"/> index. </param>
        /// <param name="value"> The object to be stored in the Trie. </param>
        public virtual bool Add(string key, object value)
        {
            trie.Put(key, value);
            // XXX
            return false;
        }

        /// <summary>
        /// Returns the value for the specified key, or null
        /// if the key does not exist.
        /// </summary>
        /// <param name="key"> A <see cref="string"/> index. </param>
        public virtual object Get(string key)
        {
            return trie.Get(key);
        }

        public override List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (contexts != null)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            List<LookupResult> res = new List<LookupResult>();
            IList<string> list;
            int count = onlyMorePopular ? num * 2 : num;
            if (usePrefix)
            {
                list = trie.MatchPrefix(key, count);
            }
            else
            {
                list = trie.MatchAlmost(key, count);
            }
            if (list == null || list.Count == 0)
            {
                return res;

            }
            int maxCnt = Math.Min(num, list.Count);
            if (onlyMorePopular)
            {
                LookupPriorityQueue queue = new LookupPriorityQueue(num);
                foreach (string s in list)
                {
                    long freq = (long)(trie.Get(s));
                    queue.InsertWithOverflow(new LookupResult(s, freq));
                }
                foreach (LookupResult lr in queue.GetResults())
                {
                    res.Add(lr);
                }
            }
            else
            {
                for (int i = 0; i < maxCnt; i++)
                {
                    string s = list[i];
                    long freq = (long)(trie.Get(s));
                    res.Add(new LookupResult(s, freq));
                }
            }
            return res;
        }

        private const sbyte LO_KID = 0x01;
        private const sbyte EQ_KID = 0x02;
        private const sbyte HI_KID = 0x04;
        private const sbyte HAS_VALUE = 0x08;

        private void ReadRecursively(DataInput @in, JaspellTernarySearchTrie.TSTNode node)
        {
            node.splitchar = @in.ReadString()[0];
            sbyte mask = (sbyte)@in.ReadByte();
            if ((mask & HAS_VALUE) != 0)
            {
                node.data = Convert.ToInt64(@in.ReadLong());
            }
            if ((mask & LO_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode(trie, '\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.LOKID] = kid;
                ReadRecursively(@in, kid);
            }
            if ((mask & EQ_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode(trie, '\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.EQKID] = kid;
                ReadRecursively(@in, kid);
            }
            if ((mask & HI_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode(trie, '\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.HIKID] = kid;
                ReadRecursively(@in, kid);
            }
        }

        private void WriteRecursively(DataOutput @out, JaspellTernarySearchTrie.TSTNode node)
        {
            if (node == null)
            {
                return;
            }
            @out.WriteString(new string(new char[] { node.splitchar }, 0, 1));
            sbyte mask = 0;
            if (node.relatives[JaspellTernarySearchTrie.TSTNode.LOKID] != null)
            {
                mask |= LO_KID;
            }
            if (node.relatives[JaspellTernarySearchTrie.TSTNode.EQKID] != null)
            {
                mask |= EQ_KID;
            }
            if (node.relatives[JaspellTernarySearchTrie.TSTNode.HIKID] != null)
            {
                mask |= HI_KID;
            }
            if (node.data != null)
            {
                mask |= HAS_VALUE;
            }
            @out.WriteByte((byte)mask);
            if (node.data != null)
            {
                @out.WriteLong((long)(node.data));
            }
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.LOKID]);
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.EQKID]);
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.HIKID]);
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVLong(count);
            JaspellTernarySearchTrie.TSTNode root = trie.Root;
            if (root == null) // empty tree
            {
                return false;
            }
            WriteRecursively(output, root);
            return true;
        }

        public override bool Load(DataInput input)
        {
            count = input.ReadVLong();
            var root = new JaspellTernarySearchTrie.TSTNode(trie, '\0', null);
            ReadRecursively(input, root);
            trie.Root = root;
            return true;
        }

        /// <summary>
        /// Returns byte size of the underlying TST. </summary>
        public override long GetSizeInBytes()
        {
            return trie.GetSizeInBytes();
        }

        public override long Count
        {
            get
            {
                return count;
            }
        }
    }
}