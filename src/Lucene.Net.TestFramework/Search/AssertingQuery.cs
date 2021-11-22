using Lucene.Net.Index;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search
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
    /// Assertion-enabled query. </summary>
    public class AssertingQuery : Query
    {
        private readonly Random random;
        private readonly Query @in;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        public AssertingQuery(Random random, Query @in)
        {
            this.random = random;
            this.@in = @in;
        }

        /// <summary>
        /// Wrap a query if necessary. </summary>
        public static Query Wrap(Random random, Query query)
        {
            return query is AssertingQuery ? query : new AssertingQuery(random, query);
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return AssertingWeight.Wrap(new J2N.Randomizer(random.NextInt64()), @in.CreateWeight(searcher));
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            @in.ExtractTerms(terms);
        }

        public override string ToString(string field)
        {
            return @in.ToString(field);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (!(obj is AssertingQuery that)) return false;
            return this.@in.Equals(that.@in);
        }

        public override int GetHashCode()
        {
            return -@in.GetHashCode();
        }

        public override object Clone()
        {
            return Wrap(new J2N.Randomizer(random.NextInt64()), (Query)@in.Clone());
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query rewritten = @in.Rewrite(reader);
            if (rewritten == @in)
            {
                return this;
            }
            else
            {
                return Wrap(new J2N.Randomizer(random.NextInt64()), rewritten);
            }
        }

        public override float Boost
        {
            get => @in.Boost;
            set => @in.Boost = value;
        }
    }
}