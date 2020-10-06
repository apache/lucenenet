using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Lucene.Net
{
    /// <summary>
    /// Provides a way to keep running the test in NUnit with a different test seed
    /// </summary>
    /// <remarks>
    /// see https://github.com/nunit/nunit/issues/1461#issuecomment-429580661
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FindFirstFailingSeedAttribute : Attribute, IWrapSetUpTearDown
    {
        public int StartingSeed { get; set; }
        public int TimeoutMilliseconds { get; set; } = Timeout.Infinite;

        public TestCommand Wrap(TestCommand command)
        {
            return new FindFirstFailingSeedCommand(command, StartingSeed, TimeoutMilliseconds);
        }

        private sealed class FindFirstFailingSeedCommand : DelegatingTestCommand
        {
            private readonly int startingSeed;
            private readonly int timeoutMilliseconds;

            public FindFirstFailingSeedCommand(TestCommand innerCommand, int startingSeed, int timeoutMilliseconds) : base(innerCommand)
            {
                this.startingSeed = startingSeed;
                this.timeoutMilliseconds = timeoutMilliseconds;
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                var stopwatch = timeoutMilliseconds == Timeout.Infinite ? null : Stopwatch.StartNew();

                for (var seed = startingSeed; ;)
                {
                    ResetRandomSeed(context, seed);
                    context.CurrentResult = context.CurrentTest.MakeTestResult();

                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult.RecordException(ex);
                    }

                    if (context.CurrentTest.Seed != seed)
                        throw new InvalidOperationException($"{nameof(FindFirstFailingSeedAttribute)} cannot be used together with an attribute or test that changes the seed.");

                    TestContext.WriteLine($"Random seed: {seed}");

                    if (context.CurrentResult.ResultState.Status == TestStatus.Failed)
                    {                        
                        break;
                    }

                    seed++;
                    if (seed == startingSeed)
                    {
                        TestContext.WriteLine("Tried every seed without producing a failure.");
                        break;
                    }

                    if (stopwatch != null && stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                    {
                        TestContext.WriteLine($"Timed out after seeds {startingSeed}–{seed} did not produce a failure.");
                        break;
                    }
                }

                return context.CurrentResult;
            }

            private static void ResetRandomSeed(TestExecutionContext context, int seed)
            {
                context.CurrentTest.Seed = seed;

                typeof(TestExecutionContext)
                    .GetField("_randomGenerator", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SetValue(context, null);
            }
        }
    }
}
