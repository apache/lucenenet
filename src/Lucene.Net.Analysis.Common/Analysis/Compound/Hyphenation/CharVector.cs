// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;

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
    /// This class implements a simple char vector with access to the underlying
    /// array.
    /// 
    /// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
    /// </summary>
    public class CharVector // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Capacity increment size
        /// </summary>
        private const int DEFAULT_BLOCK_SIZE = 2048;

        private int blockSize;

        /// <summary>
        /// The encapsulated array
        /// </summary>
        private char[] array;

        /// <summary>
        /// Points to next free item
        /// </summary>
        private int n;

        public CharVector() 
            : this(DEFAULT_BLOCK_SIZE)
        {
        }

        public CharVector(int capacity)
        {
            if (capacity > 0)
            {
                blockSize = capacity;
            }
            else
            {
                blockSize = DEFAULT_BLOCK_SIZE;
            }
            array = new char[blockSize];
            n = 0;
        }

        public CharVector(char[] a)
        {
            blockSize = DEFAULT_BLOCK_SIZE;
            array = a;
            n = a.Length;
        }

        public CharVector(char[] a, int capacity)
        {
            if (capacity > 0)
            {
                blockSize = capacity;
            }
            else
            {
                blockSize = DEFAULT_BLOCK_SIZE;
            }
            array = a;
            n = a.Length;
        }

        /// <summary>
        /// Reset Vector but don't resize or clear elements
        /// </summary>
        public virtual void Clear()
        {
            n = 0;
        }

        public virtual object Clone()
        {
            CharVector cv = new CharVector((char[])array.Clone(), blockSize);
            cv.n = this.n;
            return cv;
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] Array => array;

        /// <summary>
        /// LUCENENET indexer for .NET
        /// </summary>
        public virtual char this[int index]
        {
            get => array[index];
            set => array[index] = value;
        }

        /// <summary>
        /// return number of items in array
        /// </summary>
        public virtual int Length => n;

        /// <summary>
        /// returns current capacity of array
        /// </summary>
        public virtual int Capacity => array.Length;

        // LUCENENET NOTE: Not needed (replaced with this[])
        //public virtual void Put(int index, char val)
        //{
        //    array[index] = val;
        //}

        //public virtual char get(int index)
        //{
        //    return array[index];
        //}

        public virtual int Alloc(int size)
        {
            int index = n;
            int len = array.Length;
            if (n + size >= len)
            {
                char[] aux = new char[len + blockSize];
                Arrays.Copy(array, 0, aux, 0, len);
                array = aux;
            }
            n += size;
            return index;
        }

        public virtual void TrimToSize()
        {
            if (n < array.Length)
            {
                char[] aux = new char[n];
                Arrays.Copy(array, 0, aux, 0, n);
                array = aux;
            }
        }
    }
}