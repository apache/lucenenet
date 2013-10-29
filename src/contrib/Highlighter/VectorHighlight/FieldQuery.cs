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

using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Support.Compatibility;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;
using Lucene.Net.Support;

namespace Lucene.Net.Search.VectorHighlight
{
    public class FieldQuery
    {
        bool fieldMatch;

        // fieldMatch==true,  Map<fieldName,QueryPhraseMap>
        // fieldMatch==false, Map<null,QueryPhraseMap>
        IDictionary<String, QueryPhraseMap> rootMaps = new HashMap<String, QueryPhraseMap>();

        // fieldMatch==true,  Map<fieldName,setOfTermsInQueries>
        // fieldMatch==false, Map<null,setOfTermsInQueries>
        IDictionary<String, ISet<String>> termSetMap = new HashMap<String, ISet<String>>();

        int termOrPhraseNumber; // used for colored tag support

        // The maximum number of different matching terms accumulated from any one MultiTermQuery
        private const int MAX_MTQ_TERMS = 1024;

        public FieldQuery(Query query, IndexReader reader, bool phraseHighlight, bool fieldMatch)
        {
            this.fieldMatch = fieldMatch;
            ISet<Query> flatQueries = new HashSet<Query>();
            Flatten(query, reader, flatQueries);
            SaveTerms(flatQueries, reader);
            ISet<Query> expandQueries = Expand(flatQueries);

            foreach (Query flatQuery in expandQueries)
            {
                QueryPhraseMap rootMap = GetRootMap(flatQuery);
                rootMap.Add(flatQuery, reader);
                if (!phraseHighlight && flatQuery is PhraseQuery)
                {
                    PhraseQuery pq = (PhraseQuery)flatQuery;
                    if (pq.GetTerms().Length > 1)
                    {
                        foreach (Term term in pq.GetTerms())
                            rootMap.AddTerm(term, flatQuery.Boost);
                    }
                }
            }
        }

        /** For backwards compatibility you can initialize FieldQuery without
        * an IndexReader, which is only required to support MultiTermQuery
        */
        internal FieldQuery(Query query, bool phraseHighlight, bool fieldMatch)
            : this(query, null, phraseHighlight, fieldMatch)
        {
        }

        public void Flatten(Query sourceQuery, IndexReader reader, ISet<Query> flatQueries)
        {
            if (sourceQuery is BooleanQuery)
            {
                BooleanQuery bq = (BooleanQuery)sourceQuery;
                foreach (BooleanClause clause in bq.Clauses)
                {
                    if (!clause.IsProhibited)
                        Flatten(clause.Query, reader, flatQueries);
                }
            }
            else if (sourceQuery is DisjunctionMaxQuery)
            {
                DisjunctionMaxQuery dmq = (DisjunctionMaxQuery)sourceQuery;
                foreach (Query query in dmq)
                {
                    Flatten(query, reader, flatQueries);
                }
            }
            else if (sourceQuery is TermQuery)
            {
                if (!flatQueries.Contains(sourceQuery))
                    flatQueries.Add(sourceQuery);
            }
            else if (sourceQuery is PhraseQuery)
            {
                if (!flatQueries.Contains(sourceQuery))
                {
                    PhraseQuery pq = (PhraseQuery)sourceQuery;
                    if (pq.GetTerms().Length > 1)
                        flatQueries.Add(pq);
                    else if (pq.GetTerms().Length == 1)
                    {
                        Query q = new TermQuery(pq.GetTerms()[0]);
                        flatQueries.Add(q);
                    }
                }
            }
            else if (sourceQuery is ConstantScoreQuery)
            {
                Query q = ((ConstantScoreQuery)sourceQuery).Query;
                if (q != null)
                {
                    Flatten(q, reader, flatQueries);
                }
            }
            else if (sourceQuery is FilteredQuery)
            {
                Query q = ((FilteredQuery)sourceQuery).Query;
                if (q != null)
                {
                    Flatten(q, reader, flatQueries);
                }
            }
            else if (reader != null)
            {
                Query query = sourceQuery;
                if (sourceQuery is MultiTermQuery)
                {
                    MultiTermQuery copy = (MultiTermQuery)sourceQuery.Clone();
                    copy.SetRewriteMethod(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(MAX_MTQ_TERMS));
                    query = copy;
                }
                Query rewritten = query.Rewrite(reader);
                if (rewritten != query)
                {
                    // only rewrite once and then flatten again - the rewritten query could have a speacial treatment
                    // if this method is overwritten in a subclass.
                    Flatten(rewritten, reader, flatQueries);

                }
                // if the query is already rewritten we discard it
            }
            // else discard queries
        }

