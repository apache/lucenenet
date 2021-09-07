using Lucene.Net.Analysis.Core;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Tests the functionality of {@link DocMaker}.
    /// </summary>
    public class DocMakerTest : BenchmarkTestCase
    {
        public sealed class OneDocSource : ContentSource
        {
            private bool finish = false;

            protected override void Dispose(bool disposing)
            {
            }

            public override DocData GetNextDocData(DocData docData)
            {
                if (finish)
                {
                    throw new NoMoreDataException();
                }

                docData.Body = ("body");
                docData.SetDate("date");
                docData.Title = ("title");
                Dictionary<string, string> props = new Dictionary<string, string>();
                props["key"] = "value";
                docData.Props = props;
                finish = true;

                return docData;
            }
        }

        private void doTestIndexProperties(bool setIndexProps,
            bool indexPropsVal, int numExpectedResults)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            // Indexing configuration.
            props["analyzer"] = typeof(WhitespaceAnalyzer).AssemblyQualifiedName;
            props["content.source"] = typeof(OneDocSource).AssemblyQualifiedName;
            props["directory"] = "RAMDirectory";
            if (setIndexProps)
            {
                props["doc.index.props"] = indexPropsVal.ToString();
            }

            // Create PerfRunData
            Config config = new Config(props);
            PerfRunData runData = new PerfRunData(config);

            TaskSequence tasks = new TaskSequence(runData, TestName, null, false);
            tasks.AddTask(new CreateIndexTask(runData));
            tasks.AddTask(new AddDocTask(runData));
            tasks.AddTask(new CloseIndexTask(runData));
            tasks.DoLogic();

            IndexReader reader = DirectoryReader.Open(runData.Directory);
            IndexSearcher searcher = NewSearcher(reader);
            TopDocs td = searcher.Search(new TermQuery(new Term("key", "value")), 10);
            assertEquals(numExpectedResults, td.TotalHits);
            reader.Dispose();
        }

        private Document createTestNormsDocument(bool setNormsProp,
            bool normsPropVal, bool setBodyNormsProp, bool bodyNormsVal)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            // Indexing configuration.
            props["analyzer"] = typeof(WhitespaceAnalyzer).AssemblyQualifiedName;
            props["directory"] = "RAMDirectory";
            if (setNormsProp)
            {
                props["doc.tokenized.norms"] = normsPropVal.ToString();
            }
            if (setBodyNormsProp)
            {
                props["doc.body.tokenized.norms"] = bodyNormsVal.ToString();
            }

            // Create PerfRunData
            Config config = new Config(props);

            DocMaker dm = new DocMaker();
            dm.SetConfig(config, new OneDocSource());
            return dm.MakeDocument();
        }

        /* Tests doc.index.props property. */
        [Test]
        public void TestIndexProperties()
        {
            // default is to not index properties.
            doTestIndexProperties(false, false, 0);

            // set doc.index.props to false.
            doTestIndexProperties(true, false, 0);

            // set doc.index.props to true.
            doTestIndexProperties(true, true, 1);
        }

        /* Tests doc.tokenized.norms and doc.body.tokenized.norms properties. */
        [Test]
        public void TestNorms()
        {

            Document doc;

            // Don't set anything, use the defaults
            doc = createTestNormsDocument(false, false, false, false);
            assertTrue(doc.GetField(DocMaker.TITLE_FIELD).IndexableFieldType.OmitNorms);
            assertFalse(doc.GetField(DocMaker.BODY_FIELD).IndexableFieldType.OmitNorms);

            // Set norms to false
            doc = createTestNormsDocument(true, false, false, false);
            assertTrue(doc.GetField(DocMaker.TITLE_FIELD).IndexableFieldType.OmitNorms);
            assertFalse(doc.GetField(DocMaker.BODY_FIELD).IndexableFieldType.OmitNorms);

            // Set norms to true
            doc = createTestNormsDocument(true, true, false, false);
            assertFalse(doc.GetField(DocMaker.TITLE_FIELD).IndexableFieldType.OmitNorms);
            assertFalse(doc.GetField(DocMaker.BODY_FIELD).IndexableFieldType.OmitNorms);

            // Set body norms to false
            doc = createTestNormsDocument(false, false, true, false);
            assertTrue(doc.GetField(DocMaker.TITLE_FIELD).IndexableFieldType.OmitNorms);
            assertTrue(doc.GetField(DocMaker.BODY_FIELD).IndexableFieldType.OmitNorms);

            // Set body norms to true
            doc = createTestNormsDocument(false, false, true, true);
            assertTrue(doc.GetField(DocMaker.TITLE_FIELD).IndexableFieldType.OmitNorms);
            assertFalse(doc.GetField(DocMaker.BODY_FIELD).IndexableFieldType.OmitNorms);
        }

        [Test]
        public void TestDocMakerLeak()
        {
            // DocMaker did not close its ContentSource if resetInputs was called twice,
            // leading to a file handle leak.
            FileInfo f = new FileInfo(Path.Combine(getWorkDir().FullName, "docMakerLeak.txt"));
            TextWriter ps = new StreamWriter(new FileStream(f.FullName, FileMode.Create, FileAccess.Write), Encoding.UTF8);
            ps.WriteLine("one title\t" + (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + "\tsome content"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            ps.Dispose();

            Dictionary<string, string> props = new Dictionary<string, string>();
            props["docs.file"] = f.FullName;
            props["content.source.forever"] = "false";
            Config config = new Config(props);

            ContentSource source = new LineDocSource();
            source.SetConfig(config);

            DocMaker dm = new DocMaker();
            dm.SetConfig(config, source);
            dm.ResetInputs();
            dm.ResetInputs();
            dm.Dispose();
        }
    }
}
