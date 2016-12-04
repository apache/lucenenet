using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    /// <summary>
    /// This class is a helper for the query parser framework, it does all the three
    /// query parser phrases at once: text parsing, query processing and query
    /// building.
    /// <para>
    /// It contains methods that allows the user to change the implementation used on
    /// the three phases.
    /// </para>
    /// </summary>
    /// <seealso cref="IQueryNodeProcessor"/>
    /// <seealso cref="ISyntaxParser"/>
    /// <seealso cref="QueryBuilder"/>
    /// <seealso cref="Config.QueryConfigHandler"/>
    public class QueryParserHelper<TQuery> // LUCENENET: Made this class generic so we can avoid the cast (to Query) on the Parse method
    {
        private IQueryNodeProcessor processor;

        private ISyntaxParser syntaxParser;

        private IQueryBuilder<TQuery> builder;

        private QueryConfigHandler config;

        /**
         * Creates a query parser helper object using the specified configuration,
         * text parser, processor and builder.
         * 
         * @param queryConfigHandler
         *          the query configuration handler that will be initially set to this
         *          helper
         * @param syntaxParser
         *          the text parser that will be initially set to this helper
         * @param processor
         *          the query processor that will be initially set to this helper
         * @param builder
         *          the query builder that will be initially set to this helper
         * 
         * @see QueryNodeProcessor
         * @see SyntaxParser
         * @see QueryBuilder
         * @see QueryConfigHandler
         */
        public QueryParserHelper(QueryConfigHandler queryConfigHandler, ISyntaxParser syntaxParser, IQueryNodeProcessor processor,
            IQueryBuilder<TQuery> builder)
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

        /**
         * Returns the processor object used to process the query node tree, it
         * returns <code>null</code> if no processor is used.
         * 
         * @return the actual processor used to process the query node tree,
         *         <code>null</code> if no processor is used
         * 
         * @see QueryNodeProcessor
         * @see #setQueryNodeProcessor(QueryNodeProcessor)
         */
        public virtual IQueryNodeProcessor QueryNodeProcessor
        {
            get { return processor; }
        }

        /**
         * Sets the processor that will be used to process the query node tree. If
         * there is any {@link QueryConfigHandler} returned by
         * {@link #getQueryConfigHandler()}, it will be set on the processor. The
         * argument can be <code>null</code>, which means that no processor will be
         * used to process the query node tree.
         * 
         * @param processor
         *          the processor that will be used to process the query node tree,
         *          this argument can be <code>null</code>
         * 
         * @see #getQueryNodeProcessor()
         * @see QueryNodeProcessor
         */
        public virtual void SetQueryNodeProcessor(IQueryNodeProcessor processor)
        {
            this.processor = processor;
            this.processor.SetQueryConfigHandler(QueryConfigHandler);
        }

        /**
         * Sets the text parser that will be used to parse the query string, it cannot
         * be <code>null</code>.
         * 
         * @param syntaxParser
         *          the text parser that will be used to parse the query string
         * 
         * @see #getSyntaxParser()
         * @see SyntaxParser
         */
        public virtual void SetSyntaxParser(ISyntaxParser syntaxParser)
        {
            if (syntaxParser == null)
            {
                throw new ArgumentException("textParser should not be null!");
            }

            this.syntaxParser = syntaxParser;
        }

        /**
         * The query builder that will be used to build an object from the query node
         * tree. It cannot be <code>null</code>.
         * 
         * @param queryBuilder
         *          the query builder used to build something from the query node tree
         * 
         * @see #getQueryBuilder()
         * @see QueryBuilder
         */
        public virtual void SetQueryBuilder(IQueryBuilder<TQuery> queryBuilder)
        {
            if (queryBuilder == null)
            {
                throw new ArgumentException("queryBuilder should not be null!");
            }

            this.builder = queryBuilder;
        }

        /**
         * Returns the query configuration handler, which is used during the query
         * node tree processing. It can be <code>null</code>.
         * 
         * @return the query configuration handler used on the query processing,
         *         <code>null</code> if not query configuration handler is defined
         * 
         * @see QueryConfigHandler
         * @see #setQueryConfigHandler(QueryConfigHandler)
         */
        public virtual QueryConfigHandler QueryConfigHandler
        {
            get { return config; }
        }

        /**
         * Returns the query builder used to build a object from the query node tree.
         * The object produced by this builder is returned by
         * {@link #parse(String, String)}.
         * 
         * @return the query builder
         * 
         * @see #setQueryBuilder(QueryBuilder)
         * @see QueryBuilder
         */
        public virtual IQueryBuilder<TQuery> QueryBuilder
        {
            get { return this.builder; }
        }

        /**
         * Returns the text parser used to build a query node tree from a query
         * string. The default text parser instance returned by this method is a
         * {@link SyntaxParser}.
         * 
         * @return the text parse used to build query node trees.
         * 
         * @see SyntaxParser
         * @see #setSyntaxParser(SyntaxParser)
         */
        public virtual ISyntaxParser SyntaxParser
        {
            get { return this.syntaxParser; }
        }

        /**
         * Sets the query configuration handler that will be used during query
         * processing. It can be <code>null</code>. It's also set to the processor
         * returned by {@link #getQueryNodeProcessor()}.
         * 
         * @param config
         *          the query configuration handler used during query processing, it
         *          can be <code>null</code>
         * 
         * @see #getQueryConfigHandler()
         * @see QueryConfigHandler
         */
        public virtual void SetQueryConfigHandler(QueryConfigHandler config)
        {
            this.config = config;
            IQueryNodeProcessor processor = QueryNodeProcessor;

            if (processor != null)
            {
                processor.SetQueryConfigHandler(config);
            }
        }

        /**
         * Parses a query string to an object, usually some query object. <br/>
         * <br/>
         * In this method the three phases are executed: <br/>
         * <br/>
         * &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1st - the query string is parsed using the
         * text parser returned by {@link #getSyntaxParser()}, the result is a query
         * node tree <br/>
         * <br/>
         * &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;2nd - the query node tree is processed by the
         * processor returned by {@link #getQueryNodeProcessor()} <br/>
         * <br/>
         * &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;3th - a object is built from the query node
         * tree using the builder returned by {@link #getQueryBuilder()}
         * 
         * @param query
         *          the query string
         * @param defaultField
         *          the default field used by the text parser
         * 
         * @return the object built from the query
         * 
         * @throws QueryNodeException
         *           if something wrong happens along the three phases
         */
        public virtual TQuery Parse(string query, string defaultField)
        {
            IQueryNode queryTree = SyntaxParser.Parse(query, defaultField);

            IQueryNodeProcessor processor = QueryNodeProcessor;

            if (processor != null)
            {
                queryTree = processor.Process(queryTree);
            }

            return QueryBuilder.Build(queryTree);
        }
    }
}
