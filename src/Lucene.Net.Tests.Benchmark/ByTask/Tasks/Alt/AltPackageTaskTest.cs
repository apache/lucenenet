using NUnit.Framework;
using System;
using System.Reflection;

namespace Lucene.Net.Benchmarks.ByTask.Tasks.Alt
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
    /// Tests that tasks in alternate packages are found.
    /// </summary>
    public class AltPackageTaskTest : BenchmarkTestCase
    {
        /** Benchmark should fail loading the algorithm when alt is not specified */
        [Test]
        [Ignore("In LUCENENET, we use all referenced assemblies by default. Currently, we don't support a scenario that loads external assemblies.")]
        public void TestWithoutAlt()
        {
            try
            {
                execBenchmark(altAlg(false));
                assertFalse("Should have failed to run the algorithm", true);
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
                // expected exception, do nothing
            }
        }

        /** Benchmark should be able to load the algorithm when alt is specified */
        [Test]
        public void TestWithAlt()
        {
            Benchmark bm = execBenchmark(altAlg(true));
            assertNotNull(bm);
            assertNotNull(bm.RunData.Points);
        }

        private String[] altAlg(bool allowAlt)
        {
            String altTask = "{ AltTest }";
            if (allowAlt)
            {
                return new String[] {
                    "alt.tasks.packages = " +this.GetType().Assembly.GetName().Name,
                    altTask
                };
            }
            return new String[] { altTask };
        }
    }
}
