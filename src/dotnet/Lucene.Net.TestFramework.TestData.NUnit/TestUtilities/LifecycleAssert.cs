using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.NUnit.TestUtilities
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

    /// <summary>
    /// Assertions over the ordered lifecycle event lists that the lifecycle test fixtures record.
    /// </summary>
    public static class LifecycleAssert
    {
        /// <summary>
        /// Asserts that both <paramref name="first"/> and <paramref name="second"/> were recorded
        /// in <paramref name="events"/>, and that <paramref name="first"/> was recorded before
        /// <paramref name="second"/>. Asserting that both events exist matters: a missing event
        /// yields an index of -1, which would otherwise satisfy a bare index comparison and hide
        /// the very lifecycle regression these tests exist to catch.
        /// </summary>
        public static void AssertBefore(IList<string> events, string first, string second)
        {
            int firstIndex = events.IndexOf(first);
            int secondIndex = events.IndexOf(second);

            Assert.IsTrue(firstIndex >= 0, $"Expected event '{first}' to be recorded. Events: {string.Join(", ", events)}");
            Assert.IsTrue(secondIndex >= 0, $"Expected event '{second}' to be recorded. Events: {string.Join(", ", events)}");
            Assert.IsTrue(firstIndex < secondIndex,
                $"Expected '{first}' to run before '{second}'. Events: {string.Join(", ", events)}");
        }
    }
}
