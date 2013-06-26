using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;

public class PayloadSpanUtil
{
    private readonly IndexReaderContext context;

    public PayloadSpanUtil(IndexReaderContext context)
    {
        this.context = context;
    }

    public ICollection<sbyte[]> GetPayloadsForQuery(Query query)
    {
        ICollection<sbyte[]> payloads = new List<sbyte[]>();
        QueryToSpanQuery(query, payloads);
        return payloads;
    }

    private void QueryToSpanQuery(Query query, ICollection<sbyte[]> payloads)
    {
        if (query is BooleanQuery)
        {
            BooleanClause[] queryClauses = ((BooleanQuery) query).GetClauses();

            for (int i = 0; i < queryClauses.Length; i++)
            {
                if (!queryClauses[i].IsProhibited)
                {
                    QueryToSpanQuery(queryClauses[i].Query, payloads);
                }
            }
        }
        else if (query is PhraseQuery)
        {
            Term[] phraseQueryTerms = ((PhraseQuery) query).GetTerms();
            var clauses = new SpanQuery[phraseQueryTerms.Length];
            for (int i = 0; i < phraseQueryTerms.Length; i++)
            {
                clauses[i] = new SpanTermQuery(phraseQueryTerms[i]);
            }

            int slop = ((PhraseQuery) query).Slop;
            bool inorder = slop == 0;

            var sp = new SpanNearQuery(clauses, slop, inorder) {Boost = query.Boost};
            ;
            GetPayloads(payloads, sp);
        }
        else if (query is TermQuery)
        {
            var stq = new SpanTermQuery(((TermQuery) query).Term) {Boost = query.Boost};
            ;
            GetPayloads(payloads, stq);
        }
        else if (query is SpanQuery)
        {
            GetPayloads(payloads, (SpanQuery) query);
        }
        else if (query is FilteredQuery)
        {
            QueryToSpanQuery(((FilteredQuery) query).Query, payloads);
        }
        else if (query is DisjunctionMaxQuery)
        {
            IEnumerator<Query> enumerator = ((DisjunctionMaxQuery) query).GetEnumerator();
            while (enumerator.MoveNext())
            {
                QueryToSpanQuery(enumerator.Current, payloads);
            }
        }
        else if (query is MultiPhraseQuery)
        {
            var mpq = (MultiPhraseQuery) query;
            IList<Term[]> termArrays = mpq.GetTermArrays();
            int[] positions = mpq.GetPositions();
            if (positions.Length > 0)
            {
                int maxPosition = positions[positions.Length - 1];
                for (int i = 0; i < positions.Length - 1; ++i)
                {
                    if (positions[i] > maxPosition)
                    {
                        maxPosition = positions[i];
                    }
                }

                IList<Query>[] disjunctLists =
                    new List<Query>[maxPosition + 1];
                int distinctPositions = 0;

                for (int i = 0; i < termArrays.Count; ++i)
                {
                    Term[] termArray = termArrays[i];
                    IList<Query> disjuncts = disjunctLists[positions[i]];
                    if (disjuncts == null)
                    {
                        disjuncts = (disjunctLists[positions[i]] = new List<Query>(termArray.Length));
                        ++distinctPositions;
                    }
                    foreach (Term term in termArray)
                    {
                        disjuncts.Add(new SpanTermQuery(term));
                    }
                }

                int positionGaps = 0;
                int position = 0;
                var clauses = new SpanQuery[distinctPositions];
                for (int i = 0; i < disjunctLists.Length; ++i)
                {
                    IList<Query> disjuncts = disjunctLists[i];
                    if (disjuncts != null)
                    {
                        clauses[position++] = new SpanOrQuery(disjuncts.ToArray(new SpanQuery[disjuncts.Count]));
                    }
                    else
                    {
                        ++positionGaps;
                    }
                }

                int slop = mpq.Slop;
                bool inorder = (slop == 0);

                var sp = new SpanNearQuery(clauses, slop + positionGaps,
                                           inorder);
                sp.Boost = query.Boost;
                GetPayloads(payloads, sp);
            }
        }
    }

    private void GetPayloads(ICollection<sbyte[]> payloads, SpanQuery query)
    {
        var termContexts = new HashMap<Term, TermContext>();
        var terms = new TreeSet<Term>();
        query.ExtractTerms(terms);
        foreach (var term in terms)
        {
            termContexts.Add(term, TermContext.Build(context, term, true));
        }
        foreach (AtomicReaderContext atomicReaderContext in context.Leaves)
        {
            Spans spans = query.GetSpans(atomicReaderContext, atomicReaderContext.Reader.LiveDocs, termContexts);
            while (spans.Next())
            {
                if (spans.IsPayloadAvailable())
                {
                    ICollection<sbyte[]> payload = spans.GetPayload();
                    foreach (var bytes in payload)
                    {
                        payloads.Add(bytes);
                    }
                }
            }
        }
    }
}