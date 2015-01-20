/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core
{
	/// <summary>
	/// <p>
	/// This class is a helper for the query parser framework, it does all the three
	/// query parser phrases at once: text parsing, query processing and query
	/// building.
	/// </summary>
	/// <remarks>
	/// <p>
	/// This class is a helper for the query parser framework, it does all the three
	/// query parser phrases at once: text parsing, query processing and query
	/// building.
	/// </p>
	/// <p>
	/// It contains methods that allows the user to change the implementation used on
	/// the three phases.
	/// </p>
	/// </remarks>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
	public class QueryParserHelper
	{
		private QueryNodeProcessor processor;

		private SyntaxParser syntaxParser;

		private QueryBuilder builder;

		private QueryConfigHandler config;

		/// <summary>
		/// Creates a query parser helper object using the specified configuration,
		/// text parser, processor and builder.
		/// </summary>
		/// <remarks>
		/// Creates a query parser helper object using the specified configuration,
		/// text parser, processor and builder.
		/// </remarks>
		/// <param name="queryConfigHandler">
		/// the query configuration handler that will be initially set to this
		/// helper
		/// </param>
		/// <param name="syntaxParser">the text parser that will be initially set to this helper
		/// 	</param>
		/// <param name="processor">the query processor that will be initially set to this helper
		/// 	</param>
		/// <param name="builder">the query builder that will be initially set to this helper
		/// 	</param>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser
		/// 	</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder"
		/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public QueryParserHelper(QueryConfigHandler queryConfigHandler, SyntaxParser syntaxParser
			, QueryNodeProcessor processor, QueryBuilder builder)
		{
			this.syntaxParser = syntaxParser;
			this.config = queryConfigHandler;
			this.processor = processor;
			this.builder = builder;
			if (processor != null)
			{
				processor.SetQueryConfigHandler(queryConfigHandler);
			}
		}

		/// <summary>
		/// Returns the processor object used to process the query node tree, it
		/// returns <code>null</code> if no processor is used.
		/// </summary>
		/// <remarks>
		/// Returns the processor object used to process the query node tree, it
		/// returns <code>null</code> if no processor is used.
		/// </remarks>
		/// <returns>
		/// the actual processor used to process the query node tree,
		/// <code>null</code> if no processor is used
		/// </returns>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor</seealso>
		/// <seealso cref="SetQueryNodeProcessor(Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor)
		/// 	">SetQueryNodeProcessor(Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor)
		/// 	</seealso>
		public virtual QueryNodeProcessor GetQueryNodeProcessor()
		{
			return processor;
		}

		/// <summary>Sets the processor that will be used to process the query node tree.</summary>
		/// <remarks>
		/// Sets the processor that will be used to process the query node tree. If
		/// there is any
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// returned by
		/// <see cref="GetQueryConfigHandler()">GetQueryConfigHandler()</see>
		/// , it will be set on the processor. The
		/// argument can be <code>null</code>, which means that no processor will be
		/// used to process the query node tree.
		/// </remarks>
		/// <param name="processor">
		/// the processor that will be used to process the query node tree,
		/// this argument can be <code>null</code>
		/// </param>
		/// <seealso cref="GetQueryNodeProcessor()">GetQueryNodeProcessor()</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Processors.QueryNodeProcessor</seealso>
		public virtual void SetQueryNodeProcessor(QueryNodeProcessor processor)
		{
			this.processor = processor;
			this.processor.SetQueryConfigHandler(GetQueryConfigHandler());
		}

		/// <summary>
		/// Sets the text parser that will be used to parse the query string, it cannot
		/// be <code>null</code>.
		/// </summary>
		/// <remarks>
		/// Sets the text parser that will be used to parse the query string, it cannot
		/// be <code>null</code>.
		/// </remarks>
		/// <param name="syntaxParser">the text parser that will be used to parse the query string
		/// 	</param>
		/// <seealso cref="GetSyntaxParser()">GetSyntaxParser()</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser
		/// 	</seealso>
		public virtual void SetSyntaxParser(SyntaxParser syntaxParser)
		{
			if (syntaxParser == null)
			{
				throw new ArgumentException("textParser should not be null!");
			}
			this.syntaxParser = syntaxParser;
		}

		/// <summary>
		/// The query builder that will be used to build an object from the query node
		/// tree.
		/// </summary>
		/// <remarks>
		/// The query builder that will be used to build an object from the query node
		/// tree. It cannot be <code>null</code>.
		/// </remarks>
		/// <param name="queryBuilder">the query builder used to build something from the query node tree
		/// 	</param>
		/// <seealso cref="GetQueryBuilder()">GetQueryBuilder()</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder"
		/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
		public virtual void SetQueryBuilder(QueryBuilder queryBuilder)
		{
			if (queryBuilder == null)
			{
				throw new ArgumentException("queryBuilder should not be null!");
			}
			this.builder = queryBuilder;
		}

		/// <summary>
		/// Returns the query configuration handler, which is used during the query
		/// node tree processing.
		/// </summary>
		/// <remarks>
		/// Returns the query configuration handler, which is used during the query
		/// node tree processing. It can be <code>null</code>.
		/// </remarks>
		/// <returns>
		/// the query configuration handler used on the query processing,
		/// <code>null</code> if not query configuration handler is defined
		/// </returns>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		/// <seealso cref="SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">SetQueryConfigHandler(Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</seealso>
		public virtual QueryConfigHandler GetQueryConfigHandler()
		{
			return config;
		}

		/// <summary>Returns the query builder used to build a object from the query node tree.
		/// 	</summary>
		/// <remarks>
		/// Returns the query builder used to build a object from the query node tree.
		/// The object produced by this builder is returned by
		/// <see cref="Parse(string, string)">Parse(string, string)</see>
		/// .
		/// </remarks>
		/// <returns>the query builder</returns>
		/// <seealso cref="SetQueryBuilder(Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder)
		/// 	">SetQueryBuilder(Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder)
		/// 	</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder"
		/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
		public virtual QueryBuilder GetQueryBuilder()
		{
			return this.builder;
		}

		/// <summary>
		/// Returns the text parser used to build a query node tree from a query
		/// string.
		/// </summary>
		/// <remarks>
		/// Returns the text parser used to build a query node tree from a query
		/// string. The default text parser instance returned by this method is a
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser
		/// 	</see>
		/// .
		/// </remarks>
		/// <returns>the text parse used to build query node trees.</returns>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser
		/// 	</seealso>
		/// <seealso cref="SetSyntaxParser(Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser)
		/// 	">SetSyntaxParser(Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.SyntaxParser)
		/// 	</seealso>
		public virtual SyntaxParser GetSyntaxParser()
		{
			return this.syntaxParser;
		}

		/// <summary>
		/// Sets the query configuration handler that will be used during query
		/// processing.
		/// </summary>
		/// <remarks>
		/// Sets the query configuration handler that will be used during query
		/// processing. It can be <code>null</code>. It's also set to the processor
		/// returned by
		/// <see cref="GetQueryNodeProcessor()">GetQueryNodeProcessor()</see>
		/// .
		/// </remarks>
		/// <param name="config">
		/// the query configuration handler used during query processing, it
		/// can be <code>null</code>
		/// </param>
		/// <seealso cref="GetQueryConfigHandler()">GetQueryConfigHandler()</seealso>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public virtual void SetQueryConfigHandler(QueryConfigHandler config)
		{
			this.config = config;
			QueryNodeProcessor processor = GetQueryNodeProcessor();
			if (processor != null)
			{
				processor.SetQueryConfigHandler(config);
			}
		}

		/// <summary>Parses a query string to an object, usually some query object.</summary>
		/// <remarks>
		/// Parses a query string to an object, usually some query object. <br/>
		/// <br/>
		/// In this method the three phases are executed: <br/>
		/// <br/>
		/// &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1st - the query string is parsed using the
		/// text parser returned by
		/// <see cref="GetSyntaxParser()">GetSyntaxParser()</see>
		/// , the result is a query
		/// node tree <br/>
		/// <br/>
		/// &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2nd - the query node tree is processed by the
		/// processor returned by
		/// <see cref="GetQueryNodeProcessor()">GetQueryNodeProcessor()</see>
		/// <br/>
		/// <br/>
		/// &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;3th - a object is built from the query node
		/// tree using the builder returned by
		/// <see cref="GetQueryBuilder()">GetQueryBuilder()</see>
		/// </remarks>
		/// <param name="query">the query string</param>
		/// <param name="defaultField">the default field used by the text parser</param>
		/// <returns>the object built from the query</returns>
		/// <exception cref="QueryNodeException">if something wrong happens along the three phases
		/// 	</exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual object Parse(string query, string defaultField)
		{
			QueryNode queryTree = GetSyntaxParser().Parse(query, defaultField);
			QueryNodeProcessor processor = GetQueryNodeProcessor();
			if (processor != null)
			{
				queryTree = processor.Process(queryTree);
			}
			return GetQueryBuilder().Build(queryTree);
		}
	}
}
