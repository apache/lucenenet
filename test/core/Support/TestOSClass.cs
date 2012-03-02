using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestOSClass
    {
        // LUCENENET-216
        [Test]
        public void TestFSDirectorySync()
        {
            System.IO.DirectoryInfo path = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", ""), "testsync"));
            Lucene.Net.Store.Directory directory = new Lucene.Net.Store.SimpleFSDirectory(path, null);
            try
            {
                Lucene.Net.Store.IndexOutput io = directory.CreateOutput("syncfile");
                io.Close();
                directory.Sync("syncfile");
            }
            finally
            {
                directory.Close();
                Lucene.Net.Util._TestUtil.RmDir(path);
            }
        }
    }
}