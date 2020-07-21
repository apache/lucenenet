using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

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

                    TestContext.Write("TEST TestExecutionContext SEED: ");
                    TestContext.WriteLine(NUnit.Framework.Internal.TestExecutionContext.CurrentContext.CurrentTest.Seed);

                    TestContext.Write("TEST InitialSeed SEED: ");
                    TestContext.WriteLine(NUnit.Framework.Internal.Randomizer.InitialSeed);
                }
                catch (Exception ex)
                {
                    context.CurrentResult.RecordException(ex);
                }

                if (context.CurrentTest.Seed != seed)
                    throw new InvalidOperationException($"{nameof(FindFirstFailingSeedAttribute)} cannot be used together with an attribute or test that changes the seed.");

                if (context.CurrentResult.ResultState.Status == TestStatus.Failed)
                {
                    TestContext.WriteLine($"Random seed: {seed}");

                    TestContext.Write("TEST TestExecutionContext SEED: ");
                    TestContext.WriteLine(NUnit.Framework.Internal.TestExecutionContext.CurrentContext.CurrentTest.Seed);

                    TestContext.Write("TEST InitialSeed SEED: ");
                    TestContext.WriteLine(NUnit.Framework.Internal.Randomizer.InitialSeed);
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
            Randomizer.InitialSeed = context.CurrentTest.Seed;
            typeof(TestExecutionContext)
                .GetField("_randomGenerator", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SetValue(context, null);
        }
    }
}