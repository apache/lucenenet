using Lucene.Net.Cli.Commands;
using NUnit.Framework;

namespace Lucene.Net.Cli
{
    public class EnvironmentTest
    {
        [Test]
        public virtual void TestNotEnoughArgumentsResourceNotNull()
        {
            Assert.NotNull(CommandTestCase.FromResource("NotEnoughArguments"));
        }

        [Test]
        public virtual void TestNotEnoughArgumentsResourceNotEmpty()
        {
            Assert.IsNotEmpty(CommandTestCase.FromResource("NotEnoughArguments"));
        }
    }
}
