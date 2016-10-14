using NUnit.Framework;

namespace Lucene.Net.Util
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

    /*using RunWith = org.junit.runner.RunWith;

    using RandomizedRunner = com.carrotsearch.randomizedtesting.RandomizedRunner;*/

    public class TestInPlaceMergeSorter : BaseSortTestCase
    {
        public TestInPlaceMergeSorter()
            : base(true)
        {
        }

        public override Sorter NewSorter(Entry[] arr)
        {
            return new ArrayInPlaceMergeSorter<Entry>(arr, ArrayUtil.naturalComparator<Entry>());
        }


        #region BaseSortTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestEmpty()
        {
            base.TestEmpty();
        }

        [Test]
        public override void TestOne()
        {
            base.TestOne();
        }

        [Test]
        public override void TestTwo()
        {
            base.TestTwo();
        }

        [Test]
        public override void TestRandom()
        {
            base.TestRandom();
        }

        [Test]
        public override void TestRandomLowCardinality()
        {
            base.TestRandomLowCardinality();
        }

        [Test]
        public override void TestAscending()
        {
            base.TestAscending();
        }

        [Test]
        public override void TestAscendingSequences()
        {
            base.TestAscendingSequences();
        }

        [Test]
        public override void TestDescending()
        {
            base.TestDescending();
        }

        [Test]
        public override void TestStrictlyDescendingStrategy()
        {
            base.TestStrictlyDescendingStrategy();
        }

        #endregion
    }
}