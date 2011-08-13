// -----------------------------------------------------------------------
// <copyright company="Apache" file="PositionIncrementAttribute.cs">
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
    using System.Diagnostics.CodeAnalysis;
    using Lucene.Net.Util;

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
    /// <seealso cref="Lucene.Net.Index.DocsAndPositionEnumerator"/>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The class was called Attribute in Java. It would be fun to call it Annotation. However, " +
        "its probably best to try to honor the correlating names when possible.")]
    public class PositionIncrementAttribute : AttributeBase, IPositionIncrementAttribute
    {
        private int positionIncrement = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionIncrementAttribute"/> class.
        /// </summary>
        public PositionIncrementAttribute()
        {   
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionIncrementAttribute"/> class.
        /// </summary>
        /// <param name="positionIncrement">The position increment.</param>
        public PositionIncrementAttribute(int positionIncrement)
        {
            this.PositionIncrement = positionIncrement;
        }

        /// <summary>
        /// Gets or sets the position increment. The default value is one.
        /// </summary>
        /// <value>The position increment. The default value is one.</value>
        /// <exception cref="ArgumentOutOfRangeException">Throws when the value being set is less than zero.</exception>
        public int PositionIncrement
        {
            get
            {
                return this.positionIncrement;
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(
                        "value", 
                        "The position increment 'value' must be greater than 0.");

                this.positionIncrement = value;
            }
        }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.positionIncrement = 1;
        }

        /// <summary>
        /// Copies this instance to the specified target.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(AttributeBase attributeBase)
        {
            IPositionIncrementAttribute attribute = (IPositionIncrementAttribute)attributeBase;
            attribute.PositionIncrement = this.PositionIncrement;
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
            if (obj == this)
                return true;

            PositionIncrementAttribute attribute = obj as PositionIncrementAttribute;
            return attribute != null && attribute.PositionIncrement == this.PositionIncrement;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.PositionIncrement;
        }
    }
}