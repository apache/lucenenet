// -----------------------------------------------------------------------
// <copyright company="Apache" file="CharListCharSequenceWrapper.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Creates a <see cref="Char"/> <see cref="List{T}"/> wrapper for <see cref="ICharSequence"/>.
    /// </summary>
    public class CharListCharSequenceWrapper : ICharSequence
    {
        private readonly IList<char> value;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharListCharSequenceWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public CharListCharSequenceWrapper(IList<char> value)
        {
            this.value = value;
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get { return this.value.Count; }
        }

        /// <summary>
        /// Finds the <see cref="char"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>An instance of <see cref="Char"/>.</returns>
        public char CharAt(int index)
        {
            return this.value[index];
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///  <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return CharSequenceExtensions.IsCharSequenceEqual(this, obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return CharSequenceExtensions.CreateHashCode(this);
        }

        /// <summary>
        /// Gets the subset sequence of characters from the current sequence.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>
        /// An instance of <see cref="ICharSequence"/>.
        /// </returns>
        public ICharSequence SubSequence(int start, int end)
        {
            var list = this.value.Skip(start).Take(end - start).ToList();
            return new CharListCharSequenceWrapper(list);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return new string(this.value.ToArray());
        }
    }
}