        /*
         * Create expandQueries from flatQueries.
         * 
         * expandQueries := flatQueries + overlapped phrase queries
         * 
         * ex1) flatQueries={a,b,c}
         *      => expandQueries={a,b,c}
         * ex2) flatQueries={a,"b c","c d"}
         *      => expandQueries={a,"b c","c d","b c d"}
         */
        public ISet<Query> Expand(ISet<Query> flatQueries)
        {
            ISet<Query> expandQueries = new HashSet<Query>();
            foreach (Query query in new HashSet<Query>(flatQueries))
            {
                //Query query = i.next();
                flatQueries.Remove(query);
                expandQueries.Add(query);
                if (!(query is PhraseQuery)) continue;
                foreach (Query qj in flatQueries)
                {
                    if (!(qj is PhraseQuery)) continue;
                    CheckOverlap(expandQueries, (PhraseQuery)query, (PhraseQuery)qj);
                }
            }
            return expandQueries;
        }

        /*
         * Check if PhraseQuery A and B have overlapped part.
         * 
         * ex1) A="a b", B="b c" => overlap; expandQueries={"a b c"}
         * ex2) A="b c", B="a b" => overlap; expandQueries={"a b c"}
         * ex3) A="a b", B="c d" => no overlap; expandQueries={}
         */
        private void CheckOverlap(ISet<Query> expandQueries, PhraseQuery a, PhraseQuery b)
        {
            if (a.Slop != b.Slop) return;
            Term[] ats = a.GetTerms();
            Term[] bts = b.GetTerms();
            if (fieldMatch && !ats[0].Field.Equals(bts[0].Field)) return;
            CheckOverlap(expandQueries, ats, bts, a.Slop, a.Boost);
            CheckOverlap(expandQueries, bts, ats, b.Slop, b.Boost);
        }

        /*
         * Check if src and dest have overlapped part and if it is, create PhraseQueries and add expandQueries.
         * 
         * ex1) src="a b", dest="c d"       => no overlap
         * ex2) src="a b", dest="a b c"     => no overlap
         * ex3) src="a b", dest="b c"       => overlap; expandQueries={"a b c"}
         * ex4) src="a b c", dest="b c d"   => overlap; expandQueries={"a b c d"}
         * ex5) src="a b c", dest="b c"     => no overlap
         * ex6) src="a b c", dest="b"       => no overlap
         * ex7) src="a a a a", dest="a a a" => overlap;
         *                                     expandQueries={"a a a a a","a a a a a a"}
         * ex8) src="a b c d", dest="b c"   => no overlap
         */
        private void CheckOverlap(ISet<Query> expandQueries, Term[] src, Term[] dest, int slop, float boost)
        {
            // beginning from 1 (not 0) is safe because that the PhraseQuery has multiple terms
            // is guaranteed in flatten() method (if PhraseQuery has only one term, flatten()
            // converts PhraseQuery to TermQuery)
            for (int i = 1; i < src.Length; i++)
            {
                bool overlap = true;
                for (int j = i; j < src.Length; j++)
                {
                    if ((j - i) < dest.Length && !src[j].Text.Equals(dest[j - i].Text))
                    {
                        overlap = false;
                        break;
                    }
                }
                if (overlap && src.Length - i < dest.Length)
                {
                    PhraseQuery pq = new PhraseQuery();
                    foreach (Term srcTerm in src)
                        pq.Add(srcTerm);
                    for (int k = src.Length - i; k < dest.Length; k++)
                    {
                        pq.Add(new Term(src[0].Field, dest[k].Text));
                    }
                    pq.Slop = slop;
                    pq.Boost = boost;
                    if (!expandQueries.Contains(pq))
                        expandQueries.Add(pq);
                }
            }
        }

