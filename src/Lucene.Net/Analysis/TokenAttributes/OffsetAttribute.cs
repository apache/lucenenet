// -----------------------------------------------------------------------
// <copyright company="Apache" file="OffsetAttribute.cs">
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
    using Lucene.Net.Util;

    /// <summary>
    /// The start and end character offset of a <see cref="Token"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The class was called Attribute in Java. It would be fun to call it Annotation. However, " +
        "its probably best to try to honor the correlating names when possible.")]
    public class OffsetAttribute : AttributeBase, IOffsetAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OffsetAttribute"/> class.
        /// </summary>
        public OffsetAttribute() 
            : this(0, 0)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="OffsetAttribute"/> class.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public OffsetAttribute(int start, int end)
        {
            this.SetOffset(start, end);
        }


        /// <summary>
        /// Gets or sets the start of the offset.
        /// </summary>
        /// <value>The offset start.</value>
        /// <remarks>
        /// The difference between the offset's start and end
        /// may not be equal to the length of the term text.
        /// The term text may have been altered by a stemmer
        /// some other filter
        /// </remarks>
        public int OffsetStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the offset.
        /// </summary>
        /// <value>The offset end.</value>
        /// <remarks>
        /// This will be the end of the offset. Which is one greater than
        /// the position of the last character corresponding to this token
        /// in the source text. The length of the token in the source text
        /// is the <c>OffsetEnd</c> - <c>OffsetStart</c>
        /// </remarks>
        public int OffsetEnd { get; set; }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.SetOffset(0, 0);
        }

        /// <summary>
        /// Copies the values to the target.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(AttributeBase attributeBase)
        {
            IOffsetAttribute attribute = (IOffsetAttribute)attributeBase;
            attribute.SetOffset(this.OffsetStart, this.OffsetEnd);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            OffsetAttribute y = obj as OffsetAttribute;

            return y != null &&
                   y.OffsetStart == this.OffsetStart &&
                   y.OffsetEnd == this.OffsetEnd;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int code = this.OffsetStart;
            code = (code * 31) + this.OffsetEnd;
            return code;
        }


        /// <summary>
        /// Sets the offset.
        /// </summary>
        /// <param name="offsetStart">The start of the offset.</param>
        /// <param name="offsetEnd">The end of the offset.</param>
        public void SetOffset(int offsetStart, int offsetEnd)
        {
            this.OffsetStart = offsetStart;
            this.OffsetEnd = offsetEnd;
        }
    }
}