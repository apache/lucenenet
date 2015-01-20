/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Simple;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Automaton;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Simple
{
	/// <summary>SimpleQueryParser is used to parse human readable query syntax.</summary>
	/// <remarks>
	/// SimpleQueryParser is used to parse human readable query syntax.
	/// <p>
	/// The main idea behind this parser is that a person should be able to type
	/// whatever they want to represent a query, and this parser will do its best
	/// to interpret what to search for no matter how poorly composed the request
	/// may be. Tokens are considered to be any of a term, phrase, or subquery for the
	/// operations described below.  Whitespace including ' ' '\n' '\r' and '\t'
	/// and certain operators may be used to delimit tokens ( ) + | " .
	/// <p>
	/// Any errors in query syntax will be ignored and the parser will attempt
	/// to decipher what it can; however, this may mean odd or unexpected results.
	/// <h4>Query Operators</h4>
	/// <ul>
	/// <li>'
	/// <code>+</code>
	/// ' specifies
	/// <code>AND</code>
	/// operation: <tt>token1+token2</tt>
	/// <li>'
	/// <code>|</code>
	/// ' specifies
	/// <code>OR</code>
	/// operation: <tt>token1|token2</tt>
	/// <li>'
	/// <code>-</code>
	/// ' negates a single token: <tt>-token0</tt>
	/// <li>'
	/// <code>"</code>
	/// ' creates phrases of terms: <tt>"term1 term2 ..."</tt>
	/// <li>'
	/// <code>*</code>
	/// ' at the end of terms specifies prefix query: <tt>term*</tt>
	/// <li>'
	/// <code>~</code>
	/// N' at the end of terms specifies fuzzy query: <tt>term~1</tt>
	/// <li>'
	/// <code>~</code>
	/// N' at the end of phrases specifies near query: <tt>"term1 term2"~5</tt>
	/// <li>'
	/// <code>(</code>
	/// ' and '
	/// <code>)</code>
	/// ' specifies precedence: <tt>token1 + (token2 | token3)</tt>
	/// </ul>
	/// <p>
	/// The
	/// <see cref="SetDefaultOperator(Org.Apache.Lucene.Search.BooleanClause.Occur)">default operator
	/// 	</see>
	/// is
	/// <code>OR</code>
	/// if no other operator is specified.
	/// For example, the following will
	/// <code>OR</code>
	/// 
	/// <code>token1</code>
	/// and
	/// <code>token2</code>
	/// together:
	/// <tt>token1 token2</tt>
	/// <p>
	/// Normal operator precedence will be simple order from right to left.
	/// For example, the following will evaluate
	/// <code>token1 OR token2</code>
	/// first,
	/// then
	/// <code>AND</code>
	/// with
	/// <code>token3</code>
	/// :
	/// <blockquote>token1 | token2 + token3</blockquote>
	/// <h4>Escaping</h4>
	/// <p>
	/// An individual term may contain any possible character with certain characters
	/// requiring escaping using a '
	/// <code>\</code>
	/// '.  The following characters will need to be escaped in
	/// terms and phrases:
	/// <code>+ | " ( ) ' \</code>
	/// <p>
	/// The '
	/// <code>-</code>
	/// ' operator is a special case.  On individual terms (not phrases) the first
	/// character of a term that is
	/// <code>-</code>
	/// must be escaped; however, any '
	/// <code>-</code>
	/// ' characters
	/// beyond the first character do not need to be escaped.
	/// For example:
	/// <ul>
	/// <li>
	/// <code>-term1</code>
	/// -- Specifies
	/// <code>NOT</code>
	/// operation against
	/// <code>term1</code>
	/// <li>
	/// <code>\-term1</code>
	/// -- Searches for the term
	/// <code>-term1</code>
	/// .
	/// <li>
	/// <code>term-1</code>
	/// -- Searches for the term
	/// <code>term-1</code>
	/// .
	/// <li>
	/// <code>term\-1</code>
	/// -- Searches for the term
	/// <code>term-1</code>
	/// .
	/// </ul>
	/// <p>
	/// The '
	/// <code>*</code>
	/// ' operator is a special case. On individual terms (not phrases) the last
	/// character of a term that is '
	/// <code>*</code>
	/// ' must be escaped; however, any '
	/// <code>*</code>
	/// ' characters
	/// before the last character do not need to be escaped:
	/// <ul>
	/// <li>
	/// <code>term1*</code>
	/// --  Searches for the prefix
	/// <code>term1</code>
	/// <li>
	/// <code>term1\*</code>
	/// --  Searches for the term
	/// <code>term1*</code>
	/// <li>
	/// <code>term*1</code>
	/// --  Searches for the term
	/// <code>term*1</code>
	/// <li>
	/// <code>term\*1</code>
	/// --  Searches for the term
	/// <code>term*1</code>
	/// </ul>
	/// <p>
	/// Note that above examples consider the terms before text processing.
	/// </remarks>
	public class SimpleQueryParser : QueryBuilder
	{
		/// <summary>Map of fields to query against with their weights</summary>
		protected internal readonly IDictionary<string, float> weights;

		/// <summary>flags to the parser (to turn features on/off)</summary>
		protected internal readonly int flags;

		/// <summary>
		/// Enables
		/// <code>AND</code>
		/// operator (+)
		/// </summary>
		public const int AND_OPERATOR = 1 << 0;

		/// <summary>
		/// Enables
		/// <code>NOT</code>
		/// operator (-)
		/// </summary>
		public const int NOT_OPERATOR = 1 << 1;

		/// <summary>
		/// Enables
		/// <code>OR</code>
		/// operator (|)
		/// </summary>
		public const int OR_OPERATOR = 1 << 2;

		/// <summary>
		/// Enables
		/// <code>PREFIX</code>
		/// operator (*)
		/// </summary>
		public const int PREFIX_OPERATOR = 1 << 3;

		/// <summary>
		/// Enables
		/// <code>PHRASE</code>
		/// operator (")
		/// </summary>
		public const int PHRASE_OPERATOR = 1 << 4;

		/// <summary>
		/// Enables
		/// <code>PRECEDENCE</code>
		/// operators:
		/// <code>(</code>
		/// and
		/// <code>)</code>
		/// 
		/// </summary>
		public const int PRECEDENCE_OPERATORS = 1 << 5;

		/// <summary>
		/// Enables
		/// <code>ESCAPE</code>
		/// operator (\)
		/// </summary>
		public const int ESCAPE_OPERATOR = 1 << 6;

		/// <summary>
		/// Enables
		/// <code>WHITESPACE</code>
		/// operators: ' ' '\n' '\r' '\t'
		/// </summary>
		public const int WHITESPACE_OPERATOR = 1 << 7;

		/// <summary>
		/// Enables
		/// <code>FUZZY</code>
		/// operators: (~) on single terms
		/// </summary>
		public const int FUZZY_OPERATOR = 1 << 8;

		/// <summary>
		/// Enables
		/// <code>NEAR</code>
		/// operators: (~) on phrases
		/// </summary>
		public const int NEAR_OPERATOR = 1 << 9;

		private BooleanClause.Occur defaultOperator = BooleanClause.Occur.SHOULD;

		/// <summary>Creates a new parser searching over a single field.</summary>
		/// <remarks>Creates a new parser searching over a single field.</remarks>
		public SimpleQueryParser(Analyzer analyzer, string field) : this(analyzer, Sharpen.Collections
			.SingletonMap(field, 1.0F))
		{
		}

		/// <summary>Creates a new parser searching over multiple fields with different weights.
		/// 	</summary>
		/// <remarks>Creates a new parser searching over multiple fields with different weights.
		/// 	</remarks>
		public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights) : 
			this(analyzer, weights, -1)
		{
		}

		/// <summary>Creates a new parser with custom flags used to enable/disable certain features.
		/// 	</summary>
		/// <remarks>Creates a new parser with custom flags used to enable/disable certain features.
		/// 	</remarks>
		public SimpleQueryParser(Analyzer analyzer, IDictionary<string, float> weights, int
			 flags) : base(analyzer)
		{
			this.weights = weights;
			this.flags = flags;
		}

		/// <summary>Parses the query text and returns parsed query (or null if empty)</summary>
		public virtual Query Parse(string queryText)
		{
			char[] data = queryText.ToCharArray();
			char[] buffer = new char[data.Length];
			SimpleQueryParser.State state = new SimpleQueryParser.State(data, buffer, 0, data
				.Length);
			ParseSubQuery(state);
			return state.top;
		}

		private void ParseSubQuery(SimpleQueryParser.State state)
		{
			while (state.index < state.length)
			{
				if (state.data[state.index] == '(' && (flags & PRECEDENCE_OPERATORS) != 0)
				{
					// the beginning of a subquery has been found
					ConsumeSubQuery(state);
				}
				else
				{
					if (state.data[state.index] == ')' && (flags & PRECEDENCE_OPERATORS) != 0)
					{
						// this is an extraneous character so it is ignored
						++state.index;
					}
					else
					{
						if (state.data[state.index] == '"' && (flags & PHRASE_OPERATOR) != 0)
						{
							// the beginning of a phrase has been found
							ConsumePhrase(state);
						}
						else
						{
							if (state.data[state.index] == '+' && (flags & AND_OPERATOR) != 0)
							{
								// an and operation has been explicitly set
								// if an operation has already been set this one is ignored
								// if a term (or phrase or subquery) has not been found yet the
								// operation is also ignored since there is no previous
								// term (or phrase or subquery) to and with
								if (state.currentOperation == null && state.top != null)
								{
									state.currentOperation = BooleanClause.Occur.MUST;
								}
								++state.index;
							}
							else
							{
								if (state.data[state.index] == '|' && (flags & OR_OPERATOR) != 0)
								{
									// an or operation has been explicitly set
									// if an operation has already been set this one is ignored
									// if a term (or phrase or subquery) has not been found yet the
									// operation is also ignored since there is no previous
									// term (or phrase or subquery) to or with
									if (state.currentOperation == null && state.top != null)
									{
										state.currentOperation = BooleanClause.Occur.SHOULD;
									}
									++state.index;
								}
								else
								{
									if (state.data[state.index] == '-' && (flags & NOT_OPERATOR) != 0)
									{
										// a not operator has been found, so increase the not count
										// two not operators in a row negate each other
										++state.not;
										++state.index;
										// continue so the not operator is not reset
										// before the next character is determined
										continue;
									}
									else
									{
										if ((state.data[state.index] == ' ' || state.data[state.index] == '\t' || state.data
											[state.index] == '\n' || state.data[state.index] == '\r') && (flags & WHITESPACE_OPERATOR
											) != 0)
										{
											// ignore any whitespace found as it may have already been
											// used a delimiter across a term (or phrase or subquery)
											// or is simply extraneous
											++state.index;
										}
										else
										{
											// the beginning of a token has been found
											ConsumeToken(state);
										}
									}
								}
							}
						}
					}
				}
				// reset the not operator as even whitespace is not allowed when
				// specifying the not operation for a term (or phrase or subquery)
				state.not = 0;
			}
		}

		private void ConsumeSubQuery(SimpleQueryParser.State state)
		{
			int start = ++(flags & PRECEDENCE_OPERATORS) != 0.index;
			int precedence = 1;
			bool escaped = false;
			while (state.index < state.length)
			{
				if (!escaped)
				{
					if (state.data[state.index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
					{
						// an escape character has been found so
						// whatever character is next will become
						// part of the subquery unless the escape
						// character is the last one in the data
						escaped = true;
						++state.index;
						continue;
					}
					else
					{
						if (state.data[state.index] == '(')
						{
							// increase the precedence as there is a
							// subquery in the current subquery
							++precedence;
						}
						else
						{
							if (state.data[state.index] == ')')
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
					}
				}
				escaped = false;
				++state.index;
			}
			if (state.index == state.length)
			{
				// a closing parenthesis was never found so the opening
				// parenthesis is considered extraneous and will be ignored
				state.index = start;
			}
			else
			{
				if (state.index == start)
				{
					// a closing parenthesis was found immediately after the opening
					// parenthesis so the current operation is reset since it would
					// have been applied to this subquery
					state.currentOperation = null;
					++state.index;
				}
				else
				{
					// a complete subquery has been found and is recursively parsed by
					// starting over with a new state object
					SimpleQueryParser.State subState = new SimpleQueryParser.State(state.data, state.
						buffer, start, state.index);
					ParseSubQuery(subState);
					BuildQueryTree(state, subState.top);
					++state.index;
				}
			}
		}

		private void ConsumePhrase(SimpleQueryParser.State state)
		{
			int start = ++(flags & PHRASE_OPERATOR) != 0.index;
			int copied = 0;
			bool escaped = false;
			bool hasSlop = false;
			while (state.index < state.length)
			{
				if (!escaped)
				{
					if (state.data[state.index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
					{
						// an escape character has been found so
						// whatever character is next will become
						// part of the phrase unless the escape
						// character is the last one in the data
						escaped = true;
						++state.index;
						continue;
					}
					else
					{
						if (state.data[state.index] == '"')
						{
							// if there are still characters after the closing ", check for a
							// tilde
							if (state.length > (state.index + 1) && state.data[state.index + 1] == '~' && (flags
								 & NEAR_OPERATOR) != 0)
							{
								state.index++;
								// check for characters after the tilde
								if (state.length > (state.index + 1))
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
				}
				escaped = false;
				state.buffer[copied++] = state.data[state.index++];
			}
			if (state.index == state.length)
			{
				// a closing double quote was never found so the opening
				// double quote is considered extraneous and will be ignored
				state.index = start;
			}
			else
			{
				if (state.index == start)
				{
					// a closing double quote was found immediately after the opening
					// double quote so the current operation is reset since it would
					// have been applied to this phrase
					state.currentOperation = null;
					++state.index;
				}
				else
				{
					// a complete phrase has been found and is parsed through
					// through the analyzer from the given field
					string phrase = new string(state.buffer, 0, copied);
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
					++state.index;
				}
			}
		}

		private void ConsumeToken(SimpleQueryParser.State state)
		{
			int copied = 0;
			bool escaped = false;
			bool prefix = false;
			bool fuzzy = false;
			while (state.index < state.length)
			{
				if (!escaped)
				{
					if (state.data[state.index] == '\\' && (flags & ESCAPE_OPERATOR) != 0)
					{
						// an escape character has been found so
						// whatever character is next will become
						// part of the term unless the escape
						// character is the last one in the data
						escaped = true;
						prefix = false;
						++state.index;
						continue;
					}
					else
					{
						if (TokenFinished(state))
						{
							// this should be the end of the term
							// all characters found will used for
							// creating the term query
							break;
						}
						else
						{
							if (copied > 0 && state.data[state.index] == '~' && (flags & FUZZY_OPERATOR) != 0)
							{
								fuzzy = true;
								break;
							}
						}
					}
					// wildcard tracks whether or not the last character
					// was a '*' operator that hasn't been escaped
					// there must be at least one valid character before
					// searching for a prefixed set of terms
					prefix = copied > 0 && state.data[state.index] == '*' && (flags & PREFIX_OPERATOR
						) != 0;
				}
				escaped = false;
				state.buffer[copied++] = state.data[state.index++];
			}
			if (copied > 0)
			{
				Query branch;
				if (fuzzy && (flags & FUZZY_OPERATOR) != 0)
				{
					string token = new string(state.buffer, 0, copied);
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
				else
				{
					if (prefix)
					{
						// if a term is found with a closing '*' it is considered to be a prefix query
						// and will have prefix added as an option
						string token = new string(state.buffer, 0, copied - 1);
						branch = NewPrefixQuery(token);
					}
					else
					{
						// a standard term has been found so it will be run through
						// the entire analysis chain from the specified schema field
						string token = new string(state.buffer, 0, copied);
						branch = NewDefaultQuery(token);
					}
				}
				BuildQueryTree(state, branch);
			}
		}

		// buildQueryTree should be called after a term, phrase, or subquery
		// is consumed to be added to our existing query tree
		// this method will only add to the existing tree if the branch contained in state is not null
		private void BuildQueryTree(SimpleQueryParser.State state, Query branch)
		{
			if (branch != null)
			{
				// modify our branch to a BooleanQuery wrapper for not
				// this is necessary any time a term, phrase, or subquery is negated
				if (state.not % 2 == 1)
				{
					BooleanQuery nq = new BooleanQuery();
					nq.Add(branch, BooleanClause.Occur.MUST_NOT);
					nq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
					branch = nq;
				}
				// first term (or phrase or subquery) found and will begin our query tree
				if (state.top == null)
				{
					state.top = branch;
				}
				else
				{
					// more than one term (or phrase or subquery) found
					// set currentOperation to the default if no other operation is explicitly set
					if (state.currentOperation == null)
					{
						state.currentOperation = defaultOperator;
					}
					// operational change requiring a new parent node
					// this occurs if the previous operation is not the same as current operation
					// because the previous operation must be evaluated separately to preserve
					// the proper precedence and the current operation will take over as the top of the tree
					if (state.previousOperation != state.currentOperation)
					{
						BooleanQuery bq = new BooleanQuery();
						bq.Add(state.top, state.currentOperation);
						state.top = bq;
					}
					// reset all of the state for reuse
					((BooleanQuery)state.top).Add(branch, state.currentOperation);
					state.previousOperation = state.currentOperation;
				}
				// reset the current operation as it was intended to be applied to
				// the incoming term (or phrase or subquery) even if branch was null
				// due to other possible errors
				state.currentOperation = null;
			}
		}

		/// <summary>Helper parsing fuzziness from parsing state</summary>
		/// <returns>slop/edit distance, 0 in the case of non-parsing slop/edit string</returns>
		private int ParseFuzziness(SimpleQueryParser.State state)
		{
			char[] slopText = new char[state.length];
			int slopLength = 0;
			if (state.data[state.index] == '~')
			{
				while (state.index < state.length)
				{
					state.index++;
					// it's possible that the ~ was at the end, so check after incrementing
					// to make sure we don't go out of bounds
					if (state.index < state.length)
					{
						if (TokenFinished(state))
						{
							break;
						}
						slopText[slopLength] = state.data[state.index];
						slopLength++;
					}
				}
				int fuzziness = 0;
				try
				{
					fuzziness = System.Convert.ToInt32(new string(slopText, 0, slopLength));
				}
				catch (FormatException)
				{
				}
				// swallow number format exceptions parsing fuzziness
				// negative -> 0
				if (fuzziness < 0)
				{
					fuzziness = 0;
				}
				return fuzziness;
			}
			return 0;
		}

		/// <summary>Helper returning true if the state has reached the end of token.</summary>
		/// <remarks>Helper returning true if the state has reached the end of token.</remarks>
		private bool TokenFinished(SimpleQueryParser.State state)
		{
			if ((state.data[state.index] == '"' && (flags & PHRASE_OPERATOR) != 0) || (state.
				data[state.index] == '|' && (flags & OR_OPERATOR) != 0) || (state.data[state.index
				] == '+' && (flags & AND_OPERATOR) != 0) || (state.data[state.index] == '(' && (
				flags & PRECEDENCE_OPERATORS) != 0) || (state.data[state.index] == ')' && (flags
				 & PRECEDENCE_OPERATORS) != 0) || ((state.data[state.index] == ' ' || state.data
				[state.index] == '\t' || state.data[state.index] == '\n' || state.data[state.index
				] == '\r') && (flags & WHITESPACE_OPERATOR) != 0))
			{
				return true;
			}
			return false;
		}

		/// <summary>Factory method to generate a standard query (no phrase or prefix operators).
		/// 	</summary>
		/// <remarks>Factory method to generate a standard query (no phrase or prefix operators).
		/// 	</remarks>
		protected internal virtual Query NewDefaultQuery(string text)
		{
			BooleanQuery bq = new BooleanQuery(true);
			foreach (KeyValuePair<string, float> entry in weights.EntrySet())
			{
				Query q = CreateBooleanQuery(entry.Key, text, defaultOperator);
				if (q != null)
				{
					q.SetBoost(entry.Value);
					bq.Add(q, BooleanClause.Occur.SHOULD);
				}
			}
			return Simplify(bq);
		}

		/// <summary>Factory method to generate a fuzzy query.</summary>
		/// <remarks>Factory method to generate a fuzzy query.</remarks>
		protected internal virtual Query NewFuzzyQuery(string text, int fuzziness)
		{
			BooleanQuery bq = new BooleanQuery(true);
			foreach (KeyValuePair<string, float> entry in weights.EntrySet())
			{
				Query q = new FuzzyQuery(new Term(entry.Key, text), fuzziness);
				if (q != null)
				{
					q.SetBoost(entry.Value);
					bq.Add(q, BooleanClause.Occur.SHOULD);
				}
			}
			return Simplify(bq);
		}

		/// <summary>Factory method to generate a phrase query with slop.</summary>
		/// <remarks>Factory method to generate a phrase query with slop.</remarks>
		protected internal virtual Query NewPhraseQuery(string text, int slop)
		{
			BooleanQuery bq = new BooleanQuery(true);
			foreach (KeyValuePair<string, float> entry in weights.EntrySet())
			{
				Query q = CreatePhraseQuery(entry.Key, text, slop);
				if (q != null)
				{
					q.SetBoost(entry.Value);
					bq.Add(q, BooleanClause.Occur.SHOULD);
				}
			}
			return Simplify(bq);
		}

		/// <summary>Factory method to generate a prefix query.</summary>
		/// <remarks>Factory method to generate a prefix query.</remarks>
		protected internal virtual Query NewPrefixQuery(string text)
		{
			BooleanQuery bq = new BooleanQuery(true);
			foreach (KeyValuePair<string, float> entry in weights.EntrySet())
			{
				PrefixQuery prefix = new PrefixQuery(new Term(entry.Key, text));
				prefix.SetBoost(entry.Value);
				bq.Add(prefix, BooleanClause.Occur.SHOULD);
			}
			return Simplify(bq);
		}

		/// <summary>Helper to simplify boolean queries with 0 or 1 clause</summary>
		protected internal virtual Query Simplify(BooleanQuery bq)
		{
			if (bq.Clauses().IsEmpty())
			{
				return null;
			}
			else
			{
				if (bq.Clauses().Count == 1)
				{
					return bq.Clauses()[0].GetQuery();
				}
				else
				{
					return bq;
				}
			}
		}

		/// <summary>
		/// Returns the implicit operator setting, which will be
		/// either
		/// <code>SHOULD</code>
		/// or
		/// <code>MUST</code>
		/// .
		/// </summary>
		public virtual BooleanClause.Occur GetDefaultOperator()
		{
			return defaultOperator;
		}

		/// <summary>
		/// Sets the implicit operator setting, which must be
		/// either
		/// <code>SHOULD</code>
		/// or
		/// <code>MUST</code>
		/// .
		/// </summary>
		public virtual void SetDefaultOperator(BooleanClause.Occur @operator)
		{
			if (@operator != BooleanClause.Occur.SHOULD && @operator != BooleanClause.Occur.MUST)
			{
				throw new ArgumentException("invalid operator: only SHOULD or MUST are allowed");
			}
			this.defaultOperator = @operator;
		}

		internal class State
		{
			internal readonly char[] data;

			internal readonly char[] buffer;

			internal int index;

			internal int length;

			internal BooleanClause.Occur currentOperation;

			internal BooleanClause.Occur previousOperation;

			internal int not;

			internal Query top;

			internal State(char[] data, char[] buffer, int index, int length)
			{
				// the characters in the query string
				// a temporary buffer used to reduce necessary allocations
				this.data = data;
				this.buffer = buffer;
				this.index = index;
				this.length = length;
			}
		}
	}
}
