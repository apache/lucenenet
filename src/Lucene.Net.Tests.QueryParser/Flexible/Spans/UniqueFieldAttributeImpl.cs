﻿using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.QueryParsers.Flexible.Spans
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
    /// This attribute is used by the <see cref="UniqueFieldQueryNodeProcessor"/>
    /// processor. It holds a value that defines which is the unique field name that
    /// should be set in every <see cref="Core.Nodes.IFieldableNode"/>.
    /// </summary>
    /// <seealso cref="UniqueFieldQueryNodeProcessor"/>
    public class UniqueFieldAttribute : Attribute, IUniqueFieldAttribute
    {
        private string uniqueField;

        public UniqueFieldAttribute()
        {
            Clear();
        }

        public override void Clear()
        {
            this.uniqueField = "";
        }

        public virtual string UniqueField
        {
            get => this.uniqueField;
            set => this.uniqueField = value;
        }

        public override void CopyTo(IAttribute target)
        {
            if (target is not UniqueFieldAttribute uniqueFieldAttr)
            {
                throw new ArgumentException(
                    "cannot copy the values from attribute UniqueFieldAttribute to an instance of "
                        + target.GetType().Name);
            }

            uniqueFieldAttr.uniqueField = uniqueField.toString();
        }

        public override bool Equals(object other)
        {
            if (other is UniqueFieldAttribute uniqueFieldAttr)
            {
                return uniqueFieldAttr.uniqueField
                    .Equals(this.uniqueField, System.StringComparison.Ordinal);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.uniqueField.GetHashCode();
        }

        public override string ToString()
        {
            return "<uniqueField uniqueField='" + this.uniqueField + "'/>";
        }
    }
}
