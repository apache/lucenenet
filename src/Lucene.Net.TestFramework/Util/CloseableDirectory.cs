using Lucene.Net.Store;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;

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

    /// <summary>
    /// Attempts to close a <seealso cref="BaseDirectoryWrapper"/>.
    /// </summary>
    /// <seealso cref="LuceneTestCase.NewDirectory(Random)"/>
    internal sealed class DisposableDirectory : IDisposable
    {
        private readonly BaseDirectoryWrapper dir;

        public DisposableDirectory(BaseDirectoryWrapper dir)
        {
            this.dir = dir ?? throw new ArgumentNullException(nameof(dir));
        }

        public void Dispose()
        {
            // We only attempt to check open/closed state if there were no other test
            // failures.
            try
            {
                //if (FailureMarker.WasSuccessful() && dir.Open)
                // LUCENENET NOTE: Outcome is context sensitive and only exists after a test run.
                ResultState outcome = TestContext.CurrentContext.Result.Outcome;
                if (outcome != ResultState.Failure && outcome != ResultState.Inconclusive && dir.IsOpen)
                {
                    Assert.Fail($"Directory not disposed: {dir}");
                }
            }
            finally
            {
                // TODO: perform real close of the delegate: LUCENE-4058
                // dir.Dispose();
            }
        }
    }

}
