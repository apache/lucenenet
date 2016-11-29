using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class TestQueryNode : LuceneTestCase
    {
        /* LUCENE-2227 bug in QueryNodeImpl.add() */
        [Test]
        public void testAddChildren()
        {
            IQueryNode nodeA = new FieldQueryNode("foo", "A", 0, 1);
            IQueryNode nodeB = new FieldQueryNode("foo", "B", 1, 2);
            BooleanQueryNode bq = new BooleanQueryNode(
                Arrays.AsList(nodeA));
            bq.Add(Arrays.AsList(nodeB));
            assertEquals(2, bq.GetChildren().size());
        }

        /* LUCENE-3045 bug in QueryNodeImpl.containsTag(String key)*/
        [Test]
        public void testTags()
        {
            IQueryNode node = new FieldQueryNode("foo", "A", 0, 1);

            node.SetTag("TaG", new Object());
            assertTrue(node.TagMap.size() > 0);
            assertTrue(node.ContainsTag("tAg"));
            assertTrue(node.GetTag("tAg") != null);

        }

        /* LUCENE-5099 - QueryNodeProcessorImpl should set parent to null before returning on processing */
        [Test]
        public void testRemoveFromParent()
        {
            BooleanQueryNode booleanNode = new BooleanQueryNode(Collections.EmptyList<IQueryNode>());
            FieldQueryNode fieldNode = new FieldQueryNode("foo", "A", 0, 1);
            assertNull(fieldNode.Parent);

            booleanNode.Add(fieldNode);
            assertNotNull(fieldNode.Parent);

            fieldNode.RemoveFromParent();
            assertNull(fieldNode.Parent);

            booleanNode.Add(fieldNode);
            assertNotNull(fieldNode.Parent);

            booleanNode.Set(Collections.EmptyList<IQueryNode>());
            assertNull(fieldNode.Parent);
        }
    }
}
