using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestVersionComparator : LuceneTestCase
    {
        [Test]
        public void TestVersions()
        {
            var comp = StringHelper.VersionComparator;
            Assert.IsTrue(comp.Compare("1", "2") < 0);
            Assert.IsTrue(comp.Compare("1", "1") == 0);
            Assert.IsTrue(comp.Compare("2", "1") > 0);

            Assert.IsTrue(comp.Compare("1.1", "1") > 0);
            Assert.IsTrue(comp.Compare("1", "1.1") < 0);
            Assert.IsTrue(comp.Compare("1.1", "1.1") == 0);

            Assert.IsTrue(comp.Compare("1.0", "1") == 0);
            Assert.IsTrue(comp.Compare("1", "1.0") == 0);
            Assert.IsTrue(comp.Compare("1.0.1", "1.0") > 0);
            Assert.IsTrue(comp.Compare("1.0", "1.0.1") < 0);

            Assert.IsTrue(comp.Compare("1.02.003", "1.2.3.0") == 0);
            Assert.IsTrue(comp.Compare("1.2.3.0", "1.02.003") == 0);

            Assert.IsTrue(comp.Compare("1.10", "1.9") > 0);
            Assert.IsTrue(comp.Compare("1.9", "1.10") < 0);

            Assert.IsTrue(comp.Compare("0", "1.0") < 0);
            Assert.IsTrue(comp.Compare("00", "1.0") < 0);
            Assert.IsTrue(comp.Compare("-1.0", "1.0") < 0);
            Assert.IsTrue(comp.Compare("3.0", int.MinValue.ToString()) > 0);
        }
    }
}
