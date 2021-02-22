// Lucene version compatibility level 4.8.1
using Lucene.Net.Search;
using System;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// A base class for <see cref="ValueSource"/> implementations that retrieve values for
    /// a single field from the <see cref="Search.FieldCache"/>.
    /// </summary>
    public abstract class FieldCacheSource : ValueSource
    {
        protected readonly string m_field;
        protected readonly IFieldCache m_cache = Search.FieldCache.DEFAULT;

        protected FieldCacheSource(string field) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_field = field;
        }

        public virtual IFieldCache FieldCache => m_cache;

        public virtual string Field => m_field;

        public override string GetDescription()
        {
            return m_field;
        }

        public override bool Equals(object o)
        {
            if (o is null) return false;
            if (!(o is FieldCacheSource other)) return false;
            return m_field.Equals(other.m_field, StringComparison.Ordinal) && m_cache == other.m_cache;
        }

        public override int GetHashCode()
        {
            return m_cache.GetHashCode() + m_field.GetHashCode();
        }
    }
}