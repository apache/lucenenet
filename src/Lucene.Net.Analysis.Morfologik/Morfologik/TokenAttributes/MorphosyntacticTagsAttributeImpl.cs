// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Attribute = Lucene.Net.Util.Attribute;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Morfologik.TokenAttributes
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Morphosyntactic annotations for surface forms.
    /// </summary>
    /// <seealso cref="IMorphosyntacticTagsAttribute"/>
    public class MorphosyntacticTagsAttribute : Attribute, IMorphosyntacticTagsAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>Initializes this attribute with no tags</summary>
        public MorphosyntacticTagsAttribute() { }

        /// <summary>
        /// A list of potential tag variants for the current token.
        /// </summary>
        private IList<StringBuilder> tags;

        /// <summary>
        /// Gets or sets the POS tag of the term. If you need a copy of this char sequence, copy
        /// its contents (and clone <see cref="StringBuilder"/>s) because it changes with
        /// each new term to avoid unnecessary memory allocations.
        /// </summary>
        public virtual IList<StringBuilder> Tags
        {
            get => tags;
            set => tags = value;
        }


        public override void Clear()
        {
            tags = null;
        }


        public override bool Equals(object other)
        {
            if (other is null) return false;
            if (other is IMorphosyntacticTagsAttribute morphosyntacticTagsAttribute)
            {
                return Equal(this.Tags, morphosyntacticTagsAttribute.Tags);
            }
            return false;
        }

        private static bool Equal(object l1, object l2) // LUCENENET: CA1822: Mark members as static
        {
            return l1 is null ? (l2 is null) : (l1.Equals(l2));
        }

        public override int GetHashCode()
        {
            return this.tags is null ? 0 : tags.GetHashCode();
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IMorphosyntacticTagsAttribute other)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IMorphosyntacticTagsAttribute)}", nameof(target));

            IList<StringBuilder> cloned = null;
            if (tags != null)
            {
                cloned = new JCG.List<StringBuilder>(tags.Count);
                foreach (StringBuilder b in tags)
                {
                    cloned.Add(new StringBuilder(b.ToString()));
                }
            }
            other.Tags = cloned;
        }

        public override object Clone()
        {
            MorphosyntacticTagsAttribute cloned = new MorphosyntacticTagsAttribute();
            this.CopyTo(cloned);
            return cloned;
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            // LUCENENET: Added guard clause
            if (reflector is null)
                throw new ArgumentNullException(nameof(reflector));

            reflector.Reflect(typeof(IMorphosyntacticTagsAttribute), "tags", tags);
        }
    }
}
