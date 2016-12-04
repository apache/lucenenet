using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Flexible.Core.Builders
{
    public class TestQueryTreeBuilder : LuceneTestCase
    {
        [Test]
        public virtual void TestSetFieldBuilder()
        {
            // LUCENENET TODO: Make additional non-generic QueryTreeBuilder of type object?
            QueryTreeBuilder<object> qtb = new QueryTreeBuilder<object>();
            qtb.SetBuilder("field", new DummyBuilder());
            Object result = qtb.Build(new FieldQueryNode(new UnescapedCharSequence("field").ToString(), "foo", 0, 0));
            assertEquals("OK", result);

            qtb = new QueryTreeBuilder<object>();
            qtb.SetBuilder(typeof(DummyQueryNodeInterface), new DummyBuilder());
            result = qtb.Build(new DummyQueryNode());
            assertEquals("OK", result);
        }

        private interface DummyQueryNodeInterface : IQueryNode
        {

        }

        private abstract class AbstractDummyQueryNode : QueryNodeImpl, DummyQueryNodeInterface
        {

        }

        private class DummyQueryNode : AbstractDummyQueryNode
        {

            
        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
            {
                return "DummyQueryNode";
            }

        }

        private class DummyBuilder : IQueryBuilder<object>
        {
            public virtual object Build(IQueryNode queryNode)
            {
                return "OK";
            }

        }
    }
}
