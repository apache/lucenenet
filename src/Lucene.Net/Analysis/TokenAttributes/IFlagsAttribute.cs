// -----------------------------------------------------------------------
// <copyright file="IFlagsAttribute.cs" company="Apache">
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
    using Util;

    /// <summary>
    /// This is the contract for attributes that pass different flags down the tokenizer chain. 
    /// </summary>
    /// <remarks>
    ///      <note>
    ///         <para>
    ///         <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/analysis/tokenattributes/FlagsAttribute.java">
    ///             lucene/src/java/org/apache/lucene/analysis/tokenattributes/FlagsAttribute.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Analysis/TokenAttributes/IFlagsAttribute.cs">
    ///              src/Lucene.Net/Analysis/TokenAttributes/FlagsAttribute.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public interface IFlagsAttribute : IAttribute
    {
        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>The flags.</value>
        int Flags { get; set; }
    }
}
