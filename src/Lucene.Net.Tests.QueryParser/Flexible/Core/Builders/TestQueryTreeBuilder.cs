using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Flexible.Core.Builders
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

    public class TestQueryTreeBuilder : LuceneTestCase
    {
        [Test]
        public virtual void TestSetFieldBuilder()
        {
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

        private abstract class AbstractDummyQueryNode : QueryNode, DummyQueryNodeInterface
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