        internal QueryPhraseMap GetRootMap(Query query)
        {
            String key = GetKey(query);
            QueryPhraseMap map = rootMaps[key];
            if (map == null)
            {
                map = new QueryPhraseMap(this);
                rootMaps[key] = map;
            }
            return map;
        }

        /*
         * Return 'key' string. 'key' is the field name of the Query.
         * If not fieldMatch, 'key' will be null.
         */
        private String GetKey(Query query)
        {
            if (!fieldMatch) return null;
            if (query is TermQuery)
                return ((TermQuery)query).Term.Field;
            else if (query is PhraseQuery)
            {
                PhraseQuery pq = (PhraseQuery)query;
                Term[] terms = pq.GetTerms();
                return terms[0].Field;
            }
            else if (query is MultiTermQuery)
            {
                return ((MultiTermQuery)query).Field;
            }
            else
                throw new ApplicationException("query \"" + query + "\" must be flatten first.");
        }

        /*
         * Save the set of terms in the queries to termSetMap.
         * 
         * ex1) q=name:john
         *      - fieldMatch==true
         *          termSetMap=Map<"name",Set<"john">>
         *      - fieldMatch==false
         *          termSetMap=Map<null,Set<"john">>
         *          
         * ex2) q=name:john title:manager
         *      - fieldMatch==true
         *          termSetMap=Map<"name",Set<"john">,
         *                         "title",Set<"manager">>
         *      - fieldMatch==false
         *          termSetMap=Map<null,Set<"john","manager">>
         *          
         * ex3) q=name:"john lennon"
         *      - fieldMatch==true
         *          termSetMap=Map<"name",Set<"john","lennon">>
         *      - fieldMatch==false
         *          termSetMap=Map<null,Set<"john","lennon">>
         */
        void SaveTerms(ISet<Query> flatQueries, IndexReader reader)
        {
            foreach (Query query in flatQueries)
            {
                ISet<String> termSet = GetTermSet(query);
                if (query is TermQuery)
                    termSet.Add(((TermQuery)query).Term.Text);
                else if (query is PhraseQuery)
                {
                    foreach (Term term in ((PhraseQuery)query).GetTerms())
                        termSet.Add(term.Text);
                }
                else if (query is MultiTermQuery && reader != null)
                {
                    BooleanQuery mtqTerms = (BooleanQuery)query.Rewrite(reader);
                    foreach (BooleanClause clause in mtqTerms.Clauses)
                    {
                        termSet.Add(((TermQuery)clause.Query).Term.Text);
                    }
                }
                else
                    throw new ApplicationException("query \"" + query.ToString() + "\" must be flatten first.");
            }
        }

        private ISet<String> GetTermSet(Query query)
        {
            String key = GetKey(query);
            ISet<String> set = termSetMap[key];
            if (set == null)
            {
                set = new HashSet<String>();
                termSetMap[key] = set;
            }
            return set;
        }

        public ISet<String> GetTermSet(String field)
        {
            return termSetMap[fieldMatch ? field : null];
        }

        /*
         * 
         * <param name="fieldName"></param>
         * <param name="term"></param>
         * <returns>QueryPhraseMap</returns>
         */
        public QueryPhraseMap GetFieldTermMap(String fieldName, String term)
        {
            QueryPhraseMap rootMap = GetRootMap(fieldName);
            return rootMap == null ? null : rootMap.subMap[term];
        }

