using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// Determines how many positions this
    /// token spans.  Very few analyzer components actually
    /// produce this attribute, and indexing ignores it, but
    /// it's useful to express the graph structure naturally
    /// produced by decompounding, word splitting/joining,
    /// synonym filtering, etc.
    ///
    /// <para/>NOTE: this is optional, and most analyzers
    /// don't change the default value (1).
    /// </summary>
    public interface IPositionLengthAttribute : IAttribute
    {
        /// <summary>
        /// Gets or Sets the position length of this <see cref="Token"/> (how many positions this token
        /// spans).
        /// <para/>
        /// The default value is one.
        /// </summary>
        /// <exception cref="ArgumentException"> if value
        ///         is set to zero or negative. </exception>
        int PositionLength { set; get; }
    }
}