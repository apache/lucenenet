using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;

namespace Lucene.Net.TestFramework
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
    /// A class to assist xUnit with running <see cref="LuceneTestCase.BeforeClass()"/> and
    /// <see cref="LuceneTestCase.AfterClass()"/> methods during setup/teardown of a test class.
    /// </summary>
    public sealed class BeforeAfterClass : IDisposable
    {
        private readonly ConcurrentDictionary<Action, Action> beforeActions = new ConcurrentDictionary<Action, Action>();
        private readonly ConcurrentStack<Action> afterActions = new ConcurrentStack<Action>();

        /// <summary>
        /// Sets the <paramref name="after"/> and <paramref name="after"/> actions to run, and if this is the first
        /// call for a given action, it runs <paramref name="before"/> immediately and stores <paramref name="after"/> until
        /// <see cref="Dispose()"/> is called.
        /// <para/>
        /// This method is safe to call more than once with the same <paramref name="before"/> action, it will only function
        /// if on the fist call.
        /// <para/>
        /// If called multiple times with different <paramref name="before"/> actions, each one is fired immediately. It is still
        /// safe to call more than once with any of the <paramref name="before"/> actions that were previously provided. The
        /// <paramref name="after"/> actions are stored on a stack and executed in reverse order, once for each call to <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="before">The before action to run immediately, and only once.</param>
        /// <param name="after">The after action that will be called to clean up during <see cref="Dispose()"/>.</param>
        public void SetBeforeAfterClassActions(Action before, Action after)
        {
            // Only add each action once. 
            if (beforeActions.TryAdd(before, before))
            {
                afterActions.Push(after);
                before();
            }
        }

        /// <summary>
        /// Pops an after action from the top of the stack and runs it.
        /// One action will be run on each call until the stack is exhausted.
        /// </summary>
        public void Dispose()
        {
            if (afterActions.TryPop(out Action after))
                after();
        }
    }
}
