using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Simple
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

    // LUCENENET specific - converted constants from SimpleQueryParser
    // into a flags enum.
    [Flags]
    public enum Operator
    {
        /// <summary>Enables <c>AND</c> operator (+)</summary>
        AND_OPERATOR = 1 << 0,
        /// <summary>Enables <c>NOT</c> operator (-)</summary>
        NOT_OPERATOR = 1 << 1,
        /// <summary>Enables <c>OR</c> operator (|)</summary>
        OR_OPERATOR = 1 << 2,
        /// <summary>Enables <c>PREFIX</c> operator (*)</summary>
        PREFIX_OPERATOR = 1 << 3,
        /// <summary>Enables <c>PHRASE</c> operator (")</summary>
        PHRASE_OPERATOR = 1 << 4,
        /// <summary>Enables <c>PRECEDENCE</c> operators: <c>(</c> and <c>)</c></summary>
        PRECEDENCE_OPERATORS = 1 << 5,
        /// <summary>Enables <c>ESCAPE</c> operator (\)</summary>
        ESCAPE_OPERATOR = 1 << 6,
        /// <summary>Enables <c>WHITESPACE</c> operators: ' ' '\n' '\r' '\t'</summary>
        WHITESPACE_OPERATOR = 1 << 7,
        /// <summary>Enables <c>FUZZY</c> operators: (~) on single terms</summary>
        FUZZY_OPERATOR = 1 << 8,
        /// <summary>Enables <c>NEAR</c> operators: (~) on phrases</summary>
        NEAR_OPERATOR = 1 << 9
    }

    /// <summary>
    /// <see cref="SimpleQueryParser"/> is used to parse human readable query syntax.
    /// <para/>
    /// The main idea behind this parser is that a person should be able to type
    /// whatever they want to represent a query, and this parser will do its best
    /// to interpret what to search for no matter how poorly composed the request
    /// may be. Tokens are considered to be any of a term, phrase, or subquery for the
    /// operations described below.  Whitespace including ' ' '\n' '\r' and '\t'
    /// and certain operators may be used to delimit tokens ( ) + | " .
    /// <para/>
    /// Any errors in query syntax will be ignored and the parser will attempt
    /// to decipher what it can; however, this may mean odd or unexpected results.
    /// <h4>Query Operators</h4>
    /// <list type="bullet">
    ///  <item><description>'<c>+</c>' specifies <c>AND</c> operation: <c>token1+token2</c></description></item>
    ///  <item><description>'<c>|</c>' specifies <c>OR</c> operation: <c>token1|token2</c></description></item>
    ///  <item><description>'<c>-</c>' negates a single token: <c>-token0</c></description></item>
    ///  <item><description>'<c>"</c>' creates phrases of terms: <c>"term1 term2 ..."</c></description></item>
    ///  <item><description>'<c>*</c>' at the end of terms specifies prefix query: <c>term*</c></description></item>
    ///  <item><description>'<c>~</c>N' at the end of terms specifies fuzzy query: <c>term~1</c></description></item>
    ///  <item><description>'<c>~</c>N' at the end of phrases specifies near query: <c>"term1 term2"~5</c></description></item>
    ///  <item><description>'<c>(</c>' and '<c>)</c>' specifies precedence: <c>token1 + (token2 | token3)</c></description></item>
    /// </list>
    /// <para/>
    /// The default operator is <c>OR</c> if no other operator is specified.
    /// For example, the following will <c>OR</c> <c>token1</c> and <c>token2</c> together:
    /// <c>token1 token2</c>
    /// <para/>
    /// Normal operator precedence will be simple order from right to left.
    /// For example, the following will evaluate <c>token1 OR token2</c> first,
    /// then <c>AND</c> with <c>token3</c>:
    /// <code>token1 | token2 + token3</code>
    /// <h4>Escaping</h4>
    /// <para/>
    /// An individual term may contain any possible character with certain characters
    /// requiring escaping using a '<c>\</c>'.  The following characters will need to be escaped in
    /// terms and phrases:
    /// <c>+ | " ( ) ' \</c>
    /// <para/>
    /// The '<c>-</c>' operator is a special case.  On individual terms (not phrases) the first
    /// character of a term that is <c>-</c> must be escaped; however, any '<c>-</c>' characters
    /// beyond the first character do not need to be escaped.
    /// For example:
    /// <list type="bullet">
    ///   <item><description><c>-term1</c>   -- Specifies <c>NOT</c> operation against <c>term1</c></description></item>
    ///   <item><description><c>\-term1</c>  -- Searches for the term <c>-term1</c>.</description></item>
    ///   <item><description><c>term-1</c>   -- Searches for the term <c>term-1</c>.</description></item>
    ///   <item><description><c>term\-1</c>  -- Searches for the term <c>term-1</c>.</description></item>
    /// </list>
    /// <para/>
    /// The '<c>*</c>' operator is a special case. On individual terms (not phrases) the last
    /// character of a term that is '<c>*</c>' must be escaped; however, any '<c>*</c>' characters
    /// before the last character do not need to be escaped:
    /// <list type="bullet">
    ///   <item><description><c>term1*</c>  --  Searches for the prefix <c>term1</c></description></item>
    ///   <item><description><c>term1\*</c> --  Searches for the term <c>term1*</c></description></item>
    ///   <item><description><c>term*1</c>  --  Searches for the term <c>term*1</c></description></item>
    ///   <item><description><c>term\*1</c> --  Searches for the term <c>term*1</c></description></item>
    /// </list>
    /// <para/>
    /// Note that above examples consider the terms before text processing.
    /// </summary>
    public class SimpleQueryParser : QueryBuilder
    {
        /// <summary>Map of fields to query against with their weights</summary>
        protected readonly IDictionary<string, float> m_weights;

        // LUCENENET specific - made flags into their own [Flags] enum named Operator and de-nested from this type

        /// <summary>flags to the parser (to turn features on/off)</summary>
        protected readonly Operator m_flags;

        private Occur defaultOperator = Occur.SHOULD;

        /// <summary>Creates a new parser searching over a single field.</summary>
        public SimpleQueryParser(Analyzer analyzer, string field)
            : this(analyzer, new JCG.Dictionary<string, float>() { { field, 1.0F } })
        {
        }

        /// <summary>Creates a new parser searching over multiple fields with different weights.</summary>
        public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights)
            : this(analyzer, weights, (Operator)(-1))
        {
        }

        /// <summary>Creates a new parser with custom flags used to enable/disable certain features.</summary>
        public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights, Operator flags)
            : base(analyzer)
        {
            this.m_weights = weights;
            this.m_flags = flags;
        }

        /// <summary>Parses the query text and returns parsed query (or null if empty)</summary>
        public Query Parse(string queryText)
        {
            char[] data = queryText.ToCharArray();
            char[] buffer = new char[data.Length];

            State state = new State(data, buffer, 0, data.Length);
            ParseSubQuery(state);
            return state.Top;
        }

        private void ParseSubQuery(State state)
        {
            while (state.Index < state.Length)
            {
                if (state.Data[state.Index] == '(' && (m_flags & Operator.PRECEDENCE_OPERATORS) != 0)
                {
                    // the beginning of a subquery has been found
                    ConsumeSubQuery(state);
                }
                else if (state.Data[state.Index] == ')' && (m_flags & Operator.PRECEDENCE_OPERATORS) != 0)
                {
                    // this is an extraneous character so it is ignored
                    ++state.Index;
                }
                else if (state.Data[state.Index] == '"' && (m_flags & Operator.PHRASE_OPERATOR) != 0)
                {
                    // the beginning of a phrase has been found
                    ConsumePhrase(state);
                }
                else if (state.Data[state.Index] == '+' && (m_flags & Operator.AND_OPERATOR) != 0)
                {
                    // an and operation has been explicitly set
                    // if an operation has already been set this one is ignored
                    // if a term (or phrase or subquery) has not been found yet the
                    // operation is also ignored since there is no previous
                    // term (or phrase or subquery) to and with
                    if (!state.CurrentOperationIsSet && state.Top != null)
                    {
                        state.CurrentOperation = Occur.MUST;
                    }

                    ++state.Index;
                }
                else if (state.Data[state.Index] == '|' && (m_flags & Operator.OR_OPERATOR) != 0)
                {
                    // an or operation has been explicitly set
                    // if an operation has already been set this one is ignored
                    // if a term (or phrase or subquery) has not been found yet the
                    // operation is also ignored since there is no previous
                    // term (or phrase or subquery) to or with
                    if (!state.CurrentOperationIsSet && state.Top != null)
                    {
                        state.CurrentOperation = Occur.SHOULD;
                    }

                    ++state.Index;
                }
                else if (state.Data[state.Index] == '-' && (m_flags & Operator.NOT_OPERATOR) != 0)
                {
                    // a not operator has been found, so increase the not count
                    // two not operators in a row negate each other
                    ++state.Not;
                    ++state.Index;

                    // continue so the not operator is not reset
                    // before the next character is determined
                    continue;
                }
                else if ((state.Data[state.Index] == ' '
                  || state.Data[state.Index] == '\t'
                  || state.Data[state.Index] == '\n'
                  || state.Data[state.Index] == '\r') && (m_flags & Operator.WHITESPACE_OPERATOR) != 0)
                {
                    // ignore any whitespace found as it may have already been
                    // used a delimiter across a term (or phrase or subquery)
                    // or is simply extraneous
                    ++state.Index;
                }
                else
                {
                    // the beginning of a token has been found
                    ConsumeToken(state);
                }

                // reset the not operator as even whitespace is not allowed when
                // specifying the not operation for a term (or phrase or subquery)
                state.Not = 0;
            }
        }

        private void ConsumeSubQuery(State state)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert((m_flags & Operator.PRECEDENCE_OPERATORS) != 0);
            int start = ++state.Index;
            int precedence = 1;
            bool escaped = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (m_flags & Operator.ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the subquery unless the escape
                        // character is the last one in the data
                        escaped = true;
                        ++state.Index;

                        continue;
                    }
                    else if (state.Data[state.Index] == '(')
                    {
                        // increase the precedence as there is a
                        // subquery in the current subquery
                        ++precedence;
                    }
                    else if (state.Data[state.Index] == ')')
                    {
                        --precedence;

                        if (precedence == 0)
                        {
                            // this should be the end of the subquery
                            // all characters found will used for
                            // creating the subquery
                            break;
                        }
                    }
                }

                escaped = false;
                ++state.Index;
            }

            if (state.Index == state.Length)
            {
                // a closing parenthesis was never found so the opening
                // parenthesis is considered extraneous and will be ignored
                state.Index = start;
            }
            else if (state.Index == start)
            {
                // a closing parenthesis was found immediately after the opening
                // parenthesis so the current operation is reset since it would
                // have been applied to this subquery
                state.CurrentOperationIsSet = false;

                ++state.Index;
            }
            else
            {
                // a complete subquery has been found and is recursively parsed by
                // starting over with a new state object
                State subState = new State(state.Data, state.Buffer, start, state.Index);
                ParseSubQuery(subState);
                BuildQueryTree(state, subState.Top);

                ++state.Index;
            }
        }

        private void ConsumePhrase(State state)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert((m_flags & Operator.PHRASE_OPERATOR) != 0);
            int start = ++state.Index;
            int copied = 0;
            bool escaped = false;
            bool hasSlop = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (m_flags & Operator.ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the phrase unless the escape
                        // character is the last one in the data
                        escaped = true;
                        ++state.Index;

                        continue;
                    }
                    else if (state.Data[state.Index] == '"')
                    {
                        // if there are still characters after the closing ", check for a
                        // tilde
                        if (state.Length > (state.Index + 1) &&
                            state.Data[state.Index + 1] == '~' &&
                            (m_flags & Operator.NEAR_OPERATOR) != 0)
                        {
                            state.Index++;
                            // check for characters after the tilde
                            if (state.Length > (state.Index + 1))
                            {
                                hasSlop = true;
                            }
                            break;
                        }
                        else
                        {
                            // this should be the end of the phrase
                            // all characters found will used for
                            // creating the phrase query
                            break;
                        }
                    }
                }

                escaped = false;
                state.Buffer[copied++] = state.Data[state.Index++];
            }

            if (state.Index == state.Length)
            {
                // a closing double quote was never found so the opening
                // double quote is considered extraneous and will be ignored
                state.Index = start;
            }
            else if (state.Index == start)
            {
                // a closing double quote was found immediately after the opening
                // double quote so the current operation is reset since it would
                // have been applied to this phrase
                state.CurrentOperationIsSet = false;

                ++state.Index;
            }
            else
            {
                // a complete phrase has been found and is parsed through
                // through the analyzer from the given field
                string phrase = new string(state.Buffer, 0, copied);
                Query branch;
                if (hasSlop)
                {
                    branch = NewPhraseQuery(phrase, ParseFuzziness(state));
                }
                else
                {
                    branch = NewPhraseQuery(phrase, 0);
                }
                BuildQueryTree(state, branch);

                ++state.Index;
            }
        }

        private void ConsumeToken(State state)
        {
            int copied = 0;
            bool escaped = false;
            bool prefix = false;
            bool fuzzy = false;

            while (state.Index < state.Length)
            {
                if (!escaped)
                {
                    if (state.Data[state.Index] == '\\' && (m_flags & Operator.ESCAPE_OPERATOR) != 0)
                    {
                        // an escape character has been found so
                        // whatever character is next will become
                        // part of the term unless the escape
                        // character is the last one in the data
                        escaped = true;
                        prefix = false;
                        ++state.Index;

                        continue;
                    }
                    else if (TokenFinished(state))
                    {
                        // this should be the end of the term
                        // all characters found will used for
                        // creating the term query
                        break;
                    }
                    else if (copied > 0 && state.Data[state.Index] == '~' && (m_flags & Operator.FUZZY_OPERATOR) != 0)
                    {
                        fuzzy = true;
                        break;
                    }

                    // wildcard tracks whether or not the last character
                    // was a '*' operator that hasn't been escaped
                    // there must be at least one valid character before
                    // searching for a prefixed set of terms
                    prefix = copied > 0 && state.Data[state.Index] == '*' && (m_flags & Operator.PREFIX_OPERATOR) != 0;
                }

                escaped = false;
                state.Buffer[copied++] = state.Data[state.Index++];
            }

            if (copied > 0)
            {
                Query branch;

                if (fuzzy && (m_flags & Operator.FUZZY_OPERATOR) != 0)
                {
                    string token = new string(state.Buffer, 0, copied);
                    int fuzziness = ParseFuzziness(state);
                    // edit distance has a maximum, limit to the maximum supported
                    fuzziness = Math.Min(fuzziness, LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
                    if (fuzziness == 0)
                    {
                        branch = NewDefaultQuery(token);
                    }
                    else
                    {
                        branch = NewFuzzyQuery(token, fuzziness);
                    }
                }
                else if (prefix)
                {
                    // if a term is found with a closing '*' it is considered to be a prefix query
                    // and will have prefix added as an option
                    string token = new string(state.Buffer, 0, copied - 1);
                    branch = NewPrefixQuery(token);
                }
                else
                {
                    // a standard term has been found so it will be run through
                    // the entire analysis chain from the specified schema field
                    string token = new string(state.Buffer, 0, copied);
                    branch = NewDefaultQuery(token);
                }

                BuildQueryTree(state, branch);
            }
        }

        /// <summary>
        /// buildQueryTree should be called after a term, phrase, or subquery
        /// is consumed to be added to our existing query tree
        /// this method will only add to the existing tree if the branch contained in state is not null
        /// </summary>
        private void BuildQueryTree(State state, Query branch)
        {
            if (branch != null)
            {
                // modify our branch to a BooleanQuery wrapper for not
                // this is necessary any time a term, phrase, or subquery is negated
                if (state.Not % 2 == 1)
                {
                    BooleanQuery nq = new BooleanQuery
                    {
                        { branch, Occur.MUST_NOT },
                        { new MatchAllDocsQuery(), Occur.SHOULD }
                    };
                    branch = nq;
                }

                // first term (or phrase or subquery) found and will begin our query tree
                if (state.Top is null)
                {
                    state.Top = branch;
                }
                else
                {
                    // more than one term (or phrase or subquery) found
                    // set currentOperation to the default if no other operation is explicitly set
                    if (!state.CurrentOperationIsSet)
                    {
                        state.CurrentOperation = defaultOperator;
                    }

                    // operational change requiring a new parent node
                    // this occurs if the previous operation is not the same as current operation
                    // because the previous operation must be evaluated separately to preserve
                    // the proper precedence and the current operation will take over as the top of the tree
                    if (!state.PreviousOperationIsSet || state.PreviousOperation != state.CurrentOperation)
                    {
                        BooleanQuery bq = new BooleanQuery
                        {
                            { state.Top, state.CurrentOperation }
                        };
                        state.Top = bq;
                    }

                    // reset all of the state for reuse
                    ((BooleanQuery)state.Top).Add(branch, state.CurrentOperation);
                    state.PreviousOperation = state.CurrentOperation;
                }

                // reset the current operation as it was intended to be applied to
                // the incoming term (or phrase or subquery) even if branch was null
                // due to other possible errors
                state.CurrentOperationIsSet = false;
            }
        }

        /// <summary>
        /// Helper parsing fuzziness from parsing state
        /// </summary>
        /// <returns>slop/edit distance, 0 in the case of non-parsing slop/edit string</returns>
        private int ParseFuzziness(State state)
        {
            char[] slopText = new char[state.Length];
            int slopLength = 0;

            if (state.Data[state.Index] == '~')
            {
                while (state.Index < state.Length)
                {
                    state.Index++;
                    // it's possible that the ~ was at the end, so check after incrementing
                    // to make sure we don't go out of bounds
                    if (state.Index < state.Length)
                    {
                        if (TokenFinished(state))
                        {
                            break;
                        }
                        slopText[slopLength] = state.Data[state.Index];
                        slopLength++;
                    }
                }
                int.TryParse(new string(slopText, 0, slopLength), out int fuzziness); // LUCENENET TODO: Find a way to pass culture
                // negative -> 0
                if (fuzziness < 0)
                {
                    fuzziness = 0;
                }
                return fuzziness;
            }
            return 0;
        }

        /// <summary>
        /// Helper returning true if the state has reached the end of token.
        /// </summary>
        private bool TokenFinished(State state)
        {
            if ((state.Data[state.Index] == '"' && (m_flags & Operator.PHRASE_OPERATOR) != 0)
                || (state.Data[state.Index] == '|' && (m_flags & Operator.OR_OPERATOR) != 0)
                || (state.Data[state.Index] == '+' && (m_flags & Operator.AND_OPERATOR) != 0)
                || (state.Data[state.Index] == '(' && (m_flags & Operator.PRECEDENCE_OPERATORS) != 0)
                || (state.Data[state.Index] == ')' && (m_flags & Operator.PRECEDENCE_OPERATORS) != 0)
                || ((state.Data[state.Index] == ' '
                || state.Data[state.Index] == '\t'
                || state.Data[state.Index] == '\n'
                || state.Data[state.Index] == '\r') && (m_flags & Operator.WHITESPACE_OPERATOR) != 0))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Factory method to generate a standard query (no phrase or prefix operators).
        /// </summary>
        protected virtual Query NewDefaultQuery(string text)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in m_weights)
            {
                Query q = CreateBooleanQuery(entry.Key, text, defaultOperator);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a fuzzy query.
        /// </summary>
        protected virtual Query NewFuzzyQuery(string text, int fuzziness)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in m_weights)
            {
                Query q = new FuzzyQuery(new Term(entry.Key, text), fuzziness);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a phrase query with slop.
        /// </summary>
        protected virtual Query NewPhraseQuery(string text, int slop)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in m_weights)
            {
                Query q = CreatePhraseQuery(entry.Key, text, slop);
                if (q != null)
                {
                    q.Boost = entry.Value;
                    bq.Add(q, Occur.SHOULD);
                }
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Factory method to generate a prefix query.
        /// </summary>
        protected virtual Query NewPrefixQuery(string text)
        {
            BooleanQuery bq = new BooleanQuery(true);
            foreach (var entry in m_weights)
            {
                PrefixQuery prefix = new PrefixQuery(new Term(entry.Key, text));
                prefix.Boost = entry.Value;
                bq.Add(prefix, Occur.SHOULD);
            }
            return Simplify(bq);
        }

        /// <summary>
        /// Helper to simplify boolean queries with 0 or 1 clause
        /// </summary>
        protected virtual Query Simplify(BooleanQuery bq)
        {
            if (bq.Clauses.Count == 0)
            {
                return null;
            }
            else if (bq.Clauses.Count == 1)
            {
                return bq.Clauses[0].Query;
            }
            else
            {
                return bq;
            }
        }

        /// <summary>
        /// Gets or Sets the implicit operator setting, which will be
        /// either <see cref="Occur.SHOULD"/> or <see cref="Occur.MUST"/>.
        /// </summary>
        public virtual Occur DefaultOperator
        {
            get => defaultOperator;
            set 
            {
                if (value != Occur.SHOULD && value != Occur.MUST)
                {
                    throw new ArgumentException("invalid operator: only SHOULD or MUST are allowed");
                }
                defaultOperator = value; 
            }
        }


        internal class State
        {
            //private readonly char[] data;   // the characters in the query string
            //private readonly char[] buffer; // a temporary buffer used to reduce necessary allocations
            //private int index;
            //private int length;

            private Occur currentOperation;
            private Occur previousOperation;
            //private int not;

            //private Query top;

            internal State(char[] data, char[] buffer, int index, int length)
            {
                this.Data = data;
                this.Buffer = buffer;
                this.Index = index;
                this.Length = length;
            }

            internal char[] Data { get; private set; } // the characters in the query string
            internal char[] Buffer { get; private set; } // a temporary buffer used to reduce necessary allocations
            internal int Index { get; set; }
            internal int Length { get; set; }

            internal Occur CurrentOperation 
            {
                get => currentOperation;
                set
                {
                    currentOperation = value;
                    CurrentOperationIsSet = true;
                }
            }

            internal Occur PreviousOperation
            {
                get => previousOperation;
                set
                {
                    previousOperation = value;
                    PreviousOperationIsSet = true;
                }
            }

            internal bool CurrentOperationIsSet { get; set; }
            internal bool PreviousOperationIsSet { get; set; }

            internal int Not { get; set; }
            internal Query Top { get; set; }
        }
    }
}
