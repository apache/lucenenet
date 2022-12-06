using Lucene.Net.Index;
using System;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    internal abstract class RewriteQuery<SQ> : Search.Query
    {
        protected readonly SQ m_srndQuery;
        protected readonly string m_fieldName;
        protected readonly BasicQueryFactory m_qf;

        protected RewriteQuery(
            SQ srndQuery,
            string fieldName,
            BasicQueryFactory qf) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_srndQuery = srndQuery;
            this.m_fieldName = fieldName;
            this.m_qf = qf;
        }

        public abstract override Search.Query Rewrite(IndexReader reader);

        public override string ToString()
        {
            return ToString(null);
        }

        public override string ToString(string field)
        {
            return GetType().Name
                + (field is null ? "" : "(unused: " + field + ")")
                + "(" + m_fieldName
                + ", " + m_srndQuery.ToString()
                + ", " + m_qf.ToString()
                + ")";
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode()
                ^ m_fieldName.GetHashCode()
                ^ m_qf.GetHashCode()
                ^ m_srndQuery.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            RewriteQuery<SQ> other = (RewriteQuery<SQ>)obj;
            return m_fieldName.Equals(other.m_fieldName, StringComparison.Ordinal)
                && m_qf.Equals(other.m_qf)
                && m_srndQuery.Equals(other.m_srndQuery);
        }

        /// <summary>
        /// Not supported by this query.
        /// </summary>
        /// <exception cref="NotSupportedException">throws <see cref="NotSupportedException"/> always: clone is not supported.</exception>
        public override object Clone()
        {
            throw UnsupportedOperationException.Create();
        }
    }
}
