using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Globalization;
using System.Text;
using Float = J2N.Numerics.Single;

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
    public abstract class SrndQuery // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        //public SrndQuery() { }

        private float weight = (float)1.0;
        private bool weighted = false;

        public virtual bool IsWeighted => weighted;

        public virtual float Weight 
        { 
            get => weight;
            set
            {
                weight = value; /* as parsed from the query text */
                weighted = true;
            }
        }

        public virtual string WeightString => Float.ToString(Weight, NumberFormatInfo.InvariantInfo);

        public virtual string WeightOperator => "^";

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
        /// This method is used by <see cref="GetHashCode()"/> and <see cref="Equals(Object)"/>,
        /// see LUCENE-2945.
        /// </summary>
        public abstract override string ToString();

        public virtual bool IsFieldsSubQueryAcceptable => true;

        /// <summary> Shallow clone. Subclasses must override this if they
        /// need to clone any members deeply,
        /// </summary>
        public virtual object Clone()
        {
            return MemberwiseClone(); // LUCENENET: never throws in .NET
        }

        /// <summary>
        /// For subclasses of <see cref="SrndQuery"/> within the namespace
        /// <see cref="Lucene.Net.QueryParsers.Surround.Query"/>
        /// it is not necessary to override this method, <see cref="ToString()"/>
        /// </summary>
        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ ToString().GetHashCode();
        }

        /// <summary>
        /// For subclasses of <see cref="SrndQuery"/> within the namespace
        /// <see cref="Lucene.Net.QueryParsers.Surround.Query"/>
        /// it is not necessary to override this method, <see cref="ToString()"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            return ToString().Equals(obj.ToString(), StringComparison.Ordinal);
        }

        /// <summary> An empty Lucene query  </summary>
        public readonly static Search.Query TheEmptyLcnQuery = new EmptyLcnQuery(); /* no changes allowed */ 
  
        internal sealed class EmptyLcnQuery : BooleanQuery
        {
            public override float Boost
            {
                get => base.Boost;
                set => throw UnsupportedOperationException.Create();
            }

            public override void Add(BooleanClause clause)
            {
                throw UnsupportedOperationException.Create();
            }

            public override void Add(Search.Query query, Occur occur)
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}
