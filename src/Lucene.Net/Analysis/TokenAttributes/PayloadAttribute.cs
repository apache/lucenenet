// -----------------------------------------------------------------------
// <copyright company="Apache" file="PayloadAttribute.cs">
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
    using Lucene.Net.Index;
    using Lucene.Net.Util;

    /// <summary>
    /// The payload of a Token. 
    /// </summary>
    /// <seealso cref="Index.Payload"/>
    public class PayloadAttribute : AttributeBase, IPayloadAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadAttribute"/> class.
        /// </summary>
        public PayloadAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadAttribute"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public PayloadAttribute(Payload payload)
        {
            this.Payload = payload;
        }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>The payload.</value>
        public Index.Payload Payload { get; set; }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.Payload = null;
        }

        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public override object Clone()
        {
            PayloadAttribute clone = (PayloadAttribute)this.MemberwiseClone();
            
            if (this.Payload != null)
                clone.Payload = (Payload)this.Payload.Clone();

            return clone;
        }

        /// <summary>
        /// Copies this instance to the specified target.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(AttributeBase attributeBase)
        {
            IPayloadAttribute attribute = (IPayloadAttribute)attributeBase;
            attribute.Payload = this.Payload == null ? null : (Payload)this.Payload.Clone();
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

            PayloadAttribute attribute = obj as PayloadAttribute;

            if (attribute == null)
                return false;

            if (attribute.Payload == null || this.Payload == null)
                return attribute.Payload == null && this.Payload == null;

            return attribute.Payload.Equals(this.Payload);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Payload == null ? 0 : this.Payload.GetHashCode();
        }
    }
}