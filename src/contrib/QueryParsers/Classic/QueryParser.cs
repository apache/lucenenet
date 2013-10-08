using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.Classic
{
    public class QueryParser : QueryParserBase, IQueryParserConstants
    {
        public enum Operator
        {
            OR,
            AND
        }

        public QueryParser(Version matchVersion, String f, Analyzer a)
            : this(new FastCharStream(new StringReader("")))
        {
            Init(matchVersion, f, a);
        }

        public int Conjunction()
        {
            int ret = CONJ_NONE;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.AND:
                case QueryParserConstants.OR:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.AND:
                            jj_consume_token(QueryParserConstants.AND);
                            ret = CONJ_AND;
                            break;
                        case QueryParserConstants.OR:
                            jj_consume_token(QueryParserConstants.OR);
                            ret = CONJ_OR;
                            break;
                        default:
                            jj_la1[0] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[1] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw new Exception("Missing return statement in function");
        }

        public int Modifiers()
        {
            int ret = MOD_NONE;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.NOT:
                case QueryParserConstants.PLUS:
                case QueryParserConstants.MINUS:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.PLUS:
                            jj_consume_token(QueryParserConstants.PLUS);
                            ret = MOD_REQ;
                            break;
                        case QueryParserConstants.MINUS:
                            jj_consume_token(QueryParserConstants.MINUS);
                            ret = MOD_NOT;
                            break;
                        case QueryParserConstants.NOT:
                            jj_consume_token(QueryParserConstants.NOT);
                            ret = MOD_NOT;
                            break;
                        default:
                            jj_la1[2] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[3] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw new Exception("Missing return statement in function");
        }

        public override Query TopLevelQuery(String field)
        {
            Query q;
            q = Query(field);
            jj_consume_token(0);
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public Query Query(String field)
        {
            IList<BooleanClause> clauses = new List<BooleanClause>();
            Query q, firstQuery = null;
            int conj, mods;
            mods = Modifiers();
            q = Clause(field);
            AddClause(clauses, CONJ_NONE, mods, q);
            if (mods == MOD_NONE)
                firstQuery = q;

            while (true)
            {
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.AND:
                    case QueryParserConstants.OR:
                    case QueryParserConstants.NOT:
                    case QueryParserConstants.PLUS:
                    case QueryParserConstants.MINUS:
                    case QueryParserConstants.BAREOPER:
                    case QueryParserConstants.LPAREN:
                    case QueryParserConstants.STAR:
                    case QueryParserConstants.QUOTED:
                    case QueryParserConstants.TERM:
                    case QueryParserConstants.PREFIXTERM:
                    case QueryParserConstants.WILDTERM:
                    case QueryParserConstants.REGEXPTERM:
                    case QueryParserConstants.RANGEIN_START:
                    case QueryParserConstants.RANGEEX_START:
                    case QueryParserConstants.NUMBER:
                        ;
                        break;
                    default:
                        jj_la1[4] = jj_gen;
                        goto label_1;
                }

                conj = Conjunction();
                mods = Modifiers();
                q = Clause(field);
                AddClause(clauses, conj, mods, q);
            }

        label_1:
            if (clauses.Count == 1 && firstQuery != null)
            { 
                if (true) return firstQuery; 
            }
            else
            {
                { if (true) return GetBooleanQuery(clauses); }
            }
            throw new Exception("Missing return statement in function");
        }

        public Query Clause(String field)
        {
            Query q;
            Token fieldToken = null, boost = null;
            if (jj_2_1(2))
            {
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case QueryParserConstants.TERM:
                        fieldToken = jj_consume_token(QueryParserConstants.TERM);
                        jj_consume_token(QueryParserConstants.COLON);
                        field = DiscardEscapeChar(fieldToken.image);
                        break;
                    case QueryParserConstants.STAR:
                        jj_consume_token(QueryParserConstants.STAR);
                        jj_consume_token(QueryParserConstants.COLON);
                        field = "*";
                        break;
                    default:
                        jj_la1[5] = jj_gen;
                        jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            else
            {
                ;
            }
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.BAREOPER:
                case QueryParserConstants.STAR:
                case QueryParserConstants.QUOTED:
                case QueryParserConstants.TERM:
                case QueryParserConstants.PREFIXTERM:
                case QueryParserConstants.WILDTERM:
                case QueryParserConstants.REGEXPTERM:
                case QueryParserConstants.RANGEIN_START:
                case QueryParserConstants.RANGEEX_START:
                case QueryParserConstants.NUMBER:
                    q = Term(field);
                    break;
                case QueryParserConstants.LPAREN:
                    jj_consume_token(QueryParserConstants.LPAREN);
                    q = Query(field);
                    jj_consume_token(QueryParserConstants.RPAREN);
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.CARAT:
                            jj_consume_token(QueryParserConstants.CARAT);
                            boost = jj_consume_token(QueryParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[6] = jj_gen;
                            break;
                    }
                    break;
                default:
                    jj_la1[7] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            { if (true) return HandleBoost(q, boost); }
            throw new Exception("Missing return statement in function");
        }

        public Query Term(String field)
        {
            Token term, boost = null, fuzzySlop = null, goop1, goop2;
            bool prefix = false;
            bool wildcard = false;
            bool fuzzy = false;
            bool regexp = false;
            bool startInc = false;
            bool endInc = false;
            Query q;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case QueryParserConstants.BAREOPER:
                case QueryParserConstants.STAR:
                case QueryParserConstants.TERM:
                case QueryParserConstants.PREFIXTERM:
                case QueryParserConstants.WILDTERM:
                case QueryParserConstants.REGEXPTERM:
                case QueryParserConstants.NUMBER:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.TERM:
                            term = jj_consume_token(QueryParserConstants.TERM);
                            break;
                        case QueryParserConstants.STAR:
                            term = jj_consume_token(QueryParserConstants.STAR);
                            wildcard = true;
                            break;
                        case QueryParserConstants.PREFIXTERM:
                            term = jj_consume_token(QueryParserConstants.PREFIXTERM);
                            prefix = true;
                            break;
                        case QueryParserConstants.WILDTERM:
                            term = jj_consume_token(QueryParserConstants.WILDTERM);
                            wildcard = true;
                            break;
                        case QueryParserConstants.REGEXPTERM:
                            term = jj_consume_token(QueryParserConstants.REGEXPTERM);
                            regexp = true;
                            break;
                        case QueryParserConstants.NUMBER:
                            term = jj_consume_token(QueryParserConstants.NUMBER);
                            break;
                        case QueryParserConstants.BAREOPER:
                            term = jj_consume_token(QueryParserConstants.BAREOPER);
                            term.image = term.image.Substring(0, 1);
                            break;
                        default:
                            jj_la1[8] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(QueryParserConstants.FUZZY_SLOP);
                            fuzzy = true;
                            break;
                        default:
                            jj_la1[9] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.CARAT:
                            jj_consume_token(QueryParserConstants.CARAT);
                            boost = jj_consume_token(QueryParserConstants.NUMBER);
                            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                            {
                                case QueryParserConstants.FUZZY_SLOP:
                                    fuzzySlop = jj_consume_token(QueryParserConstants.FUZZY_SLOP);
                                    fuzzy = true;
                                    break;
                                default:
                                    jj_la1[10] = jj_gen;
                                    break;
                            }
                            break;
                        default:
                            jj_la1[11] = jj_gen;
                            break;
                    }
                    q = HandleBareTokenQuery(field, term, fuzzySlop, prefix, wildcard, fuzzy, regexp);
                    break;
                case QueryParserConstants.RANGEIN_START:
                case QueryParserConstants.RANGEEX_START:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.RANGEIN_START:
                            jj_consume_token(QueryParserConstants.RANGEIN_START);
                            startInc = true;
                            break;
                        case QueryParserConstants.RANGEEX_START:
                            jj_consume_token(QueryParserConstants.RANGEEX_START);
                            break;
                        default:
                            jj_la1[12] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.RANGE_GOOP:
                            goop1 = jj_consume_token(QueryParserConstants.RANGE_GOOP);
                            break;
                        case QueryParserConstants.RANGE_QUOTED:
                            goop1 = jj_consume_token(QueryParserConstants.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[13] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.RANGE_TO:
                            jj_consume_token(QueryParserConstants.RANGE_TO);
                            break;
                        default:
                            jj_la1[14] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.RANGE_GOOP:
                            goop2 = jj_consume_token(QueryParserConstants.RANGE_GOOP);
                            break;
                        case QueryParserConstants.RANGE_QUOTED:
                            goop2 = jj_consume_token(QueryParserConstants.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[15] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.RANGEIN_END:
                            jj_consume_token(QueryParserConstants.RANGEIN_END);
                            endInc = true;
                            break;
                        case QueryParserConstants.RANGEEX_END:
                            jj_consume_token(QueryParserConstants.RANGEEX_END);
                            break;
                        default:
                            jj_la1[16] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.CARAT:
                            jj_consume_token(QueryParserConstants.CARAT);
                            boost = jj_consume_token(QueryParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[17] = jj_gen;
                            break;
                    }
                    bool startOpen = false;
                    bool endOpen = false;
                    if (goop1.kind == QueryParserConstants.RANGE_QUOTED)
                    {
                        goop1.image = goop1.image.Substring(1, goop1.image.Length - 1);
                    }
                    else if ("*".Equals(goop1.image))
                    {
                        startOpen = true;
                    }
                    if (goop2.kind == QueryParserConstants.RANGE_QUOTED)
                    {
                        goop2.image = goop2.image.Substring(1, goop2.image.Length - 1);
                    }
                    else if ("*".Equals(goop2.image))
                    {
                        endOpen = true;
                    }
                    q = GetRangeQuery(field, startOpen ? null : DiscardEscapeChar(goop1.image), endOpen ? null : DiscardEscapeChar(goop2.image), startInc, endInc);
                    break;
                case QueryParserConstants.QUOTED:
                    term = jj_consume_token(QueryParserConstants.QUOTED);
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(QueryParserConstants.FUZZY_SLOP);
                            break;
                        default:
                            jj_la1[18] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case QueryParserConstants.CARAT:
                            jj_consume_token(QueryParserConstants.CARAT);
                            boost = jj_consume_token(QueryParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[19] = jj_gen;
                            break;
                    }
                    q = HandleQuotedTerm(field, term, fuzzySlop);
                    break;
                default:
                    jj_la1[20] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            { if (true) return HandleBoost(q, boost); }
            throw new Exception("Missing return statement in function");
        }

        private bool jj_2_1(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_1(); }
            catch (LookaheadSuccess) { return true; }
            finally { jj_save(0, xla); }
        }

        private bool jj_3R_2()
        {
            if (jj_scan_token(QueryParserConstants.TERM)) return true;
            if (jj_scan_token(QueryParserConstants.COLON)) return true;
            return false;
        }

        private bool jj_3_1()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_2())
            {
                jj_scanpos = xsp;
                if (jj_3R_3()) return true;
            }
            return false;
        }

        private bool jj_3R_3()
        {
            if (jj_scan_token(QueryParserConstants.STAR)) return true;
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
        private readonly int[] jj_la1 = new int[21];
        static private int[] jj_la1_0;
        static private int[] jj_la1_1;

        static QueryParser()
        {
            jj_la1_init_0();
            jj_la1_init_1();
        }

        private static void jj_la1_init_0()
        {
            jj_la1_0 = new int[] { 0x300, 0x300, 0x1c00, 0x1c00, 0xfda7f00, 0x120000, 0x40000, 0xfda6000, 0x9d22000, 0x200000, 0x200000, 0x40000, 0x6000000, unchecked((int)0x80000000), 0x10000000, unchecked((int)0x80000000), 0x60000000, 0x40000, 0x200000, 0x40000, 0xfda2000, };
        }
        private static void jj_la1_init_1()
        {
            jj_la1_1 = new int[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x0, 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, };
        }

        private readonly JJCalls[] jj_2_rtns = new JJCalls[1];
        private bool jj_rescan = false;
        private int jj_gc = 0;

        /** Constructor with user supplied CharStream. */
        protected QueryParser(ICharStream stream)
        {
            token_source = new QueryParserTokenManager(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 21; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public override void ReInit(ICharStream stream)
        {
            token_source.ReInit(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 21; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Constructor with generated Token Manager. */
        protected QueryParser(QueryParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 21; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public void ReInit(QueryParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 21; i++) jj_la1[i] = -1;
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

        private sealed class LookaheadSuccess : Exception { }

        private readonly LookaheadSuccess jj_ls = new LookaheadSuccess();

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

                foreach (int[] oldentry in jj_expentries)
                {
                    bool shouldContinueOuter = false;
                    if (oldentry.Length == jj_expentry.Length)
                    {
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
            bool[] la1tokens = new bool[33];
            if (jj_kind >= 0)
            {
                la1tokens[jj_kind] = true;
                jj_kind = -1;
            }
            for (int i = 0; i < 21; i++)
            {
                if (jj_la1[i] == jj_gen)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if ((jj_la1_0[i] & (1 << j)) != 0)
                        {
                            la1tokens[j] = true;
                        }
                        if ((jj_la1_1[i] & (1 << j)) != 0)
                        {
                            la1tokens[32 + j] = true;
                        }
                    }
                }
            }
            for (int i = 0; i < 33; i++)
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
        public virtual void EnableTracing()
        {
        }

        /** Disable tracing. */
        public virtual void DisableTracing()
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
                catch (LookaheadSuccess) { }
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

        internal sealed class JJCalls
        {
            public int gen;
            public Token first;
            public int arg;
            public JJCalls next;
        }
    }
}
