// -----------------------------------------------------------------------
// <copyright company="Apache" file="AttributeSourceState.cs">
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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Support;

    /// <summary>
    /// The state of an attribute source.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         In the java version this class is AttributeSource.State. However, the type is
    ///         public and does nothing that requires for it to be a nested class. Thus it
    ///         was moved outside of <see cref="AttributeSource"/> into its own class.
    ///     </note>
    /// </remarks>
    public sealed class AttributeSourceState : ICloneable, ICloneable<AttributeSourceState>
    {
        /// <summary>
        /// Gets or sets the attribute.
        /// </summary>
        /// <value>The attribute.</value>
        public AttributeBase Attribute { get; set; }

        /// <summary>
        /// Gets or sets the next state.
        /// </summary>
        /// <value>The next.</value>
        public AttributeSourceState Next { get; set; }

        /// <summary>
        /// Fully clones this instance.
        /// </summary>
        /// <returns>an instance of the cloned <see cref="AttributeSourceState"/>.</returns>
        public AttributeSourceState Clone()
        {
            AttributeSourceState state = new AttributeSourceState { Attribute = this.Attribute.Clone() };

            if (this.Next != null)
                state.Next = this.Next.Clone();

            return state;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }
}