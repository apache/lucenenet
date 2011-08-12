// -----------------------------------------------------------------------
// <copyright company="Apache" file="TypeAttribute.cs">
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
    using Lucene.Net.Util;

    /// <summary>
    /// A <see cref="Token"/>'s lexical type. The default value is 'word'.
    /// </summary>
    public class TypeAttribute : AttributeBase, ITypeAttribute
    {
        /// <summary>
        /// The default type for the <see cref="Token"/>'s lexical type.
        /// </summary>
        public const string DefaultType = "word";

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeAttribute"/> class.
        /// </summary>
        public TypeAttribute() 
            : this(DefaultType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeAttribute"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public TypeAttribute(string type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets or sets the <see cref="Token"/>'s lexical type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.Type = DefaultType;
        }

        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public override AttributeBase Clone()
        {
            return new TypeAttribute() { Type = this.Type };
        }

        /// <summary>
        /// Copies this instance to the specified target.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(AttributeBase attributeBase)
        {
            ITypeAttribute attribute = (ITypeAttribute)attributeBase;
            attribute.Type = this.Type;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            TypeAttribute attribute = obj as TypeAttribute;

            return attribute != null && attribute.Type.Equals(this.Type);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Type.GetHashCode();
        }       
    }
}