        /*
         * 
         * <param name="fieldName"></param>
         * <param name="phraseCandidate"></param>
         * <returns>QueryPhraseMap</returns>
         */
        public QueryPhraseMap SearchPhrase(String fieldName, IList<TermInfo> phraseCandidate)
        {
            QueryPhraseMap root = GetRootMap(fieldName);
            if (root == null) return null;
            return root.SearchPhrase(phraseCandidate);
        }

        private QueryPhraseMap GetRootMap(String fieldName)
        {
            return rootMaps[fieldMatch ? fieldName : null];
        }

        int NextTermOrPhraseNumber()
        {
            return termOrPhraseNumber++;
        }

        public class QueryPhraseMap
        {
            bool terminal;
            int slop;   // valid if terminal == true and phraseHighlight == true
            float boost;  // valid if terminal == true
            int termOrPhraseNumber;   // valid if terminal == true
            FieldQuery fieldQuery;
            internal IDictionary<String, QueryPhraseMap> subMap = new HashMap<String, QueryPhraseMap>();

            public QueryPhraseMap(FieldQuery fieldQuery)
            {
                this.fieldQuery = fieldQuery;
            }

            public void AddTerm(Term term, float boost)
            {
                QueryPhraseMap map = GetOrNewMap(subMap, term.Text);
                map.MarkTerminal(boost);
            }

            private QueryPhraseMap GetOrNewMap(IDictionary<String, QueryPhraseMap> subMap, String term)
            {
                QueryPhraseMap map = subMap[term];
                if (map == null)
                {
                    map = new QueryPhraseMap(fieldQuery);
                    subMap[term] = map;
                }
                return map;
            }

            public void Add(Query query, IndexReader reader)
            {
                if (query is TermQuery)
                {
                    AddTerm(((TermQuery)query).Term, query.Boost);
                }
                else if (query is PhraseQuery)
                {
                    PhraseQuery pq = (PhraseQuery)query;
                    Term[] terms = pq.GetTerms();
                    IDictionary<String, QueryPhraseMap> map = subMap;
                    QueryPhraseMap qpm = null;
                    foreach (Term term in terms)
                    {
                        qpm = GetOrNewMap(map, term.Text);
                        map = qpm.subMap;
                    }
                    qpm.MarkTerminal(pq.Slop, pq.Boost);
                }
                else
                    throw new ApplicationException("query \"" + query.ToString() + "\" must be flatten first.");
            }

            public QueryPhraseMap GetTermMap(String term)
            {
                return subMap[term];
            }

            private void MarkTerminal(float boost)
            {
                MarkTerminal(0, boost);
            }

            private void MarkTerminal(int slop, float boost)
            {
                this.terminal = true;
                this.slop = slop;
                this.boost = boost;
                this.termOrPhraseNumber = fieldQuery.NextTermOrPhraseNumber();
            }

            public bool IsTerminal
            {
                get { return terminal; }
            }

            public int Slop
            {
                get { return slop; }
            }

            public float Boost
            {
                get { return boost; }
            }

            public int TermOrPhraseNumber
            {
                get { return termOrPhraseNumber; }
            }

            public QueryPhraseMap SearchPhrase(IList<TermInfo> phraseCandidate)
            {
                QueryPhraseMap currMap = this;
                foreach (TermInfo ti in phraseCandidate)
                {
                    currMap = currMap.subMap[ti.Text];
                    if (currMap == null) return null;
                }
                return currMap.IsValidTermOrPhrase(phraseCandidate) ? currMap : null;
            }

            public bool IsValidTermOrPhrase(IList<TermInfo> phraseCandidate)
            {
                // check terminal
                if (!terminal) return false;

                // if the candidate is a term, it is valid
                if (phraseCandidate.Count == 1) return true;

                // else check whether the candidate is valid phrase
                // compare position-gaps between terms to slop

                int pos = phraseCandidate[0].Position;
                for (int i = 1; i < phraseCandidate.Count; i++)
                {
                    int nextPos = phraseCandidate[i].Position;
                    if (Math.Abs(nextPos - pos - 1) > slop) return false;
                    pos = nextPos;
                }
                return true;
            }
        }
    }
}
