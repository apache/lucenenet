using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Builders
{
    public class TestQueryTreeBuilder : LuceneTestCase
    {
        [Test]
        public virtual void TestSetFieldBuilder()
        {
            QueryTreeBuilder qtb = new QueryTreeBuilder();
            qtb.SetBuilder("field", new DummyBuilder());
            Object result = qtb.Build(new FieldQueryNode(new UnescapedCharSequence("field").ToString(), "foo", 0, 0));
            assertEquals("OK", result);

            qtb = new QueryTreeBuilder();
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

        private class DummyBuilder : IQueryBuilder
        {
            public virtual object Build(IQueryNode queryNode)
            {
                return "OK";
            }

        }
    }
}
