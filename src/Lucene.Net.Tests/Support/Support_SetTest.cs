// Adapted from Apache Harmony tests: https://github.com/apache/harmony/blob/trunk/classlib/support/src/test/java/tests/support/Support_SetTest.java
using Lucene.Net.Util;
using System.Collections.Generic;

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

    public class Support_SetTest : LuceneTestCase
    {
        ISet<int> set; // must contain the Integers 0 to 99

        // LUCENENET: removed unused string argument and overload
        public Support_SetTest(/*String p1,*/ ISet<int> s)
            //: base(p1)
        {
            set = s;
        }

        public void RunTest() {
            // add
            assertTrue("Set Test - Adding a duplicate element changed the set",
                !set.Add(50));
            assertTrue("Set Test - Removing an element did not change the set", set
                .Remove(50));
            assertTrue(
                "Set Test - Adding and removing a duplicate element failed to remove it",
                !set.Contains(50));
            set.Add(50);
            new Support_CollectionTest(set).RunTest();
        }
    }
}
