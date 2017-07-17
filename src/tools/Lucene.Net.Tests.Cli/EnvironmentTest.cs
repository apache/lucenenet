using Lucene.Net.Attributes;
using Lucene.Net.Cli.Commands;
using NUnit.Framework;

namespace Lucene.Net.Cli
{
    public class EnvironmentTest
    {
        [Test]
        [LuceneNetSpecific]
        public virtual void TestNotEnoughArgumentsResourceNotNull()
        {
            Assert.NotNull(CommandTestCase.FromResource("NotEnoughArguments"));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestNotEnoughArgumentsResourceNotEmpty()
        {
            Assert.IsNotEmpty(CommandTestCase.FromResource("NotEnoughArguments"));
        }
    }
}
