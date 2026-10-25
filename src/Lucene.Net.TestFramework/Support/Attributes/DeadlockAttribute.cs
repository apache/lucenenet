using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Threading;

namespace Lucene.Net.Attributes
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
    /// Indicates a test has contention between concurrent processes and may deadlock.
    /// </summary>
    /// <remarks>
    /// In order to take advantage of the <see cref="CancelAfterAttribute"/> base class' timeout cancellation support,
    /// the test method must take a <see cref="CancellationToken"/> and check for cancellation (or pass to supporting
    /// APIs that do).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class DeadlockAttribute : CancelAfterAttribute, IApplyToTest
    {
        public DeadlockAttribute() : base(600000) { }

        void IApplyToTest.ApplyToTest(Test test)
        {
            test.Properties.Add(PropertyNames.Category, "Deadlock");
        }
    }
}
