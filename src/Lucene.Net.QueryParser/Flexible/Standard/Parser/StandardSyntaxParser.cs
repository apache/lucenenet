using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
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
    /// Parser for the standard Lucene syntax
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This class is based on generated code")]
    [SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "This class is based on generated code")]
    [SuppressMessage("Style", "IDE0028:Collection initialization can be simplified", Justification = "This class is based on generated code")]
    public class StandardSyntaxParser : ISyntaxParser /*, StandardSyntaxParserConstants*/
    {
        private const int CONJ_NONE = 0;
        private const int CONJ_AND = 2;
        private const int CONJ_OR = 2;


        // syntax parser constructor
        public StandardSyntaxParser()
            : this(new FastCharStream(new StringReader("")))
        {
        }

        /// <summary>
        /// Parses a query string, returning a <see cref="IQueryNode"/>.
        /// </summary>
        /// <param name="query">the query string to be parsed.</param>
        /// <param name="field"></param>
        /// <returns></returns>
        /// <exception cref="ParseException">if the parsing fails</exception>
        public IQueryNode Parse(string query, string field)
        {
            ReInit(new FastCharStream(new StringReader(query)));
            try
            {
                // TopLevelQuery is a Query followed by the end-of-input (EOF)
                IQueryNode querynode = TopLevelQuery(field);
                return querynode;
            }
            catch (Lucene.Net.QueryParsers.Flexible.Standard.Parser.ParseException tme) // LUCENENET: Flexible QueryParser has its own ParseException that is different than the one in Support
            {
                // LUCENENET specific - removed NLS support (since .NET already has localization) so we need to re-throw the exception here with a different message. However,
                // unlike the original Lucene code we also preserve the original message in the InnerException.
                throw new Lucene.Net.QueryParsers.Flexible.Standard.Parser.ParseException(string.Format(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, string.Empty), tme) { Query = query };
            }
            catch (Exception tme) when (tme.IsError())
            {
                // LUCENENET specific - removed NLS support (since .NET already has localization) so we pass everything through the constructor of the
                // exception.
                throw new QueryNodeParseException(string.Format(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, tme.Message), tme) { Query = query };
            }
        }

        // *   Query  ::= ( Clause )*
        // *   Clause ::= ["+", "-"] [<TERM> ":"] ( <TERM> | "(" Query ")" )
        public int Conjunction()
        {
            int ret = CONJ_NONE;
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.AND:
                case RegexpToken.OR:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.AND:
                            Jj_consume_token(RegexpToken.AND);
                            ret = CONJ_AND;
                            break;
                        case RegexpToken.OR:
                            Jj_consume_token(RegexpToken.OR);
                            ret = CONJ_OR;
                            break;
                        default:
                            jj_la1[0] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[1] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw Error.Create("Missing return statement in function");
        }

        public Modifier Modifiers()
        {
            Modifier ret = Modifier.MOD_NONE;
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.NOT:
                case RegexpToken.PLUS:
                case RegexpToken.MINUS:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.PLUS:
                            Jj_consume_token(RegexpToken.PLUS);
                            ret = Modifier.MOD_REQ;
                            break;
                        case RegexpToken.MINUS:
                            Jj_consume_token(RegexpToken.MINUS);
                            ret = Modifier.MOD_NOT;
                            break;
                        case RegexpToken.NOT:
                            Jj_consume_token(RegexpToken.NOT);
                            ret = Modifier.MOD_NOT;
                            break;
                        default:
                            jj_la1[2] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[3] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw Error.Create("Missing return statement in function");
        }

        // This makes sure that there is no garbage after the query string
        public IQueryNode TopLevelQuery(string field)
        {
            IQueryNode q;
            q = Query(field);
            Jj_consume_token(0);
            { if (true) return q; }
            throw Error.Create("Missing return statement in function");
        }


        // These changes were made to introduce operator precedence:
        // - Clause() now returns a QueryNode.
        // - The modifiers are consumed by Clause() and returned as part of the QueryNode Object
        // - Query does not consume conjunctions (AND, OR) anymore.
        // - This is now done by two new non-terminals: ConjClause and DisjClause
        // The parse tree looks similar to this:
        //       Query ::= DisjQuery ( DisjQuery )*
        //   DisjQuery ::= ConjQuery ( OR ConjQuery )*
        //   ConjQuery ::= Clause ( AND Clause )*
        //      Clause ::= [ Modifier ] ...
        public IQueryNode Query(string field)
        {
            IList<IQueryNode> clauses = null;
            IQueryNode c, first = null;
            first = DisjQuery(field);

            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.NOT:
                    case RegexpToken.PLUS:
                    case RegexpToken.MINUS:
                    case RegexpToken.LPAREN:
                    case RegexpToken.QUOTED:
                    case RegexpToken.TERM:
                    case RegexpToken.REGEXPTERM:
                    case RegexpToken.RANGEIN_START:
                    case RegexpToken.RANGEEX_START:
                    case RegexpToken.NUMBER:
                        break;
                    default:
                        jj_la1[4] = jj_gen;
                        goto label_1_break;
                }
                c = DisjQuery(field);
                if (clauses is null)
                {
                    clauses = new JCG.List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_1_break:
            if (clauses != null)
            {
                { if (true) return new BooleanQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw Error.Create("Missing return statement in function");
        }

        public IQueryNode DisjQuery(string field)
        {
            IQueryNode first, c;
            IList<IQueryNode> clauses = null;
            first = ConjQuery(field);

            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.OR:
                        break;
                    default:
                        jj_la1[5] = jj_gen;
                        goto label_2_break;
                }
                Jj_consume_token(RegexpToken.OR);
                c = ConjQuery(field);
                if (clauses is null)
                {
                    clauses = new JCG.List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_2_break:
            if (clauses != null)
            {
                { if (true) return new OrQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw Error.Create("Missing return statement in function");
        }

        public IQueryNode ConjQuery(string field)
        {
            IQueryNode first, c;
            IList<IQueryNode> clauses = null;
            first = ModClause(field);

            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.AND:
                        break;
                    default:
                        jj_la1[6] = jj_gen;
                        goto label_3_break;
                }
                Jj_consume_token(RegexpToken.AND);
                c = ModClause(field);
                if (clauses is null)
                {
                    clauses = new JCG.List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_3_break:
            if (clauses != null)
            {
                { if (true) return new AndQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw Error.Create("Missing return statement in function");
        }

        // QueryNode Query(CharSequence field) :
        // {
        // List clauses = new ArrayList();
        //   List modifiers = new ArrayList();
        //   QueryNode q, firstQuery=null;
        //   ModifierQueryNode.Modifier mods;
        //   int conj;
        // }
        // {
        //   mods=Modifiers() q=Clause(field)
        //   {
        //     if (mods == ModifierQueryNode.Modifier.MOD_NONE) firstQuery=q;
        //
        //     // do not create modifier nodes with MOD_NONE
        //      if (mods != ModifierQueryNode.Modifier.MOD_NONE) {
        //        q = new ModifierQueryNode(q, mods);
        //      }
        //      clauses.add(q);
        //   }
        //   (
        //     conj=Conjunction() mods=Modifiers() q=Clause(field)
        //     {
        //       // do not create modifier nodes with MOD_NONE
        //        if (mods != ModifierQueryNode.Modifier.MOD_NONE) {
        //          q = new ModifierQueryNode(q, mods);
        //        }
        //        clauses.add(q);
        //        //TODO: figure out what to do with AND and ORs
        //   }
        //   )*
        //     {
        //      if (clauses.size() == 1 && firstQuery != null)
        //         return firstQuery;
        //       else {
        //       return new BooleanQueryNode(clauses);
        //       }
        //     }
        // }
        public IQueryNode ModClause(string field)
        {
            IQueryNode q;
            Modifier mods;
            mods = Modifiers();
            q = Clause(field);
            if (mods != Modifier.MOD_NONE)
            {
                q = new ModifierQueryNode(q, mods);
            }
            { if (true) return q; }
            throw Error.Create("Missing return statement in function");
        }

        public IQueryNode Clause(string field)
        {
            IQueryNode q;
            Token fieldToken = null, boost = null, @operator = null, term = null;
            FieldQueryNode qLower, qUpper;
            bool lowerInclusive, upperInclusive;

            bool group = false;
            if (Jj_2_2(3))
            {
                fieldToken = Jj_consume_token(RegexpToken.TERM);
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.OP_COLON:
                    case RegexpToken.OP_EQUAL:
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.OP_COLON:
                                Jj_consume_token(RegexpToken.OP_COLON);
                                break;
                            case RegexpToken.OP_EQUAL:
                                Jj_consume_token(RegexpToken.OP_EQUAL);
                                break;
                            default:
                                jj_la1[7] = jj_gen;
                                Jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntax.DiscardEscapeChar(fieldToken.Image).ToString();
                        q = Term(field);
                        break;
                    case RegexpToken.OP_LESSTHAN:
                    case RegexpToken.OP_LESSTHANEQ:
                    case RegexpToken.OP_MORETHAN:
                    case RegexpToken.OP_MORETHANEQ:
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.OP_LESSTHAN:
                                @operator = Jj_consume_token(RegexpToken.OP_LESSTHAN);
                                break;
                            case RegexpToken.OP_LESSTHANEQ:
                                @operator = Jj_consume_token(RegexpToken.OP_LESSTHANEQ);
                                break;
                            case RegexpToken.OP_MORETHAN:
                                @operator = Jj_consume_token(RegexpToken.OP_MORETHAN);
                                break;
                            case RegexpToken.OP_MORETHANEQ:
                                @operator = Jj_consume_token(RegexpToken.OP_MORETHANEQ);
                                break;
                            default:
                                jj_la1[8] = jj_gen;
                                Jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntax.DiscardEscapeChar(fieldToken.Image).ToString();
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.TERM:
                                term = Jj_consume_token(RegexpToken.TERM);
                                break;
                            case RegexpToken.QUOTED:
                                term = Jj_consume_token(RegexpToken.QUOTED);
                                break;
                            case RegexpToken.NUMBER:
                                term = Jj_consume_token(RegexpToken.NUMBER);
                                break;
                            default:
                                jj_la1[9] = jj_gen;
                                Jj_consume_token(-1);
                                throw new ParseException();
                        }
                        if (term.Kind == RegexpToken.QUOTED)
                        {
                            term.Image = term.Image.Substring(1, (term.Image.Length - 1) - 1);
                        }
                        switch (@operator.Kind)
                        {
                            case RegexpToken.OP_LESSTHAN:
                                lowerInclusive = true;
                                upperInclusive = false;

                                qLower = new FieldQueryNode(field,
                                                           "*", term.BeginColumn, term.EndColumn);
                                qUpper = new FieldQueryNode(field,
                                                     EscapeQuerySyntax.DiscardEscapeChar(term.Image), term.BeginColumn, term.EndColumn);

                                break;
                            case RegexpToken.OP_LESSTHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         "*", term.BeginColumn, term.EndColumn);
                                qUpper = new FieldQueryNode(field,
                                                         EscapeQuerySyntax.DiscardEscapeChar(term.Image), term.BeginColumn, term.EndColumn);
                                break;
                            case RegexpToken.OP_MORETHAN:
                                lowerInclusive = false;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         EscapeQuerySyntax.DiscardEscapeChar(term.Image), term.BeginColumn, term.EndColumn);
                                qUpper = new FieldQueryNode(field,
                                                         "*", term.BeginColumn, term.EndColumn);
                                break;
                            case RegexpToken.OP_MORETHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         EscapeQuerySyntax.DiscardEscapeChar(term.Image), term.BeginColumn, term.EndColumn);
                                qUpper = new FieldQueryNode(field,
                                                         "*", term.BeginColumn, term.EndColumn);
                                break;
                            default:
                                { if (true) throw Error.Create("Unhandled case: operator=" + @operator.ToString()); }
                        }
                        q = new TermRangeQueryNode(qLower, qUpper, lowerInclusive, upperInclusive);
                        break;
                    default:
                        jj_la1[10] = jj_gen;
                        Jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            else
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.LPAREN:
                    case RegexpToken.QUOTED:
                    case RegexpToken.TERM:
                    case RegexpToken.REGEXPTERM:
                    case RegexpToken.RANGEIN_START:
                    case RegexpToken.RANGEEX_START:
                    case RegexpToken.NUMBER:
                        if (Jj_2_1(2))
                        {
                            fieldToken = Jj_consume_token(RegexpToken.TERM);
                            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                            {
                                case RegexpToken.OP_COLON:
                                    Jj_consume_token(RegexpToken.OP_COLON);
                                    break;
                                case RegexpToken.OP_EQUAL:
                                    Jj_consume_token(RegexpToken.OP_EQUAL);
                                    break;
                                default:
                                    jj_la1[11] = jj_gen;
                                    Jj_consume_token(-1);
                                    throw new ParseException();
                            }
                            field = EscapeQuerySyntax.DiscardEscapeChar(fieldToken.Image).ToString();
                        }
                        else
                        {
                            /* LUCENENET: intentionally blank */
                        }
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.QUOTED:
                            case RegexpToken.TERM:
                            case RegexpToken.REGEXPTERM:
                            case RegexpToken.RANGEIN_START:
                            case RegexpToken.RANGEEX_START:
                            case RegexpToken.NUMBER:
                                q = Term(field);
                                break;
                            case RegexpToken.LPAREN:
                                Jj_consume_token(RegexpToken.LPAREN);
                                q = Query(field);
                                Jj_consume_token(RegexpToken.RPAREN);
                                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                                {
                                    case RegexpToken.CARAT:
                                        Jj_consume_token(RegexpToken.CARAT);
                                        boost = Jj_consume_token(RegexpToken.NUMBER);
                                        break;
                                    default:
                                        jj_la1[12] = jj_gen;
                                        break;
                                }
                                group = true;
                                break;
                            default:
                                jj_la1[13] = jj_gen;
                                Jj_consume_token(-1);
                                throw new ParseException();
                        }
                        break;
                    default:
                        jj_la1[14] = jj_gen;
                        Jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            if (boost != null)
            {
                // LUCENENET specific - parse without throwing exceptions
                float f = float.TryParse(boost.Image, NumberStyles.Float, CultureInfo.InvariantCulture, out float temp) ? temp : 1.0f;
                try
                {
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception ignored) when (ignored.IsException())
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                         * boost number is invalid)
                         */
                }
            }
            if (group) { q = new GroupQueryNode(q); }
            { if (true) return q; }
            throw Error.Create("Missing return statement in function");
        }

        public IQueryNode Term(string field)
        {
            Token term, boost = null, fuzzySlop = null, goop1, goop2;
            bool fuzzy = false;
            bool regexp = false;
            bool startInc = false;
            bool endInc = false;
            IQueryNode q = null;
            FieldQueryNode qLower, qUpper;
#pragma warning disable 612, 618
            float defaultMinSimilarity = Search.FuzzyQuery.DefaultMinSimilarity;
#pragma warning restore 612, 618
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.TERM:
                case RegexpToken.REGEXPTERM:
                case RegexpToken.NUMBER:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.TERM:
                            term = Jj_consume_token(RegexpToken.TERM);
                            q = new FieldQueryNode(field, EscapeQuerySyntax.DiscardEscapeChar(term.Image), term.BeginColumn, term.EndColumn);
                            break;
                        case RegexpToken.REGEXPTERM:
                            term = Jj_consume_token(RegexpToken.REGEXPTERM);
                            regexp = true;
                            break;
                        case RegexpToken.NUMBER:
                            term = Jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[15] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.FUZZY_SLOP:
                            fuzzySlop = Jj_consume_token(RegexpToken.FUZZY_SLOP);
                            fuzzy = true;
                            break;
                        default:
                            jj_la1[16] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            Jj_consume_token(RegexpToken.CARAT);
                            boost = Jj_consume_token(RegexpToken.NUMBER);
                            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                            {
                                case RegexpToken.FUZZY_SLOP:
                                    fuzzySlop = Jj_consume_token(RegexpToken.FUZZY_SLOP);
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
                        // LUCENENET specific: parse without throwing exceptions
#if FEATURE_NUMBER_PARSE_READONLYSPAN
                        float fms = float.TryParse(fuzzySlop.Image.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float temp) ? temp : defaultMinSimilarity;
#else
                        float fms = float.TryParse(fuzzySlop.Image.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float temp) ? temp: defaultMinSimilarity;
#endif
                        if (fms < 0.0f)
                        {
                            { if (true) throw new ParseException(QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS); }
                        }
                        else if (fms >= 1.0f && fms != (int)fms)
                        {
                            { if (true) throw new ParseException(QueryParserMessages.INVALID_SYNTAX_FUZZY_EDITS); }
                        }
                        q = new FuzzyQueryNode(field, EscapeQuerySyntax.DiscardEscapeChar(term.Image), fms, term.BeginColumn, term.EndColumn);
                    }
                    else if (regexp)
                    {
                        string re = term.Image.Substring(1, (term.Image.Length - 1) - 1);
                        q = new RegexpQueryNode(field, re, 0, re.Length);
                    }
                    break;
                case RegexpToken.RANGEIN_START:
                case RegexpToken.RANGEEX_START:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGEIN_START:
                            Jj_consume_token(RegexpToken.RANGEIN_START);
                            startInc = true;
                            break;
                        case RegexpToken.RANGEEX_START:
                            Jj_consume_token(RegexpToken.RANGEEX_START);
                            break;
                        default:
                            jj_la1[19] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_GOOP:
                            goop1 = Jj_consume_token(RegexpToken.RANGE_GOOP);
                            break;
                        case RegexpToken.RANGE_QUOTED:
                            goop1 = Jj_consume_token(RegexpToken.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[20] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_TO:
                            Jj_consume_token(RegexpToken.RANGE_TO);
                            break;
                        default:
                            jj_la1[21] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_GOOP:
                            goop2 = Jj_consume_token(RegexpToken.RANGE_GOOP);
                            break;
                        case RegexpToken.RANGE_QUOTED:
                            goop2 = Jj_consume_token(RegexpToken.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[22] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGEIN_END:
                            Jj_consume_token(RegexpToken.RANGEIN_END);
                            endInc = true;
                            break;
                        case RegexpToken.RANGEEX_END:
                            Jj_consume_token(RegexpToken.RANGEEX_END);
                            break;
                        default:
                            jj_la1[23] = jj_gen;
                            Jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            Jj_consume_token(RegexpToken.CARAT);
                            boost = Jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[24] = jj_gen;
                            break;
                    }
                    if (goop1.Kind == RegexpToken.RANGE_QUOTED)
                    {
                        goop1.Image = goop1.Image.Substring(1, (goop1.Image.Length - 1) - 1);
                    }
                    if (goop2.Kind == RegexpToken.RANGE_QUOTED)
                    {
                        goop2.Image = goop2.Image.Substring(1, (goop2.Image.Length - 1) - 1);
                    }

                    qLower = new FieldQueryNode(field,
                                             EscapeQuerySyntax.DiscardEscapeChar(goop1.Image), goop1.BeginColumn, goop1.EndColumn);
                    qUpper = new FieldQueryNode(field,
                                                 EscapeQuerySyntax.DiscardEscapeChar(goop2.Image), goop2.BeginColumn, goop2.EndColumn);
                    q = new TermRangeQueryNode(qLower, qUpper, startInc ? true : false, endInc ? true : false);
                    break;
                case RegexpToken.QUOTED:
                    term = Jj_consume_token(RegexpToken.QUOTED);
                    q = new QuotedFieldQueryNode(field, EscapeQuerySyntax.DiscardEscapeChar(term.Image.Substring(1, (term.Image.Length - 1) - 1)), term.BeginColumn + 1, term.EndColumn - 1);
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.FUZZY_SLOP:
                            fuzzySlop = Jj_consume_token(RegexpToken.FUZZY_SLOP);
                            break;
                        default:
                            jj_la1[25] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            Jj_consume_token(RegexpToken.CARAT);
                            boost = Jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[26] = jj_gen;
                            break;
                    }
                    int phraseSlop = 0;

                    if (fuzzySlop != null)
                    {
                        // LUCENENET specific: parse without throwing exceptions
#if FEATURE_NUMBER_PARSE_READONLYSPAN
                        if (float.TryParse(fuzzySlop.Image.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float temp))
#else
                        if (float.TryParse(fuzzySlop.Image.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float temp))
#endif
                        {
                            try
                            {
                                phraseSlop = (int)temp;
                                q = new SlopQueryNode(q, phraseSlop);
                            }
                            catch (Exception ignored) when (ignored.IsException())
                            {
                                /* Should this be handled somehow? (defaults to "no PhraseSlop", if
                               * slop number is invalid)
                               */
                            }
                        }
                    }
                    break;
                default:
                    jj_la1[27] = jj_gen;
                    Jj_consume_token(-1);
                    throw new ParseException();
            }
            if (boost != null)
            {
                // LUCENENET specific: parse without throwing exceptions
                float f = float.TryParse(boost.Image, NumberStyles.Float, CultureInfo.InvariantCulture, out float temp) ? temp : 1.0f;
                try
                {
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception ignored) when (ignored.IsException())
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                       * boost number is invalid)
                       */
                }
            }
            { if (true) return q; }
            throw Error.Create("Missing return statement in function");
        }

        private bool Jj_2_1(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = Token;
            try { return !Jj_3_1(); }
#pragma warning disable 168
            catch (LookaheadSuccess ls) { return true; }
#pragma warning restore 168
            finally { Jj_save(0, xla); }
        }

        private bool Jj_2_2(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = Token;
            try { return !Jj_3_2(); }
#pragma warning disable 168
            catch (LookaheadSuccess ls) { return true; }
#pragma warning restore 168
            finally { Jj_save(1, xla); }
        }

        private bool Jj_3_2()
        {
            if (Jj_scan_token(RegexpToken.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_3R_4())
            {
                jj_scanpos = xsp;
                if (Jj_3R_5()) return true;
            }
            return false;
        }

        private bool Jj_3R_12()
        {
            if (Jj_scan_token(RegexpToken.RANGEIN_START)) return true;
            return false;
        }

        private bool Jj_3R_11()
        {
            if (Jj_scan_token(RegexpToken.REGEXPTERM)) return true;
            return false;
        }

        private bool Jj_3_1()
        {
            if (Jj_scan_token(RegexpToken.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (Jj_scan_token(16)) return true;
            }
            return false;
        }

        private bool Jj_3R_8()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_3R_12())
            {
                jj_scanpos = xsp;
                if (Jj_scan_token(27)) return true;
            }
            return false;
        }

        private bool Jj_3R_10()
        {
            if (Jj_scan_token(RegexpToken.TERM)) return true;
            return false;
        }

        private bool Jj_3R_7()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_3R_10())
            {
                jj_scanpos = xsp;
                if (Jj_3R_11())
                {
                    jj_scanpos = xsp;
                    if (Jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool Jj_3R_9()
        {
            if (Jj_scan_token(RegexpToken.QUOTED)) return true;
            return false;
        }

        private bool Jj_3R_5()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_scan_token(17))
            {
                jj_scanpos = xsp;
                if (Jj_scan_token(18))
                {
                    jj_scanpos = xsp;
                    if (Jj_scan_token(19))
                    {
                        jj_scanpos = xsp;
                        if (Jj_scan_token(20)) return true;
                    }
                }
            }
            xsp = jj_scanpos;
            if (Jj_scan_token(23))
            {
                jj_scanpos = xsp;
                if (Jj_scan_token(22))
                {
                    jj_scanpos = xsp;
                    if (Jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool Jj_3R_4()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (Jj_scan_token(16)) return true;
            }
            if (Jj_3R_6()) return true;
            return false;
        }

        private bool Jj_3R_6()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (Jj_3R_7())
            {
                jj_scanpos = xsp;
                if (Jj_3R_8())
                {
                    jj_scanpos = xsp;
                    if (Jj_3R_9()) return true;
                }
            }
            return false;
        }

        /// <summary>Generated Token Manager.</summary>
        public StandardSyntaxParserTokenManager TokenSource { get; set; }
        /// <summary>Current token.</summary>
        public Token Token { get; set; }
        /// <summary>Next token.</summary>
        public Token Jj_nt { get; set; }
        private int jj_ntk;
        private Token jj_scanpos, jj_lastpos;
        private int jj_la;
        private int jj_gen;
        readonly private int[] jj_la1 = new int[28];
        static private uint[] jj_la1_0;
        static private int[] jj_la1_1;
        static StandardSyntaxParser()
        {
            Jj_la1_init_0();
            Jj_la1_init_1();
        }
        private static void Jj_la1_init_0()
        {
            jj_la1_0 = new uint[] { 0x300, 0x300, 0x1c00, 0x1c00, 0x1ec03c00, 0x200, 0x100, 0x18000, 0x1e0000, 0x10c00000, 0x1f8000, 0x18000, 0x200000, 0x1ec02000, 0x1ec02000, 0x12800000, 0x1000000, 0x1000000, 0x200000, 0xc000000, 0x0, 0x20000000, 0x0, 0xc0000000, 0x200000, 0x1000000, 0x200000, 0x1ec00000, };
        }
        private static void Jj_la1_init_1()
        {
            jj_la1_1 = new int[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3, 0x0, 0x3, 0x0, 0x0, 0x0, 0x0, 0x0, };
        }
        readonly private JJCalls[] jj_2_rtns = new JJCalls[2];
        private bool jj_rescan = false;
        private int jj_gc = 0;

        /// <summary>
        /// Constructor with user supplied <see cref="ICharStream"/>.
        /// </summary>
        public StandardSyntaxParser(ICharStream stream)
        {
            TokenSource = new StandardSyntaxParserTokenManager(stream);
            Token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /// <summary>Reinitialize.</summary>
        public void ReInit(ICharStream stream)
        {
            TokenSource.ReInit(stream);
            Token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /// <summary>Constructor with generated Token Manager.</summary>
        public StandardSyntaxParser(StandardSyntaxParserTokenManager tm)
        {
            TokenSource = tm;
            Token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /// <summary>Reinitialize.</summary>
        public void ReInit(StandardSyntaxParserTokenManager tm)
        {
            TokenSource = tm;
            Token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        private Token Jj_consume_token(int kind)
        {
            Token oldToken;
            if ((oldToken = Token).Next != null) Token = Token.Next;
            else Token = Token.Next = TokenSource.GetNextToken();
            jj_ntk = -1;
            if (Token.Kind == kind)
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
                return Token;
            }
            Token = oldToken;
            jj_kind = kind;
            throw GenerateParseException();
        }

        // LUCENENET: It is no longer good practice to use binary serialization.
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        internal sealed class LookaheadSuccess : Exception
        {
            public LookaheadSuccess()
            { }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            public LookaheadSuccess(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }
        readonly private LookaheadSuccess jj_ls = new LookaheadSuccess();
        private bool Jj_scan_token(int kind)
        {
            if (jj_scanpos == jj_lastpos)
            {
                jj_la--;
                if (jj_scanpos.Next is null)
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.Next = TokenSource.GetNextToken();
                }
                else
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.Next;
                }
            }
            else
            {
                jj_scanpos = jj_scanpos.Next;
            }
            if (jj_rescan)
            {
                int i = 0; Token tok = Token;
                while (tok != null && tok != jj_scanpos) { i++; tok = tok.Next; }
                if (tok != null) Jj_add_error_token(kind, i);
            }
            if (jj_scanpos.Kind != kind) return true;
            if (jj_la == 0 && jj_scanpos == jj_lastpos) throw jj_ls;
            return false;
        }


        /// <summary>Get the next Token.</summary>
        public Token GetNextToken()
        {
            if (Token.Next != null) Token = Token.Next;
            else Token = Token.Next = TokenSource.GetNextToken();
            jj_ntk = -1;
            jj_gen++;
            return Token;
        }

        /// <summary>Get the specific Token.</summary>
        public Token GetToken(int index)
        {
            Token t = Token;
            for (int i = 0; i < index; i++)
            {
                if (t.Next != null) t = t.Next;
                else t = t.Next = TokenSource.GetNextToken();
            }
            return t;
        }

        private int Jj_ntk()
        {
            if ((Jj_nt = Token.Next) is null)
                return (jj_ntk = (Token.Next = TokenSource.GetNextToken()).Kind);
            else
                return (jj_ntk = Jj_nt.Kind);
        }

        private readonly IList<int[]> jj_expentries = new JCG.List<int[]>(); // LUCENENET: marked readonly
        private int[] jj_expentry;
        private int jj_kind = -1;
        private readonly int[] jj_lasttokens = new int[100]; // LUCENENET: marked readonly
        private int jj_endpos;

        private void Jj_add_error_token(int kind, int pos)
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
                using (var it = jj_expentries.GetEnumerator())
                {
                    while (it.MoveNext())
                    {
                        int[] oldentry = (int[])(it.Current);
                        if (oldentry.Length == jj_expentry.Length)
                        {
                            for (int i = 0; i < jj_expentry.Length; i++)
                            {
                                if (oldentry[i] != jj_expentry[i])
                                {
                                    goto jj_entries_loop_continue;
                                }
                            }
                            jj_expentries.Add(jj_expentry);
                            goto jj_entries_loop_break;
                        }
                        jj_entries_loop_continue: { }
                    }
                    jj_entries_loop_break:
                        if (pos != 0) jj_lasttokens[(jj_endpos = pos) - 1] = kind;
                }
            }
        }

        /// <summary>Generate ParseException.</summary>
        public virtual ParseException GenerateParseException()
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
            Jj_rescan_token();
            Jj_add_error_token(0, 0);
            int[][] exptokseq = new int[jj_expentries.Count][];
            for (int i = 0; i < jj_expentries.Count; i++)
            {
                exptokseq[i] = jj_expentries[i];
            }
            return new ParseException(Token, exptokseq, StandardSyntaxParserConstants.TokenImage);
        }


        /// <summary>Enable tracing.</summary>
        public void Enable_tracing()
        {
            // LUCENENET: Intentionally blank
        }

        /// <summary>Disable tracing.</summary>
        public void Disable_tracing()
        {
            // LUCENENET: Intentionally blank
        }

        private void Jj_rescan_token()
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
                                case 0: Jj_3_1(); break;
                                case 1: Jj_3_2(); break;
                            }
                        }
                        p = p.next;
                    } while (p != null);
                }
#pragma warning disable 168
                catch (LookaheadSuccess ls) { }
#pragma warning restore 168
            }
            jj_rescan = false;
        }

        private void Jj_save(int index, int xla)
        {
            JJCalls p = jj_2_rtns[index];
            while (p.gen > jj_gen)
            {
                if (p.next is null) { p = p.next = new JJCalls(); break; }
                p = p.next;
            }
            p.gen = jj_gen + xla - jj_la; p.first = Token; p.arg = xla;
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
