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
        protected readonly SQ srndQuery;
        protected readonly string fieldName;
        protected readonly BasicQueryFactory qf;

        public RewriteQuery(
            SQ srndQuery,
            string fieldName,
            BasicQueryFactory qf)
        {
            this.srndQuery = srndQuery;
            this.fieldName = fieldName;
            this.qf = qf;
        }

        public abstract override Search.Query Rewrite(IndexReader reader);

        public override string ToString()
        {
            return ToString(null);
        }

        public override string ToString(string field)
        {
            return GetType().Name
                + (field == null ? "" : "(unused: " + field + ")")
                + "(" + fieldName
                + ", " + srndQuery.ToString()
                + ", " + qf.ToString()
                + ")";
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode()
                ^ fieldName.GetHashCode()
                ^ qf.GetHashCode()
                ^ srndQuery.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            RewriteQuery<SQ> other = (RewriteQuery<SQ>)obj;
            return fieldName.Equals(other.fieldName)
                && qf.Equals(other.qf)
                && srndQuery.Equals(other.srndQuery);
        }

        /// <summary>
        /// Not supported by this query.
        /// </summary>
        /// <exception cref="NotSupportedException">throws <see cref="NotSupportedException"/> always: clone is not supported.</exception>
        public override object Clone()
        {
            throw new NotSupportedException();
        }
    }
}
