// -----------------------------------------------------------------------
// <copyright company="Apache" file="IPositionIncrementAttribute.cs">
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
    using Lucene.Net.Index;

    /// <summary>
    /// The <see cref="PositionIncrement"/> determines the position of
    /// this token relative to the previous <see cref="Token"/> in a 
    /// <see cref="TokenStream"/>.  This is used in phrase searching.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Set the <see cref="PositionIncrement"/> to Zero</b> to put multiple terms
    ///         in the same position. An example of this would be if a word has multiple
    ///         stems. A Search for phrases that includes either stem will match. In
    ///         this case, all but the first stem's <see cref="PositionIncrement"/> should be
    ///         set to zero. 
    ///     </para>
    ///     <para>
    ///         The increment of the first instance should be one.
    ///         Repeating a token with an increment of zero can also be used
    ///         to boost the scores of matches of that token
    ///     </para>
    ///     <para>
    ///         <b>Set the <see cref="PositionIncrement"/> to values greater than one</b> to in
    ///         inhibit exact phrase matches. For example, if one does not want phrases to match 
    ///         across remove stop words, then one could build a stop word filter that removes stop
    ///         words. It can also set the <see cref="PositionIncrement"/> to the number of stop
    ///         words remove before each non-stop word. Exact phrase queries will then only match
    ///         when the terms occurs with no intervening stop words.
    ///     </para>
    /// </remarks>
    /// <seealso cref="DocsAndPositionEnumerator"/>
    public interface IPositionIncrementAttribute
    {
        /// <summary>
        /// Gets or sets the position increment. The default value is one.
        /// </summary>
        /// <value>The position increment. The default value is one.</value>
        int PositionIncrement { get; set; }
    }
}
