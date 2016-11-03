using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Text;

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

    /// <summary>
    /// Lowest level base class for surround queries 
    /// </summary>
    public abstract class SrndQuery
#if FEATURE_CLONEABLE
    : ICloneable
#endif
    {
        //public SrndQuery() { }

        private float weight = (float)1.0;
        private bool weighted = false;

        public virtual bool IsWeighted { get { return weighted; } }

        public virtual float Weight 
        { 
            get { return weight; }
            set
            {
                weight = value; /* as parsed from the query text */
                weighted = true;
            }
        }

        public virtual string WeightString { get { return Number.ToString(Weight); } }

        public virtual string WeightOperator { get { return "^"; } }

        protected virtual void WeightToString(StringBuilder r)
        { 
            /* append the weight part of a query */
            if (IsWeighted)
            {
                r.Append(WeightOperator);
                r.Append(WeightString);
            }
        }

        public virtual Search.Query MakeLuceneQueryField(string fieldName, BasicQueryFactory qf)
        {
            Search.Query q = MakeLuceneQueryFieldNoBoost(fieldName, qf);
            if (IsWeighted)
            {
                q.Boost=(Weight * q.Boost); /* weight may be at any level in a SrndQuery */
            }
            return q;
        }

        public abstract Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf);

        /// <summary>
        /// This method is used by <see cref="M:GetHashCode()"/> and <see cref="M:Equals(Object)"/>,
        /// see LUCENE-2945.
        /// </summary>
        /// <returns></returns>
        public abstract override string ToString();

        public virtual bool IsFieldsSubQueryAcceptable { get { return true; } }

        /// <summary> Shallow clone. Subclasses must override this if they
        /// need to clone any members deeply,
        /// </summary>
        public virtual object Clone()
        {
            object clone = null;
            try
            {
                clone = base.MemberwiseClone();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message, e); // shouldn't happen
            }
            return clone;
        }

        /// <summary>
        /// For subclasses of <see cref="SrndQuery"/> within the package
        /// {@link org.apache.lucene.queryparser.surround.query}
        /// it is not necessary to override this method, <see cref="M:ToString()"/>
        /// </summary>
        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ ToString().GetHashCode();
        }

        /// <summary>
        /// For subclasses of <see cref="SrndQuery"/> within the package
        /// {@link org.apache.lucene.queryparser.surround.query}
        /// it is not necessary to override this method,
        /// @see #toString()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            return ToString().Equals(obj.ToString());
        }

        /// <summary> An empty Lucene query  </summary>
        public readonly static Search.Query TheEmptyLcnQuery = new EmptyLcnQuery(); /* no changes allowed */ 
  
        internal sealed class EmptyLcnQuery : BooleanQuery
        {
            public override float Boost
            {
                get { return base.Boost; }
                set { throw new NotSupportedException(); }
            }

            public override void Add(BooleanClause clause)
            {
                throw new NotSupportedException();
            }

            public override void Add(Search.Query query, BooleanClause.Occur occur)
            {
                throw new NotSupportedException();
            }
        }
    }
}
