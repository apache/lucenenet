using J2N.Numerics;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
        private JaspellTernarySearchTrie trie = new JaspellTernarySearchTrie();
        private readonly bool usePrefix = true;
        private readonly int editDistance = 2;

        /// <summary>
        /// Number of entries the lookup was built with </summary>
        private long count = 0;

        /// <summary>
        /// Creates a new empty trie
        /// </summary>
        /// <seealso cref="Build(IInputEnumerator)"/>
        public JaspellLookup()
        {
        }

        public override void Build(IInputEnumerator enumerator)
        {
            // LUCENENET: Added guard clause for null
            if (enumerator is null)
                throw new ArgumentNullException(nameof(enumerator));

            if (enumerator.HasPayloads)
            {
                throw new ArgumentException("this suggester doesn't support payloads");
            }
            if (enumerator.Comparer != null)
            {
                // make sure it's unsorted
                // WTF - this could result in yet another sorted iteration....
                enumerator = new UnsortedInputEnumerator(enumerator);
            }
            if (enumerator.HasContexts)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            count = 0;
            trie = new JaspellTernarySearchTrie { MatchAlmostDiff = editDistance };
            BytesRef spare;

            var charsSpare = new CharsRef();

            while (enumerator.MoveNext())
            {
                spare = enumerator.Current;
                long weight = enumerator.Weight;
                if (spare.Length == 0)
                {
                    continue;
                }
                charsSpare.Grow(spare.Length);
                UnicodeUtil.UTF8toUTF16(spare.Bytes, spare.Offset, spare.Length, charsSpare);
                trie.Put(charsSpare.ToString(), J2N.Numerics.Int64.GetInstance(weight));
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

        public override IList<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (contexts != null)
            {
                throw new ArgumentException("this suggester doesn't support contexts");
            }
            IList<LookupResult> res = new JCG.List<LookupResult>();
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
            if (list is null || list.Count == 0)
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
                node.data = @in.ReadInt64();
            }
            if ((mask & LO_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode('\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.LOKID] = kid;
                ReadRecursively(@in, kid);
            }
            if ((mask & EQ_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode('\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.EQKID] = kid;
                ReadRecursively(@in, kid);
            }
            if ((mask & HI_KID) != 0)
            {
                var kid = new JaspellTernarySearchTrie.TSTNode('\0', node);
                node.relatives[JaspellTernarySearchTrie.TSTNode.HIKID] = kid;
                ReadRecursively(@in, kid);
            }
        }

        private void WriteRecursively(DataOutput @out, JaspellTernarySearchTrie.TSTNode node)
        {
            if (node is null)
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
                @out.WriteInt64(((Number)node.data).ToInt64());
            }
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.LOKID]);
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.EQKID]);
            WriteRecursively(@out, node.relatives[JaspellTernarySearchTrie.TSTNode.HIKID]);
        }

        public override bool Store(DataOutput output)
        {
            output.WriteVInt64(count);
            JaspellTernarySearchTrie.TSTNode root = trie.Root;
            if (root is null) // empty tree
            {
                return false;
            }
            WriteRecursively(output, root);
            return true;
        }

        public override bool Load(DataInput input)
        {
            count = input.ReadVInt64();
            var root = new JaspellTernarySearchTrie.TSTNode('\0', null);
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

        public override long Count => count;
    }
}