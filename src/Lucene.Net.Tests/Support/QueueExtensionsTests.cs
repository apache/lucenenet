using System.Collections.Generic;
using NUnit.Framework;

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using Lucene.Net.Support;
using System;

using Assert = Lucene.Net.TestFramework.Assert;


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
#if !FEATURE_QUEUE_TRYDEQUEUE_TRYPEEK
        [Test, LuceneNetSpecific]
        public void TryDequeue_ThrowsWhenQueueNull()
        {
            Queue<int> queue = null;
            var ex = Assert.Throws<ArgumentNullException>(() => queue.TryDequeue(out int _));
            Assert.AreEqual(((ArgumentNullException)ex).ParamName, "queue");
        }

        [Test, LuceneNetSpecific]
        public void TryDequeue_QueueEmpty()
        {
            Queue<int> queue = new Queue<int>();
            bool found = queue.TryDequeue(out int result);
            Assert.AreEqual(found, false);
            Assert.AreEqual(result, default(int));
        }

        [Test, LuceneNetSpecific]
        public void TryDequeue_QueueNotEmpty()
        {
            Queue<int> queue = new Queue<int>();
            int item = 1;
            queue.Enqueue(item);
            int countBefore = queue.Count;
            bool found = queue.TryDequeue(out int result);
            Assert.AreEqual(found, true);
            Assert.AreEqual(result, item);
            Assert.AreEqual(queue.Count, countBefore - 1);
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_ThrowsWhenQueueNull()
        {
            Queue<int> queue = null;
            var ex = Assert.Throws<ArgumentNullException>(() => queue.TryPeek(out int _));
            Assert.AreEqual(((ArgumentNullException)ex).ParamName, "queue");
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_QueueEmpty()
        {
            Queue<int> queue = new Queue<int>();
            bool found = queue.TryPeek(out int result);
            Assert.AreEqual(found, false);
            Assert.AreEqual(result, default(int));
        }

        [Test, LuceneNetSpecific]
        public void TryPeek_QueueNotEmpty()
        {
            Queue<int> queue = new Queue<int>();
            int item = 1;
            queue.Enqueue(item);
            int countBefore = queue.Count;
            bool found = queue.TryPeek(out int result);
            Assert.AreEqual(found, true);
            Assert.AreEqual(result, item);
            Assert.AreEqual(queue.Count, countBefore);
        }
#endif
    }
}
