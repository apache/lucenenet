using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using JCG = J2N.Collections.Generic;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;

namespace Lucene.Net.Search.VectorHighlight
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
    /// <see cref="FieldQuery"/> breaks down query object into terms/phrases and keeps
    /// them in a <see cref="QueryPhraseMap"/> structure.
    /// </summary>
    public class FieldQuery
    {
        internal readonly bool fieldMatch;

        // fieldMatch==true,  Map<fieldName,QueryPhraseMap>
        // fieldMatch==false, Map<null,QueryPhraseMap>
        internal IDictionary<string, QueryPhraseMap> rootMaps = new JCG.Dictionary<string, QueryPhraseMap>();

        // fieldMatch==true,  Map<fieldName,setOfTermsInQueries>
        // fieldMatch==false, Map<null,setOfTermsInQueries>
        internal IDictionary<string, ISet<string>> termSetMap = new JCG.Dictionary<string, ISet<string>>();

        internal int termOrPhraseNumber; // used for colored tag support

        // The maximum number of different matching terms accumulated from any one MultiTermQuery
        private const int MAX_MTQ_TERMS = 1024;

        internal FieldQuery(Query query, IndexReader reader, bool phraseHighlight, bool fieldMatch)
        {
            this.fieldMatch = fieldMatch;
            // LUCENENET NOTE: LinkedHashSet cares about insertion order
            ISet<Query> flatQueries = new JCG.LinkedHashSet<Query>();
            Flatten(query, reader, flatQueries);
            SaveTerms(flatQueries, reader);
            ICollection<Query> expandQueries = Expand(flatQueries);

            foreach (Query flatQuery in expandQueries)
            {
                QueryPhraseMap rootMap = GetRootMap(flatQuery);
                rootMap.Add(flatQuery /*, reader // LUCENENET: Never read */);
                if (!phraseHighlight && flatQuery is PhraseQuery pq)
                {
                    if (pq.GetTerms().Length > 1)
                    {
                        foreach (Term term in pq.GetTerms())
                            rootMap.AddTerm(term, flatQuery.Boost);
                    }
                }
            }
        }

        /// <summary>
        /// For backwards compatibility you can initialize <see cref="FieldQuery"/> without
        /// an <see cref="IndexReader"/>, which is only required to support <see cref="MultiTermQuery"/>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="phraseHighlight"></param>
        /// <param name="fieldMatch"></param>
        internal FieldQuery(Query query, bool phraseHighlight, bool fieldMatch)
                  : this(query, null, phraseHighlight, fieldMatch)
        {
        }

        internal void Flatten(Query sourceQuery, IndexReader reader, ICollection<Query> flatQueries)
        {
            if (sourceQuery is BooleanQuery bq)
            {
                foreach (BooleanClause clause in bq)
                {
                    if (!clause.IsProhibited)
                    {
                        Flatten(ApplyParentBoost(clause.Query, bq), reader, flatQueries);
                    }
                }
            }
            else if (sourceQuery is DisjunctionMaxQuery dmq)
            {
                foreach (Query query in dmq)
                {
                    Flatten(ApplyParentBoost(query, dmq), reader, flatQueries);
                }
            }
            else if (sourceQuery is TermQuery)
            {
                if (!flatQueries.Contains(sourceQuery))
                    flatQueries.Add(sourceQuery);
            }
            else if (sourceQuery is PhraseQuery pq)
            {
                if (!flatQueries.Contains(sourceQuery)) // LUCENENET - set semantics, but this is a list. The original logic was already correct.
                {
                    if (pq.GetTerms().Length > 1)
                        flatQueries.Add(pq);
                    else if (pq.GetTerms().Length == 1)
                    {
                        Query flat = new TermQuery(pq.GetTerms()[0])
                        {
                            Boost = pq.Boost
                        };
                        flatQueries.Add(flat);
                    }
                }
            }
            else if (sourceQuery is ConstantScoreQuery constantScoreQuery)
            {
                Query q = constantScoreQuery.Query;
                if (q != null)
                {
                    Flatten(ApplyParentBoost(q, sourceQuery), reader, flatQueries);
                }
            }
            else if (sourceQuery is FilteredQuery filteredQuery)
            {
                Query q = filteredQuery.Query;
                if (q != null)
                {
                    Flatten(ApplyParentBoost(q, sourceQuery), reader, flatQueries);
                }
            }
            else if (reader != null)
            {
                Query query = sourceQuery;
                if (sourceQuery is MultiTermQuery)
                {
                    MultiTermQuery copy = (MultiTermQuery)sourceQuery.Clone();
                    copy.MultiTermRewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(MAX_MTQ_TERMS);
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

        /// <summary>
        /// Push parent's boost into a clone of query if parent has a non 1 boost.
        /// </summary>
        protected virtual Query ApplyParentBoost(Query query, Query parent)
        {
            if (parent.Boost == 1)
            {
                return query;
            }
            Query cloned = (Query)query.Clone();
            cloned.Boost = query.Boost * parent.Boost;
            return cloned;
        }

        /// <summary>
        /// Create expandQueries from <paramref name="flatQueries"/>.
        /// 
        /// <code>
        /// expandQueries := flatQueries + overlapped phrase queries
        /// 
        /// ex1) flatQueries={a,b,c}
        ///     => expandQueries={a,b,c}
        /// ex2) flatQueries={a,"b c","c d"}
        ///     => expandQueries={a,"b c","c d","b c d"}
        /// </code>
        /// </summary>
        /// <param name="flatQueries"></param>
        /// <returns></returns>
        internal ICollection<Query> Expand(ICollection<Query> flatQueries)
        {
            ISet<Query> expandQueries = new JCG.LinkedHashSet<Query>();

            for (int i = 0; i < flatQueries.Count; )
            {
                Query query = flatQueries.ElementAt(i);
                //i.Remove();
                if (!flatQueries.Remove(query))
                {
                    i++;
                }
                expandQueries.Add(query);
                if (!(query is PhraseQuery)) continue;
                using IEnumerator<Query> j = flatQueries.GetEnumerator();
                while (j.MoveNext())
                {
                    Query qj = j.Current;
                    if (!(qj is PhraseQuery)) continue;
                    CheckOverlap(expandQueries, (PhraseQuery)query, (PhraseQuery)qj);
                }
            }

            //for (IEnumerator<Query> i = flatQueries.GetEnumerator(); i.MoveNext();)
            //{
            //    Query query = i.Current;
            //    i.Remove();
            //    expandQueries.Add(query);
            //    if (!(query is PhraseQuery)) continue;
            //    for (IEnumerator<Query> j = flatQueries.GetEnumerator(); j.MoveNext();)
            //    {
            //        Query qj = j.Current;
            //        if (!(qj is PhraseQuery)) continue;
            //        CheckOverlap(expandQueries, (PhraseQuery)query, (PhraseQuery)qj);
            //    }
            //}
            return expandQueries;
        }

        /// <summary>
        /// Check if <see cref="PhraseQuery"/> A and B have overlapped part.
        /// 
        /// <code>
        /// ex1) A="a b", B="b c" => overlap; expandQueries={"a b c"}
        /// ex2) A="b c", B="a b" => overlap; expandQueries={"a b c"}
        /// ex3) A="a b", B="c d" => no overlap; expandQueries={}
        /// </code>
        /// </summary>
        private void CheckOverlap(ICollection<Query> expandQueries, PhraseQuery a, PhraseQuery b)
        {
            if (a.Slop != b.Slop) return;
            Term[] ats = a.GetTerms();
            Term[] bts = b.GetTerms();
            if (fieldMatch && !ats[0].Field.Equals(bts[0].Field, StringComparison.Ordinal)) return;
            CheckOverlap(expandQueries, ats, bts, a.Slop, a.Boost);
            CheckOverlap(expandQueries, bts, ats, b.Slop, b.Boost);
        }

        /// <summary>
        /// Check if src and dest have overlapped part and if it is, create <see cref="PhraseQuery"/>s and add <paramref name="expandQueries"/>.
        /// 
        /// <code>
        /// ex1) src="a b", dest="c d"       => no overlap
        /// ex2) src="a b", dest="a b c"     => no overlap
        /// ex3) src="a b", dest="b c"       => overlap; expandQueries={"a b c"}
        /// ex4) src="a b c", dest="b c d"   => overlap; expandQueries={"a b c d"}
        /// ex5) src="a b c", dest="b c"     => no overlap
        /// ex6) src="a b c", dest="b"       => no overlap
        /// ex7) src="a a a a", dest="a a a" => overlap;
        ///                                     expandQueries={"a a a a a","a a a a a a"}
        /// ex8) src="a b c d", dest="b c"   => no overlap
        /// </code>
        /// </summary>
        private static void CheckOverlap(ICollection<Query> expandQueries, Term[] src, Term[] dest, int slop, float boost) // LUCENENET: CA1822: Mark members as static
        {
            // beginning from 1 (not 0) is safe because that the PhraseQuery has multiple terms
            // is guaranteed in flatten() method (if PhraseQuery has only one term, flatten()
            // converts PhraseQuery to TermQuery)
            for (int i = 1; i < src.Length; i++)
            {
                bool overlap = true;
                for (int j = i; j < src.Length; j++)
                {
                    if ((j - i) < dest.Length && !src[j].Text.Equals(dest[j - i].Text, StringComparison.Ordinal))
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
                    if (!expandQueries.Contains(pq)) // LUCENENET specific - set semantics, but this is a list
                        expandQueries.Add(pq);
                }
            }
        }

        internal QueryPhraseMap GetRootMap(Query query)
        {
            string key = GetKey(query);
            if (!rootMaps.TryGetValue(key, out QueryPhraseMap map) || map is null)
            {
                map = new QueryPhraseMap(this);
                rootMaps[key] = map;
            }
            return map;
        }

        /// <summary>
        /// Return 'key' string. 'key' is the field name of the <see cref="Query"/>.
        /// If not fieldMatch, 'key' will be null.
        /// </summary>
        private string GetKey(Query query)
        {
            if (!fieldMatch) return null;
            if (query is TermQuery termQuery)
                return termQuery.Term.Field;
            else if (query is PhraseQuery pq)
            {
                Term[] terms = pq.GetTerms();
                return terms[0].Field;
            }
            else if (query is MultiTermQuery multiTermQuery)
            {
                return multiTermQuery.Field;
            }
            else
                throw RuntimeException.Create("query \"" + query.ToString() + "\" must be flatten first.");
        }

        /// <summary>
        /// Save the set of terms in the queries to <see cref="termSetMap"/>.
        /// 
        /// <code>
        /// ex1) q=name:john
        ///      - fieldMatch==true
        ///          termSetMap=IDictionary&lt;"name",ISet&lt;"john"&gt;&gt;
        ///      - fieldMatch==false
        ///          termSetMap=IDictionary&lt;null,ISet&lt;"john"&gt;&gt;
        /// ex2) q=name:john title:manager
        ///      - fieldMatch==true
        ///          termSetMap=IDictionary&lt;"name",ISet&lt;"john"&gt;,
        ///                         "title",ISet&lt;"manager"&gt;&gt;
        ///      - fieldMatch==false
        ///          termSetMap=IDictionary&lt;null,ISet&lt;"john","manager"&gt;&gt;
        /// ex3) q=name:"john lennon"
        ///      - fieldMatch==true
        ///          termSetMap=IDictionary&lt;"name",ISet&lt;"john","lennon"&gt;&gt;
        ///      - fieldMatch==false
        ///          termSetMap=IDictionary&lt;null,ISet&lt;"john","lennon"&gt;&gt;
        /// </code>
        /// </summary>
        internal void SaveTerms(ICollection<Query> flatQueries, IndexReader reader)
        {
            foreach (Query query in flatQueries)
            {
                ISet<string> termSet = GetTermSet(query);
                if (query is TermQuery termQuery)
                    termSet.Add(termQuery.Term.Text);
                else if (query is PhraseQuery phraseQuery)
                {
                    foreach (Term term in phraseQuery.GetTerms())
                        termSet.Add(term.Text);
                }
                else if (query is MultiTermQuery && reader != null)
                {
                    BooleanQuery mtqTerms = (BooleanQuery)query.Rewrite(reader);
                    foreach (BooleanClause clause in mtqTerms.GetClauses())
                    {
                        termSet.Add(((TermQuery)clause.Query).Term.Text);
                    }
                }
                else
                    throw RuntimeException.Create("query \"" + query.ToString() + "\" must be flatten first.");
            }
        }

        private ISet<string> GetTermSet(Query query)
        {
            string key = GetKey(query);
            if (!termSetMap.TryGetValue(key, out ISet<string> set) || set is null)
            {
                set = new JCG.HashSet<string>();
                termSetMap[key] = set;
            }
            return set;
        }

        internal ISet<string> GetTermSet(string field)
        {
            termSetMap.TryGetValue(fieldMatch ? field : null, out ISet<string> result);
            return result;
        }

        /// <returns>QueryPhraseMap</returns>
        public virtual QueryPhraseMap GetFieldTermMap(string fieldName, string term)
        {
            QueryPhraseMap rootMap = GetRootMap(fieldName);
            if (rootMap is null) return null;
            rootMap.subMap.TryGetValue(term, out QueryPhraseMap result);
            return result;
        }

        /// <returns>QueryPhraseMap</returns>
        public virtual QueryPhraseMap SearchPhrase(string fieldName, IList<TermInfo> phraseCandidate)
        {
            QueryPhraseMap root = GetRootMap(fieldName);
            if (root is null) return null;
            return root.SearchPhrase(phraseCandidate);
        }

        private QueryPhraseMap GetRootMap(string fieldName)
        {
            rootMaps.TryGetValue(fieldMatch ? fieldName : null, out QueryPhraseMap result);
            return result;
        }

        internal int NextTermOrPhraseNumber()
        {
            return termOrPhraseNumber++;
        }

        /// <summary>
        /// Internal structure of a query for highlighting: represents
        /// a nested query structure
        /// </summary>
        public class QueryPhraseMap
        {
            internal bool terminal;
            internal int slop;   // valid if terminal == true and phraseHighlight == true
            internal float boost;  // valid if terminal == true
            internal int termOrPhraseNumber;   // valid if terminal == true
            internal FieldQuery fieldQuery;
            internal IDictionary<string, QueryPhraseMap> subMap = new Dictionary<string, QueryPhraseMap>();

            public QueryPhraseMap(FieldQuery fieldQuery)
            {
                this.fieldQuery = fieldQuery;
            }

            internal void AddTerm(Term term, float boost)
            {
                QueryPhraseMap map = GetOrNewMap(subMap, term.Text);
                map.MarkTerminal(boost);
            }

            private QueryPhraseMap GetOrNewMap(IDictionary<string, QueryPhraseMap> subMap, string term)
            {
                if (!subMap.TryGetValue(term, out QueryPhraseMap map) || map is null)
                {
                    map = new QueryPhraseMap(fieldQuery);
                    subMap[term] = map;
                }
                return map;
            }

            internal void Add(Query query /*, IndexReader reader // LUCENENET: Never read */)
            {
                if (query is TermQuery termQuery)
                {
                    AddTerm(termQuery.Term, query.Boost);
                }
                else if (query is PhraseQuery pq)
                {
                    Term[] terms = pq.GetTerms();
                    IDictionary<string, QueryPhraseMap> map = subMap;
                    QueryPhraseMap qpm = null;
                    foreach (Term term in terms)
                    {
                        qpm = GetOrNewMap(map, term.Text);
                        map = qpm.subMap;
                    }
                    qpm.MarkTerminal(pq.Slop, pq.Boost);
                }
                else
                    throw RuntimeException.Create("query \"" + query.ToString() + "\" must be flatten first.");
            }

            public virtual QueryPhraseMap GetTermMap(string term)
            {
                subMap.TryGetValue(term, out QueryPhraseMap result);
                return result;
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

            public virtual bool IsTerminal => terminal;

            public virtual int Slop => slop;

            public virtual float Boost => boost;

            public virtual int TermOrPhraseNumber => termOrPhraseNumber;

            public virtual QueryPhraseMap SearchPhrase(IList<TermInfo> phraseCandidate)
            {
                QueryPhraseMap currMap = this;
                foreach (TermInfo ti in phraseCandidate)
                {
                    currMap.subMap.TryGetValue(ti.Text, out currMap);

                    if (currMap is null) return null;
                }
                return currMap.IsValidTermOrPhrase(phraseCandidate) ? currMap : null;
            }

            public virtual bool IsValidTermOrPhrase(IList<TermInfo> phraseCandidate)
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
