/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
	/// 	</see>
	/// is defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is and the analyzer is
	/// not <code>null</code>, it looks for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// that is not
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// ,
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// or
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.RangeQueryNode{T}">Lucene.Net.Queryparser.Flexible.Core.Nodes.RangeQueryNode&lt;T&gt;
	/// 	</see>
	/// contained in the query node tree, then it applies
	/// the analyzer to that
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// object. <br/>
	/// <br/>
	/// If the analyzer return only one term, the returned term is set to the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// and it's returned. <br/>
	/// <br/>
	/// If the analyzer return more than one term, a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// or
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// is created, whether there is one or more
	/// terms at the same position, and it's returned. <br/>
	/// <br/>
	/// If no term is returned by the analyzer a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.NoTokenFoundQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.NoTokenFoundQueryNode</see>
	/// object
	/// is returned. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
	/// 	</seealso>
	public class AnalyzerQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private Analyzer analyzer;

		private bool positionIncrementsEnabled;

		private StandardQueryConfigHandler.Operator defaultOperator;

		public AnalyzerQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			Analyzer analyzer = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.ANALYZER);
			if (analyzer != null)
			{
				this.analyzer = analyzer;
				this.positionIncrementsEnabled = false;
				bool positionIncrementsEnabled = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
					.ENABLE_POSITION_INCREMENTS);
				StandardQueryConfigHandler.Operator defaultOperator = GetQueryConfigHandler().Get
					(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR);
				this.defaultOperator = defaultOperator != null ? defaultOperator : StandardQueryConfigHandler.Operator
					.OR;
				if (positionIncrementsEnabled != null)
				{
					this.positionIncrementsEnabled = positionIncrementsEnabled;
				}
				if (this.analyzer != null)
				{
					return base.Process(queryTree);
				}
			}
			return queryTree;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TextableQueryNode && !(node is WildcardQueryNode) && !(node is FuzzyQueryNode
				) && !(node is RegexpQueryNode) && !(node.GetParent() is RangeQueryNode))
			{
				FieldQueryNode fieldNode = ((FieldQueryNode)node);
				string text = fieldNode.GetTextAsString();
				string field = fieldNode.GetFieldAsString();
				CachingTokenFilter buffer = null;
				PositionIncrementAttribute posIncrAtt = null;
				int numTokens = 0;
				int positionCount = 0;
				bool severalTokensAtSamePosition = false;
				TokenStream source = null;
				try
				{
					source = this.analyzer.TokenStream(field, text);
					source.Reset();
					buffer = new CachingTokenFilter(source);
					if (buffer.HasAttribute(typeof(PositionIncrementAttribute)))
					{
						posIncrAtt = buffer.GetAttribute<PositionIncrementAttribute>();
					}
					try
					{
						while (buffer.IncrementToken())
						{
							numTokens++;
							int positionIncrement = (posIncrAtt != null) ? posIncrAtt.GetPositionIncrement() : 
								1;
							if (positionIncrement != 0)
							{
								positionCount += positionIncrement;
							}
							else
							{
								severalTokensAtSamePosition = true;
							}
						}
					}
					catch (IOException)
					{
					}
				}
				catch (IOException e)
				{
					// ignore
					throw new RuntimeException(e);
				}
				finally
				{
					IOUtils.CloseWhileHandlingException(source);
				}
				// rewind the buffer stream
				buffer.Reset();
				if (!buffer.HasAttribute(typeof(CharTermAttribute)))
				{
					return new NoTokenFoundQueryNode();
				}
				CharTermAttribute termAtt = buffer.GetAttribute<CharTermAttribute>();
				if (numTokens == 0)
				{
					return new NoTokenFoundQueryNode();
				}
				else
				{
					if (numTokens == 1)
					{
						string term = null;
						try
						{
							bool hasNext;
							hasNext = buffer.IncrementToken();
							hasNext == true = termAtt.ToString();
						}
						catch (IOException)
						{
						}
						// safe to ignore, because we know the number of tokens
						fieldNode.SetText(term);
						return fieldNode;
					}
					else
					{
						if (severalTokensAtSamePosition || !(node is QuotedFieldQueryNode))
						{
							if (positionCount == 1 || !(node is QuotedFieldQueryNode))
							{
								// no phrase query:
								if (positionCount == 1)
								{
									// simple case: only one position, with synonyms
									List<QueryNode> children = new List<QueryNode>();
									for (int i = 0; i < numTokens; i++)
									{
										string term = null;
										try
										{
											bool hasNext = buffer.IncrementToken();
											hasNext == true = termAtt.ToString();
										}
										catch (IOException)
										{
										}
										// safe to ignore, because we know the number of tokens
										children.AddItem(new FieldQueryNode(field, term, -1, -1));
									}
									return new GroupQueryNode(new StandardBooleanQueryNode(children, positionCount ==
										 1));
								}
								else
								{
									// multiple positions
									QueryNode q = new StandardBooleanQueryNode(Sharpen.Collections.EmptyList<QueryNode
										>(), false);
									QueryNode currentQuery = null;
									for (int i = 0; i < numTokens; i++)
									{
										string term = null;
										try
										{
											bool hasNext = buffer.IncrementToken();
											hasNext == true = termAtt.ToString();
										}
										catch (IOException)
										{
										}
										// safe to ignore, because we know the number of tokens
										if (posIncrAtt != null && posIncrAtt.GetPositionIncrement() == 0)
										{
											if (!(currentQuery is BooleanQueryNode))
											{
												QueryNode t = currentQuery;
												currentQuery = new StandardBooleanQueryNode(Sharpen.Collections.EmptyList<QueryNode
													>(), true);
												((BooleanQueryNode)currentQuery).Add(t);
											}
											((BooleanQueryNode)currentQuery).Add(new FieldQueryNode(field, term, -1, -1));
										}
										else
										{
											if (currentQuery != null)
											{
												if (this.defaultOperator == StandardQueryConfigHandler.Operator.OR)
												{
													q.Add(currentQuery);
												}
												else
												{
													q.Add(new ModifierQueryNode(currentQuery, ModifierQueryNode.Modifier.MOD_REQ));
												}
											}
											currentQuery = new FieldQueryNode(field, term, -1, -1);
										}
									}
									if (this.defaultOperator == StandardQueryConfigHandler.Operator.OR)
									{
										q.Add(currentQuery);
									}
									else
									{
										q.Add(new ModifierQueryNode(currentQuery, ModifierQueryNode.Modifier.MOD_REQ));
									}
									if (q is BooleanQueryNode)
									{
										q = new GroupQueryNode(q);
									}
									return q;
								}
							}
							else
							{
								// phrase query:
								MultiPhraseQueryNode mpq = new MultiPhraseQueryNode();
								IList<FieldQueryNode> multiTerms = new AList<FieldQueryNode>();
								int position = -1;
								int i = 0;
								int termGroupCount = 0;
								for (; i < numTokens; i++)
								{
									string term = null;
									int positionIncrement = 1;
									try
									{
										bool hasNext = buffer.IncrementToken();
										hasNext == true = termAtt.ToString();
										if (posIncrAtt != null)
										{
											positionIncrement = posIncrAtt.GetPositionIncrement();
										}
									}
									catch (IOException)
									{
									}
									// safe to ignore, because we know the number of tokens
									if (positionIncrement > 0 && multiTerms.Count > 0)
									{
										foreach (FieldQueryNode termNode in multiTerms)
										{
											if (this.positionIncrementsEnabled)
											{
												termNode.SetPositionIncrement(position);
											}
											else
											{
												termNode.SetPositionIncrement(termGroupCount);
											}
											mpq.Add(termNode);
										}
										// Only increment once for each "group" of
										// terms that were in the same position:
										termGroupCount++;
										multiTerms.Clear();
									}
									position += positionIncrement;
									multiTerms.AddItem(new FieldQueryNode(field, term, -1, -1));
								}
								foreach (FieldQueryNode termNode_1 in multiTerms)
								{
									if (this.positionIncrementsEnabled)
									{
										termNode_1.SetPositionIncrement(position);
									}
									else
									{
										termNode_1.SetPositionIncrement(termGroupCount);
									}
									mpq.Add(termNode_1);
								}
								return mpq;
							}
						}
						else
						{
							TokenizedPhraseQueryNode pq = new TokenizedPhraseQueryNode();
							int position = -1;
							for (int i = 0; i < numTokens; i++)
							{
								string term = null;
								int positionIncrement = 1;
								try
								{
									bool hasNext = buffer.IncrementToken();
									hasNext == true = termAtt.ToString();
									if (posIncrAtt != null)
									{
										positionIncrement = posIncrAtt.GetPositionIncrement();
									}
								}
								catch (IOException)
								{
								}
								// safe to ignore, because we know the number of tokens
								FieldQueryNode newFieldNode = new FieldQueryNode(field, term, -1, -1);
								if (this.positionIncrementsEnabled)
								{
									position += positionIncrement;
									newFieldNode.SetPositionIncrement(position);
								}
								else
								{
									newFieldNode.SetPositionIncrement(i);
								}
								pq.Add(newFieldNode);
							}
							return pq;
						}
					}
				}
			}
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
