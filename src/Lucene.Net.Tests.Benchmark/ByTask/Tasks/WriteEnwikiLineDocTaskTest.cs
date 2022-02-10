using J2N.Text;
using J2N.Threading.Atomic;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Tests the functionality of {@link WriteEnwikiLineDocTask}.
    /// </summary>
    public class WriteEnwikiLineDocTaskTest : BenchmarkTestCase
    {
        // class has to be public so that Class.forName.newInstance() will work
        /** Interleaves category docs with regular docs */
        public sealed class WriteLineCategoryDocMaker : DocMaker
        {

            AtomicInt32 flip = new AtomicInt32(0);

            public override Document MakeDocument()
            {
                bool isCategory = (flip.IncrementAndGet() % 2 == 0);
                Document doc = new Document();
                doc.Add(new StringField(BODY_FIELD, "body text", Field.Store.NO));
                doc.Add(new StringField(TITLE_FIELD, isCategory ? "Category:title text" : "title text", Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date text", Field.Store.NO));
                return doc;
            }

        }

        private PerfRunData createPerfRunData(FileInfo file, String docMakerName)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["doc.maker"] = docMakerName;
            props["line.file.out"] = file.FullName;
            props["directory"] = "RAMDirectory"; // no accidental FS dir.
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        private void doReadTest(FileInfo file, String expTitle,
                                String expDate, String expBody)
        {
            doReadTest(2, file, expTitle, expDate, expBody);
            FileInfo categoriesFile = WriteEnwikiLineDocTask.CategoriesLineFile(file);
            doReadTest(2, categoriesFile, "Category:" + expTitle, expDate, expBody);
        }

        private void doReadTest(int n, FileInfo file, String expTitle, String expDate, String expBody)
        {
            Stream @in = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
            TextReader br = new StreamReader(@in, Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                WriteLineDocTaskTest.assertHeaderLine(line);
                for (int i = 0; i < n; i++)
                {
                    line = br.ReadLine();
                    assertNotNull(line);
                    String[] parts = line.Split(WriteLineDocTask.SEP).TrimEnd();
                    int numExpParts = expBody is null ? 2 : 3;
                    assertEquals(numExpParts, parts.Length);
                    assertEquals(expTitle, parts[0]);
                    assertEquals(expDate, parts[1]);
                    if (expBody != null)
                    {
                        assertEquals(expBody, parts[2]);
                    }
                }
                assertNull(br.ReadLine());
            }
            finally
            {
                br.Dispose();
            }
        }

        [Test]
        public void TestCategoryLines()
        {
            // WriteLineDocTask replaced only \t characters w/ a space, since that's its
            // separator char. However, it didn't replace newline characters, which
            // resulted in errors in LineDocSource.
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "two-lines-each.txt"));
            PerfRunData runData = createPerfRunData(file, typeof(WriteLineCategoryDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteEnwikiLineDocTask(runData);
            for (int i = 0; i < 4; i++)
            { // four times so that each file should have 2 lines. 
                wldt.DoLogic();
            }
            wldt.Dispose();


            doReadTest(file, "title text", "date text", "body text");
        }
    }
}
