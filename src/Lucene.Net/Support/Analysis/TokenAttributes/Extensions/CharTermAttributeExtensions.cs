using System;

namespace Lucene.Net.Analysis.TokenAttributes.Extensions
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
    /// Extension methods on <see cref="ICharTermAttribute"/>.
    /// </summary>
    public static class CharTermAttributeExtensions
    {
        /// <summary>
        /// Set number of valid characters (length of the term) in
        /// the termBuffer array. Use this to truncate the termBuffer
        /// or to synchronize with external manipulation of the termBuffer.
        /// Note: to grow the size of the array,
        /// use <see cref="ICharTermAttribute.ResizeBuffer(int)"/> first.
        /// <para />
        /// NOTE: This is exactly the same operation as calling the <see cref="ICharTermAttribute.Length"/> setter, the primary
        /// difference is that this method returns a reference to the current object so it can be chained.
        /// <code>
        /// obj.SetLength(30).Append("hey you");
        /// </code>
        /// </summary>
        /// <param name="length">The truncated length</param>
        public static T SetLength<T>(this T termAttr, int length)
            where T : ICharTermAttribute
        {
            if (termAttr is null)
            {
                throw new ArgumentNullException(nameof(termAttr));
            }

            termAttr.Length = length;
            return termAttr;
        }

        /// <summary>
        /// Sets the length of the termBuffer to zero.
        /// Use this method before appending contents.
        /// <para />
        /// NOTE: This is exactly the same operation as calling <see cref="ICharTermAttribute.Clear()"/>, the primary
        /// difference is that this method returns a reference to the current object so it can be chained.
        /// <code>
        /// obj.SetEmpty().Append("hey you");
        /// </code>
        /// </summary>
        public static T SetEmpty<T>(this T termAttr)
            where T : ICharTermAttribute
        {
            if (termAttr is null)
            {
                throw new ArgumentNullException(nameof(termAttr));
            }

            termAttr.Clear();
            return termAttr;
        }
    }
}
