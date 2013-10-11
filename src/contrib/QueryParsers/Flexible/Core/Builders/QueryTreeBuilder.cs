using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Builders
{
    public class QueryTreeBuilder : IQueryBuilder
    {
        public static readonly string QUERY_TREE_BUILDER_TAGID = typeof(QueryTreeBuilder).FullName;

        private HashMap<Type, IQueryBuilder> queryNodeBuilders;

        private HashMap<string, IQueryBuilder> fieldNameBuilders;

        public QueryTreeBuilder()
        {
            // empty constructor
        }

        public void SetBuilder(string fieldName, IQueryBuilder builder)
        {
            if (this.fieldNameBuilders == null)
            {
                this.fieldNameBuilders = new HashMap<String, IQueryBuilder>();
            }

            this.fieldNameBuilders[fieldName] = builder;
        }

        public void SetBuilder(Type queryNodeClass, IQueryBuilder builder)
        {
            if (this.queryNodeBuilders == null)
            {
                this.queryNodeBuilders = new HashMap<Type, IQueryBuilder>();
            }

            this.queryNodeBuilders[queryNodeClass] = builder;
        }

        private void Process(IQueryNode node)
        {
            if (node != null)
            {
                IQueryBuilder builder = GetBuilder(node);

                if (!(builder is QueryTreeBuilder))
                {
                    IList<IQueryNode> children = node.Children;

                    if (children != null)
                    {
                        foreach (IQueryNode child in children)
                        {
                            Process(child);
                        }
                    }
                }

                ProcessNode(node, builder);
            }
        }

        private IQueryBuilder GetBuilder(IQueryNode node)
        {
            IQueryBuilder builder = null;

            if (this.fieldNameBuilders != null && node is IFieldableNode)
            {
                ICharSequence field = ((IFieldableNode)node).Field;
                
                builder = this.fieldNameBuilders[field.ToString()];
            }

            if (builder == null && this.queryNodeBuilders != null)
            {
                Type clazz = node.GetType();

                do
                {
                    builder = GetQueryBuilder(clazz);

                    if (builder == null)
                    {
                        Type[] classes = node.GetType().GetInterfaces();

                        foreach (Type actualClass in classes)
                        {
                            builder = GetQueryBuilder(actualClass);

                            if (builder != null)
                            {
                                break;
                            }
                        }
                    }

                } while (builder == null && (clazz = clazz.BaseType) != null);
            }

            return builder;
        }

        private void ProcessNode(IQueryNode node, IQueryBuilder builder)
        {
            if (builder == null)
            {
                throw new QueryNodeException(new Message(
                    QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR, node
                        .ToQueryString(new EscapeQuerySyntax()), node.GetType().FullName));
            }

            Object obj = builder.Build(node);

            if (obj != null)
            {
                node.SetTag(QUERY_TREE_BUILDER_TAGID, obj);
            }
        }

        private IQueryBuilder GetQueryBuilder(Type clazz)
        {
            if (typeof(IQueryNode).IsAssignableFrom(clazz))
            {
                return this.queryNodeBuilders[clazz];
            }

            return null;
        }

        public object Build(IQueryNode queryNode)
        {
            Process(queryNode);

            return queryNode.GetTag(QUERY_TREE_BUILDER_TAGID);
        }
    }
}
