/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Index;
using IndexReader = Lucene.Net.Index.IndexReader;
using System.Collections.Generic;
using Lucene.Net.Util;
using System.Collections.ObjectModel;
using System.Text;

namespace Lucene.Net.Search
{

    /// <summary> A query that wraps a filter and simply returns a constant score equal to the
    /// query boost for every document in the filter.
    /// </summary>
    [Serializable]
    public class ConstantScoreQuery : Query
    {
        protected readonly Filter filter;
        protected readonly Query query;

        public ConstantScoreQuery(Query query)
        {
            if (query == null)
                throw new NullReferenceException("Query may not be null");
            this.filter = null;
            this.query = query;
        }

        public ConstantScoreQuery(Filter filter)
        {
            if (filter == null)
                throw new NullReferenceException("Filter may not be null");
            this.filter = filter;
            this.query = null;
        }

        /// <summary>Returns the encapsulated filter </summary>
        public virtual Filter Filter
        {
            get { return filter; }
        }

        public virtual Query Query
        {
            get { return query; }
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (query != null)
            {
                Query rewritten = query.Rewrite(reader);
                if (rewritten != query)
                {
                    rewritten = new ConstantScoreQuery(rewritten);
                    rewritten.Boost = this.Boost;
                    return rewritten;
                }
            }
            return this;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            // TODO: OK to not add any terms when wrapped a filter
            // and used with MultiSearcher, but may not be OK for
            // highlighting.
            // If a query was wrapped, we delegate to query.
            if (query != null)
                query.ExtractTerms(terms);
        }

        [Serializable]
        protected internal class ConstantWeight : Weight
        {
            private readonly ConstantScoreQuery enclosingInstance;

            private readonly Weight innerWeight;
            private float queryNorm;
            private float queryWeight;

            public ConstantWeight(ConstantScoreQuery enclosingInstance, IndexSearcher searcher)
            {
                this.enclosingInstance = enclosingInstance;
                this.innerWeight = (enclosingInstance.query == null) ? null : enclosingInstance.query.CreateWeight(searcher);
            }

