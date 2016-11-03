// Copyright (c) 2005 Bruno Martins
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 1. Redistributions of source code must retain the above copyright 
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the organization nor the names of its contributors
//    may be used to endorse or promote products derived from this software
//    without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Lucene.Net.Search.Suggest.Jaspell
{
    /// <summary>
    /// Implementation of a Ternary Search Trie, a data structure for storing
    /// <see cref="string"/>s that combines the compact size of a binary search
    /// tree with the speed of a digital search trie, and is therefore ideal for
    /// practical use in sorting and searching data.</p>
    /// <para>
    /// 
    /// This data structure is faster than hashing for many typical search problems,
    /// and supports a broader range of useful problems and operations. Ternary
    /// searches are faster than hashing and more powerful, too.
    /// </para>
    /// <para>
    /// 
    /// The theory of ternary search trees was described at a symposium in 1997 (see
    /// "Fast Algorithms for Sorting and Searching Strings," by J.L. Bentley and R.
    /// Sedgewick, Proceedings of the 8th Annual ACM-SIAM Symposium on Discrete
    /// Algorithms, January 1997). Algorithms in C, Third Edition, by Robert
    /// Sedgewick (Addison-Wesley, 1998) provides yet another view of ternary search
    /// trees.
    /// </para>
    /// </summary>
    public class JaspellTernarySearchTrie
    {

        /// <summary>
        /// An inner class of Ternary Search Trie that represents a node in the trie.
        /// </summary>
        public sealed class TSTNode
        {
            private readonly JaspellTernarySearchTrie outerInstance;


            /// <summary>
            /// Index values for accessing relatives array. </summary>
            internal const int PARENT = 0, LOKID = 1, EQKID = 2, HIKID = 3;

            /// <summary>
            /// The key to the node. </summary>
            internal object data;

            /// <summary>
            /// The relative nodes. </summary>
            internal readonly TSTNode[] relatives = new TSTNode[4];

            /// <summary>
            /// The char used in the split. </summary>
            internal char splitchar;

            /// <summary>
            /// Constructor method.
            /// </summary>
            /// <param name="splitchar">
            ///          The char used in the split. </param>
            /// <param name="parent">
            ///          The parent node. </param>
            internal TSTNode(JaspellTernarySearchTrie outerInstance, char splitchar, TSTNode parent)
            {
                this.outerInstance = outerInstance;
                this.splitchar = splitchar;
                relatives[PARENT] = parent;
            }

            /// <summary>
            /// Return an approximate memory usage for this node and its sub-nodes. </summary>
            public long GetSizeInBytes()
            {
                long mem = RamUsageEstimator.ShallowSizeOf(this) + RamUsageEstimator.ShallowSizeOf(relatives);
                foreach (TSTNode node in relatives)
                {
                    // LUCENENET NOTE: Going with the summary of this method, which says it should not
                    // include the parent node. When we include the parent node, it results in overflowing
                    // the thread stack because we have infinite recursion.
                    //
                    // However, in version 6.2 of Lucene (latest) it mentions we should also estimate the parent node.
                    // https://github.com/apache/lucene-solr/blob/764d0f19151dbff6f5fcd9fc4b2682cf934590c5/lucene/suggest/src/java/org/apache/lucene/search/suggest/jaspell/JaspellTernarySearchTrie.java#L104
                    // Not sure what the reason for that is, but it seems like a recipe for innaccuracy.
                    if (node != null && node != relatives[PARENT])
                    {
                        mem += node.GetSizeInBytes();
                    }
                }
                return mem;
            }
        }

        /// <summary>
        /// Compares characters by alfabetical order.
        /// </summary>
        /// <param name="cCompare2">
        ///          The first char in the comparison. </param>
        /// <param name="cRef">
        ///          The second char in the comparison. </param>
        /// <param name="culture">The culture used for lowercasing.</param>
        /// <returns> A negative number, 0 or a positive number if the second char is
        ///         less, equal or greater. </returns>
        ///         
        private static int CompareCharsAlphabetically(char cCompare2, char cRef, CultureInfo culture)
        {
            var textInfo = culture.TextInfo;
            return textInfo.ToLower(cCompare2) - textInfo.ToLower(cRef);
        }

        /* what follows is the original Jaspell code. 
        private static int compareCharsAlphabetically(int cCompare2, int cRef) {
        int cCompare = 0;
        if (cCompare2 >= 65) {
            if (cCompare2 < 89) {
            cCompare = (2 * cCompare2) - 65;
            } else if (cCompare2 < 97) {
            cCompare = cCompare2 + 24;
            } else if (cCompare2 < 121) {
            cCompare = (2 * cCompare2) - 128;
            } else cCompare = cCompare2;
        } else cCompare = cCompare2;
        if (cRef < 65) {
            return cCompare - cRef;
        }
        if (cRef < 89) {
            return cCompare - ((2 * cRef) - 65);
        }
        if (cRef < 97) {
            return cCompare - (cRef + 24);
        }
        if (cRef < 121) {
            return cCompare - ((2 * cRef) - 128);
        }
        return cCompare - cRef;
        }
        */

        /// <summary>
        /// The default number of values returned by the <see cref="MatchAlmost"/>
        /// method.
        /// </summary>
        private int defaultNumReturnValues = -1;

        /// <summary>
        /// the number of differences allowed in a call to the <see cref="MatchAlmost"/>
        /// <paramref cref="key"/>.
        /// </summary>
        private int matchAlmostDiff;

        /// <summary>
        /// The base node in the trie. </summary>
        private TSTNode rootNode;

        // LUCENENET NOTE: Renamed from locale to culture.
        private readonly CultureInfo culture;

        /// <summary>
        /// Constructs an empty Ternary Search Trie.
        /// </summary>
        public JaspellTernarySearchTrie()
            : this(CultureInfo.CurrentCulture)
        {
        }

        /// <summary>
        /// Constructs an empty Ternary Search Trie,
        /// specifying the <see cref="CultureInfo"/> used for lowercasing.
        /// </summary>
        public JaspellTernarySearchTrie(CultureInfo culture)
        {
            this.culture = culture;
        }

        // for loading
        internal virtual TSTNode Root
        {
            set
            {
                rootNode = value;
            }
            get
            {
                return rootNode;
            }
        }


        /// <summary>
        /// Constructs a Ternary Search Trie and loads data from a <see cref="FileInfo"/>
        /// into the Trie. The file is a normal text document, where each line is of
        /// the form word TAB float.
        /// 
        /// <para>Uses the culture of the current thread to lowercase words before comparing.</para>
        /// </summary>
        /// <param name="file">
        ///          The <see cref="FileInfo"/> with the data to load into the Trie. </param>
        /// <exception cref="System.IO.IOException">
        ///              A problem occured while reading the data. </exception>
        public JaspellTernarySearchTrie(FileInfo file)
            : this(file, false, CultureInfo.CurrentCulture)
        {
        }

        /// <summary>
        /// Constructs a Ternary Search Trie and loads data from a <see cref="FileInfo"/>
        /// into the Trie. The file is a normal text document, where each line is of
        /// the form word TAB float.
        /// 
        /// <para>Uses the supplied culture to lowercase words before comparing.</para>
        /// </summary>
        /// <param name="file">
        ///          The <see cref="FileInfo"/> with the data to load into the Trie. </param>
        /// <param name="culture">The culture used for lowercasing.</param>
        /// <exception cref="System.IO.IOException">
        ///              A problem occured while reading the data. </exception>
        public JaspellTernarySearchTrie(FileInfo file, CultureInfo culture)
            : this(file, false, culture)
        {
        }

        /// <summary>
        /// Constructs a Ternary Search Trie and loads data from a <see cref="FileInfo"/>
        /// into the Trie. The file is a normal text document, where each line is of
        /// the form "word TAB float".
        /// 
        /// <para>Uses the culture of the current thread to lowercase words before comparing.</para>
        /// </summary>
        /// <param name="file">
        ///          The <see cref="FileInfo"/> with the data to load into the Trie. </param>
        /// <param name="compression">
        ///          If true, the file is compressed with the GZIP algorithm, and if
        ///          false, the file is a normal text document. </param>
        /// <exception cref="System.IO.IOException">
        ///              A problem occured while reading the data. </exception>
        public JaspellTernarySearchTrie(FileInfo file, bool compression)
            : this(file, compression, CultureInfo.CurrentCulture)
        { }

        /// <summary>
        /// Constructs a Ternary Search Trie and loads data from a <see cref="FileInfo"/>
        /// into the Trie. The file is a normal text document, where each line is of
        /// the form "word TAB float".
        /// 
        /// <para>Uses the supplied culture to lowercase words before comparing.</para>
        /// </summary>
        /// <param name="file">
        ///          The <see cref="FileInfo"/> with the data to load into the Trie. </param>
        /// <param name="compression">
        ///          If true, the file is compressed with the GZIP algorithm, and if
        ///          false, the file is a normal text document. </param>
        /// <param name="culture">The culture used for lowercasing.</param>
        /// <exception cref="System.IO.IOException">
        ///              A problem occured while reading the data. </exception>
        public JaspellTernarySearchTrie(FileInfo file, bool compression, CultureInfo culture)
            : this(culture)
        {
            using (TextReader @in = (compression) ?
                IOUtils.GetDecodingReader(new GZipStream(new FileStream(file.FullName, FileMode.Open), CompressionMode.Decompress), Encoding.UTF8) :
                IOUtils.GetDecodingReader(new FileStream(file.FullName, FileMode.Open), Encoding.UTF8))
            {
                string word;
                int pos;
                float? occur, one = new float?(1);
                while ((word = @in.ReadLine()) != null)
                {
                    pos = word.IndexOf('\t');
                    occur = one;
                    if (pos != -1)
                    {
                        occur = Convert.ToSingle(word.Substring(pos + 1).Trim());
                        word = word.Substring(0, pos);
                    }
                    string key = culture.TextInfo.ToLower(word);
                    if (rootNode == null)
                    {
                        rootNode = new TSTNode(this, key[0], null);
                    }
                    TSTNode node = null;
                    if (key.Length > 0 && rootNode != null)
                    {
                        TSTNode currentNode = rootNode;
                        int charIndex = 0;
                        while (true)
                        {
                            if (currentNode == null)
                            {
                                break;
                            }
                            int charComp = CompareCharsAlphabetically(key[charIndex], currentNode.splitchar, culture);
                            if (charComp == 0)
                            {
                                charIndex++;
                                if (charIndex == key.Length)
                                {
                                    node = currentNode;
                                    break;
                                }
                                currentNode = currentNode.relatives[TSTNode.EQKID];
                            }
                            else if (charComp < 0)
                            {
                                currentNode = currentNode.relatives[TSTNode.LOKID];
                            }
                            else
                            {
                                currentNode = currentNode.relatives[TSTNode.HIKID];
                            }
                        }
                        float? occur2 = null;
                        if (node != null)
                        {
                            occur2 = ((float?)(node.data));
                        }
                        if (occur2 != null)
                        {
                            occur += (float)occur2;
                        }
                        currentNode = GetOrCreateNode(culture.TextInfo.ToLower(word.Trim()));
                        currentNode.data = occur;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the node passed in as an argument. If this node has non-null data,
        /// then both the node and the data will be deleted. It also deletes any other
        /// nodes in the trie that are no longer needed after the deletion of the node.
        /// </summary>
        /// <param name="nodeToDelete"> The node to delete. </param>
        private void DeleteNode(TSTNode nodeToDelete)
        {
            if (nodeToDelete == null)
            {
                return;
            }
            nodeToDelete.data = null;
            while (nodeToDelete != null)
            {
                nodeToDelete = DeleteNodeRecursion(nodeToDelete);
                // deleteNodeRecursion(nodeToDelete);
            }
        }

        /// <summary>
        /// Recursively visits each node to be deleted.
        /// 
        /// To delete a node, first set its data to null, then pass it into this
        /// method, then pass the node returned by this method into this method (make
        /// sure you don't delete the data of any of the nodes returned from this
        /// method!) and continue in this fashion until the node returned by this
        /// method is <c>null</c>.
        /// 
        /// The TSTNode instance returned by this method will be next node to be
        /// operated on by <see cref="DeleteNodeRecursion(TSTNode)"/> (This emulates recursive
        /// method call while avoiding the overhead normally associated with a
        /// recursive method.)
        /// </summary>
        /// <param name="currentNode"> The node to delete. </param>
        /// <returns> The next node to be called in deleteNodeRecursion. </returns>
        private TSTNode DeleteNodeRecursion(TSTNode currentNode)
        {
            if (currentNode == null)
            {
                return null;
            }
            if (currentNode.relatives[TSTNode.EQKID] != null || currentNode.data != null)
            {
                return null;
            }
            // can't delete this node if it has a non-null eq kid or data
            TSTNode currentParent = currentNode.relatives[TSTNode.PARENT];
            bool lokidNull = currentNode.relatives[TSTNode.LOKID] == null;
            bool hikidNull = currentNode.relatives[TSTNode.HIKID] == null;
            int childType;
            if (currentParent.relatives[TSTNode.LOKID] == currentNode)
            {
                childType = TSTNode.LOKID;
            }
            else if (currentParent.relatives[TSTNode.EQKID] == currentNode)
            {
                childType = TSTNode.EQKID;
            }
            else if (currentParent.relatives[TSTNode.HIKID] == currentNode)
            {
                childType = TSTNode.HIKID;
            }
            else
            {
                rootNode = null;
                return null;
            }
            if (lokidNull && hikidNull)
            {
                currentParent.relatives[childType] = null;
                return currentParent;
            }
            if (lokidNull)
            {
                currentParent.relatives[childType] = currentNode.relatives[TSTNode.HIKID];
                currentNode.relatives[TSTNode.HIKID].relatives[TSTNode.PARENT] = currentParent;
                return currentParent;
            }
            if (hikidNull)
            {
                currentParent.relatives[childType] = currentNode.relatives[TSTNode.LOKID];
                currentNode.relatives[TSTNode.LOKID].relatives[TSTNode.PARENT] = currentParent;
                return currentParent;
            }
            int deltaHi = currentNode.relatives[TSTNode.HIKID].splitchar - currentNode.splitchar;
            int deltaLo = currentNode.splitchar - currentNode.relatives[TSTNode.LOKID].splitchar;
            int movingKid;
            TSTNode targetNode;
            if (deltaHi == deltaLo)
            {
                if (new Random(1).NextDouble() < 0.5)
                {
                    deltaHi++;
                }
                else
                {
                    deltaLo++;
                }
            }
            if (deltaHi > deltaLo)
            {
                movingKid = TSTNode.HIKID;
                targetNode = currentNode.relatives[TSTNode.LOKID];
            }
            else
            {
                movingKid = TSTNode.LOKID;
                targetNode = currentNode.relatives[TSTNode.HIKID];
            }
            while (targetNode.relatives[movingKid] != null)
            {
                targetNode = targetNode.relatives[movingKid];
            }
            targetNode.relatives[movingKid] = currentNode.relatives[movingKid];
            currentParent.relatives[childType] = targetNode;
            targetNode.relatives[TSTNode.PARENT] = currentParent;
            if (!lokidNull)
            {
                currentNode.relatives[TSTNode.LOKID] = null;
            }
            if (!hikidNull)
            {
                currentNode.relatives[TSTNode.HIKID] = null;
            }
            return currentParent;
        }

        /// <summary>
        /// Retrieve the object indexed by a key.
        /// </summary>
        /// <param name="key"> A <see cref="string"/> index. </param>
        /// <returns> The object retrieved from the Ternary Search Trie. </returns>
        public virtual object Get(string key)
        {
            TSTNode node = GetNode(key);
            if (node == null)
            {
                return null;
            }
            return node.data;
        }

        /// <summary>
        /// Retrieve the <see cref="System.Nullable{float}"/> indexed by key, increment it by one unit
        /// and store the new <see cref="System.Nullable{float}"/>.
        /// </summary>
        /// <param name="key"> A <see cref="string"/> index. </param>
        /// <param name="culture">The culture used for lowercasing.</param>
        /// <returns> The <see cref="System.Nullable{float}"/> retrieved from the Ternary Search Trie. </returns>
        public virtual float? GetAndIncrement(string key)
        {
            string key2 = culture.TextInfo.ToLower(key.Trim());
            TSTNode node = GetNode(key2);
            if (node == null)
            {
                return null;
            }
            float? aux = (float?)(node.data);
            if (aux == null)
            {
                aux = new float?(1);
            }
            else
            {
                aux = new float?((int)aux + 1);
            }
            Put(key2, aux);
            return aux;
        }

        /// <summary>
        /// Returns the key that indexes the node argument.
        /// </summary>
        /// <param name="node">
        ///          The node whose index is to be calculated. </param>
        /// <returns> The <see cref="string"/> that indexes the node argument. </returns>
        protected internal virtual string GetKey(TSTNode node)
        {
            StringBuilder getKeyBuffer = new StringBuilder();
            getKeyBuffer.Length = 0;
            getKeyBuffer.Append("" + node.splitchar);
            TSTNode currentNode;
            TSTNode lastNode;
            currentNode = node.relatives[TSTNode.PARENT];
            lastNode = node;
            while (currentNode != null)
            {
                if (currentNode.relatives[TSTNode.EQKID] == lastNode)
                {
                    getKeyBuffer.Append("" + currentNode.splitchar);
                }
                lastNode = currentNode;
                currentNode = currentNode.relatives[TSTNode.PARENT];
            }

            // LUCENENET NOTE: Reverse doesn't happen in place in a .NET StringBuilder,
            // so we need to return the reversed result.
            return getKeyBuffer.Reverse().ToString();
        }

        /// <summary>
        /// Returns the node indexed by key, or <c>null</c> if that node doesn't
        /// exist. Search begins at root node.
        /// </summary>
        /// <param name="key">
        ///          A <see cref="string"/> that indexes the node that is returned. </param>
        /// <returns> The node object indexed by key. This object is an instance of an
        ///         inner class named <see cref="TSTNode"/>. </returns>
        public virtual TSTNode GetNode(string key)
        {
            return GetNode(key, rootNode);
        }

        /// <summary>
        /// Returns the node indexed by key, or <c>null</c> if that node doesn't
        /// exist. The search begins at root node.
        /// </summary>
        /// <param name="key">
        ///          A <see cref="string"/> that indexes the node that is returned. </param>
        /// <param name="startNode">
        ///          The top node defining the subtrie to be searched. </param>
        /// <returns> The node object indexed by key. This object is an instance of an
        ///         inner class named <see cref="TSTNode"/>. </returns>
        protected internal virtual TSTNode GetNode(string key, TSTNode startNode)
        {
            if (key == null || startNode == null || key.Length == 0)
            {
                return null;
            }
            TSTNode currentNode = startNode;
            int charIndex = 0;
            while (true)
            {
                if (currentNode == null)
                {
                    return null;
                }
                int charComp = CompareCharsAlphabetically(key[charIndex], currentNode.splitchar, this.culture);
                if (charComp == 0)
                {
                    charIndex++;
                    if (charIndex == key.Length)
                    {
                        return currentNode;
                    }
                    currentNode = currentNode.relatives[TSTNode.EQKID];
                }
                else if (charComp < 0)
                {
                    currentNode = currentNode.relatives[TSTNode.LOKID];
                }
                else
                {
                    currentNode = currentNode.relatives[TSTNode.HIKID];
                }
            }
        }

        /// <summary>
        /// Returns the node indexed by key, creating that node if it doesn't exist,
        /// and creating any required intermediate nodes if they don't exist.
        /// </summary>
        /// <param name="key">
        ///          A <see cref="string"/> that indexes the node that is returned. </param>
        /// <returns> The node object indexed by key. This object is an instance of an
        ///         inner class named <see cref="TSTNode"/>. </returns>
        /// <exception cref="NullReferenceException">
        ///              If the key is <c>null</c>. </exception>
        /// <exception cref="ArgumentException">
        ///              If the key is an empty <see cref="string"/>. </exception>
        protected internal virtual TSTNode GetOrCreateNode(string key)
        {
            if (key == null)
            {
                throw new NullReferenceException("attempt to get or create node with null key");
            }
            if (key.Length == 0)
            {
                throw new System.ArgumentException("attempt to get or create node with key of zero length");
            }
            if (rootNode == null)
            {
                rootNode = new TSTNode(this, key[0], null);
            }
            TSTNode currentNode = rootNode;
            int charIndex = 0;
            while (true)
            {
                int charComp = CompareCharsAlphabetically(key[charIndex], currentNode.splitchar, this.culture);
                if (charComp == 0)
                {
                    charIndex++;
                    if (charIndex == key.Length)
                    {
                        return currentNode;
                    }
                    if (currentNode.relatives[TSTNode.EQKID] == null)
                    {
                        currentNode.relatives[TSTNode.EQKID] = new TSTNode(this, key[charIndex], currentNode);
                    }
                    currentNode = currentNode.relatives[TSTNode.EQKID];
                }
                else if (charComp < 0)
                {
                    if (currentNode.relatives[TSTNode.LOKID] == null)
                    {
                        currentNode.relatives[TSTNode.LOKID] = new TSTNode(this, key[charIndex], currentNode);
                    }
                    currentNode = currentNode.relatives[TSTNode.LOKID];
                }
                else
                {
                    if (currentNode.relatives[TSTNode.HIKID] == null)
                    {
                        currentNode.relatives[TSTNode.HIKID] = new TSTNode(this, key[charIndex], currentNode);
                    }
                    currentNode = currentNode.relatives[TSTNode.HIKID];
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="List{String}"/> of keys that almost match the argument key.
        /// Keys returned will have exactly diff characters that do not match the
        /// target key, where diff is equal to the last value set
        /// to the <see cref="MatchAlmostDiff"/> property.
        /// <para>
        /// If the <see cref="MatchAlmost"/> method is called before the
        /// <see cref="MatchAlmostDiff"/> property has been called for the first time,
        /// then diff = 0.
        /// 
        /// </para>
        /// </summary>
        /// <param name="key">
        ///          The target key. </param>
        /// <returns> A <see cref="List{String}"/> with the results. </returns>
        public virtual List<string> MatchAlmost(string key)
        {
            return MatchAlmost(key, defaultNumReturnValues);
        }

        /// <summary>
        /// Returns a <see cref="List{String}"/> of keys that almost match the argument key.
        /// Keys returned will have exactly diff characters that do not match the
        /// target key, where diff is equal to the last value set
        /// to the <see cref="MatchAlmostDiff"/> property.
        /// <para>
        /// If the <see cref="MatchAlmost"/> method is called before the
        /// <see cref="MatchAlmostDiff"/> property has been called for the first time,
        /// then diff = 0.
        /// 
        /// </para>
        /// </summary>
        /// <param name="key"> The target key. </param>
        /// <param name="numReturnValues"> The maximum number of values returned by this method. </param>
        /// <returns> A <see cref="List{String}"/> with the results </returns>
        public virtual List<string> MatchAlmost(string key, int numReturnValues)
        {
            return MatchAlmostRecursion(rootNode, 0, matchAlmostDiff, key, ((numReturnValues < 0) ? -1 : numReturnValues), new List<string>(), false);
        }

        /// <summary>
        /// Recursivelly vists the nodes in order to find the ones that almost match a
        /// given key.
        /// </summary>
        /// <param name="currentNode">
        ///          The current node. </param>
        /// <param name="charIndex">
        ///          The current char. </param>
        /// <param name="d">
        ///          The number of differences so far. </param>
        /// <param name="matchAlmostNumReturnValues">
        ///          The maximum number of values in the result <see cref="List{String}"/>. </param>
        /// <param name="matchAlmostResult2">
        ///          The results so far. </param>
        /// <param name="upTo">
        ///          If true all keys having up to and including <see cref="MatchAlmostDiff"/> 
        ///          mismatched letters will be included in the result (including a key
        ///          that is exactly the same as the target string) otherwise keys will
        ///          be included in the result only if they have exactly
        ///          <see cref="MatchAlmostDiff"/> number of mismatched letters. </param>
        /// <param name="matchAlmostKey">
        ///          The key being searched. </param>
        /// <returns> A <see cref="List{String}"/> with the results. </returns>
        private List<string> MatchAlmostRecursion(TSTNode currentNode, int charIndex, int d, string matchAlmostKey, int matchAlmostNumReturnValues, List<string> matchAlmostResult2, bool upTo)
        {
            if ((currentNode == null) || (matchAlmostNumReturnValues != -1 && matchAlmostResult2.Count >= matchAlmostNumReturnValues) || (d < 0) || (charIndex >= matchAlmostKey.Length))
            {
                return matchAlmostResult2;
            }
            int charComp = CompareCharsAlphabetically(matchAlmostKey[charIndex], currentNode.splitchar, this.culture);
            List<string> matchAlmostResult = matchAlmostResult2;
            if ((d > 0) || (charComp < 0))
            {
                matchAlmostResult = MatchAlmostRecursion(currentNode.relatives[TSTNode.LOKID], charIndex, d, matchAlmostKey, matchAlmostNumReturnValues, matchAlmostResult, upTo);
            }
            int nextD = (charComp == 0) ? d : d - 1;
            bool cond = (upTo) ? (nextD >= 0) : (nextD == 0);
            if ((matchAlmostKey.Length == charIndex + 1) && cond && (currentNode.data != null))
            {
                matchAlmostResult.Add(GetKey(currentNode));
            }
            matchAlmostResult = MatchAlmostRecursion(currentNode.relatives[TSTNode.EQKID], charIndex + 1, nextD, matchAlmostKey, matchAlmostNumReturnValues, matchAlmostResult, upTo);
            if ((d > 0) || (charComp > 0))
            {
                matchAlmostResult = MatchAlmostRecursion(currentNode.relatives[TSTNode.HIKID], charIndex, d, matchAlmostKey, matchAlmostNumReturnValues, matchAlmostResult, upTo);
            }
            return matchAlmostResult;
        }

        /// <summary>
        /// Returns an alphabetical <see cref="List{String}"/> of all keys in the trie that
        /// begin with a given prefix. Only keys for nodes having non-null data are
        /// included in the <see cref="List{String}"/>.
        /// </summary>
        /// <param name="prefix"> Each key returned from this method will begin with the characters
        ///          in prefix. </param>
        /// <returns> A <see cref="List{String}"/> with the results. </returns>
        public virtual List<string> MatchPrefix(string prefix)
        {
            return MatchPrefix(prefix, defaultNumReturnValues);
        }

        /// <summary>
        /// Returns an alphabetical <see cref="List{String}"/> of all keys in the trie that
        /// begin with a given prefix. Only keys for nodes having non-null data are
        /// included in the <see cref="List{String}"/>.
        /// </summary>
        /// <param name="prefix"> Each key returned from this method will begin with the characters
        ///          in prefix. </param>
        /// <param name="numReturnValues"> The maximum number of values returned from this method. </param>
        /// <returns> A <see cref="List{String}"/> with the results </returns>
        public virtual List<string> MatchPrefix(string prefix, int numReturnValues)
        {
            List<string> sortKeysResult = new List<string>();
            TSTNode startNode = GetNode(prefix);
            if (startNode == null)
            {
                return sortKeysResult;
            }
            if (startNode.data != null)
            {
                sortKeysResult.Add(GetKey(startNode));
            }
            return SortKeysRecursion(startNode.relatives[TSTNode.EQKID], ((numReturnValues < 0) ? -1 : numReturnValues), sortKeysResult);
        }

        /// <summary>
        /// Returns the number of nodes in the trie that have non-null data.
        /// </summary>
        /// <returns> The number of nodes in the trie that have non-null data. </returns>
        public virtual int NumDataNodes()
        {
            return NumDataNodes(rootNode);
        }

        /// <summary>
        /// Returns the number of nodes in the subtrie below and including the starting
        /// node. The method counts only nodes that have non-null data.
        /// </summary>
        /// <param name="startingNode">
        ///          The top node of the subtrie. the node that defines the subtrie. </param>
        /// <returns> The total number of nodes in the subtrie. </returns>
        protected internal virtual int NumDataNodes(TSTNode startingNode)
        {
            return RecursiveNodeCalculator(startingNode, true, 0);
        }

        /// <summary>
        /// Returns the total number of nodes in the trie. The method counts nodes
        /// whether or not they have data.
        /// </summary>
        /// <returns> The total number of nodes in the trie. </returns>
        public virtual int NumNodes()
        {
            return NumNodes(rootNode);
        }

        /// <summary>
        /// Returns the total number of nodes in the subtrie below and including the
        /// starting Node. The method counts nodes whether or not they have data.
        /// </summary>
        /// <param name="startingNode">
        ///          The top node of the subtrie. The node that defines the subtrie. </param>
        /// <returns> The total number of nodes in the subtrie. </returns>
        protected internal virtual int NumNodes(TSTNode startingNode)
        {
            return RecursiveNodeCalculator(startingNode, false, 0);
        }

        /// <summary>
        /// Stores a value in the trie. The value may be retrieved using the key.
        /// </summary>
        /// <param name="key"> A <see cref="string"/> that indexes the object to be stored. </param>
        /// <param name="value"> The object to be stored in the Trie. </param>
        public virtual void Put(string key, object value)
        {
            GetOrCreateNode(key).data = value;
        }

        /// <summary>
        /// Recursivelly visists each node to calculate the number of nodes.
        /// </summary>
        /// <param name="currentNode">
        ///          The current node. </param>
        /// <param name="checkData">
        ///          If true we check the data to be different of <c>null</c>. </param>
        /// <param name="numNodes2">
        ///          The number of nodes so far. </param>
        /// <returns> The number of nodes accounted. </returns>
        private int RecursiveNodeCalculator(TSTNode currentNode, bool checkData, int numNodes2)
        {
            if (currentNode == null)
            {
                return numNodes2;
            }
            int numNodes = RecursiveNodeCalculator(currentNode.relatives[TSTNode.LOKID], checkData, numNodes2);
            numNodes = RecursiveNodeCalculator(currentNode.relatives[TSTNode.EQKID], checkData, numNodes);
            numNodes = RecursiveNodeCalculator(currentNode.relatives[TSTNode.HIKID], checkData, numNodes);
            if (checkData)
            {
                if (currentNode.data != null)
                {
                    numNodes++;
                }
            }
            else
            {
                numNodes++;
            }
            return numNodes;
        }

        /// <summary>
        /// Removes the value indexed by key. Also removes all nodes that are rendered
        /// unnecessary by the removal of this data.
        /// </summary>
        /// <param name="key"> A <see cref="string"/> that indexes the object to be removed from
        ///          the Trie. </param>
        public virtual void Remove(string key)
        {
            DeleteNode(GetNode(this.culture.TextInfo.ToLower(key.Trim())));
        }

        /// <summary>
        /// Sets the number of characters by which words can differ from target word
        /// when calling the <see cref="MatchAlmost"/> method.
        /// <para>
        /// Arguments less than 0 will set the char difference to 0, and arguments
        /// greater than 3 will set the char difference to 3.
        /// 
        /// </para>
        /// </summary>
        /// <param name="diff">
        ///          The number of characters by which words can differ from target
        ///          word. </param>
        public virtual int MatchAlmostDiff
        {
            set
            {
                if (value < 0)
                {
                    matchAlmostDiff = 0;
                }
                else if (value > 3)
                {
                    matchAlmostDiff = 3;
                }
                else
                {
                    matchAlmostDiff = value;
                }
            }
        }

        /// <summary>
        /// Sets the default maximum number of values returned from the
        /// <see cref="MatchPrefix"/> and <see cref="MatchAlmost"/> methods.
        /// <para>
        /// The value should be set this to -1 to get an unlimited number of return
        /// values. note that the methods mentioned above provide overloaded versions
        /// that allow you to specify the maximum number of return values, in which
        /// case this value is temporarily overridden.
        /// </para>
        /// </summary>
        /// <param name="num"> The number of values that will be returned when calling the
        ///          methods above. </param>
        public virtual int NumReturnValues
        {
            set
            {
                defaultNumReturnValues = (value < 0) ? -1 : value;
            }
        }

        /// <summary>
        /// Returns keys sorted in alphabetical order. This includes the start Node and
        /// all nodes connected to the start Node.
        /// <para>
        /// The number of keys returned is limited to numReturnValues. To get a list
        /// that isn't limited in size, set numReturnValues to -1.
        /// 
        /// </para>
        /// </summary>
        /// <param name="startNode">
        ///          The top node defining the subtrie to be searched. </param>
        /// <param name="numReturnValues">
        ///          The maximum number of values returned from this method. </param>
        /// <returns> A <see cref="List{String}"/> with the results. </returns>
        protected internal virtual List<string> SortKeys(TSTNode startNode, int numReturnValues)
        {
            return SortKeysRecursion(startNode, ((numReturnValues < 0) ? -1 : numReturnValues), new List<string>());
        }

        /// <summary>
        /// Returns keys sorted in alphabetical order. This includes the current Node
        /// and all nodes connected to the current Node.
        /// <para>
        /// Sorted keys will be appended to the end of the resulting <see cref="List{String}"/>.
        /// The result may be empty when this method is invoked, but may not be
        /// <c>null</c>.
        /// 
        /// </para>
        /// </summary>
        /// <param name="currentNode">
        ///          The current node. </param>
        /// <param name="sortKeysNumReturnValues">
        ///          The maximum number of values in the result. </param>
        /// <param name="sortKeysResult2">
        ///          The results so far. </param>
        /// <returns> A <see cref="List{String}"/> with the results. </returns>
        private List<string> SortKeysRecursion(TSTNode currentNode, int sortKeysNumReturnValues, List<string> sortKeysResult2)
        {
            if (currentNode == null)
            {
                return sortKeysResult2;
            }
            List<string> sortKeysResult = SortKeysRecursion(currentNode.relatives[TSTNode.LOKID], sortKeysNumReturnValues, sortKeysResult2);
            if (sortKeysNumReturnValues != -1 && sortKeysResult.Count >= sortKeysNumReturnValues)
            {
                return sortKeysResult;
            }
            if (currentNode.data != null)
            {
                sortKeysResult.Add(GetKey(currentNode));
            }
            sortKeysResult = SortKeysRecursion(currentNode.relatives[TSTNode.EQKID], sortKeysNumReturnValues, sortKeysResult);
            return SortKeysRecursion(currentNode.relatives[TSTNode.HIKID], sortKeysNumReturnValues, sortKeysResult);
        }

        /// <summary>
        /// Return an approximate memory usage for this trie.
        /// </summary>
        public virtual long GetSizeInBytes()
        {
            long mem = RamUsageEstimator.ShallowSizeOf(this);
            TSTNode root = Root;
            if (root != null)
            {
                mem += root.GetSizeInBytes();
            }
            return mem;
        }
    }
}