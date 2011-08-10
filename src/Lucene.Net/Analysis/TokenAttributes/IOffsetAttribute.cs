// -----------------------------------------------------------------------
// <copyright company="Apache" file="IOffsetAttribute.cs">
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

namespace Lucene.Net.Analysis.TokenAttributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// The start and end character offset of a <see cref="Token"/>
    /// </summary>
    public interface IOffsetAttribute
    {
        /// <summary>
        /// Gets or sets the start of the offset.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The difference between the offset's start and end
        ///         may not be equal to the length of the term text. 
        ///         The term text may have been altered by a stemmer
        ///         some other filter
        ///     </para>
        /// </remarks>
        /// <value>The offset start.</value>
        int OffsetStart { get; set; }


        /// <summary>
        /// Gets or sets the end of the offset.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///        This will be the end of the offset. Which is one greater than
        ///         the position of the last character corresponding to this token
        ///         in the source text. The length of the token in the source text
        ///         is the <c>OffsetEnd</c> - <c>OffsetStart</c>
        ///     </para>
        /// </remarks>
        /// <value>The offset end.</value>
        int OffsetEnd { get; set; }

        /// <summary>
        /// Sets the offset.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        void SetOffset(int start, int end);
    }
}
