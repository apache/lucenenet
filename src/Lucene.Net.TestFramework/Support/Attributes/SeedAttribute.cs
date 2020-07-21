using System;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SeedAttribute : Attribute, IWrapSetUpTearDown
{
    public SeedAttribute(int randomSeed)
    {
        RandomSeed = randomSeed;
    }

    public int RandomSeed { get; }

    public TestCommand Wrap(TestCommand command)
    {
        return new SeedCommand(command, RandomSeed);
    }

    private sealed class SeedCommand : DelegatingTestCommand
    {
        private readonly int randomSeed;

        public SeedCommand(TestCommand innerCommand, int randomSeed) : base(innerCommand)
        {
            this.randomSeed = randomSeed;
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            ResetRandomSeed(context, randomSeed);
            try
            {
                return innerCommand.Execute(context);
            }
            finally
            {
                if (context.CurrentTest.Seed != randomSeed)
                    throw new InvalidOperationException($"{nameof(SeedAttribute)} cannot be used together with an attribute or test that changes the seed.");
            }
        }
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