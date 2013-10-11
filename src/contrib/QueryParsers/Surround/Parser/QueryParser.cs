using Lucene.Net.QueryParsers.Surround.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Parser
{
    public class QueryParser
    {
        readonly int minimumPrefixLength = 3;
        readonly int minimumCharsInTrunc = 3;
        readonly String truncationErrorMessage = "Too unrestrictive truncation: ";
        readonly String boostErrorMessage = "Cannot handle boost value: ";

        /* CHECKME: These should be the same as for the tokenizer. How? */
        const char truncator = '*';
        const char anyChar = '?';
        const char quote = '"';
        const char fieldOperator = ':';
        const char comma = ','; /* prefix list separator */
        const char carat = '^'; /* weight operator */

        public static SrndQuery Parse(String query)
        {
            QueryParser parser = new QueryParser();
            return parser.Parse2(query);
        }

        public QueryParser()
            : this(new FastCharStream(new StringReader("")))
        {
        }

        public SrndQuery Parse2(String query)
        {
            ReInit(new FastCharStream(new StringReader(query)));
            try
            {
                return TopSrndQuery();
            }
            catch (TokenMgrError tme)
            {
                throw new ParseException(tme.Message);
            }
        }

        protected SrndQuery GetFieldsQuery(
            SrndQuery q, List<String> fieldNames)
        {
            /* FIXME: check acceptable subquery: at least one subquery should not be
             * a fields query.
             */
            return new FieldsQuery(q, fieldNames, fieldOperator);
        }

        protected SrndQuery GetOrQuery(List<SrndQuery> queries, bool infix, Token orToken)
        {
            return new OrQuery(queries, infix, orToken.image);
        }

        protected SrndQuery GetAndQuery(List<SrndQuery> queries, bool infix, Token andToken)
        {
            return new AndQuery(queries, infix, andToken.image);
        }

        protected SrndQuery GetNotQuery(List<SrndQuery> queries, Token notToken)
        {
            return new NotQuery(queries, notToken.image);
        }

        protected static int GetOpDistance(String distanceOp)
        {
            /* W, 2W, 3W etc -> 1, 2 3, etc. Same for N, 2N ... */
            return distanceOp.Length == 1
              ? 1
              : int.Parse(distanceOp.Substring(0, distanceOp.Length - 1));
        }

        protected static void CheckDistanceSubQueries(DistanceQuery distq, String opName)
        {
            String m = distq.DistanceSubQueryNotAllowed;
            if (m != null)
            {
                throw new ParseException("Operator " + opName + ": " + m);
            }
        }

        protected SrndQuery GetDistanceQuery(
              List<SrndQuery> queries,
              bool infix,
              Token dToken,
              bool ordered)
        {
            DistanceQuery dq = new DistanceQuery(queries,
                                                infix,
                                                GetOpDistance(dToken.image),
                                                dToken.image,
                                                ordered);
            CheckDistanceSubQueries(dq, dToken.image);
            return dq;
        }

        protected SrndQuery GetTermQuery(
              String term, bool quoted)
        {
            return new SrndTermQuery(term, quoted);
        }

        protected bool AllowedSuffix(String suffixed)
        {
            return (suffixed.Length - 1) >= minimumPrefixLength;
        }

        protected SrndQuery GetPrefixQuery(
            String prefix, bool quoted)
        {
            return new SrndPrefixQuery(prefix, quoted, truncator);
        }

        protected bool AllowedTruncation(String truncated)
        {
            /* At least 3 normal characters needed. */
            int nrNormalChars = 0;
            for (int i = 0; i < truncated.Length; i++)
            {
                char c = truncated[i];
                if ((c != truncator) && (c != anyChar))
                {
                    nrNormalChars++;
                }
            }
            return nrNormalChars >= minimumCharsInTrunc;
        }

        protected SrndQuery GetTruncQuery(String truncated)
        {
            return new SrndTruncQuery(truncated, truncator, anyChar);
        }

        public SrndQuery TopSrndQuery()
        {
            SrndQuery q;
            q = FieldsQuery();
            jj_consume_token(0);
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery FieldsQuery()
        {
            SrndQuery q;
            List<String> fieldNames;
            fieldNames = OptionalFields();
            q = OrQuery();
            { if (true) return (fieldNames == null) ? q : GetFieldsQuery(q, fieldNames); }
            throw new Exception("Missing return statement in function");
        }

        public List<String> OptionalFields()
        {
            Token fieldName;
            List<String> fieldNames = null;

            while (true)
            {
                if (jj_2_1(2))
                {
                    ;
                }
                else
                {
                    break;
                }
                // to the colon
                fieldName = jj_consume_token(QueryParserConstants.TERM);
                jj_consume_token(QueryParserConstants.COLON);
                if (fieldNames == null)
                {
                    fieldNames = new List<String>();
                }
                fieldNames.Add(fieldName.image);
            }
            { if (true) return fieldNames; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery OrQuery()
        {
            SrndQuery q;
            List<SrndQuery> queries = null;
            Token oprt = null;
            q = AndQuery();
        
            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.OR:
                        ;
                        break;
                    default:
                        jj_la1[0] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                oprt = jj_consume_token(QueryParserConstants.OR);
                /* keep only last used operator */
                if (queries == null)
                {
                    queries = new List<SrndQuery>();
                    queries.Add(q);
                }
                q = AndQuery();
                queries.Add(q);
            }
            { if (true) return (queries == null) ? q : GetOrQuery(queries, true /* infix */, oprt); }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery AndQuery()
        {
            SrndQuery q;
            List<SrndQuery> queries = null;
            Token oprt = null;
            q = NotQuery();
            bool shouldBreakWhile = false;
            while (true)
            {
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.AND:
                        ;
                        break;
                    default:
                        jj_la1[1] = jj_gen;
                        shouldBreakWhile = true;

                        break;
                }
                if (shouldBreakWhile)
                    break;
                oprt = jj_consume_token(QueryParserConstants.AND);
                /* keep only last used operator */
                if (queries == null)
                {
                    queries = new List<SrndQuery>();
                    queries.Add(q);
                }
                q = NotQuery();
                queries.Add(q);
            }
            { if (true) return (queries == null) ? q : GetAndQuery(queries, true /* infix */, oprt); }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery NotQuery()
        {
            SrndQuery q;
            List<SrndQuery> queries = null;
            Token oprt = null;
            q = NQuery();

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.NOT:
                        ;
                        break;
                    default:
                        jj_la1[2] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                oprt = jj_consume_token(QueryParserConstants.NOT);
                /* keep only last used operator */
                if (queries == null)
                {
                    queries = new List<SrndQuery>();
                    queries.Add(q);
                }
                q = NQuery();
                queries.Add(q);
            }
            { if (true) return (queries == null) ? q : GetNotQuery(queries, oprt); }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery NQuery()
        {
            SrndQuery q;
            List<SrndQuery> queries;
            Token dt;
            q = WQuery();

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.N:
                        ;
                        break;
                    default:
                        jj_la1[3] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                dt = jj_consume_token(QueryParserConstants.N);
                queries = new List<SrndQuery>();
                queries.Add(q); /* left associative */

                q = WQuery();
                queries.Add(q);
                q = GetDistanceQuery(queries, true /* infix */, dt, false /* not ordered */);
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery WQuery()
        {
            SrndQuery q;
            List<SrndQuery> queries;
            Token wt;
            q = PrimaryQuery();

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.W:
                        ;
                        break;
                    default:
                        jj_la1[4] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                wt = jj_consume_token(QueryParserConstants.W);
                queries = new List<SrndQuery>();
                queries.Add(q); /* left associative */

                q = PrimaryQuery();
                queries.Add(q);
                q = GetDistanceQuery(queries, true /* infix */, wt, true /* ordered */);
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery PrimaryQuery()
        {
            /* bracketed weighted query or weighted term */
            SrndQuery q;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.LPAREN:
                    jj_consume_token(QueryParserConstants.LPAREN);
                    q = FieldsQuery();
                    jj_consume_token(QueryParserConstants.RPAREN);
                    break;
                case QueryParserConstants.OR:
                case QueryParserConstants.AND:
                case QueryParserConstants.W:
                case QueryParserConstants.N:
                    q = PrefixOperatorQuery();
                    break;
                case QueryParserConstants.TRUNCQUOTED:
                case QueryParserConstants.QUOTED:
                case QueryParserConstants.SUFFIXTERM:
                case QueryParserConstants.TRUNCTERM:
                case QueryParserConstants.TERM:
                    q = SimpleTerm();
                    break;
                default:
                    jj_la1[5] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            OptionalWeights(q);
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery PrefixOperatorQuery()
        {
            Token oprt;
            List<SrndQuery> queries;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.OR:
                    oprt = jj_consume_token(QueryParserConstants.OR);
                    /* prefix OR */
                    queries = FieldsQueryList();
                    { if (true) return GetOrQuery(queries, false /* not infix */, oprt); }
                    break;
                case QueryParserConstants.AND:
                    oprt = jj_consume_token(QueryParserConstants.AND);
                    /* prefix AND */
                    queries = FieldsQueryList();
                    { if (true) return GetAndQuery(queries, false /* not infix */, oprt); }
                    break;
                case QueryParserConstants.N:
                    oprt = jj_consume_token(QueryParserConstants.N);
                    /* prefix N */
                    queries = FieldsQueryList();
                    { if (true) return GetDistanceQuery(queries, false /* not infix */, oprt, false /* not ordered */); }
                    break;
                case QueryParserConstants.W:
                    oprt = jj_consume_token(QueryParserConstants.W);
                    /* prefix W */
                    queries = FieldsQueryList();
                    { if (true) return GetDistanceQuery(queries, false  /* not infix */, oprt, true /* ordered */); }
                    break;
                default:
                    jj_la1[6] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            throw new Exception("Missing return statement in function");
        }

        public List<SrndQuery> FieldsQueryList()
        {
            SrndQuery q;
            List<SrndQuery> queries = new List<SrndQuery>();
            jj_consume_token(QueryParserConstants.LPAREN);
            q = FieldsQuery();
            queries.Add(q);
        
            while (true)
            {
                jj_consume_token(QueryParserConstants.COMMA);
                q = FieldsQuery();
                queries.Add(q);
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.COMMA:
                        ;
                        break;
                    default:
                        jj_la1[7] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }

                if (shouldBreakWhile)
                    break;
            }
            jj_consume_token(QueryParserConstants.RPAREN);
            { if (true) return queries; }
            throw new Exception("Missing return statement in function");
        }

        public SrndQuery SimpleTerm()
        {
            Token term;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.TERM:
                    term = jj_consume_token(QueryParserConstants.TERM);
                    { if (true) return GetTermQuery(term.image, false /* not quoted */); }
                    break;
                case QueryParserConstants.QUOTED:
                    term = jj_consume_token(QueryParserConstants.QUOTED);
                    { if (true) return GetTermQuery(term.image.Substring(1, term.image.Length - 1), true /* quoted */); }
                    break;
                case QueryParserConstants.SUFFIXTERM:
                    term = jj_consume_token(QueryParserConstants.SUFFIXTERM);
                    /* ending in * */
                    if (!AllowedSuffix(term.image))
                    {
                        { if (true) throw new ParseException(truncationErrorMessage + term.image); }
                    }
                    { if (true) return GetPrefixQuery(term.image.Substring(0, term.image.Length - 1), false /* not quoted */); }
                    break;
                case QueryParserConstants.TRUNCTERM:
                    term = jj_consume_token(QueryParserConstants.TRUNCTERM);
                    /* with at least one * or ? */
                    if (!AllowedTruncation(term.image))
                    {
                        { if (true) throw new ParseException(truncationErrorMessage + term.image); }
                    }
                    { if (true) return GetTruncQuery(term.image); }
                    break;
                case QueryParserConstants.TRUNCQUOTED:
                    term = jj_consume_token(QueryParserConstants.TRUNCQUOTED);
                    /* eg. "9b-b,m"* */
                    if ((term.image.Length - 3) < minimumPrefixLength)
                    {
                        { if (true) throw new ParseException(truncationErrorMessage + term.image); }
                    }
                    { if (true) return GetPrefixQuery(term.image.Substring(1, term.image.Length - 2), true /* quoted */); }
                    break;
                default:
                    jj_la1[8] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            throw new Exception("Missing return statement in function");
        }

        public void OptionalWeights(SrndQuery q)
        {
            Token weight = null;
        
            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.CARAT:
                        ;
                        break;
                    default:
                        jj_la1[9] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                jj_consume_token(QueryParserConstants.CARAT);
                weight = jj_consume_token(QueryParserConstants.NUMBER);
                float f;
                try
                {
                    f = float.Parse(weight.image);
                }
                catch (Exception floatExc)
                {
                    { if (true) throw new ParseException(boostErrorMessage + weight.image + " (" + floatExc + ")"); }
                }
                if (f <= 0.0)
                {
                    { if (true) throw new ParseException(boostErrorMessage + weight.image); }
                }
                q.Weight = f * q.Weight; /* left associative, fwiw */

            }
        }

        private bool jj_2_1(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_1(); }
            catch (LookaheadSuccess) { return true; }
            finally { jj_save(0, xla); }
        }

        private bool jj_3_1()
        {
            if (jj_scan_token(QueryParserConstants.TERM)) return true;
            if (jj_scan_token(QueryParserConstants.COLON)) return true;
            return false;
        }

        /** Generated Token Manager. */
        public QueryParserTokenManager token_source;
        /** Current token. */
        public Token token;
        /** Next token. */
        public Token jj_nt;
        private int _jj_ntk;
        private Token jj_scanpos, jj_lastpos;
        private int jj_la;
        private int jj_gen;
        private int[] jj_la1 = new int[10];
        static private int[] jj_la1_0;
        static QueryParser()
        {
            jj_la1_init_0();
        }
        private static void jj_la1_init_0()
        {
            jj_la1_0 = new int[] { 0x100, 0x200, 0x400, 0x1000, 0x800, 0x7c3b00, 0x1b00, 0x8000, 0x7c0000, 0x20000, };
        }
        private readonly JJCalls[] jj_2_rtns = new JJCalls[1];
        private bool jj_rescan = false;
        private int jj_gc = 0;

        /** Constructor with user supplied CharStream. */
        public QueryParser(ICharStream stream)
        {
            token_source = new QueryParserTokenManager(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 10; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public void ReInit(ICharStream stream)
        {
            token_source.ReInit(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 10; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Constructor with generated Token Manager. */
        public QueryParser(QueryParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 10; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public void ReInit(QueryParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 10; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        private Token jj_consume_token(int kind)
        {
            Token oldToken;
            if ((oldToken = token).next != null) token = token.next;
            else token = token.next = token_source.GetNextToken();
            _jj_ntk = -1;
            if (token.kind == kind)
            {
                jj_gen++;
                if (++jj_gc > 100)
                {
                    jj_gc = 0;
                    for (int i = 0; i < jj_2_rtns.Length; i++)
                    {
                        JJCalls c = jj_2_rtns[i];
                        while (c != null)
                        {
                            if (c.gen < jj_gen) c.first = null;
                            c = c.next;
                        }
                    }
                }
                return token;
            }
            token = oldToken;
            jj_kind = kind;
            throw GenerateParseException();
        }

        private class LookaheadSuccess : Exception { }
        private LookaheadSuccess jj_ls = new LookaheadSuccess();
        private bool jj_scan_token(int kind)
        {
            if (jj_scanpos == jj_lastpos)
            {
                jj_la--;
                if (jj_scanpos.next == null)
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.next = token_source.GetNextToken();
                }
                else
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.next;
                }
            }
            else
            {
                jj_scanpos = jj_scanpos.next;
            }
            if (jj_rescan)
            {
                int i = 0; Token tok = token;
                while (tok != null && tok != jj_scanpos) { i++; tok = tok.next; }
                if (tok != null) jj_add_error_token(kind, i);
            }
            if (jj_scanpos.kind != kind) return true;
            if (jj_la == 0 && jj_scanpos == jj_lastpos) throw jj_ls;
            return false;
        }


        /** Get the next Token. */
        public Token GetNextToken()
        {
            if (token.next != null) token = token.next;
            else token = token.next = token_source.GetNextToken();
            _jj_ntk = -1;
            jj_gen++;
            return token;
        }

        /** Get the specific Token. */
        public Token GetToken(int index)
        {
            Token t = token;
            for (int i = 0; i < index; i++)
            {
                if (t.next != null) t = t.next;
                else t = t.next = token_source.GetNextToken();
            }
            return t;
        }

        private int jj_ntk()
        {
            if ((jj_nt = token.next) == null)
                return (_jj_ntk = (token.next = token_source.GetNextToken()).kind);
            else
                return (_jj_ntk = jj_nt.kind);
        }

        private IList<int[]> jj_expentries = new List<int[]>();
        private int[] jj_expentry;
        private int jj_kind = -1;
        private int[] jj_lasttokens = new int[100];
        private int jj_endpos;

        private void jj_add_error_token(int kind, int pos)
        {
            if (pos >= 100) return;
            if (pos == jj_endpos + 1)
            {
                jj_lasttokens[jj_endpos++] = kind;
            }
            else if (jj_endpos != 0)
            {
                jj_expentry = new int[jj_endpos];
                for (int i = 0; i < jj_endpos; i++)
                {
                    jj_expentry[i] = jj_lasttokens[i];
                }

                foreach (int[] it in jj_expentries)
                {
                    int[] oldentry = it;
                    if (oldentry.Length == jj_expentry.Length)
                    {
                        bool shouldContinueOuter = false;
                        for (int i = 0; i < jj_expentry.Length; i++)
                        {
                            if (oldentry[i] != jj_expentry[i])
                            {
                                shouldContinueOuter = true;
                                break;
                            }
                        }

                        if (shouldContinueOuter)
                            continue;

                        jj_expentries.Add(jj_expentry);
                        break;
                    }
                }
                if (pos != 0) jj_lasttokens[(jj_endpos = pos) - 1] = kind;
            }
        }

        /** Generate ParseException. */
        public ParseException GenerateParseException()
        {
            jj_expentries.Clear();
            bool[] la1tokens = new bool[24];
            if (jj_kind >= 0)
            {
                la1tokens[jj_kind] = true;
                jj_kind = -1;
            }
            for (int i = 0; i < 10; i++)
            {
                if (jj_la1[i] == jj_gen)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if ((jj_la1_0[i] & (1 << j)) != 0)
                        {
                            la1tokens[j] = true;
                        }
                    }
                }
            }
            for (int i = 0; i < 24; i++)
            {
                if (la1tokens[i])
                {
                    jj_expentry = new int[1];
                    jj_expentry[0] = i;
                    jj_expentries.Add(jj_expentry);
                }
            }
            jj_endpos = 0;
            jj_rescan_token();
            jj_add_error_token(0, 0);
            int[][] exptokseq = new int[jj_expentries.Count][];
            for (int i = 0; i < jj_expentries.Count; i++)
            {
                exptokseq[i] = jj_expentries[i];
            }
            return new ParseException(token, exptokseq, QueryParserConstants.tokenImage);
        }

        /** Enable tracing. */
        public void enable_tracing()
        {
        }

        /** Disable tracing. */
        public void disable_tracing()
        {
        }

        private void jj_rescan_token()
        {
            jj_rescan = true;
            for (int i = 0; i < 1; i++)
            {
                try
                {
                    JJCalls p = jj_2_rtns[i];
                    do
                    {
                        if (p.gen > jj_gen)
                        {
                            jj_la = p.arg; jj_lastpos = jj_scanpos = p.first;
                            switch (i)
                            {
                                case 0: jj_3_1(); break;
                            }
                        }
                        p = p.next;
                    } while (p != null);
                }
                catch (LookaheadSuccess ls) { }
            }
            jj_rescan = false;
        }

        private void jj_save(int index, int xla)
        {
            JJCalls p = jj_2_rtns[index];
            while (p.gen > jj_gen)
            {
                if (p.next == null) { p = p.next = new JJCalls(); break; }
                p = p.next;
            }
            p.gen = jj_gen + xla - jj_la; p.first = token; p.arg = xla;
        }

        internal class JJCalls
        {
            internal int gen;
            internal Token first;
            internal int arg;
            internal JJCalls next;
        }

    }

}