            public override Query Query
            {
                get { return enclosingInstance; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    // we calculate sumOfSquaredWeights of the inner weight, but ignore it (just to initialize everything)
                    // .NET Port: was this a bug in the Java code?
                    //if (innerWeight != null) innerWeight.ValueForNormalization;
                    queryWeight = enclosingInstance.Boost;
                    return queryWeight * queryWeight;
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                this.queryNorm = norm * topLevelBoost;
                queryWeight *= this.queryNorm;
                // we normalize the inner weight, but ignore it (just to initialize everything)
                if (innerWeight != null) innerWeight.Normalize(norm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                DocIdSetIterator disi;
                if (enclosingInstance.filter != null)
                {
                    //assert query == null;
                    DocIdSet dis = enclosingInstance.filter.GetDocIdSet(context, acceptDocs);
                    if (dis == null)
                    {
                        return null;
                    }
                    disi = dis.Iterator();
                }
                else
                {
                    //assert query != null && innerWeight != null;
                    disi = innerWeight.Scorer(context, scoreDocsInOrder, topScorer, acceptDocs);
                }

                if (disi == null)
                {
                    return null;
                }
                return new ConstantScorer(enclosingInstance, disi, this, queryWeight);
            }

            public override bool ScoresDocsOutOfOrder
            {
                get
                {
                    return (innerWeight != null) ? innerWeight.ScoresDocsOutOfOrder : false;
                }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer cs = Scorer(context, true, false, ((AtomicReader)context.Reader).LiveDocs);
                bool exists = (cs != null && cs.Advance(doc) == doc);

                ComplexExplanation result = new ComplexExplanation();
                if (exists)
                {
                    result.Description = enclosingInstance.ToString() + ", product of:";
                    result.Value = queryWeight;
                    result.Match = true;
                    result.AddDetail(new Explanation(enclosingInstance.Boost, "boost"));
                    result.AddDetail(new Explanation(queryNorm, "queryNorm"));
                }
                else
                {
                    result.Description = enclosingInstance.ToString() + " doesn't match id " + doc;
                    result.Value = 0;
                    result.Match = false;
                }
                return result;
            }
        }

        protected internal class ConstantScorer : Scorer
        {
            private readonly ConstantScoreQuery enclosingInstance;

            internal readonly DocIdSetIterator docIdSetIterator;
            internal readonly float theScore;

            public ConstantScorer(ConstantScoreQuery enclosingInstance, DocIdSetIterator docIdSetIterator, Weight w, float theScore)
                : base(w)
            {
                this.enclosingInstance = enclosingInstance;
                this.theScore = theScore;
                this.docIdSetIterator = docIdSetIterator;
            }

            public override int NextDoc()
            {
                return docIdSetIterator.NextDoc();
            }

            public override int DocID
            {
                get { return docIdSetIterator.DocID; }
            }

            public override float Score()
            {
                return theScore;
            }

            public override int Freq
            {
                get { return 1; }
            }

            public override int Advance(int target)
            {
                return docIdSetIterator.Advance(target);
            }

            public override long Cost
            {
                get { return docIdSetIterator.Cost; }
            }

            private Collector WrapCollector(Collector collector)
            {
                return new AnonymousWrappedCollector(this, collector);
            }

            private sealed class AnonymousWrappedCollector : Collector
            {
                private readonly ConstantScorer enclosingInstance;
                private readonly Collector collector;

                public AnonymousWrappedCollector(ConstantScorer enclosingInstance, Collector collector)
                {
                    this.enclosingInstance = enclosingInstance;
                    this.collector = collector;
                }

                public override void SetScorer(Scorer scorer)
                {
                    // we must wrap again here, but using the scorer passed in as parameter:
                    collector.SetScorer(new ConstantScorer(enclosingInstance.enclosingInstance, scorer, enclosingInstance.weight, enclosingInstance.theScore));
                }

                public override void Collect(int doc)
                {
                    collector.Collect(doc);
                }

                public override void SetNextReader(AtomicReaderContext context)
                {
                    collector.SetNextReader(context);
                }

                public override bool AcceptsDocsOutOfOrder
                {
                    get { return collector.AcceptsDocsOutOfOrder; }
                }
            }

            public override void Score(Collector collector)
            {
                if (docIdSetIterator is Scorer)
                {
                    ((Scorer)docIdSetIterator).Score(WrapCollector(collector));
                }
                else
                {
                    base.Score(collector);
                }
            }

            public override bool Score(Collector collector, int max, int firstDocID)
            {
                if (docIdSetIterator is Scorer)
                {
                    return ((Scorer)docIdSetIterator).Score(WrapCollector(collector), max, firstDocID);
                }
                else
                {
                    return base.Score(collector, max, firstDocID);
                }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    if (docIdSetIterator is Scorer)
                        return new[] { new ChildScorer((Scorer)docIdSetIterator, "constant") };
                    else
                        return new ReadOnlyCollection<ChildScorer>(new ChildScorer[0]);
                }
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new ConstantScoreQuery.ConstantWeight(this, searcher);
        }

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            return new StringBuilder("ConstantScore(")
              .Append((query == null) ? filter.ToString() : query.ToString(field))
              .Append(')')
              .Append(ToStringUtils.Boost(Boost))
              .ToString();
        }

        /// <summary>Returns true if <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (this == o)
                return true;
            if (!base.Equals(o))
                return false;
            if (o is ConstantScoreQuery)
            {
                ConstantScoreQuery other = (ConstantScoreQuery)o;
                return
                  ((this.filter == null) ? other.filter == null : this.filter.Equals(other.filter)) &&
                  ((this.query == null) ? other.query == null : this.query.Equals(other.query));
            }
            return false;
        }

        /// <summary>Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return 31 * base.GetHashCode() +
                ((query == null) ? (object)filter : query).GetHashCode();
        }
    }
}