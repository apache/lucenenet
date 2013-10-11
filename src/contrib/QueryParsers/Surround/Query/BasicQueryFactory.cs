using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class BasicQueryFactory
    {
        public BasicQueryFactory(int maxBasicQueries)
        {
            this.maxBasicQueries = maxBasicQueries;
            this.queriesMade = 0;
        }

        public BasicQueryFactory()
            : this(1024)
        {
        }

        private int maxBasicQueries;
        private int queriesMade;

        public int NrQueriesMade { get { return queriesMade; } }
        public int MaxBasicQueries { get { return maxBasicQueries; } }

        public override string ToString()
        {
            return GetType().FullName
                + "(maxBasicQueries: " + maxBasicQueries
                + ", queriesMade: " + queriesMade
                + ")";
        }

        private bool AtMax
        {
            get
            {
                return queriesMade >= maxBasicQueries;
            }
        }

        protected void CheckMax()
        {
            lock (this)
            {
                if (AtMax)
                    throw new TooManyBasicQueries(MaxBasicQueries);
                queriesMade++;
            }
        }

        public TermQuery NewTermQuery(Term term)
        {
            CheckMax();
            return new TermQuery(term);
        }

        public SpanTermQuery NewSpanTermQuery(Term term)
        {
            CheckMax();
            return new SpanTermQuery(term);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ (AtMax ? 7 : 31 * 32);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BasicQueryFactory))
                return false;
            BasicQueryFactory other = (BasicQueryFactory)obj;
            return AtMax == other.AtMax;
        }
    }
}
