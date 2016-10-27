/*
 Copyright (c) 2003-2016 Niels Kokholm, Peter Sestoft, and Rasmus Lystrøm
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/

using Lucene.Net.Support.C5;
using System;
using SCG = System.Collections.Generic;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A sorted generic dictionary based on a red-black tree set.
    /// </summary>
    [Serializable]
    public class TreeDictionary<K, V> : SortedDictionaryBase<K, V>, IDictionary<K, V>, ISortedDictionary<K, V>
    {

        #region Constructors

        /// <summary>
        /// Create a red-black tree dictionary using the natural comparer for keys.
        /// <exception cref="ArgumentException"/> if the key type K is not comparable.
        /// </summary>
		public TreeDictionary(MemoryType memoryType = MemoryType.Normal) : this(SCG.Comparer<K>.Default, EqualityComparer<K>.Default, memoryType) { }

        /// <summary>
        /// Create a red-black tree dictionary using an external comparer for keys.
        /// </summary>
        /// <param name="comparer">The external comparer</param>
		/// <param name = "memoryType"></param>
		public TreeDictionary(SCG.IComparer<K> comparer, MemoryType memoryType = MemoryType.Normal) : this(comparer, new ComparerZeroHashCodeEqualityComparer<K>(comparer)) { }

        TreeDictionary(SCG.IComparer<K> comparer, SCG.IEqualityComparer<K> equalityComparer, MemoryType memoryType = MemoryType.Normal)
            : base(comparer, equalityComparer, memoryType)
        {
            pairs = sortedpairs = new TreeSet<KeyValuePair<K, V>>(new KeyValuePairComparer<K, V>(comparer));
            if (memoryType != MemoryType.Normal)
                throw new Exception("TreeDictionary doesn't support MemoryType Strict or Safe");

        }

        #endregion

        //TODO: put in interface
        /// <summary>
        /// Make a snapshot of the current state of this dictionary
        /// </summary>
        /// <returns>The snapshot</returns>
        public SCG.IEnumerable<KeyValuePair<K, V>> Snapshot()
        {
            TreeDictionary<K, V> res = (TreeDictionary<K, V>)MemberwiseClone();

            res.pairs = (TreeSet<KeyValuePair<K, V>>)((TreeSet<KeyValuePair<K, V>>)sortedpairs).Snapshot();
            return res;
        }
    }
}