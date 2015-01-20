/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor is used to convert
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// s to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</see>
	/// s. It looks for
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	</see>
	/// set in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.FieldConfig
	/// 	</see>
	/// of
	/// every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// found. If
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	</see>
	/// is found, it considers that
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// to be a numeric range query and convert it to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</see>
	/// .
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</seealso>
	public class NumericRangeQueryNodeProcessor : QueryNodeProcessorImpl
	{
		/// <summary>
		/// Constructs an empty
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</see>
		/// object.
		/// </summary>
		public NumericRangeQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TermRangeQueryNode)
			{
				QueryConfigHandler config = GetQueryConfigHandler();
				if (config != null)
				{
					TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
					FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(termRangeNode
						.GetField()));
					if (fieldConfig != null)
					{
						NumericConfig numericConfig = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys
							.NUMERIC_CONFIG);
						if (numericConfig != null)
						{
							FieldQueryNode lower = termRangeNode.GetLowerBound();
							FieldQueryNode upper = termRangeNode.GetUpperBound();
							string lowerText = lower.GetTextAsString();
							string upperText = upper.GetTextAsString();
							NumberFormat numberFormat = numericConfig.GetNumberFormat();
							Number lowerNumber = null;
							Number upperNumber = null;
							if (lowerText.Length > 0)
							{
								try
								{
									lowerNumber = numberFormat.Parse(lowerText);
								}
								catch (ParseException e)
								{
									throw new QueryNodeParseException(new MessageImpl(QueryParserMessages.COULD_NOT_PARSE_NUMBER
										, lower.GetTextAsString(), numberFormat.GetType().GetCanonicalName()), e);
								}
							}
							if (upperText.Length > 0)
							{
								try
								{
									upperNumber = numberFormat.Parse(upperText);
								}
								catch (ParseException e)
								{
									throw new QueryNodeParseException(new MessageImpl(QueryParserMessages.COULD_NOT_PARSE_NUMBER
										, upper.GetTextAsString(), numberFormat.GetType().GetCanonicalName()), e);
								}
							}
							switch (numericConfig.GetType())
							{
								case FieldType.NumericType.LONG:
								{
									if (upperNumber != null)
									{
										upperNumber = upperNumber;
									}
									if (lowerNumber != null)
									{
										lowerNumber = lowerNumber;
									}
									break;
								}

								case FieldType.NumericType.INT:
								{
									if (upperNumber != null)
									{
										upperNumber = upperNumber;
									}
									if (lowerNumber != null)
									{
										lowerNumber = lowerNumber;
									}
									break;
								}

								case FieldType.NumericType.DOUBLE:
								{
									if (upperNumber != null)
									{
										upperNumber = upperNumber;
									}
									if (lowerNumber != null)
									{
										lowerNumber = lowerNumber;
									}
									break;
								}

								case FieldType.NumericType.FLOAT:
								{
									if (upperNumber != null)
									{
										upperNumber = upperNumber;
									}
									if (lowerNumber != null)
									{
										lowerNumber = lowerNumber;
									}
								}
							}
							NumericQueryNode lowerNode = new NumericQueryNode(termRangeNode.GetField(), lowerNumber
								, numberFormat);
							NumericQueryNode upperNode = new NumericQueryNode(termRangeNode.GetField(), upperNumber
								, numberFormat);
							bool lowerInclusive = termRangeNode.IsLowerInclusive();
							bool upperInclusive = termRangeNode.IsUpperInclusive();
							return new NumericRangeQueryNode(lowerNode, upperNode, lowerInclusive, upperInclusive
								, numericConfig);
						}
					}
				}
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
