using Lucene.Net.Util;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Lucene.Net.Support.Util
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
    /// When applied to an attribute, this will be called back from the
    /// <see cref="NUnitTestFixtureBuilder"/> during test discovery.
    /// This is early enough in the pipeline to set the <see cref="Test.RunState"/>
    /// to <see cref="RunState.Skipped"/> and add a <see cref="PropertyNames.SkipReason"/>
    /// to the <see cref="Test.Properties"/> bag in order to keep the execution engine
    /// from considering the test as runnable. When this is the case, the test engine will
    /// not call methods with <see cref="NUnit.Framework.OneTimeSetUpAttribute"/>
    /// or <see cref="NUnit.Framework.OneTimeTearDownAttribute"/>.
    /// </summary>
    internal interface IApplyTestSkipRule
    {
        /// <summary>
        /// A lower priority takes precence over a higher one. Each fixture is ordered
        /// in priority. The first one that returns <c>true</c> wins and the rest are skipped.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Applies the test skip rule according to the specified test.
        /// </summary>
        /// <param name="test">The test to evaluate.</param>
        /// <returns><see langword="true"/> to skip all lower priority rules;
        /// <see langword="false"/> to continue processing the next lower priority rule
        /// in sequence.</returns>
        bool ApplyTestSkipRule(Test test);
    }
}
