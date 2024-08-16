using System.Collections.Generic;
using NUnit.Framework;

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using Lucene.Net.Support;
using System;


namespace Lucene.Net
{
    public class QueueExtensionsTests : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TryDequeue_ThrowsWhenQueueNull()
        {
            Queue<int> queue = null;
            var ex = Assert.Throws<ArgumentNullException>(() => QueueExtensions.TryDequeue(queue, out var _));
            Assert.That(ex.ParamName, Is.EqualTo("queue"));
        }

        [Test, LuceneNetSpecific]
        public void TryDequeue_QueueEmpty()
        {
            Queue<int> queue = new Queue<int>();
            var found = QueueExtensions.TryDequeue(queue, out var result);
            Assert.That(found, Is.EqualTo(false));
            Assert.That(result, Is.EqualTo(default(int)));
        }

        [Test, LuceneNetSpecific]
        public void TryDequeue_QueueNotEmpty()
        {
            Queue<int> queue = new Queue<int>();
            var item = 1;
            queue.Enqueue(item);
            var countBefore = queue.Count;
            var found = QueueExtensions.TryDequeue(queue, out var result);
            Assert.That(found, Is.EqualTo(true));
            Assert.That(result, Is.EqualTo(item));
            Assert.That(queue.Count, Is.EqualTo(countBefore - 1));
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_ThrowsWhenQueueNull()
        {
            Queue<int> queue = null;
            var ex = Assert.Throws<ArgumentNullException>(() => QueueExtensions.TryPeek(queue, out var _));
            Assert.That(ex.ParamName, Is.EqualTo("queue"));
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_QueueEmpty()
        {
            Queue<int> queue = new Queue<int>();
            var found = QueueExtensions.TryPeek(queue, out var result);
            Assert.That(found, Is.EqualTo(false));
            Assert.That(result, Is.EqualTo(default(int)));
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_QueueNotEmpty()
        {
            Queue<int> queue = new Queue<int>();
            var item = 1;
            queue.Enqueue(item);
            var countBefore = queue.Count;
            var found = QueueExtensions.TryPeek(queue, out var result);
            Assert.That(found, Is.EqualTo(true));
            Assert.That(result, Is.EqualTo(item));
            Assert.That(queue.Count, Is.EqualTo(countBefore));
        }
    }
}
