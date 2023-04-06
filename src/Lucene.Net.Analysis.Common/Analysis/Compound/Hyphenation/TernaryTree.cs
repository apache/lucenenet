// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     * 
     *      http://www.apache.org/licenses/LICENSE-2.0
     * 
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// <h2>Ternary Search Tree.</h2>
    /// 
    /// <para>
    /// A ternary search tree is a hybrid between a binary tree and a digital search
    /// tree (trie). Keys are limited to strings. A data value of type char is stored
    /// in each leaf node. It can be used as an index (or pointer) to the data.
    /// Branches that only contain one key are compressed to one node by storing a
    /// pointer to the trailer substring of the key. This class is intended to serve
    /// as base class or helper class to implement Dictionary collections or the
    /// like. Ternary trees have some nice properties as the following: the tree can
    /// be traversed in sorted order, partial matches (wildcard) can be implemented,
    /// retrieval of all keys within a given distance from the target, etc. The
    /// storage requirements are higher than a binary tree but a lot less than a
    /// trie. Performance is comparable with a hash table, sometimes it outperforms a
    /// hash function (most of the time can determine a miss faster than a hash).
    /// </para>
    /// 
    /// <para>
    /// The main purpose of this java port is to serve as a base for implementing
    /// TeX's hyphenation algorithm (see The TeXBook, appendix H). Each language
    /// requires from 5000 to 15000 hyphenation patterns which will be keys in this
    /// tree. The strings patterns are usually small (from 2 to 5 characters), but
    /// each char in the tree is stored in a node. Thus memory usage is the main
    /// concern. We will sacrifice 'elegance' to keep memory requirements to the
    /// minimum. Using java's char type as pointer (yes, I know pointer it is a
    /// forbidden word in java) we can keep the size of the node to be just 8 bytes
    /// (3 pointers and the data char). This gives room for about 65000 nodes. In my
    /// tests the english patterns took 7694 nodes and the german patterns 10055
    /// nodes, so I think we are safe.
    /// </para>
    /// 
    /// <para>
    /// All said, this is a map with strings as keys and char as value. Pretty
    /// limited!. It can be extended to a general map by using the string
    /// representation of an object and using the char value as an index to an array
    /// that contains the object values.
    /// </para>
    /// 
    /// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
    /// </summary>

    public class TernaryTree // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        // We use 4 arrays to represent a node.I guess I should have created a proper
        // node class, but somehow Knuth's pascal code made me forget we now have a
        // portable language with virtual memory management and automatic garbage
        // collection! And now is kind of late, furthermore, if it ain't broken, don't
        // fix it.

        /// <summary>
        /// Pointer to low branch and to rest of the key when it is stored directly in
        /// this node, we don't have unions in java!
        /// </summary>
        protected char[] m_lo;

        /// <summary>
        /// Pointer to high branch.
        /// </summary>
        protected char[] m_hi;

        /// <summary>
        /// Pointer to equal branch and to data when this node is a string terminator.
        /// </summary>
        protected char[] m_eq;

        /// <summary>
        /// <para>
        /// The character stored in this node: splitchar. Two special values are
        /// reserved:
        /// </para>
        /// <list type="bullet">
        ///     <item><description>0x0000 as string terminator</description></item>
        ///     <item><description>0xFFFF to indicate that the branch starting at this node is compressed</description></item>
        /// </list>
        /// <para>
        /// This shouldn't be a problem if we give the usual semantics to strings since
        /// 0xFFFF is guaranteed not to be an Unicode character.
        /// </para>
        /// </summary>
        protected char[] m_sc;

        /// <summary>
        /// This vector holds the trailing of the keys when the branch is compressed.
        /// </summary>
        protected CharVector m_kv;

        protected char m_root;

        protected char m_freenode;

        protected int m_length; // number of items in tree

        protected const int BLOCK_SIZE = 2048; // allocation size for arrays

        internal TernaryTree()
        {
            Init();
        }

         // LUCENENET specific - S1699 - marked non-virtual because calling
         // virtual members from the constructor is not a safe operation in .NET
        protected void Init()
        {
            m_root = (char)0;
            m_freenode = (char)1;
            m_length = 0;
            m_lo = new char[BLOCK_SIZE];
            m_hi = new char[BLOCK_SIZE];
            m_eq = new char[BLOCK_SIZE];
            m_sc = new char[BLOCK_SIZE];
            m_kv = new CharVector();
        }

        /// <summary>
        /// Branches are initially compressed, needing one node per key plus the size
        /// of the string key. They are decompressed as needed when another key with
        /// same prefix is inserted. This saves a lot of space, specially for long
        /// keys.
        /// </summary>
        public virtual void Insert(string key, char val)
        {
            // make sure we have enough room in the arrays
            int len = key.Length + 1; // maximum number of nodes that may be generated
            if (m_freenode + len > m_eq.Length)
            {
                RedimNodeArrays(m_eq.Length + BLOCK_SIZE);
            }
            char[] strkey = new char[len--];
            key.CopyTo(0, strkey, 0, len - 0);
            strkey[len] = (char)0;
            m_root = Insert(m_root, strkey, 0, val);
        }

        public virtual void Insert(char[] key, int start, char val)
        {
            int len = StrLen(key) + 1;
            if (m_freenode + len > m_eq.Length)
            {
                RedimNodeArrays(m_eq.Length + BLOCK_SIZE);
            }
            m_root = Insert(m_root, key, start, val);
        }

        /// <summary>
        /// The actual insertion function, recursive version.
        /// </summary>
        private char Insert(char p, char[] key, int start, char val)
        {
            int len = StrLen(key, start);
            if (p == 0)
            {
                // this means there is no branch, this node will start a new branch.
                // Instead of doing that, we store the key somewhere else and create
                // only one node with a pointer to the key
                p = m_freenode++;
                m_eq[p] = val; // holds data
                m_length++;
                m_hi[p] = (char)0;
                if (len > 0)
                {
                    m_sc[p] = (char)0xFFFF; // indicates branch is compressed
                    m_lo[p] = (char)m_kv.Alloc(len + 1); // use 'lo' to hold pointer to key
                    StrCpy(m_kv.Array, m_lo[p], key, start);
                }
                else
                {
                    m_sc[p] = (char)0;
                    m_lo[p] = (char)0;
                }
                return p;
            }

            if (m_sc[p] == 0xFFFF)
            {
                // branch is compressed: need to decompress
                // this will generate garbage in the external key array
                // but we can do some garbage collection later
                char pp = m_freenode++;
                m_lo[pp] = m_lo[p]; // previous pointer to key
                m_eq[pp] = m_eq[p]; // previous pointer to data
                m_lo[p] = (char)0;
                if (len > 0)
                {
                    m_sc[p] = m_kv[m_lo[pp]];
                    m_eq[p] = pp;
                    m_lo[pp]++;
                    if (m_kv[m_lo[pp]] == 0)
                    {
                        // key completly decompressed leaving garbage in key array
                        m_lo[pp] = (char)0;
                        m_sc[pp] = (char)0;
                        m_hi[pp] = (char)0;
                    }
                    else
                    {
                        // we only got first char of key, rest is still there
                        m_sc[pp] = (char)0xFFFF;
                    }
                }
                else
                {
                    // In this case we can save a node by swapping the new node
                    // with the compressed node
                    m_sc[pp] = (char)0xFFFF;
                    m_hi[p] = pp;
                    m_sc[p] = (char)0;
                    m_eq[p] = val;
                    m_length++;
                    return p;
                }
            }
            char s = key[start];
            if (s < m_sc[p])
            {
                m_lo[p] = Insert(m_lo[p], key, start, val);
            }
            else if (s == m_sc[p])
            {
                if (s != 0)
                {
                    m_eq[p] = Insert(m_eq[p], key, start + 1, val);
                }
                else
                {
                    // key already in tree, overwrite data
                    m_eq[p] = val;
                }
            }
            else
            {
                m_hi[p] = Insert(m_hi[p], key, start, val);
            }
            return p;
        }

        /// <summary>
        /// Compares 2 null terminated char arrays
        /// </summary>
        public static int StrCmp(char[] a, int startA, char[] b, int startB)
        {
            for (; a[startA] == b[startB]; startA++, startB++)
            {
                if (a[startA] == 0)
                {
                    return 0;
                }
            }
            return a[startA] - b[startB];
        }

        /// <summary>
        /// Compares a string with null terminated char array
        /// </summary>
        public static int StrCmp(string str, char[] a, int start)
        {
            int i, d, len = str.Length;
            for (i = 0; i < len; i++)
            {
                d = (int)str[i] - a[start + i];
                if (d != 0)
                {
                    return d;
                }
                if (a[start + i] == 0)
                {
                    return d;
                }
            }
            if (a[start + i] != 0)
            {
                return -a[start + i];
            }
            return 0;

        }

        public static void StrCpy(char[] dst, int di, char[] src, int si)
        {
            while (src[si] != 0)
            {
                dst[di++] = src[si++];
            }
            dst[di] = (char)0;
        }

        public static int StrLen(char[] a, int start)
        {
            int len = 0;
            for (int i = start; i < a.Length && a[i] != 0; i++)
            {
                len++;
            }
            return len;
        }

        public static int StrLen(char[] a)
        {
            return StrLen(a, 0);
        }

        public virtual int Find(string key)
        {
            int len = key.Length;
            char[] strkey = new char[len + 1];
            key.CopyTo(0, strkey, 0, len - 0);
            strkey[len] = (char)0;

            return Find(strkey, 0);
        }

        public virtual int Find(char[] key, int start)
        {
            int d;
            char p = m_root;
            int i = start;
            char c;

            while (p != 0)
            {
                if (m_sc[p] == 0xFFFF)
                {
                    if (StrCmp(key, i, m_kv.Array, m_lo[p]) == 0)
                    {
                        return m_eq[p];
                    }
                    else
                    {
                        return -1;
                    }
                }
                c = key[i];
                d = c - m_sc[p];
                if (d == 0)
                {
                    if (c == 0)
                    {
                        return m_eq[p];
                    }
                    i++;
                    p = m_eq[p];
                }
                else if (d < 0)
                {
                    p = m_lo[p];
                }
                else
                {
                    p = m_hi[p];
                }
            }
            return -1;
        }

        public virtual bool Knows(string key)
        {
            return (Find(key) >= 0);
        }

        // redimension the arrays
        private void RedimNodeArrays(int newsize)
        {
            int len = newsize < m_lo.Length ? newsize : m_lo.Length;
            char[] na = new char[newsize];
            Arrays.Copy(m_lo, 0, na, 0, len);
            m_lo = na;
            na = new char[newsize];
            Arrays.Copy(m_hi, 0, na, 0, len);
            m_hi = na;
            na = new char[newsize];
            Arrays.Copy(m_eq, 0, na, 0, len);
            m_eq = na;
            na = new char[newsize];
            Arrays.Copy(m_sc, 0, na, 0, len);
            m_sc = na;
        }

        public virtual int Length => m_length;

        public virtual object Clone()
        {
            TernaryTree t = new TernaryTree();
            t.m_lo = (char[])this.m_lo.Clone();
            t.m_hi = (char[])this.m_hi.Clone();
            t.m_eq = (char[])this.m_eq.Clone();
            t.m_sc = (char[])this.m_sc.Clone();
            t.m_kv = (CharVector)this.m_kv.Clone();
            t.m_root = this.m_root;
            t.m_freenode = this.m_freenode;
            t.m_length = this.m_length;

            return t;
        }

        /// <summary>
        /// Recursively insert the median first and then the median of the lower and
        /// upper halves, and so on in order to get a balanced tree. The array of keys
        /// is assumed to be sorted in ascending order.
        /// </summary>
        protected virtual void InsertBalanced(string[] k, char[] v, int offset, int n)
        {
            int m;
            if (n < 1)
            {
                return;
            }
            m = n >> 1;

            Insert(k[m + offset], v[m + offset]);
            InsertBalanced(k, v, offset, m);

            InsertBalanced(k, v, offset + m + 1, n - m - 1);
        }

        /// <summary>
        /// Balance the tree for best search performance
        /// </summary>
        public virtual void Balance()
        {
            // System.out.print("Before root splitchar = ");
            // System.out.println(sc[root]);

            int i = 0, n = m_length;
            string[] k = new string[n];
            char[] v = new char[n];
            using (Enumerator iter = new Enumerator(this))
            {
                while (iter.MoveNext())
                {
                    v[i] = iter.Value;
                    k[i++] = iter.Current;
                }
            }
            Init();
            InsertBalanced(k, v, 0, n);

            // With uniform letter distribution sc[root] should be around 'm'
            // System.out.print("After root splitchar = ");
            // System.out.println(sc[root]);
        }

        /// <summary>
        /// Each node stores a character (splitchar) which is part of some key(s). In a
        /// compressed branch (one that only contain a single string key) the trailer
        /// of the key which is not already in nodes is stored externally in the kv
        /// array. As items are inserted, key substrings decrease. Some substrings may
        /// completely disappear when the whole branch is totally decompressed. The
        /// tree is traversed to find the key substrings actually used. In addition,
        /// duplicate substrings are removed using a map (implemented with a
        /// TernaryTree!).
        /// 
        /// </summary>
        public virtual void TrimToSize()
        {
            // first balance the tree for best performance
            Balance();

            // redimension the node arrays
            RedimNodeArrays(m_freenode);

            // ok, compact kv array
            CharVector kx = new CharVector();
            kx.Alloc(1);
            TernaryTree map = new TernaryTree();
            Compact(kx, map, m_root);
            m_kv = kx;
            m_kv.TrimToSize();
        }

        private void Compact(CharVector kx, TernaryTree map, char p)
        {
            int k;
            if (p == 0)
            {
                return;
            }
            if (m_sc[p] == 0xFFFF)
            {
                k = map.Find(m_kv.Array, m_lo[p]);
                if (k < 0)
                {
                    k = kx.Alloc(StrLen(m_kv.Array, m_lo[p]) + 1);
                    StrCpy(kx.Array, k, m_kv.Array, m_lo[p]);
                    map.Insert(kx.Array, k, (char)k);
                }
                m_lo[p] = (char)k;
            }
            else
            {
                Compact(kx, map, m_lo[p]);
                if (m_sc[p] != 0)
                {
                    Compact(kx, map, m_eq[p]);
                }
                Compact(kx, map, m_hi[p]);
            }
        }

        /// <summary>
        /// Gets an enumerator over the keys of this <see cref="TernaryTree"/>.
        /// <para/>
        /// NOTE: This was keys() in Lucene.
        /// </summary>
        /// <returns>An enumerator over the keys of this <see cref="TernaryTree"/>.</returns>
        public virtual IEnumerator<string> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Enumerator for TernaryTree
        /// <para/>
        /// LUCENENET NOTE: This differs a bit from its Java counterpart to adhere to
        /// .NET IEnumerator semantics. In Java, when the <see cref="Enumerator"/> is
        /// instantiated, it is already positioned at the first element. However,
        /// to act like a .NET IEnumerator, the initial state is undefined and considered
        /// to be before the first element until <see cref="MoveNext"/> is called, and
        /// if a move took place it will return <c>true</c>;
        /// </summary>
        public class Enumerator : IEnumerator<string>
        {
            private readonly TernaryTree outerInstance;

            /// <summary>
            /// current node index
            /// </summary>
            private int cur;

            /// <summary>
            /// current key
            /// </summary>
            private string curkey;

            private class Item // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
            {
                internal char parent;
                internal char child;

                // LUCENENET: This constructor is unnecessary
                //public Item()
                //{
                //    parent = (char)0;
                //    child = (char)0;
                //}

                public Item(char p, char c)
                {
                    parent = p;
                    child = c;
                }

                public object Clone()
                {
                    return new Item(parent, child);
                }
            }

            /// <summary>
            /// Node stack
            /// </summary>
            private readonly Stack<Item> ns;

            /// <summary>
            /// key stack implemented with a <see cref="StringBuilder"/>
            /// </summary>
            private readonly StringBuilder ks;

            private bool isInitialized = false;

            public Enumerator(TernaryTree ternaryTree)
            {
                this.outerInstance = ternaryTree;
                cur = -1;
                ns = new Stack<Item>();
                ks = new StringBuilder();
                isInitialized = false;
            }

            public virtual void Rewind()
            {
                ns.Clear();
                ks.Length = 0;
                cur = outerInstance.m_root;
                Run();
            }

            public virtual char Value
            {
                get
                {
                    if (cur >= 0)
                    {
                        return outerInstance.m_eq[cur];
                    }
                    return (char)0;
                }
            }

            /// <summary>
            /// traverse upwards
            /// </summary>
            private int Up()
            {
                Item i/* = new Item()*/; // LUCENENET: Removed unnecessary assignment
                int res = 0;

                if (ns.Count == 0)
                {
                    return -1;
                }

                if (cur != 0 && outerInstance.m_sc[cur] == 0)
                {
                    return outerInstance.m_lo[cur];
                }

                bool climb = true;

                while (climb)
                {
                    i = ns.Pop();
                    i.child++;
                    switch ((int)i.child)
                    {
                        case 1:
                            if (outerInstance.m_sc[i.parent] != 0)
                            {
                                res = outerInstance.m_eq[i.parent];
                                ns.Push((Item)i.Clone());
                                ks.Append(outerInstance.m_sc[i.parent]);
                            }
                            else
                            {
                                i.child++;
                                ns.Push((Item)i.Clone());
                                res = outerInstance.m_hi[i.parent];
                            }
                            climb = false;
                            break;

                        case 2:
                            res = outerInstance.m_hi[i.parent];
                            ns.Push((Item)i.Clone());
                            if (ks.Length > 0)
                            {
                                ks.Length = ks.Length - 1; // pop
                            }
                            climb = false;
                            break;

                        default:
                            if (ns.Count == 0)
                            {
                                return -1;
                            }
                            climb = true;
                            break;
                    }
                }
                return res;
            }

            /// <summary>
            /// traverse the tree to find next key
            /// </summary>
            private int Run()
            {
                if (cur == -1)
                {
                    return -1;
                }

                bool leaf = false;
                while (true)
                {
                    // first go down on low branch until leaf or compressed branch
                    while (cur != 0)
                    {
                        if (outerInstance.m_sc[cur] == 0xFFFF)
                        {
                            leaf = true;
                            break;
                        }
                        ns.Push(new Item((char)cur, '\u0000'));
                        if (outerInstance.m_sc[cur] == 0)
                        {
                            leaf = true;
                            break;
                        }
                        cur = outerInstance.m_lo[cur];
                    }
                    if (leaf)
                    {
                        break;
                    }
                    // nothing found, go up one node and try again
                    cur = Up();
                    if (cur == -1)
                    {
                        return -1;
                    }
                }
                // The current node should be a data node and
                // the key should be in the key stack (at least partially)
                StringBuilder buf = new StringBuilder(ks.ToString());
                if (outerInstance.m_sc[cur] == 0xFFFF)
                {
                    int p = outerInstance.m_lo[cur];
                    while (outerInstance.m_kv[p] != 0)
                    {
                        buf.Append(outerInstance.m_kv[p++]);
                    }
                }
                curkey = buf.ToString();
                return 0;
            }

            #region Added for better .NET support
            public string Current => curkey;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            // LUCENENET specific - implemented proper dispose pattern
            protected virtual void Dispose(bool disposing)
            {
                // nothing to do
            }

            public bool MoveNext()
            {
                if (!isInitialized)
                {
                    Rewind();
                    isInitialized = true;
                    return cur != -1;
                }
                if (cur == -1)
                {
                    return false;
                }
                cur = Up();
                Run();
                return cur != -1;
            }

            public void Reset()
            {
                throw UnsupportedOperationException.Create();
            }

            #endregion
        }

        public virtual void PrintStats(TextWriter @out)
        {
            @out.WriteLine("Number of keys = " + Convert.ToString(m_length)); // LUCENENET: Intentionally using current culture
            @out.WriteLine("Node count = " + Convert.ToString(m_freenode)); // LUCENENET: Intentionally using current culture
            // System.out.println("Array length = " + Integer.toString(eq.length));
            @out.WriteLine("Key Array length = " + Convert.ToString(m_kv.Length)); // LUCENENET: Intentionally using current culture

            /*
             * for(int i=0; i<kv.length(); i++) if ( kv.get(i) != 0 )
             * System.out.print(kv.get(i)); else System.out.println("");
             * System.out.println("Keys:"); for(Enumeration enum = keys();
             * enum.hasMoreElements(); ) System.out.println(enum.nextElement());
             */
        }
        /*
          public static void main(String[] args) {
            TernaryTree tt = new TernaryTree();
            tt.insert("Carlos", 'C');
            tt.insert("Car", 'r');
            tt.insert("palos", 'l');
            tt.insert("pa", 'p');
            tt.trimToSize();
            System.out.println((char) tt.find("Car"));
            System.out.println((char) tt.find("Carlos"));
            System.out.println((char) tt.find("alto"));
            tt.printStats(System.out);
          }
          */

    }
}