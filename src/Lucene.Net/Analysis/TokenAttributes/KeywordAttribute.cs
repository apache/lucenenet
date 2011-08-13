// -----------------------------------------------------------------------
// <copyright company="Apache" file="KeywordAttribute.cs">
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
    /// The attribute that can be used to make a token as a keyword. Keyword
    /// aware <see cref="TokenStream"/>s can decide to modify a token
    /// based on the return value of <see cref="IsKeyword"/>, if the token
    /// is modified. Stemming filters for instance can use this attribute
    /// to conditionally skip a term if <see cref="IsKeyword"/> returns <c>true</c>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The class was called Attribute in Java. It would be fun to call it Annotation. However, " +
        "its probably best to try to honor the correlating names when possible.")]
    public class KeywordAttribute : Util.AttributeBase, IKeywordAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordAttribute"/> class.
        /// </summary>
        public KeywordAttribute()
        {  
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordAttribute"/> class.
        /// </summary>
        /// <param name="isKeyword">if set to <c>true</c> [is keyword].</param>
        public KeywordAttribute(bool isKeyword)
        {
            this.IsKeyword = isKeyword;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is keyword.
        /// </summary>
        /// <value>
        ///    <c>true</c> if this instance is keyword; otherwise, <c>false</c>.
        /// </value>
        public bool IsKeyword { get; set; }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.IsKeyword = false;
        }

        /// <summary>
        /// Copies to the target attribute base.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(Util.AttributeBase attributeBase)
        {
            IKeywordAttribute attr = (IKeywordAttribute)attributeBase;
            attr.IsKeyword = this.IsKeyword;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///    <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            if (this.GetType() != obj.GetType())
                return false;

            KeywordAttribute y = obj as KeywordAttribute;
            return y != null && this.IsKeyword == y.IsKeyword;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.IsKeyword ? 31 : 37;
        }
    }
}