using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestLRUCache
    {
        [Test]
        public void Test()
        {
            Lucene.Net.Util.Cache.SimpleLRUCache<string, string> cache = new Lucene.Net.Util.Cache.SimpleLRUCache<string, string>(3);
            cache.Put("a", "a");
            cache.Put("b", "b");
            cache.Put("c", "c");
            Assert.IsNotNull(cache.Get("a"));
            Assert.IsNotNull(cache.Get("b"));
            Assert.IsNotNull(cache.Get("c"));
            cache.Put("d", "d");
            Assert.IsNull(cache.Get("a"));
            Assert.IsNotNull(cache.Get("c"));
            cache.Put("e", "e");
            cache.Put("f", "f");
            Assert.IsNotNull(cache.Get("c"));
        }
    }
}