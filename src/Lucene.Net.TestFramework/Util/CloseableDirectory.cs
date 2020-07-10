#if TESTFRAMEWORK
// LUCENENET NOTE: This is incomplete

namespace Lucene.Net.Util
{

    using NUnit.Framework;
    using System;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    //using Assert = org.junit.Assert;

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
    /// <seealso> cref= LuceneTestCase#newDirectory(java.util.Random) </seealso>
    internal sealed class IDisposableDirectory : IDisposable
    {
      private readonly BaseDirectoryWrapper Dir;
      private readonly TestRuleMarkFailure FailureMarker;

      public IDisposableDirectory(BaseDirectoryWrapper dir, TestRuleMarkFailure failureMarker)
      {
        this.Dir = dir;
        this.FailureMarker = failureMarker;
      }

      public void Dispose()
      {
        // We only attempt to check open/closed state if there were no other test
        // failures.
        try
        {
          if (FailureMarker.WasSuccessful() && Dir.Open)
          {
            Assert.Fail("Directory not closed: " + Dir);
          }
        }
        finally
        {
          // TODO: perform real close of the delegate: LUCENE-4058
          // dir.close();
        }
      }
    }

}
#endif