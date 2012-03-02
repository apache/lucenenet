using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestCloseableThreadLocal
    {
        [Test]
        public void TestMemLeakage()
        {
            CloseableThreadLocalProfiler.EnableCloseableThreadLocalProfiler = true;

            int LoopCount = 100;
            Analyzer[] analyzers = new Analyzer[LoopCount];
            RAMDirectory[] dirs = new RAMDirectory[LoopCount];
            IndexWriter[] indexWriters = new IndexWriter[LoopCount];

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      analyzers[i] = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Version.LUCENE_CURRENT);
                                                                      dirs[i] = new RAMDirectory();
                                                                      indexWriters[i] = new IndexWriter(dirs[i], analyzers[i], true, IndexWriter.MaxFieldLength.UNLIMITED);
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      Document document = new Document();
                                                                      document.Add(new Field("field", "some test", Field.Store.NO, Field.Index.ANALYZED));
                                                                      indexWriters[i].AddDocument(document);
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      analyzers[i].Close();
                                                                      indexWriters[i].Close();
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) =>
                                                                  {
                                                                      IndexSearcher searcher = new IndexSearcher(dirs[i]);
                                                                      TopDocs d = searcher.Search(new TermQuery(new Term("field", "test")), 10);
                                                                      searcher.Close();
                                                                  });

            System.Threading.Tasks.Parallel.For(0, LoopCount, (i) => dirs[i].Close());

            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();

            int aliveObjects = 0;
            foreach (WeakReference w in CloseableThreadLocalProfiler.Instances)
            {
                object o = w.Target;
                if (o != null) aliveObjects++;
            }

            CloseableThreadLocalProfiler.EnableCloseableThreadLocalProfiler = false;

            Assert.AreEqual(0, aliveObjects);
        }
    }
}