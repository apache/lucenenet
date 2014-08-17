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


namespace Lucene.Net.Support
{
    /// <summary>
    /// Class AtomicReferenceArray.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AtomicReferenceArray<T>
    {
        // ReSharper disable once StaticFieldInGenericType
        private static readonly object s_syncLock = new object();
        private readonly T[] array;


        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicReferenceArray{T}"/> class.
        /// </summary>
        /// <param name="array">The array.</param>
        public AtomicReferenceArray(T[] array)
        {
            Check.NotNull("array", array);
            
            int length = array.Length,
                i = 0;

            this.array = new T[length];

            if (length <= 0) 
                return;

            for (; i < length; i++)
                this.Set(i, array[i]);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="AtomicReferenceArray{T}"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public AtomicReferenceArray(int capacity)
        {
            this.array = new T[capacity];
        }


        /// <summary>
        /// Gets or sets the <see cref="T"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>T.</returns>
        public T this[int index]
        {
            get { return this.Get(index); }
            set { this.Set(index, value);}
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get { return this.array.Length; }
        }

        /// <summary>
        /// Compares the value at the specified index with the expected index and sets
        /// the value if the values are equal.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="value">The update value.</param>
        /// <returns><c>true</c> if the index was updated, <c>false</c> otherwise.</returns>
        public bool CompareAndSet(int index, T expected, T value)
        {
            lock (s_syncLock)
            {
                var currentValue = this.array[index];
                if (!expected.Equals(currentValue)) 
                    return false;
                
                this.array[index] = currentValue;
                return true;
            }
        }


        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Length"/>.
        /// </exception>
        private T Get(int index)
        {
            //Check.InRangeOfLength(0, this.Length, index);

            return this.array[index];
        }


        /// <summary>
        /// Gets the old value from the specified index, updates the value, and returns
        /// the old value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The new value at the index.</param>
        /// <returns>The old <typeparam name="T" /> value.</returns>
        public T GetAndSet(int index, T value)
        {
            //Check.InRangeOfLength(0, this.Length, index);

            lock (s_syncLock)
            {
                var currentValue = this.array[index];
                this.array[index] = value;

                return currentValue;
            }
        }

        /// <summary>
        /// Sets the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Length"/>.
        /// </exception>
        private void Set(int index, T value)
        {
          

            lock (s_syncLock)
            {
                array[index] = value;
            }
        }
    }
}
