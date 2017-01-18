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

using Lucene.Net.Search;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// A base class for ValueSource implementations that retrieve values for
    /// a single field from the <seealso cref="org.apache.lucene.search.FieldCache"/>.
    /// 
    /// 
    /// </summary>
    public abstract class FieldCacheSource : ValueSource
    {
        protected internal readonly string field;
        protected internal readonly IFieldCache cache = Search.FieldCache.DEFAULT;

        protected FieldCacheSource(string field)
        {
            this.field = field;
        }

        public virtual IFieldCache FieldCache
        {
            get
            {
                return cache;
            }
        }

        public virtual string Field
        {
            get
            {
                return field;
            }
        }

        public override string GetDescription()
        {
            return field;
        }

        public override bool Equals(object o)
        {
            var other = o as FieldCacheSource;
            if (other == null)
            {
                return false;
            }
            return field.Equals(other.field) && cache == other.cache;
        }

        public override int GetHashCode()
        {
            return cache.GetHashCode() + field.GetHashCode();
        }
    }
}