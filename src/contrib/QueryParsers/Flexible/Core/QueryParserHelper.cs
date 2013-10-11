using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    public class QueryParserHelper
    {
        private IQueryNodeProcessor processor;

        private ISyntaxParser syntaxParser;

        private IQueryBuilder builder;

        private QueryConfigHandler config;

        public QueryParserHelper(QueryConfigHandler queryConfigHandler, ISyntaxParser syntaxParser, IQueryNodeProcessor processor, IQueryBuilder builder)
        {
            this.syntaxParser = syntaxParser;
            this.config = queryConfigHandler;
            this.processor = processor;
            this.builder = builder;

            if (processor != null)
            {
                processor.QueryConfigHandler = queryConfigHandler;
            }
        }

        public IQueryNodeProcessor QueryNodeProcessor
        {
            get
            {
                return processor;
            }
            set
            {
                this.processor = value;
                this.processor.QueryConfigHandler = QueryConfigHandler;
            }
        }

        public ISyntaxParser SyntaxParser
        {
            get { return syntaxParser; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("textParser should not be null!");
                }

                this.syntaxParser = value;
            }
        }

        public IQueryBuilder QueryBuilder
        {
            get { return builder; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("queryBuilder should not be null!");
                }

                this.builder = value;
            }
        }

        public QueryConfigHandler QueryConfigHandler
        {
            get { return config; }
            set
            {
                this.config = value;
                IQueryNodeProcessor processor = QueryNodeProcessor;

                if (processor != null)
                {
                    processor.QueryConfigHandler = config;
                }
            }
        }

        public virtual object Parse(string query, string defaultField)
        {
            IQueryNode queryTree = SyntaxParser.Parse(new StringCharSequenceWrapper(query), new StringCharSequenceWrapper(defaultField));

            IQueryNodeProcessor processor = QueryNodeProcessor;

            if (processor != null)
            {
                queryTree = processor.Process(queryTree);
            }

            return QueryBuilder.Build(queryTree);
        }
    }
}
