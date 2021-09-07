using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;

namespace Lucene.Net.QueryParsers.Flexible.Core
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
    /// <seealso cref="Builders.IQueryBuilder{TQuery}"/>
    /// <seealso cref="Config.QueryConfigHandler"/>
    public class QueryParserHelper<TQuery> // LUCENENET: Made this class generic so we can avoid the cast (to Query) on the Parse method
    {
        private IQueryNodeProcessor processor;

        private ISyntaxParser syntaxParser;

        private IQueryBuilder<TQuery> builder;

        private QueryConfigHandler config;

        /// <summary>
        /// Creates a query parser helper object using the specified configuration,
        /// text parser, processor and builder.
        /// </summary>
        /// <param name="queryConfigHandler">the query configuration handler that will be initially set to this helper</param>
        /// <param name="syntaxParser">the text parser that will be initially set to this helper</param>
        /// <param name="processor">the query processor that will be initially set to this helper</param>
        /// <param name="builder">the query builder that will be initially set to this helper</param>
        /// <seealso cref="IQueryNodeProcessor"/>
        /// <seealso cref="ISyntaxParser"/>
        /// <seealso cref="IQueryBuilder{TQuery}"/>
        /// <seealso cref="Config.QueryConfigHandler"/>
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

        /// <summary>
        /// Gets the processor object used to process the query node tree, it
        /// returns <c>null</c> if no processor is used.
        /// </summary>
        /// <seealso cref="IQueryNodeProcessor"/>.
        /// <seealso cref="SetQueryNodeProcessor(IQueryNodeProcessor)"/>
        public virtual IQueryNodeProcessor QueryNodeProcessor => processor;

        /// <summary>
        /// Sets the processor that will be used to process the query node tree. If
        /// there is any <see cref="Config.QueryConfigHandler"/> returned by
        /// <see cref="QueryConfigHandler"/>, it will be set on the processor. The
        /// argument can be <c>null</c>, which means that no processor will be
        /// used to process the query node tree.
        /// </summary>
        /// <param name="processor">the processor that will be used to process the query node tree,
        /// this argument can be <c>null</c></param>
        /// <seealso cref="QueryNodeProcessor"/>
        /// <seealso cref="IQueryNodeProcessor"/>
        public virtual void SetQueryNodeProcessor(IQueryNodeProcessor processor)
        {
            this.processor = processor;
            this.processor.SetQueryConfigHandler(QueryConfigHandler);
        }

        /// <summary>
        /// Sets the text parser that will be used to parse the query string, it cannot
        /// be <c>null</c>.
        /// </summary>
        /// <param name="syntaxParser">the text parser that will be used to parse the query string</param>
        /// <seealso cref="SyntaxParser"/>
        /// <seealso cref="ISyntaxParser"/>
        public virtual void SetSyntaxParser(ISyntaxParser syntaxParser)
        {
            this.syntaxParser = syntaxParser ?? throw new ArgumentNullException(nameof(syntaxParser), "textParser should not be null!"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// The query builder that will be used to build an object from the query node
        /// tree. It cannot be <c>null</c>.
        /// </summary>
        /// <param name="queryBuilder">the query builder used to build something from the query node tree</param>
        /// <seealso cref="QueryBuilder"/>
        /// <seealso cref="IQueryBuilder{TQuery}"/>
        public virtual void SetQueryBuilder(IQueryBuilder<TQuery> queryBuilder)
        {
            this.builder = queryBuilder ?? throw new ArgumentNullException(nameof(queryBuilder), "queryBuilder should not be null!"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Returns the query configuration handler, which is used during the query
        /// node tree processing. It can be <c>null</c>.
        /// </summary>
        /// <seealso cref="Config.QueryConfigHandler"/>
        /// <seealso cref="SetQueryConfigHandler(QueryConfigHandler)"/>
        public virtual QueryConfigHandler QueryConfigHandler => config;

        /// <summary>
        /// Returns the query builder used to build a object from the query node tree.
        /// The object produced by this builder is returned by <see cref="Parse(string, string)"/>.
        /// </summary>
        /// <seealso cref="SetQueryBuilder(IQueryBuilder{TQuery})"/>
        /// <seealso cref="IQueryBuilder{TQuery}"/>
        public virtual IQueryBuilder<TQuery> QueryBuilder => this.builder;

        /// <summary>
        /// Returns the text parser used to build a query node tree from a query
        /// string. The default text parser instance returned by this method is a
        /// <see cref="ISyntaxParser"/>.
        /// </summary>
        /// <seealso cref="ISyntaxParser"/>
        /// <seealso cref="SetSyntaxParser(ISyntaxParser)"/>
        public virtual ISyntaxParser SyntaxParser => this.syntaxParser;

        /// <summary>
        /// Sets the query configuration handler that will be used during query
        /// processing. It can be <c>null</c>. It's also set to the processor
        /// returned by <see cref="QueryNodeProcessor"/>.
        /// </summary>
        /// <param name="config">the query configuration handler used during query processing, it
        /// can be <c>null</c></param>
        /// <seealso cref="QueryConfigHandler"/>
        /// <seealso cref="Config.QueryConfigHandler"/>
        public virtual void SetQueryConfigHandler(QueryConfigHandler config)
        {
            this.config = config;
            IQueryNodeProcessor processor = QueryNodeProcessor;

            if (processor != null)
            {
                processor.SetQueryConfigHandler(config);
            }
        }

        /// <summary>
        /// Parses a query string to an object, usually some query object.
        /// <para/>
        /// In this method the three phases are executed:
        /// <para/>
        /// <list type="number">
        ///     <item><description>
        ///     the query string is parsed using the
        ///     text parser returned by <see cref="SyntaxParser"/>, the result is a query
        ///     node tree.
        ///     </description></item>
        ///     <item><description>
        ///     the query node tree is processed by the
        ///     processor returned by <see cref="QueryNodeProcessor"/>.
        ///     </description></item>
        ///     <item><description>
        ///     a object is built from the query node
        ///     tree using the builder returned by <see cref="QueryBuilder"/>.
        ///     </description></item>
        /// </list>
        /// </summary>
        /// <param name="query">the query string</param>
        /// <param name="defaultField">the default field used by the text parser</param>
        /// <returns>the object built from the query</returns>
        /// <exception cref="QueryNodeException">if something wrong happens along the three phases</exception>
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
