using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
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

    public class TestQueryNode : LuceneTestCase
    {
        /* LUCENE-2227 bug in QueryNodeImpl.add() */
        [Test]
        public void TestAddChildren()
        {
            IQueryNode nodeA = new FieldQueryNode("foo", "A", 0, 1);
            IQueryNode nodeB = new FieldQueryNode("foo", "B", 1, 2);
            BooleanQueryNode bq = new BooleanQueryNode(
                new IQueryNode[] { nodeA });
            bq.Add(new IQueryNode[] { nodeB });
            assertEquals(2, bq.GetChildren().Count);
        }

        /* LUCENE-3045 bug in QueryNodeImpl.containsTag(String key)*/
        [Test]
        public void TestTags()
        {
            IQueryNode node = new FieldQueryNode("foo", "A", 0, 1);

            node.SetTag("TaG", new Object());
            assertTrue(node.TagMap.size() > 0);
            assertTrue(node.ContainsTag("tAg"));
            assertTrue(node.GetTag("tAg") != null);

        }

        /* LUCENE-5099 - QueryNodeProcessor should set parent to null before returning on processing */
        [Test]
        public void TestRemoveFromParent()
        {
            BooleanQueryNode booleanNode = new BooleanQueryNode(Collections.EmptyList<IQueryNode>());
            FieldQueryNode fieldNode = new FieldQueryNode("foo", "A", 0, 1);
            assertNull(fieldNode.Parent);

            booleanNode.Add(fieldNode);
            assertNotNull(fieldNode.Parent);

            fieldNode.RemoveFromParent();
            assertNull(fieldNode.Parent);
            /* LUCENE-5805 - QueryNodeImpl.removeFromParent does a lot of work without any effect */
            assertFalse(booleanNode.GetChildren().Contains(fieldNode));

            booleanNode.Add(fieldNode);
            assertNotNull(fieldNode.Parent);

            booleanNode.Set(Collections.EmptyList<IQueryNode>());
            assertNull(fieldNode.Parent);
        }

        // LUCENENET: Added this patch in Lucene.NET 4.8.0 from Lucene 5.3.0
        [Test]
        public void TestRemoveChildren()
        {
            BooleanQueryNode booleanNode = new BooleanQueryNode(Collections.EmptyList<IQueryNode>());
            FieldQueryNode fieldNode = new FieldQueryNode("foo", "A", 0, 1);

            booleanNode.Add(fieldNode);
            assertTrue(booleanNode.GetChildren().Count == 1);

            booleanNode.RemoveChildren(fieldNode);
            assertTrue(booleanNode.GetChildren().Count == 0);
            assertNull(fieldNode.Parent);
        }
    }
}
