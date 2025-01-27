using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System;
#nullable enable

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

    public abstract partial class LuceneTestCase
    {
        /// <summary>
        /// Specifies that a test should be run multiple times. If any repetition fails,
        /// the remaining ones are not run and a failure is reported.
        /// </summary>
        /// <remarks>
        /// This attribute differs from <see cref="NUnit.Framework.RepeatAttribute"/> in that
        /// it is aware of <see cref="RandomizedContext"/> and will reset the test seed on each
        /// iteration. As a result, if there is a test failure, the seed that is reported will
        /// duplicate the exact test conditions on the first try.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public class RepeatAttribute : System.Attribute, IRepeatTest
        {
            private readonly int repeatCount;

            /// <summary>
            /// Initializes a new instance of <see cref="RepeatAttribute"/>.
            /// </summary>
            /// <param name="repeatCount">The number of times to run the test.</param>
            public RepeatAttribute(int repeatCount)
            {
                this.repeatCount = repeatCount;
            }

            /// <summary>
            /// Wrap a command and return the result.
            /// </summary>
            /// <param name="command">The command to be wrapped.</param>
            /// <returns>The wrapped command.</returns>
            public TestCommand Wrap(TestCommand command)
            {
                return new RepeatTestCommand(command, repeatCount);
            }

            /// <summary>
            /// The test command for the <see cref="RepeatAttribute"/>.
            /// </summary>
            private class RepeatTestCommand : BeforeAndAfterTestCommand
            {
                private readonly int repeatCount;

                /// <summary>
                /// Initializes a new instance of the <see cref="RepeatTestCommand"/> class.
                /// </summary>
                /// <param name="innerCommand">The inner command.</param>
                /// <param name="repeatCount">The number of repetitions</param>
                public RepeatTestCommand(TestCommand innerCommand, int repeatCount)
                    : base(innerCommand)
                {
                    this.repeatCount = repeatCount;
                }

                /// <summary>
                /// Runs the test, saving a <see cref="TestResult"/> in the supplied <see cref="TestExecutionContext"/>.
                /// </summary>
                /// <param name="context">The context in which the test should run.</param>
                /// <returns>A <see cref="TestResult"/>.</returns>
                public override TestResult Execute(TestExecutionContext context)
                {
                    if (context.CurrentTest.TypeInfo is null || !context.CurrentTest.TypeInfo.Type.IsSubclassOf(typeof(LuceneTestCase)))
                    {
                        return SetResultErrorNonLuceneNetTestCaseSubclass(context);
                    }

                    RandomizedContext? randomizedContext = context.CurrentTest.GetRandomizedContext();
                    if (randomizedContext is null)
                    {
                        return SetResultErrorNonLuceneNetTestCaseSubclass(context);
                    }

                    var random = new J2N.Randomizer(randomizedContext.RandomSeed);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        // Regenerate the test seed for this iteration
                        randomizedContext.ResetSeed(testSeed: random.NextInt64());

                        try
                        {
                            // Execute the SetUp, Test, and TearDown with the seed
                            context.CurrentResult = innerCommand.Execute(context);
                        }
                        catch (Exception ex)
                        {
                            if (context.CurrentResult is null) context.CurrentResult = context.CurrentTest.MakeTestResult();
                            context.CurrentResult.RecordException(ex);
                        }

                        if (context.CurrentResult.ResultState != ResultState.Success)
                        {
                            if (context.CurrentResult.ResultState == ResultState.Failure || context.CurrentResult.ResultState == ResultState.Error)
                            {
                                string message = $"Repeat failed on iteration '{i}'.{Environment.NewLine}{Environment.NewLine}{context.CurrentResult.Message}";
                                context.CurrentResult.SetResult(context.CurrentResult.ResultState, message, context.CurrentResult.StackTrace);
                            }

                            break; // Only repeat for successuful test runs
                        }

                        context.CurrentRepeatCount++;
                    }

                    return context.CurrentResult;
                }

                private static TestResult SetResultErrorNonLuceneNetTestCaseSubclass(TestExecutionContext context)
                {
                    if (context.CurrentResult is null) context.CurrentResult = context.CurrentTest.MakeTestResult();
                    // We only want this attribute to be used on subclasses of LuceneTestCase. This is an error.
                    context.CurrentResult.SetResult(ResultState.Error,
                        $"{typeof(RepeatAttribute).FullName} may only be used on a test in a subclass of {nameof(LuceneTestCase)}.");

                    return context.CurrentResult;
                }
            }
        }
    }
}
