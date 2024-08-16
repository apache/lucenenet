using System.Collections.Generic;
using NUnit.Framework;

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using Lucene.Net.Support;
using System;


namespace Lucene.Net
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
