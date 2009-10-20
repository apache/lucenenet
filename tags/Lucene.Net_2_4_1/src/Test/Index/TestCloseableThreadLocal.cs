using NUnit.Framework;

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using CloseableThreadLocal = Lucene.Net.Util.CloseableThreadLocal;

namespace Lucene.Net.Index
{
    [TestFixture]
    public class TestCloseableThreadLocal
    {
        public const string TEST_VALUE = "initvaluetest";

        [Test]
        public void TestInitValue()
        {
            InitValueThreadLocal tl = new InitValueThreadLocal();
            string str = (string)tl.Get();
            Assert.AreEqual(TEST_VALUE, str);
        }

        public class InitValueThreadLocal : CloseableThreadLocal
        {
            override protected object InitialValue()
            {
                return TEST_VALUE;
            }
        }
    }
}
