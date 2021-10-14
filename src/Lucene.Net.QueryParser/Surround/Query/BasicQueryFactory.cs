using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support.Threading;
using System.Runtime.CompilerServices;

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


     // Create basic queries to be used during rewrite.
     // The basic queries are TermQuery and SpanTermQuery.
     // An exception can be thrown when too many of these are used.
     // SpanTermQuery and TermQuery use IndexReader.termEnum(Term), which causes the buffer usage.
     
     // Use this class to limit the buffer usage for reading terms from an index.
     // Default is 1024, the same as the max. number of subqueries for a BooleanQuery.



    /// <summary>
    /// Factory for creating basic term queries
    /// </summary>
    public class BasicQueryFactory
    {
        private readonly static object _lock = new object();

        public BasicQueryFactory(int maxBasicQueries)
        {
            this.maxBasicQueries = maxBasicQueries;
            this.queriesMade = 0;
        }

        public BasicQueryFactory()
            : this(1024)
        {
        }

        private readonly int maxBasicQueries; // LUCENENET: marked readonly
        private int queriesMade;

        public virtual int NrQueriesMade => queriesMade;
        public virtual int MaxBasicQueries => maxBasicQueries;

        public override string ToString()
        {
            return GetType().Name
                + "(maxBasicQueries: " + maxBasicQueries
                + ", queriesMade: " + queriesMade
                + ")";
        }

        private bool AtMax => queriesMade >= maxBasicQueries;

        protected virtual void CheckMax()
        {
            UninterruptableMonitor.Enter(_lock);
            try
            {
                if (AtMax)
                    throw new TooManyBasicQueries(MaxBasicQueries);

                queriesMade++;
            }
            finally
            {
                UninterruptableMonitor.Exit(_lock);
            }
        }

        public virtual TermQuery NewTermQuery(Term term)
        {
            CheckMax();
            return new TermQuery(term);
        }

        public virtual SpanTermQuery NewSpanTermQuery(Term term)
        {
            CheckMax();
            return new SpanTermQuery(term);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() ^ (AtMax ? 7 : 31 * 32);
        }

        /// <summary>
        /// Two BasicQueryFactory's are equal when they generate
        /// the same types of basic queries, or both cannot generate queries anymore.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is BasicQueryFactory))
                return false;
            BasicQueryFactory other = (BasicQueryFactory)obj;
            return AtMax == other.AtMax;
        }
    }
}
