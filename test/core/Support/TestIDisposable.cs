using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestIDisposable
    {
        [Test]
        public void TestReadersWriters()
        {
            Directory dir;
            
            using(dir = new RAMDirectory())
            {
                Document doc;
                IndexWriter writer;
                IndexReader reader;

                using (writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    Field field = new Field("name", "value", Field.Store.YES,Field.Index.ANALYZED);
                    doc = new Document();
                    doc.Add(field);
                    writer.AddDocument(doc);
                    writer.Commit();

                    using (reader = writer.GetReader())
                    {
                        IndexReader r1 =  reader.Reopen();
                    }

                    try
                    {
                        IndexReader r2 = reader.Reopen();
                        Assert.Fail("IndexReader shouldn't be open here");
                    }
                    catch (AlreadyClosedException)
                    {
                    }
                }
                try
                {
                    writer.AddDocument(doc);
                    Assert.Fail("IndexWriter shouldn't be open here");
                }
                catch (AlreadyClosedException)
                {
                }

                Assert.IsTrue(dir.isOpen_ForNUnit, "RAMDirectory");
            }
            Assert.IsFalse(dir.isOpen_ForNUnit, "RAMDirectory");
        }
    }
}