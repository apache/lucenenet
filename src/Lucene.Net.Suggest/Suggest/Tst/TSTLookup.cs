using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Suggest implementation based on a 
    /// <a href="http://en.wikipedia.org/wiki/Ternary_search_tree">Ternary Search Tree</a>
    /// </summary>
    /// <seealso cref="TSTAutocomplete"/>
    public class TSTLookup : Lookup
    {
        internal TernaryTreeNode root = new TernaryTreeNode();
        internal TSTAutocomplete autocomplete = new TSTAutocomplete();

        /// <summary>
        /// Number of entries the lookup was built with
        /// </summary>
        private long count = 0;

        /// <summary>
        /// Creates a new TSTLookup with an empty Ternary Search Tree. </summary>
        /// <seealso cref="Build(IInputIterator)"/>
        public TSTLookup()
        {
        }

        public override void Build(IInputIterator tfit)
        {
            if (tfit.HasPayloads)
            {
                throw new System.ArgumentException("this suggester doesn't support payloads");
            }
            if (tfit.HasContexts)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            root = new TernaryTreeNode();
            // buffer first
#pragma warning disable 612, 618
            if (tfit.Comparer != BytesRef.UTF8SortedAsUTF16Comparer)
            {
                // make sure it's sorted and the comparer uses UTF16 sort order
                tfit = new SortedInputIterator(tfit, BytesRef.UTF8SortedAsUTF16Comparer);
            }
#pragma warning restore 612, 618

            List<string> tokens = new List<string>();
            List<object> vals = new List<object>(); // LUCENENET TODO: Should this be long? in Java it was Number, but we can probably do better than object
            BytesRef spare;
            CharsRef charsSpare = new CharsRef();
            while ((spare = tfit.Next()) != null)
            {
                charsSpare.Grow(spare.Length);
                UnicodeUtil.UTF8toUTF16(spare.Bytes, spare.Offset, spare.Length, charsSpare);
                tokens.Add(charsSpare.ToString());
                vals.Add(Convert.ToInt64(tfit.Weight));
            }
            autocomplete.BalancedTree(tokens.ToArray(), vals.ToArray(), 0, tokens.Count - 1, root);
        }

        /// <summary>
        /// Adds a new node if <code>key</code> already exists,
        /// otherwise replaces its value.
        /// <para>
        /// This method always returns true.
        /// </para>
        /// </summary>
        public virtual bool Add(string key, object value)
        {
            autocomplete.Insert(root, key, value, 0);
            // XXX we don't know if a new node was created
            return true;
        }

        /// <summary>
        /// Returns the value for the specified key, or null
        /// if the key does not exist.
        /// </summary>
        public virtual object Get(string key)
        {
            IList<TernaryTreeNode> list = autocomplete.PrefixCompletion(root, key, 0);
            if (list == null || list.Count == 0)
            {
                return null;
            }
            foreach (TernaryTreeNode n in list)
            {
                if (CharSeqEquals(n.token, key))
                {
                    return n.val;
                }
            }
            return null;
        }

        private static bool CharSeqEquals(string left, string right)
        {
            int len = left.Length;
            if (len != right.Length)
            {
                return false;
            }
            for (int i = 0; i < len; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            return true;
        }
       
        public override List<LookupResult> DoLookup(string key, IEnumerable<BytesRef> contexts, bool onlyMorePopular, int num)
        {
            if (contexts != null)
            {
                throw new System.ArgumentException("this suggester doesn't support contexts");
            }
            IList<TernaryTreeNode> list = autocomplete.PrefixCompletion(root, key, 0);
            List<LookupResult> res = new List<LookupResult>();
            if (list == null || list.Count == 0)
            {
                return res;
            }
            int maxCnt = Math.Min(num, list.Count);
            if (onlyMorePopular)
            {
                LookupPriorityQueue queue = new LookupPriorityQueue(num);

                foreach (TernaryTreeNode ttn in list)
                {
                    queue.InsertWithOverflow(new LookupResult(ttn.token, (long)ttn.val));
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
                    TernaryTreeNode ttn = list[i];
                    res.Add(new LookupResult(ttn.token, (long)ttn.val));
                }
            }
            return res;
        }

        private const sbyte LO_KID = 0x01;
        private const sbyte EQ_KID = 0x02;
        private const sbyte HI_KID = 0x04;
        private const sbyte HAS_TOKEN = 0x08;
        private const sbyte HAS_VALUE = 0x10;

        // pre-order traversal
        private void ReadRecursively(DataInput @in, TernaryTreeNode node)
        {
            node.splitchar = @in.ReadString().First();
            sbyte mask = (sbyte)@in.ReadByte();
            if ((mask & HAS_TOKEN) != 0)
            {
                node.token = @in.ReadString();
            }
            if ((mask & HAS_VALUE) != 0)
            {
                node.val = Convert.ToInt64(@in.ReadLong());
            }
            if ((mask & LO_KID) != 0)
            {
                node.loKid = new TernaryTreeNode();
                ReadRecursively(@in, node.loKid);
            }
            if ((mask & EQ_KID) != 0)
            {
                node.eqKid = new TernaryTreeNode();
                ReadRecursively(@in, node.eqKid);
            }
            if ((mask & HI_KID) != 0)
            {
                node.hiKid = new TernaryTreeNode();
                ReadRecursively(@in, node.hiKid);
            }
        }

        // pre-order traversal
        private void WriteRecursively(DataOutput @out, TernaryTreeNode node)
        {
            // write out the current node
            @out.WriteString(new string(new char[] { node.splitchar }, 0, 1));
            // prepare a mask of kids
            sbyte mask = 0;
            if (node.eqKid != null)
            {
                mask |= EQ_KID;
            }
            if (node.loKid != null)
            {
                mask |= LO_KID;
            }
            if (node.hiKid != null)
            {
                mask |= HI_KID;
            }
            if (node.token != null)
            {
                mask |= HAS_TOKEN;
            }
            if (node.val != null)
            {
                mask |= HAS_VALUE;
            }
            @out.WriteByte((byte)mask);
            if (node.token != null)
            {
                @out.WriteString(node.token);
            }
            if (node.val != null)
            {
                @out.WriteLong((long)node.val);
            }
            // recurse and write kids
            if (node.loKid != null)
            {
                WriteRecursively(@out, node.loKid);
            }
            if (node.eqKid != null)
            {
                WriteRecursively(@out, node.eqKid);
            }
            if (node.hiKid != null)
            {
                WriteRecursively(@out, node.hiKid);
            }
        }

        public override bool Store(DataOutput output)
        {
            lock (this)
            {
                output.WriteVLong(count);
                WriteRecursively(output, root);
                return true;
            }
        }

        public override bool Load(DataInput input)
        {
            lock (this)
            {
                count = input.ReadVLong();
                root = new TernaryTreeNode();
                ReadRecursively(input, root);
                return true;
            }
        }

        /// <summary>
        /// Returns byte size of the underlying TST
        /// </summary>
        public override long GetSizeInBytes()
        {
            long mem = RamUsageEstimator.ShallowSizeOf(this);
            if (root != null)
            {
                mem += root.GetSizeInBytes();
            }
            return mem;
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