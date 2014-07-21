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


namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;


    /// <summary>
    /// 
    /// </summary>
    public abstract class Sorter : IComparer<int>
    {
        static readonly int THRESHOLD = 20;


        protected Sorter() { }

        /// <summary>
        /// Sort a slice or range which begins at the <paramref name="start"/> index to the <paramref name="end"/> index.
        /// </summary>
        /// <param name="start">The position to start the slice.</param>
        /// <param name="end">The position to end the slice. </param>
        /// <exception cref="IndexOutOfRangeException">Throws when start is greater or equal the length or when the start + count </exception>
        public abstract void SortSlice(int start, int end);


        /// <summary>
        /// Performs a comparison of two integers and returns a value whether the values are equal to, less than, or greater than
        /// the other value.
        /// </summary>
        /// <param name="x">The left value</param>
        /// <param name="y">The right value.</param>
        /// <returns>Returns 0 if the values are equal, -1 if the value x is less than y, and 1 if the value x is greater than y.</returns>
        protected abstract int Compare(int x, int y);

        /// <summary>
        /// Switchs the index position of the values. 
        /// </summary>
        /// <param name="x">The left value.</param>
        /// <param name="y">The right value.</param>
        protected abstract void Swap(int x, int y);

        /// <summary>
        /// Throws an exception when start is greater than end.
        /// </summary>
        /// <param name="start">The start index position.</param>
        /// <param name="end">the end index position.</param>
        protected void CheckSlice(int start, int end)
        {
            if(start > end)
            {
                string message = string.Format("The start parameter must be less than the end parameter." +
                    " start was {0} and end was {1}", start, end);

                throw new ArgumentException(message);
            }
        }


        protected void MergeInPlace(int start, int middle, int end)
        {

        }

        #region IComparer<int>

        int IComparer<int>.Compare(int x, int y)
        {
            return this.Compare(x, y);
        }

        #endregion
    }
}
