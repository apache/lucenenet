using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public class StandardSyntaxParser : ISyntaxParser
    {
        private const int CONJ_NONE = 0;
        private const int CONJ_AND = 2;
        private const int CONJ_OR = 2;

        public StandardSyntaxParser()
            : this(new FastCharStream(new StringReader("")))
        {
        }

        public IQueryNode Parse(ICharSequence query, ICharSequence field)
        {
            ReInit(new FastCharStream(new StringReader(query.ToString())));
            try
            {
                // TopLevelQuery is a Query followed by the end-of-input (EOF)
                IQueryNode querynode = TopLevelQuery(field);
                return querynode;
            }
            catch (ParseException tme)
            {
                tme.Query = query;
                throw tme;
            }
            catch (Exception tme)
            {
                IMessage message = new Message(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, tme.Message);
                QueryNodeParseException e = new QueryNodeParseException(tme);
                e.Query = query;
                e.SetNonLocalizedMessage(message);
                throw e;
            }
        }

        public int Conjunction()
        {
            int ret = CONJ_NONE;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case StandardSyntaxParserConstants.AND:
                case StandardSyntaxParserConstants.OR:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.AND:
                            jj_consume_token(StandardSyntaxParserConstants.AND);
                            ret = CONJ_AND;
                            break;
                        case StandardSyntaxParserConstants.OR:
                            jj_consume_token(StandardSyntaxParserConstants.OR);
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

        public ModifierQueryNode.Modifier Modifiers()
        {
            ModifierQueryNode.Modifier ret = ModifierQueryNode.Modifier.MOD_NONE;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case StandardSyntaxParserConstants.NOT:
                case StandardSyntaxParserConstants.PLUS:
                case StandardSyntaxParserConstants.MINUS:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.PLUS:
                            jj_consume_token(StandardSyntaxParserConstants.PLUS);
                            ret = ModifierQueryNode.Modifier.MOD_REQ;
                            break;
                        case StandardSyntaxParserConstants.MINUS:
                            jj_consume_token(StandardSyntaxParserConstants.MINUS);
                            ret = ModifierQueryNode.Modifier.MOD_NOT;
                            break;
                        case StandardSyntaxParserConstants.NOT:
                            jj_consume_token(StandardSyntaxParserConstants.NOT);
                            ret = ModifierQueryNode.Modifier.MOD_NOT;
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

        public IQueryNode TopLevelQuery(ICharSequence field)
        {
            IQueryNode q;
            q = Query(field);
            jj_consume_token(0);
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode Query(ICharSequence field)
        {
            List<IQueryNode> clauses = null;
            IQueryNode c, first = null;
            first = DisjQuery(field);

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case StandardSyntaxParserConstants.NOT:
                    case StandardSyntaxParserConstants.PLUS:
                    case StandardSyntaxParserConstants.MINUS:
                    case StandardSyntaxParserConstants.LPAREN:
                    case StandardSyntaxParserConstants.QUOTED:
                    case StandardSyntaxParserConstants.TERM:
                    case StandardSyntaxParserConstants.REGEXPTERM:
                    case StandardSyntaxParserConstants.RANGEIN_START:
                    case StandardSyntaxParserConstants.RANGEEX_START:
                    case StandardSyntaxParserConstants.NUMBER:
                        ;
                        break;
                    default:
                        jj_la1[4] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }

                if (shouldBreakWhile)
                    break;

                c = DisjQuery(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            if (clauses != null)
            {
                { if (true) return new BooleanQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode DisjQuery(ICharSequence field)
        {
            IQueryNode first, c;
            List<IQueryNode> clauses = null;
            first = ConjQuery(field);

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case StandardSyntaxParserConstants.OR:
                        ;
                        break;
                    default:
                        jj_la1[5] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                jj_consume_token(StandardSyntaxParserConstants.OR);
                c = ConjQuery(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            if (clauses != null)
            {
                { if (true) return new OrQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode ConjQuery(ICharSequence field)
        {
            IQueryNode first, c;
            List<IQueryNode> clauses = null;
            first = ModClause(field);

            while (true)
            {
                bool shouldBreakWhile = false;
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case StandardSyntaxParserConstants.AND:
                        ;
                        break;
                    default:
                        jj_la1[6] = jj_gen;
                        shouldBreakWhile = true;
                        break;
                }
                if (shouldBreakWhile)
                    break;
                jj_consume_token(StandardSyntaxParserConstants.AND);
                c = ModClause(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            if (clauses != null)
            {
                { if (true) return new AndQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode ModClause(ICharSequence field)
        {
            IQueryNode q;
            ModifierQueryNode.Modifier mods;
            mods = Modifiers();
            q = Clause(field);
            if (mods != ModifierQueryNode.Modifier.MOD_NONE)
            {
                q = new ModifierQueryNode(q, mods);
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode Clause(ICharSequence field)
        {
            IQueryNode q;
            Token fieldToken = null, boost = null, @operator = null, term = null;
            FieldQueryNode qLower, qUpper;
            bool lowerInclusive, upperInclusive;

            bool group = false;
            if (jj_2_2(3))
            {
                fieldToken = jj_consume_token(StandardSyntaxParserConstants.TERM);
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case StandardSyntaxParserConstants.OP_COLON:
                    case StandardSyntaxParserConstants.OP_EQUAL:
                        switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                        {
                            case StandardSyntaxParserConstants.OP_COLON:
                                jj_consume_token(StandardSyntaxParserConstants.OP_COLON);
                                break;
                            case StandardSyntaxParserConstants.OP_EQUAL:
                                jj_consume_token(StandardSyntaxParserConstants.OP_EQUAL);
                                break;
                            default:
                                jj_la1[7] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
                        q = Term(field);
                        break;
                    case StandardSyntaxParserConstants.OP_LESSTHAN:
                    case StandardSyntaxParserConstants.OP_LESSTHANEQ:
                    case StandardSyntaxParserConstants.OP_MORETHAN:
                    case StandardSyntaxParserConstants.OP_MORETHANEQ:
                        switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                        {
                            case StandardSyntaxParserConstants.OP_LESSTHAN:
                                @operator = jj_consume_token(StandardSyntaxParserConstants.OP_LESSTHAN);
                                break;
                            case StandardSyntaxParserConstants.OP_LESSTHANEQ:
                                @operator = jj_consume_token(StandardSyntaxParserConstants.OP_LESSTHANEQ);
                                break;
                            case StandardSyntaxParserConstants.OP_MORETHAN:
                                @operator = jj_consume_token(StandardSyntaxParserConstants.OP_MORETHAN);
                                break;
                            case StandardSyntaxParserConstants.OP_MORETHANEQ:
                                @operator = jj_consume_token(StandardSyntaxParserConstants.OP_MORETHANEQ);
                                break;
                            default:
                                jj_la1[8] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
                        switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                        {
                            case StandardSyntaxParserConstants.TERM:
                                term = jj_consume_token(StandardSyntaxParserConstants.TERM);
                                break;
                            case StandardSyntaxParserConstants.QUOTED:
                                term = jj_consume_token(StandardSyntaxParserConstants.QUOTED);
                                break;
                            case StandardSyntaxParserConstants.NUMBER:
                                term = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                                break;
                            default:
                                jj_la1[9] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        if (term.kind == StandardSyntaxParserConstants.QUOTED)
                        {
                            term.image = term.image.Substring(1, term.image.Length - 1);
                        }
                        switch (@operator.kind)
                        {
                            case StandardSyntaxParserConstants.OP_LESSTHAN:
                                lowerInclusive = true;
                                upperInclusive = false;

                                qLower = new FieldQueryNode(field, new StringCharSequenceWrapper("*"), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);

                                break;
                            case StandardSyntaxParserConstants.OP_LESSTHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field, new StringCharSequenceWrapper("*"), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                break;
                            case StandardSyntaxParserConstants.OP_MORETHAN:
                                lowerInclusive = false;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field, new StringCharSequenceWrapper("*"), term.beginColumn, term.endColumn);
                                break;
                            case StandardSyntaxParserConstants.OP_MORETHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field, new StringCharSequenceWrapper("*"), term.beginColumn, term.endColumn);
                                break;
                            default:
                                { if (true) throw new Exception("Unhandled case: operator=" + @operator.ToString()); }
                        }
                        q = new TermRangeQueryNode(qLower, qUpper, lowerInclusive, upperInclusive);
                        break;
                    default:
                        jj_la1[10] = jj_gen;
                        jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            else
            {
                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                {
                    case StandardSyntaxParserConstants.LPAREN:
                    case StandardSyntaxParserConstants.QUOTED:
                    case StandardSyntaxParserConstants.TERM:
                    case StandardSyntaxParserConstants.REGEXPTERM:
                    case StandardSyntaxParserConstants.RANGEIN_START:
                    case StandardSyntaxParserConstants.RANGEEX_START:
                    case StandardSyntaxParserConstants.NUMBER:
                        if (jj_2_1(2))
                        {
                            fieldToken = jj_consume_token(StandardSyntaxParserConstants.TERM);
                            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                            {
                                case StandardSyntaxParserConstants.OP_COLON:
                                    jj_consume_token(StandardSyntaxParserConstants.OP_COLON);
                                    break;
                                case StandardSyntaxParserConstants.OP_EQUAL:
                                    jj_consume_token(StandardSyntaxParserConstants.OP_EQUAL);
                                    break;
                                default:
                                    jj_la1[11] = jj_gen;
                                    jj_consume_token(-1);
                                    throw new ParseException();
                            }
                            field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
                        }
                        else
                        {
                            ;
                        }
                        switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                        {
                            case StandardSyntaxParserConstants.QUOTED:
                            case StandardSyntaxParserConstants.TERM:
                            case StandardSyntaxParserConstants.REGEXPTERM:
                            case StandardSyntaxParserConstants.RANGEIN_START:
                            case StandardSyntaxParserConstants.RANGEEX_START:
                            case StandardSyntaxParserConstants.NUMBER:
                                q = Term(field);
                                break;
                            case StandardSyntaxParserConstants.LPAREN:
                                jj_consume_token(StandardSyntaxParserConstants.LPAREN);
                                q = Query(field);
                                jj_consume_token(StandardSyntaxParserConstants.RPAREN);
                                switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                                {
                                    case StandardSyntaxParserConstants.CARAT:
                                        jj_consume_token(StandardSyntaxParserConstants.CARAT);
                                        boost = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                                        break;
                                    default:
                                        jj_la1[12] = jj_gen;
                                        break;
                                }
                                group = true;
                                break;
                            default:
                                jj_la1[13] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        break;
                    default:
                        jj_la1[14] = jj_gen;
                        jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = Convert.ToSingle(boost.image);
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception)
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                         * boost number is invalid)
                         */
                }
            }
            if (group) { q = new GroupQueryNode(q); }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode Term(ICharSequence field)
        {
            Token term, boost = null, fuzzySlop = null, goop1, goop2;
            bool fuzzy = false;
            bool regexp = false;
            bool startInc = false;
            bool endInc = false;
            IQueryNode q = null;
            FieldQueryNode qLower, qUpper;
            float defaultMinSimilarity = FuzzyQuery.defaultMinSimilarity;
            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
            {
                case StandardSyntaxParserConstants.TERM:
                case StandardSyntaxParserConstants.REGEXPTERM:
                case StandardSyntaxParserConstants.NUMBER:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.TERM:
                            term = jj_consume_token(StandardSyntaxParserConstants.TERM);
                            q = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                            break;
                        case StandardSyntaxParserConstants.REGEXPTERM:
                            term = jj_consume_token(StandardSyntaxParserConstants.REGEXPTERM);
                            regexp = true;
                            break;
                        case StandardSyntaxParserConstants.NUMBER:
                            term = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[15] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(StandardSyntaxParserConstants.FUZZY_SLOP);
                            fuzzy = true;
                            break;
                        default:
                            jj_la1[16] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.CARAT:
                            jj_consume_token(StandardSyntaxParserConstants.CARAT);
                            boost = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                            switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                            {
                                case StandardSyntaxParserConstants.FUZZY_SLOP:
                                    fuzzySlop = jj_consume_token(StandardSyntaxParserConstants.FUZZY_SLOP);
                                    fuzzy = true;
                                    break;
                                default:
                                    jj_la1[17] = jj_gen;
                                    break;
                            }
                            break;
                        default:
                            jj_la1[18] = jj_gen;
                            break;
                    }
                    if (fuzzy)
                    {
                        float fms = defaultMinSimilarity;
                        try
                        {
                            fms = Convert.ToSingle(fuzzySlop.image.Substring(1));
                        }
                        catch (Exception) { }
                        if (fms < 0.0f)
                        {
                            { if (true) throw new ParseException(new Message(QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS)); }
                        }
                        else if (fms >= 1.0f && fms != (int)fms)
                        {
                            { if (true) throw new ParseException(new Message(QueryParserMessages.INVALID_SYNTAX_FUZZY_EDITS)); }
                        }
                        q = new FuzzyQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), fms, term.beginColumn, term.endColumn);
                    }
                    else if (regexp)
                    {
                        String re = term.image.Substring(1, term.image.Length - 1);
                        q = new RegexpQueryNode(field, new StringCharSequenceWrapper(re), 0, re.Length);
                    }
                    break;
                case StandardSyntaxParserConstants.RANGEIN_START:
                case StandardSyntaxParserConstants.RANGEEX_START:
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.RANGEIN_START:
                            jj_consume_token(StandardSyntaxParserConstants.RANGEIN_START);
                            startInc = true;
                            break;
                        case StandardSyntaxParserConstants.RANGEEX_START:
                            jj_consume_token(StandardSyntaxParserConstants.RANGEEX_START);
                            break;
                        default:
                            jj_la1[19] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.RANGE_GOOP:
                            goop1 = jj_consume_token(StandardSyntaxParserConstants.RANGE_GOOP);
                            break;
                        case StandardSyntaxParserConstants.RANGE_QUOTED:
                            goop1 = jj_consume_token(StandardSyntaxParserConstants.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[20] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.RANGE_TO:
                            jj_consume_token(StandardSyntaxParserConstants.RANGE_TO);
                            break;
                        default:
                            jj_la1[21] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.RANGE_GOOP:
                            goop2 = jj_consume_token(StandardSyntaxParserConstants.RANGE_GOOP);
                            break;
                        case StandardSyntaxParserConstants.RANGE_QUOTED:
                            goop2 = jj_consume_token(StandardSyntaxParserConstants.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[22] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.RANGEIN_END:
                            jj_consume_token(StandardSyntaxParserConstants.RANGEIN_END);
                            endInc = true;
                            break;
                        case StandardSyntaxParserConstants.RANGEEX_END:
                            jj_consume_token(StandardSyntaxParserConstants.RANGEEX_END);
                            break;
                        default:
                            jj_la1[23] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.CARAT:
                            jj_consume_token(StandardSyntaxParserConstants.CARAT);
                            boost = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[24] = jj_gen;
                            break;
                    }
                    if (goop1.kind == StandardSyntaxParserConstants.RANGE_QUOTED)
                    {
                        goop1.image = goop1.image.Substring(1, goop1.image.Length - 1);
                    }
                    if (goop2.kind == StandardSyntaxParserConstants.RANGE_QUOTED)
                    {
                        goop2.image = goop2.image.Substring(1, goop2.image.Length - 1);
                    }

                    qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(goop1.image), goop1.beginColumn, goop1.endColumn);
                    qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(goop2.image), goop2.beginColumn, goop2.endColumn);
                    q = new TermRangeQueryNode(qLower, qUpper, startInc ? true : false, endInc ? true : false);
                    break;
                case StandardSyntaxParserConstants.QUOTED:
                    term = jj_consume_token(StandardSyntaxParserConstants.QUOTED);
                    q = new QuotedFieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image.Substring(1, term.image.Length - 1)), term.beginColumn + 1, term.endColumn - 1);
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(StandardSyntaxParserConstants.FUZZY_SLOP);
                            break;
                        default:
                            jj_la1[25] = jj_gen;
                            break;
                    }
                    switch ((_jj_ntk == -1) ? jj_ntk() : _jj_ntk)
                    {
                        case StandardSyntaxParserConstants.CARAT:
                            jj_consume_token(StandardSyntaxParserConstants.CARAT);
                            boost = jj_consume_token(StandardSyntaxParserConstants.NUMBER);
                            break;
                        default:
                            jj_la1[26] = jj_gen;
                            break;
                    }
                    int phraseSlop = 0;

                    if (fuzzySlop != null)
                    {
                        try
                        {
                            phraseSlop = (int)Convert.ToSingle(fuzzySlop.image.Substring(1));
                            q = new SlopQueryNode(q, phraseSlop);
                        }
                        catch (Exception)
                        {
                            /* Should this be handled somehow? (defaults to "no PhraseSlop", if
                           * slop number is invalid)
                           */
                        }
                    }
                    break;
                default:
                    jj_la1[27] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = Convert.ToSingle(boost.image);
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception)
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                       * boost number is invalid)
                       */
                }
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        private bool jj_2_1(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_1(); }
            catch (LookaheadSuccess) { return true; }
            finally { jj_save(0, xla); }
        }

        private bool jj_2_2(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_2(); }
            catch (LookaheadSuccess) { return true; }
            finally { jj_save(1, xla); }
        }

        private bool jj_3_2()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_4())
            {
                jj_scanpos = xsp;
                if (jj_3R_5()) return true;
            }
            return false;
        }

        private bool jj_3R_12()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.RANGEIN_START)) return true;
            return false;
        }

        private bool jj_3R_11()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.REGEXPTERM)) return true;
            return false;
        }

        private bool jj_3_1()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(16)) return true;
            }
            return false;
        }

        private bool jj_3R_8()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_12())
            {
                jj_scanpos = xsp;
                if (jj_scan_token(27)) return true;
            }
            return false;
        }

        private bool jj_3R_10()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.TERM)) return true;
            return false;
        }

        private bool jj_3R_7()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_10())
            {
                jj_scanpos = xsp;
                if (jj_3R_11())
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool jj_3R_9()
        {
            if (jj_scan_token(StandardSyntaxParserConstants.QUOTED)) return true;
            return false;
        }

        private bool jj_3R_5()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(17))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(18))
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(19))
                    {
                        jj_scanpos = xsp;
                        if (jj_scan_token(20)) return true;
                    }
                }
            }
            xsp = jj_scanpos;
            if (jj_scan_token(23))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(22))
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool jj_3R_4()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(16)) return true;
            }
            if (jj_3R_6()) return true;
            return false;
        }

        private bool jj_3R_6()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_7())
            {
                jj_scanpos = xsp;
                if (jj_3R_8())
                {
                    jj_scanpos = xsp;
                    if (jj_3R_9()) return true;
                }
            }
            return false;
        }

        /** Generated Token Manager. */
        public StandardSyntaxParserTokenManager token_source;
        /** Current token. */
        public Token token;
        /** Next token. */
        public Token jj_nt;
        private int _jj_ntk;
        private Token jj_scanpos, jj_lastpos;
        private int jj_la;
        private int jj_gen;
        private readonly int[] jj_la1 = new int[28];
        static private int[] jj_la1_0;
        static private int[] jj_la1_1;

        static StandardSyntaxParser()
        {
            jj_la1_init_0();
            jj_la1_init_1();
        }

        private static void jj_la1_init_0()
        {
            jj_la1_0 = new int[] { 0x300, 0x300, 0x1c00, 0x1c00, 0x1ec03c00, 0x200, 0x100, 0x18000, 0x1e0000, 0x10c00000, 0x1f8000, 0x18000, 0x200000, 0x1ec02000, 0x1ec02000, 0x12800000, 0x1000000, 0x1000000, 0x200000, 0xc000000, 0x0, 0x20000000, 0x0, unchecked((int)0xc0000000), 0x200000, 0x1000000, 0x200000, 0x1ec00000, };
        }
        private static void jj_la1_init_1()
        {
            jj_la1_1 = new int[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3, 0x0, 0x3, 0x0, 0x0, 0x0, 0x0, 0x0, };
        }

        readonly private JJCalls[] jj_2_rtns = new JJCalls[2];
        private bool jj_rescan = false;
        private int jj_gc = 0;

        public StandardSyntaxParser(ICharStream stream)
        {
            token_source = new StandardSyntaxParserTokenManager(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        public void ReInit(ICharStream stream)
        {
            token_source.ReInit(stream);
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        public StandardSyntaxParser(StandardSyntaxParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        public void ReInit(StandardSyntaxParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            _jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
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
        readonly private LookaheadSuccess jj_ls = new LookaheadSuccess();

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

        public Token GetNextToken()
        {
            if (token.next != null) token = token.next;
            else token = token.next = token_source.GetNextToken();
            _jj_ntk = -1;
            jj_gen++;
            return token;
        }

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
                    bool shouldContinueForeach = false;

                    int[] oldentry = it;
                    if (oldentry.Length == jj_expentry.Length)
                    {
                        for (int i = 0; i < jj_expentry.Length; i++)
                        {
                            if (oldentry[i] != jj_expentry[i])
                            {
                                shouldContinueForeach = true;
                                break;
                            }
                        }

                        if (shouldContinueForeach)
                            continue;

                        jj_expentries.Add(jj_expentry);

                        break;
                    }
                }
                if (pos != 0) jj_lasttokens[(jj_endpos = pos) - 1] = kind;
            }
        }

        public ParseException GenerateParseException()
        {
            jj_expentries.Clear();
            bool[] la1tokens = new bool[34];
            if (jj_kind >= 0)
            {
                la1tokens[jj_kind] = true;
                jj_kind = -1;
            }
            for (int i = 0; i < 28; i++)
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
            for (int i = 0; i < 34; i++)
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
            return new ParseException(token, exptokseq, StandardSyntaxParserConstants.tokenImage);
        }

        public void enable_tracing()
        {
        }

        public void disable_tracing()
        {
        }

        private void jj_rescan_token()
        {
            jj_rescan = true;
            for (int i = 0; i < 2; i++)
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
                                case 1: jj_3_2(); break;
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
            internal int gen;
            internal Token first;
            internal int arg;
            internal JJCalls next;
        }
    }
}
