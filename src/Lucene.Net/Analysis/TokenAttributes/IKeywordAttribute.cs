// -----------------------------------------------------------------------
// <copyright company="Apache" file="IKeywordAttribute.cs">
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
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The contract for attributes that can be used to make a token as a keyword. Keyword
    /// aware <see cref="TokenStream"/>s can decide to modify a token
    /// based on the return value of <see cref="IsKeyword"/>, if the token
    /// is modified. Stemming filters for instance can use this attribute
    /// to conditionally skip a term if <see cref="IsKeyword"/> returns <c>true</c>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The class was called Attribute in Java. It would be fun to call it Annotation. However, " +
        "its probably best to try to honor the correlating names when possible.")]
    public interface IKeywordAttribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is keyword.
        /// </summary>
        /// <value>
        ///    <c>true</c> if this instance is keyword; otherwise, <c>false</c>.
        /// </value>
        bool IsKeyword { get; set; }
    }
}
