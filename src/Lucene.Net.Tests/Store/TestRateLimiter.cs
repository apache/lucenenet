using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SimpleRateLimiter = Lucene.Net.Store.RateLimiter.SimpleRateLimiter;

    /// <summary>
    /// Simple testcase for RateLimiter.SimpleRateLimiter
    /// </summary>
    [TestFixture]
    public sealed class TestRateLimiter : LuceneTestCase
    {
        [Test]
        public void TestPause()
        {
            SimpleRateLimiter limiter = new SimpleRateLimiter(10); // 10 MB / Sec
            limiter.Pause(2); //init
            long pause = 0;
            for (int i = 0; i < 3; i++)
            {
                pause += limiter.Pause(4 * 1024 * 1024); // fire up 3 * 4 MB
            }
            //long convert = TimeUnit.MILLISECONDS.convert(pause, TimeUnit.NANOSECONDS);

            // 1000000 Milliseconds per nanosecond
            long convert = pause / 1000000;
            Assert.IsTrue(convert < 2000L, "we should sleep less than 2 seconds but did: " + convert + " millis");
            Assert.IsTrue(convert > 1000L, "we should sleep at least 1 second but did only: " + convert + " millis");
        }
    }
}