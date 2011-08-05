// -----------------------------------------------------------------------
// <copyright file="FlagsAttribute.cs" company="Apache">
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
    using Util;

    // DOCS: enhance FlagsAttribute summary

    /// <summary>
    /// This attribute is used to pass different flags down the tokenizer chain. i.e. from one TokenFilter to another
    /// TokenFilter.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/analysis/tokenattributes/FlagsAttributeImpl.java">
    ///             lucene/src/java/org/apache/lucene/analysis/tokenattributes/FlagsAttributeImpl.java
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Analysis/TokenAttributes/FlagsAttribute.cs">
    ///              src/Lucene.Net/Analysis/TokenAttributes/FlagsAttribute.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Analysis/TokenAttributes/FlagsAttributeTest.cs">
    ///             test/Lucene.Net.Test/Analysis/TokenAttributes/FlagsAttributeTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public class FlagsAttribute : AttributeBase, IFlagsAttribute
    {
        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>The flags.</value>
        public int Flags { get; set; }

        /// <summary>
        /// Clears the instance of its flags.
        /// </summary>
        public override void Clear()
        {
            this.Flags = 0;
        }

        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public override object Clone()
        {
            return new FlagsAttribute { Flags = this.Flags };
        }


        /// <summary>
        ///     Copies to the specified attribute.
        /// </summary>
        /// <param name="attributeBase">The <see cref="AttributeBase"/> object that is being copied to.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="attributeBase"/> is not an <see cref="FlagsAttribute"/>.
        /// </exception>
        public override void CopyTo(AttributeBase attributeBase)
        {
            if (!(attributeBase is FlagsAttribute))
                throw new ArgumentException(
                    string.Format("attributeBase must be of type {0} in order to be copied", this.GetType().FullName), 
                    "attributeBase");

            FlagsAttribute attribute = (FlagsAttribute)attributeBase;
            attribute.Flags = this.Flags;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Flags;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            
            if (obj is FlagsAttribute)
                return ((FlagsAttribute)obj).Flags == this.Flags;

            return false;
        }
    }
}
