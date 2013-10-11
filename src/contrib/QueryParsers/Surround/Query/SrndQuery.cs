using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public abstract class SrndQuery : ICloneable
    {
        public SrndQuery() { }

        private float weight = (float)1.0;
        private bool weighted = false;

        public float Weight
        {
            get { return weight; }
            set
            {
                weight = value; /* as parsed from the query text */
                weighted = true;
            }
        }

        public bool IsWeighted
        {
            get { return weighted; }
        }

        public string WeightString
        {
            get { return Weight.ToString(); }
        }

        public string WeightOperator
        {
            get { return "^"; }
        }

        protected void WeightToString(StringBuilder r)
        { 
            /* append the weight part of a query */
            if (IsWeighted)
            {
                r.Append(WeightOperator);
                r.Append(WeightString);
            }
        }

        public Lucene.Net.Search.Query MakeLuceneQueryField(String fieldName, BasicQueryFactory qf)
        {
            var q = MakeLuceneQueryFieldNoBoost(fieldName, qf);
            if (IsWeighted)
            {
                q.Boost = Weight * q.Boost; /* weight may be at any level in a SrndQuery */
            }
            return q;
        }

        public abstract Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(String fieldName, BasicQueryFactory qf);

        public abstract override string ToString();

        public virtual bool IsFieldsSubQueryAcceptable { get { return true; } }
        
        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            return ToString().Equals(obj.ToString());
        }

        public static readonly Lucene.Net.Search.Query theEmptyLcnQuery = new AnonymousEmptyBooleanQuery();

        private sealed class AnonymousEmptyBooleanQuery : BooleanQuery
        {
            public override float Boost
            {
                get
                {
                    return base.Boost;
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override void Add(BooleanClause clause)
            {
                throw new NotSupportedException();
            }

            public override void Add(Search.Query query, Occur occur)
            {
                throw new NotSupportedException();
            }
        }
    }
}
