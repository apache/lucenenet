/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Lucene.Net.Queryparser.Flexible.Messages;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Parser;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Parser
{
	/// <summary>Parser for the standard Lucene syntax</summary>
	public class StandardSyntaxParser : SyntaxParser, StandardSyntaxParserConstants
	{
		private const int CONJ_NONE = 0;

		private const int CONJ_AND = 2;

		private const int CONJ_OR = 2;

		public StandardSyntaxParser() : this(new FastCharStream(new StringReader(string.Empty
			)))
		{
		}

		// syntax parser constructor
		/// <summary>
		/// Parses a query string, returning a
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="query">the query string to be parsed.</param>
		/// <exception cref="ParseException">if the parsing fails</exception>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeParseException
		/// 	"></exception>
		public virtual QueryNode Parse(CharSequence query, CharSequence field)
		{
			ReInit(new FastCharStream(new StringReader(query.ToString())));
			try
			{
				// TopLevelQuery is a Query followed by the end-of-input (EOF)
				QueryNode querynode = TopLevelQuery(field);
				return querynode;
			}
			catch (ParseException tme)
			{
				tme.SetQuery(query);
				throw;
			}
			catch (Error tme)
			{
				Message message = new MessageImpl(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE
					, query, tme.Message);
				QueryNodeParseException e = new QueryNodeParseException(tme);
				e.SetQuery(query);
				e.SetNonLocalizedMessage(message);
				throw e;
			}
		}

		// *   Query  ::= ( Clause )*
		// *   Clause ::= ["+", "-"] [<TERM> ":"] ( <TERM> | "(" Query ")" )
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public int Conjunction()
		{
			int ret = CONJ_NONE;
			switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
			{
				case AND:
				case OR:
				{
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case AND:
						{
							Jj_consume_token(AND);
							ret = CONJ_AND;
							break;
						}

						case OR:
						{
							Jj_consume_token(OR);
							ret = CONJ_OR;
							break;
						}

						default:
						{
							jj_la1[0] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					break;
				}

				default:
				{
					jj_la1[1] = jj_gen;
					break;
				}
			}
			{
				if (true)
				{
					return ret;
				}
			}
			throw new Error("Missing return statement in function");
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public ModifierQueryNode.Modifier Modifiers()
		{
			ModifierQueryNode.Modifier ret = ModifierQueryNode.Modifier.MOD_NONE;
			switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
			{
				case NOT:
				case PLUS:
				case MINUS:
				{
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case PLUS:
						{
							Jj_consume_token(PLUS);
							ret = ModifierQueryNode.Modifier.MOD_REQ;
							break;
						}

						case MINUS:
						{
							Jj_consume_token(MINUS);
							ret = ModifierQueryNode.Modifier.MOD_NOT;
							break;
						}

						case NOT:
						{
							Jj_consume_token(NOT);
							ret = ModifierQueryNode.Modifier.MOD_NOT;
							break;
						}

						default:
						{
							jj_la1[2] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					break;
				}

				default:
				{
					jj_la1[3] = jj_gen;
					break;
				}
			}
			{
				if (true)
				{
					return ret;
				}
			}
			throw new Error("Missing return statement in function");
		}

		// This makes sure that there is no garbage after the query string
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode TopLevelQuery(CharSequence field)
		{
			QueryNode q;
			q = Query(field);
			Jj_consume_token(0);
			{
				if (true)
				{
					return q;
				}
			}
			throw new Error("Missing return statement in function");
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
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode Query(CharSequence field)
		{
			Vector<QueryNode> clauses = null;
			QueryNode c;
			QueryNode first = null;
			first = DisjQuery(field);
			while (true)
			{
				switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
				{
					case NOT:
					case PLUS:
					case MINUS:
					case LPAREN:
					case QUOTED:
					case TERM:
					case REGEXPTERM:
					case RANGEIN_START:
					case RANGEEX_START:
					case NUMBER:
					{
						break;
					}

					default:
					{
						jj_la1[4] = jj_gen;
						goto label_1_break;
						break;
					}
				}
				c = DisjQuery(field);
				if (clauses == null)
				{
					clauses = new Vector<QueryNode>();
					clauses.Add(first);
				}
				clauses.Add(c);
label_1_continue: ;
			}
label_1_break: ;
			if (clauses != null)
			{
				{
					if (true)
					{
						return new BooleanQueryNode(clauses);
					}
				}
			}
			else
			{
				{
					if (true)
					{
						return first;
					}
				}
			}
			throw new Error("Missing return statement in function");
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode DisjQuery(CharSequence field)
		{
			QueryNode first;
			QueryNode c;
			Vector<QueryNode> clauses = null;
			first = ConjQuery(field);
			while (true)
			{
				switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
				{
					case OR:
					{
						break;
					}

					default:
					{
						jj_la1[5] = jj_gen;
						goto label_2_break;
						break;
					}
				}
				Jj_consume_token(OR);
				c = ConjQuery(field);
				if (clauses == null)
				{
					clauses = new Vector<QueryNode>();
					clauses.Add(first);
				}
				clauses.Add(c);
label_2_continue: ;
			}
label_2_break: ;
			if (clauses != null)
			{
				{
					if (true)
					{
						return new OrQueryNode(clauses);
					}
				}
			}
			else
			{
				{
					if (true)
					{
						return first;
					}
				}
			}
			throw new Error("Missing return statement in function");
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode ConjQuery(CharSequence field)
		{
			QueryNode first;
			QueryNode c;
			Vector<QueryNode> clauses = null;
			first = ModClause(field);
			while (true)
			{
				switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
				{
					case AND:
					{
						break;
					}

					default:
					{
						jj_la1[6] = jj_gen;
						goto label_3_break;
						break;
					}
				}
				Jj_consume_token(AND);
				c = ModClause(field);
				if (clauses == null)
				{
					clauses = new Vector<QueryNode>();
					clauses.Add(first);
				}
				clauses.Add(c);
label_3_continue: ;
			}
label_3_break: ;
			if (clauses != null)
			{
				{
					if (true)
					{
						return new AndQueryNode(clauses);
					}
				}
			}
			else
			{
				{
					if (true)
					{
						return first;
					}
				}
			}
			throw new Error("Missing return statement in function");
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
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode ModClause(CharSequence field)
		{
			QueryNode q;
			ModifierQueryNode.Modifier mods;
			mods = Modifiers();
			q = Clause(field);
			if (mods != ModifierQueryNode.Modifier.MOD_NONE)
			{
				q = new ModifierQueryNode(q, mods);
			}
			{
				if (true)
				{
					return q;
				}
			}
			throw new Error("Missing return statement in function");
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode Clause(CharSequence field)
		{
			QueryNode q;
			Token fieldToken = null;
			Token boost = null;
			Token @operator = null;
			Token term = null;
			FieldQueryNode qLower;
			FieldQueryNode qUpper;
			bool lowerInclusive;
			bool upperInclusive;
			bool group = false;
			if (Jj_2_2(3))
			{
				fieldToken = Jj_consume_token(TERM);
				switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
				{
					case OP_COLON:
					case OP_EQUAL:
					{
						switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
						{
							case OP_COLON:
							{
								Jj_consume_token(OP_COLON);
								break;
							}

							case OP_EQUAL:
							{
								Jj_consume_token(OP_EQUAL);
								break;
							}

							default:
							{
								jj_la1[7] = jj_gen;
								Jj_consume_token(-1);
								throw new ParseException();
							}
						}
						field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
						q = Term(field);
						break;
					}

					case OP_LESSTHAN:
					case OP_LESSTHANEQ:
					case OP_MORETHAN:
					case OP_MORETHANEQ:
					{
						switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
						{
							case OP_LESSTHAN:
							{
								@operator = Jj_consume_token(OP_LESSTHAN);
								break;
							}

							case OP_LESSTHANEQ:
							{
								@operator = Jj_consume_token(OP_LESSTHANEQ);
								break;
							}

							case OP_MORETHAN:
							{
								@operator = Jj_consume_token(OP_MORETHAN);
								break;
							}

							case OP_MORETHANEQ:
							{
								@operator = Jj_consume_token(OP_MORETHANEQ);
								break;
							}

							default:
							{
								jj_la1[8] = jj_gen;
								Jj_consume_token(-1);
								throw new ParseException();
							}
						}
						field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
						switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
						{
							case TERM:
							{
								term = Jj_consume_token(TERM);
								break;
							}

							case QUOTED:
							{
								term = Jj_consume_token(QUOTED);
								break;
							}

							case NUMBER:
							{
								term = Jj_consume_token(NUMBER);
								break;
							}

							default:
							{
								jj_la1[9] = jj_gen;
								Jj_consume_token(-1);
								throw new ParseException();
							}
						}
						if (term.kind == QUOTED)
						{
							term.image = Sharpen.Runtime.Substring(term.image, 1, term.image.Length - 1);
						}
						switch (@operator.kind)
						{
							case OP_LESSTHAN:
							{
								lowerInclusive = true;
								upperInclusive = false;
								qLower = new FieldQueryNode(field, "*", term.beginColumn, term.endColumn);
								qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image
									), term.beginColumn, term.endColumn);
								break;
							}

							case OP_LESSTHANEQ:
							{
								lowerInclusive = true;
								upperInclusive = true;
								qLower = new FieldQueryNode(field, "*", term.beginColumn, term.endColumn);
								qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image
									), term.beginColumn, term.endColumn);
								break;
							}

							case OP_MORETHAN:
							{
								lowerInclusive = false;
								upperInclusive = true;
								qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image
									), term.beginColumn, term.endColumn);
								qUpper = new FieldQueryNode(field, "*", term.beginColumn, term.endColumn);
								break;
							}

							case OP_MORETHANEQ:
							{
								lowerInclusive = true;
								upperInclusive = true;
								qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image
									), term.beginColumn, term.endColumn);
								qUpper = new FieldQueryNode(field, "*", term.beginColumn, term.endColumn);
								break;
							}

							default:
							{
								if (true)
								{
									throw new Error("Unhandled case: operator=" + @operator.ToString());
								}
								break;
							}
						}
						q = new TermRangeQueryNode(qLower, qUpper, lowerInclusive, upperInclusive);
						break;
					}

					default:
					{
						jj_la1[10] = jj_gen;
						Jj_consume_token(-1);
						throw new ParseException();
					}
				}
			}
			else
			{
				switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
				{
					case LPAREN:
					case QUOTED:
					case TERM:
					case REGEXPTERM:
					case RANGEIN_START:
					case RANGEEX_START:
					case NUMBER:
					{
						if (Jj_2_1(2))
						{
							fieldToken = Jj_consume_token(TERM);
							switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
							{
								case OP_COLON:
								{
									Jj_consume_token(OP_COLON);
									break;
								}

								case OP_EQUAL:
								{
									Jj_consume_token(OP_EQUAL);
									break;
								}

								default:
								{
									jj_la1[11] = jj_gen;
									Jj_consume_token(-1);
									throw new ParseException();
								}
							}
							field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image);
						}
						switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
						{
							case QUOTED:
							case TERM:
							case REGEXPTERM:
							case RANGEIN_START:
							case RANGEEX_START:
							case NUMBER:
							{
								q = Term(field);
								break;
							}

							case LPAREN:
							{
								Jj_consume_token(LPAREN);
								q = Query(field);
								Jj_consume_token(RPAREN);
								switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
								{
									case CARAT:
									{
										Jj_consume_token(CARAT);
										boost = Jj_consume_token(NUMBER);
										break;
									}

									default:
									{
										jj_la1[12] = jj_gen;
										break;
									}
								}
								group = true;
								break;
							}

							default:
							{
								jj_la1[13] = jj_gen;
								Jj_consume_token(-1);
								throw new ParseException();
							}
						}
						break;
					}

					default:
					{
						jj_la1[14] = jj_gen;
						Jj_consume_token(-1);
						throw new ParseException();
					}
				}
			}
			if (boost != null)
			{
				float f = (float)1.0;
				try
				{
					f = float.ValueOf(boost.image);
					// avoid boosting null queries, such as those caused by stop words
					if (q != null)
					{
						q = new BoostQueryNode(q, f);
					}
				}
				catch (Exception)
				{
				}
			}
			if (group)
			{
				q = new GroupQueryNode(q);
			}
			{
				if (true)
				{
					return q;
				}
			}
			throw new Error("Missing return statement in function");
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public QueryNode Term(CharSequence field)
		{
			Token term;
			Token boost = null;
			Token fuzzySlop = null;
			Token goop1;
			Token goop2;
			bool fuzzy = false;
			bool regexp = false;
			bool startInc = false;
			bool endInc = false;
			QueryNode q = null;
			FieldQueryNode qLower;
			FieldQueryNode qUpper;
			float defaultMinSimilarity = FuzzyQuery.defaultMinSimilarity;
			switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
			{
				case TERM:
				case REGEXPTERM:
				case NUMBER:
				{
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case TERM:
						{
							term = Jj_consume_token(TERM);
							q = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image)
								, term.beginColumn, term.endColumn);
							break;
						}

						case REGEXPTERM:
						{
							term = Jj_consume_token(REGEXPTERM);
							regexp = true;
							break;
						}

						case NUMBER:
						{
							term = Jj_consume_token(NUMBER);
							break;
						}

						default:
						{
							jj_la1[15] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case FUZZY_SLOP:
						{
							fuzzySlop = Jj_consume_token(FUZZY_SLOP);
							fuzzy = true;
							break;
						}

						default:
						{
							jj_la1[16] = jj_gen;
							break;
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case CARAT:
						{
							Jj_consume_token(CARAT);
							boost = Jj_consume_token(NUMBER);
							switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
							{
								case FUZZY_SLOP:
								{
									fuzzySlop = Jj_consume_token(FUZZY_SLOP);
									fuzzy = true;
									break;
								}

								default:
								{
									jj_la1[17] = jj_gen;
									break;
								}
							}
							break;
						}

						default:
						{
							jj_la1[18] = jj_gen;
							break;
						}
					}
					if (fuzzy)
					{
						float fms = defaultMinSimilarity;
						try
						{
							fms = float.ValueOf(Sharpen.Runtime.Substring(fuzzySlop.image, 1));
						}
						catch (Exception)
						{
						}
						if (fms < 0.0f)
						{
							{
								if (true)
								{
									throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS
										));
								}
							}
						}
						else
						{
							if (fms >= 1.0f && fms != (int)fms)
							{
								{
									if (true)
									{
										throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_FUZZY_EDITS
											));
									}
								}
							}
						}
						q = new FuzzyQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image)
							, fms, term.beginColumn, term.endColumn);
					}
					else
					{
						if (regexp)
						{
							string re = Sharpen.Runtime.Substring(term.image, 1, term.image.Length - 1);
							q = new RegexpQueryNode(field, re, 0, re.Length);
						}
					}
					break;
				}

				case RANGEIN_START:
				case RANGEEX_START:
				{
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case RANGEIN_START:
						{
							Jj_consume_token(RANGEIN_START);
							startInc = true;
							break;
						}

						case RANGEEX_START:
						{
							Jj_consume_token(RANGEEX_START);
							break;
						}

						default:
						{
							jj_la1[19] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case RANGE_GOOP:
						{
							goop1 = Jj_consume_token(RANGE_GOOP);
							break;
						}

						case RANGE_QUOTED:
						{
							goop1 = Jj_consume_token(RANGE_QUOTED);
							break;
						}

						default:
						{
							jj_la1[20] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case RANGE_TO:
						{
							Jj_consume_token(RANGE_TO);
							break;
						}

						default:
						{
							jj_la1[21] = jj_gen;
							break;
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case RANGE_GOOP:
						{
							goop2 = Jj_consume_token(RANGE_GOOP);
							break;
						}

						case RANGE_QUOTED:
						{
							goop2 = Jj_consume_token(RANGE_QUOTED);
							break;
						}

						default:
						{
							jj_la1[22] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case RANGEIN_END:
						{
							Jj_consume_token(RANGEIN_END);
							endInc = true;
							break;
						}

						case RANGEEX_END:
						{
							Jj_consume_token(RANGEEX_END);
							break;
						}

						default:
						{
							jj_la1[23] = jj_gen;
							Jj_consume_token(-1);
							throw new ParseException();
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case CARAT:
						{
							Jj_consume_token(CARAT);
							boost = Jj_consume_token(NUMBER);
							break;
						}

						default:
						{
							jj_la1[24] = jj_gen;
							break;
						}
					}
					if (goop1.kind == RANGE_QUOTED)
					{
						goop1.image = Sharpen.Runtime.Substring(goop1.image, 1, goop1.image.Length - 1);
					}
					if (goop2.kind == RANGE_QUOTED)
					{
						goop2.image = Sharpen.Runtime.Substring(goop2.image, 1, goop2.image.Length - 1);
					}
					qLower = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(goop1.
						image), goop1.beginColumn, goop1.endColumn);
					qUpper = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(goop2.
						image), goop2.beginColumn, goop2.endColumn);
					q = new TermRangeQueryNode(qLower, qUpper, startInc ? true : false, endInc ? true
						 : false);
					break;
				}

				case QUOTED:
				{
					term = Jj_consume_token(QUOTED);
					q = new QuotedFieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(Sharpen.Runtime.Substring
						(term.image, 1, term.image.Length - 1)), term.beginColumn + 1, term.endColumn - 
						1);
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case FUZZY_SLOP:
						{
							fuzzySlop = Jj_consume_token(FUZZY_SLOP);
							break;
						}

						default:
						{
							jj_la1[25] = jj_gen;
							break;
						}
					}
					switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
					{
						case CARAT:
						{
							Jj_consume_token(CARAT);
							boost = Jj_consume_token(NUMBER);
							break;
						}

						default:
						{
							jj_la1[26] = jj_gen;
							break;
						}
					}
					int phraseSlop = 0;
					if (fuzzySlop != null)
					{
						try
						{
							phraseSlop = float.ValueOf(Sharpen.Runtime.Substring(fuzzySlop.image, 1));
							q = new SlopQueryNode(q, phraseSlop);
						}
						catch (Exception)
						{
						}
					}
					break;
				}

				default:
				{
					jj_la1[27] = jj_gen;
					Jj_consume_token(-1);
					throw new ParseException();
				}
			}
			if (boost != null)
			{
				float f = (float)1.0;
				try
				{
					f = float.ValueOf(boost.image);
					// avoid boosting null queries, such as those caused by stop words
					if (q != null)
					{
						q = new BoostQueryNode(q, f);
					}
				}
				catch (Exception)
				{
				}
			}
			{
				if (true)
				{
					return q;
				}
			}
			throw new Error("Missing return statement in function");
		}

		private bool Jj_2_1(int xla)
		{
			jj_la = xla;
			jj_lastpos = jj_scanpos = token;
			try
			{
				return !Jj_3_1();
			}
			catch (StandardSyntaxParser.LookaheadSuccess)
			{
				return true;
			}
			finally
			{
				Jj_save(0, xla);
			}
		}

		private bool Jj_2_2(int xla)
		{
			jj_la = xla;
			jj_lastpos = jj_scanpos = token;
			try
			{
				return !Jj_3_2();
			}
			catch (StandardSyntaxParser.LookaheadSuccess)
			{
				return true;
			}
			finally
			{
				Jj_save(1, xla);
			}
		}

		private bool Jj_3_2()
		{
			if (Jj_scan_token(TERM))
			{
				return true;
			}
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_3R_4())
			{
				jj_scanpos = xsp;
				if (Jj_3R_5())
				{
					return true;
				}
			}
			return false;
		}

		private bool Jj_3R_12()
		{
			if (Jj_scan_token(RANGEIN_START))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3R_11()
		{
			if (Jj_scan_token(REGEXPTERM))
			{
				return true;
			}
			return false;
		}

		private bool Jj_3_1()
		{
			if (Jj_scan_token(TERM))
			{
				return true;
			}
			Token xsp;
			xsp = jj_scanpos;
			if (Jj_scan_token(15))
			{
				jj_scanpos = xsp;
				if (Jj_scan_token(16))
				{
					return true;
				}
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
				if (Jj_scan_token(27))
				{
					return true;
				}
			}
			return false;
		}

		private bool Jj_3R_10()
		{
			if (Jj_scan_token(TERM))
			{
				return true;
			}
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
					if (Jj_scan_token(28))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool Jj_3R_9()
		{
			if (Jj_scan_token(QUOTED))
			{
				return true;
			}
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
						if (Jj_scan_token(20))
						{
							return true;
						}
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
					if (Jj_scan_token(28))
					{
						return true;
					}
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
				if (Jj_scan_token(16))
				{
					return true;
				}
			}
			if (Jj_3R_6())
			{
				return true;
			}
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
					if (Jj_3R_9())
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Generated Token Manager.</summary>
		/// <remarks>Generated Token Manager.</remarks>
		public StandardSyntaxParserTokenManager token_source;

		/// <summary>Current token.</summary>
		/// <remarks>Current token.</remarks>
		public Token token;

		/// <summary>Next token.</summary>
		/// <remarks>Next token.</remarks>
		public Token jj_nt;

		private int jj_ntk;

		private Token jj_scanpos;

		private Token jj_lastpos;

		private int jj_la;

		private int jj_gen;

		private readonly int[] jj_la1 = new int[28];

		private static int[] jj_la1_0;

		private static int[] jj_la1_1;

		static StandardSyntaxParser()
		{
			Jj_la1_init_0();
			Jj_la1_init_1();
		}

		private static void Jj_la1_init_0()
		{
			jj_la1_0 = new int[] { unchecked((int)(0x300)), unchecked((int)(0x300)), unchecked(
				(int)(0x1c00)), unchecked((int)(0x1c00)), unchecked((int)(0x1ec03c00)), unchecked(
				(int)(0x200)), unchecked((int)(0x100)), unchecked((int)(0x18000)), unchecked((int
				)(0x1e0000)), unchecked((int)(0x10c00000)), unchecked((int)(0x1f8000)), unchecked(
				(int)(0x18000)), unchecked((int)(0x200000)), unchecked((int)(0x1ec02000)), unchecked(
				(int)(0x1ec02000)), unchecked((int)(0x12800000)), unchecked((int)(0x1000000)), unchecked(
				(int)(0x1000000)), unchecked((int)(0x200000)), unchecked((int)(0xc000000)), unchecked(
				(int)(0x0)), unchecked((int)(0x20000000)), unchecked((int)(0x0)), unchecked((int
				)(0xc0000000)), unchecked((int)(0x200000)), unchecked((int)(0x1000000)), unchecked(
				(int)(0x200000)), unchecked((int)(0x1ec00000)) };
		}

		private static void Jj_la1_init_1()
		{
			jj_la1_1 = new int[] { unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int
				)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked(
				(int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0))
				, unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked(
				(int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0))
				, unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked(
				(int)(0x3)), unchecked((int)(0x0)), unchecked((int)(0x3)), unchecked((int)(0x0))
				, unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked((int)(0x0)), unchecked(
				(int)(0x0)) };
		}

		private readonly StandardSyntaxParser.JJCalls[] jj_2_rtns = new StandardSyntaxParser.JJCalls
			[2];

		private bool jj_rescan = false;

		private int jj_gc = 0;

		/// <summary>Constructor with user supplied CharStream.</summary>
		/// <remarks>Constructor with user supplied CharStream.</remarks>
		public StandardSyntaxParser(CharStream stream)
		{
			token_source = new StandardSyntaxParserTokenManager(stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new StandardSyntaxParser.JJCalls();
			}
		}

		/// <summary>Reinitialise.</summary>
		/// <remarks>Reinitialise.</remarks>
		public virtual void ReInit(CharStream stream)
		{
			token_source.ReInit(stream);
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new StandardSyntaxParser.JJCalls();
			}
		}

		/// <summary>Constructor with generated Token Manager.</summary>
		/// <remarks>Constructor with generated Token Manager.</remarks>
		public StandardSyntaxParser(StandardSyntaxParserTokenManager tm)
		{
			token_source = tm;
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new StandardSyntaxParser.JJCalls();
			}
		}

		/// <summary>Reinitialise.</summary>
		/// <remarks>Reinitialise.</remarks>
		public virtual void ReInit(StandardSyntaxParserTokenManager tm)
		{
			token_source = tm;
			token = new Token();
			jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 28; i++)
			{
				jj_la1[i] = -1;
			}
			for (int i_1 = 0; i_1 < jj_2_rtns.Length; i_1++)
			{
				jj_2_rtns[i_1] = new StandardSyntaxParser.JJCalls();
			}
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		private Token Jj_consume_token(int kind)
		{
			Token oldToken;
			if ((oldToken = token).next != null)
			{
				token = token.next;
			}
			else
			{
				token = token.next = token_source.GetNextToken();
			}
			jj_ntk = -1;
			if (token.kind == kind)
			{
				jj_gen++;
				if (++jj_gc > 100)
				{
					jj_gc = 0;
					for (int i = 0; i < jj_2_rtns.Length; i++)
					{
						StandardSyntaxParser.JJCalls c = jj_2_rtns[i];
						while (c != null)
						{
							if (c.gen < jj_gen)
							{
								c.first = null;
							}
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

		[System.Serializable]
		private sealed class LookaheadSuccess : Error
		{
		}

		private readonly StandardSyntaxParser.LookaheadSuccess jj_ls = new StandardSyntaxParser.LookaheadSuccess
			();

		private bool Jj_scan_token(int kind)
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
				int i = 0;
				Token tok = token;
				while (tok != null && tok != jj_scanpos)
				{
					i++;
					tok = tok.next;
				}
				if (tok != null)
				{
					Jj_add_error_token(kind, i);
				}
			}
			if (jj_scanpos.kind != kind)
			{
				return true;
			}
			if (jj_la == 0 && jj_scanpos == jj_lastpos)
			{
				throw jj_ls;
			}
			return false;
		}

		/// <summary>Get the next Token.</summary>
		/// <remarks>Get the next Token.</remarks>
		public Token GetNextToken()
		{
			if (token.next != null)
			{
				token = token.next;
			}
			else
			{
				token = token.next = token_source.GetNextToken();
			}
			jj_ntk = -1;
			jj_gen++;
			return token;
		}

		/// <summary>Get the specific Token.</summary>
		/// <remarks>Get the specific Token.</remarks>
		public Token GetToken(int index)
		{
			Token t = token;
			for (int i = 0; i < index; i++)
			{
				if (t.next != null)
				{
					t = t.next;
				}
				else
				{
					t = t.next = token_source.GetNextToken();
				}
			}
			return t;
		}

		private int Jj_ntk()
		{
			if ((jj_nt = token.next) == null)
			{
				return (jj_ntk = (token.next = token_source.GetNextToken()).kind);
			}
			else
			{
				return (jj_ntk = jj_nt.kind);
			}
		}

		private IList<int[]> jj_expentries = new AList<int[]>();

		private int[] jj_expentry;

		private int jj_kind = -1;

		private int[] jj_lasttokens = new int[100];

		private int jj_endpos;

		private void Jj_add_error_token(int kind, int pos)
		{
			if (pos >= 100)
			{
				return;
			}
			if (pos == jj_endpos + 1)
			{
				jj_lasttokens[jj_endpos++] = kind;
			}
			else
			{
				if (jj_endpos != 0)
				{
					jj_expentry = new int[jj_endpos];
					for (int i = 0; i < jj_endpos; i++)
					{
						jj_expentry[i] = jj_lasttokens[i];
					}
					for (Iterator<object> it = jj_expentries.Iterator(); it.HasNext(); )
					{
						int[] oldentry = (int[])(it.Next());
						if (oldentry.Length == jj_expentry.Length)
						{
							for (int i_1 = 0; i_1 < jj_expentry.Length; i_1++)
							{
								if (oldentry[i_1] != jj_expentry[i_1])
								{
									goto jj_entries_loop_continue;
								}
							}
							jj_expentries.AddItem(jj_expentry);
							goto jj_entries_loop_break;
						}
jj_entries_loop_continue: ;
					}
jj_entries_loop_break: ;
					if (pos != 0)
					{
						jj_lasttokens[(jj_endpos = pos) - 1] = kind;
					}
				}
			}
		}

		/// <summary>Generate ParseException.</summary>
		/// <remarks>Generate ParseException.</remarks>
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
			for (int i_1 = 0; i_1 < 34; i_1++)
			{
				if (la1tokens[i_1])
				{
					jj_expentry = new int[1];
					jj_expentry[0] = i_1;
					jj_expentries.AddItem(jj_expentry);
				}
			}
			jj_endpos = 0;
			Jj_rescan_token();
			Jj_add_error_token(0, 0);
			int[][] exptokseq = new int[jj_expentries.Count][];
			for (int i_2 = 0; i_2 < jj_expentries.Count; i_2++)
			{
				exptokseq[i_2] = jj_expentries[i_2];
			}
			return new ParseException(token, exptokseq, tokenImage);
		}

		/// <summary>Enable tracing.</summary>
		/// <remarks>Enable tracing.</remarks>
		public void Enable_tracing()
		{
		}

		/// <summary>Disable tracing.</summary>
		/// <remarks>Disable tracing.</remarks>
		public void Disable_tracing()
		{
		}

		private void Jj_rescan_token()
		{
			jj_rescan = true;
			for (int i = 0; i < 2; i++)
			{
				try
				{
					StandardSyntaxParser.JJCalls p = jj_2_rtns[i];
					do
					{
						if (p.gen > jj_gen)
						{
							jj_la = p.arg;
							jj_lastpos = jj_scanpos = p.first;
							switch (i)
							{
								case 0:
								{
									Jj_3_1();
									break;
								}

								case 1:
								{
									Jj_3_2();
									break;
								}
							}
						}
						p = p.next;
					}
					while (p != null);
				}
				catch (StandardSyntaxParser.LookaheadSuccess)
				{
				}
			}
			jj_rescan = false;
		}

		private void Jj_save(int index, int xla)
		{
			StandardSyntaxParser.JJCalls p = jj_2_rtns[index];
			while (p.gen > jj_gen)
			{
				if (p.next == null)
				{
					p = p.next = new StandardSyntaxParser.JJCalls();
					break;
				}
				p = p.next;
			}
			p.gen = jj_gen + xla - jj_la;
			p.first = token;
			p.arg = xla;
		}

		internal sealed class JJCalls
		{
			internal int gen;

			internal Token first;

			internal int arg;

			internal StandardSyntaxParser.JJCalls next;
		}
	}
}
