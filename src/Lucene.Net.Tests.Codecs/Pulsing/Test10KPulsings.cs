using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Codecs.Pulsing
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
    /// Pulses 10k terms/docs, 
    /// originally designed to find JRE bugs (https://issues.apache.org/jira/browse/LUCENE-3335)
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    [Nightly]
    public class Test10KPulsings : LuceneTestCase
    {
        [Test]
        public virtual void Test10kPulsed()
        {
            // we always run this test with pulsing codec.
            Codec cp = TestUtil.AlwaysPostingsFormat(new Pulsing41PostingsFormat(1));

            DirectoryInfo f = CreateTempDir("10kpulsed");
            BaseDirectoryWrapper dir = NewFSDirectory(f);
            dir.CheckIndexOnDispose = false; // we do this ourselves explicitly
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));

            Document document = new Document();
            FieldType ft = new FieldType(TextField.TYPE_STORED);

            switch (TestUtil.NextInt32(Random, 0, 2))
            {
                case 0:
                    ft.IndexOptions = IndexOptions.DOCS_ONLY;
                    break;
                case 1:
                    ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
                    break;
                default:
                    ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    break;
            }

            Field field = NewField("field", "", ft);
            document.Add(field);

            //NumberFormat df = new DecimalFormat("00000", new DecimalFormatSymbols(Locale.ROOT));  // LUCENENET specific:  Use .ToString formating instead

            for (int i = 0; i < 10050; i++)
            {
                //field.StringValue = df.format(i);
                field.SetStringValue(i.ToString("00000", CultureInfo.InvariantCulture));
                iw.AddDocument(document);
            }

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            TermsEnum te = MultiFields.GetTerms(ir, "field").GetEnumerator();
            DocsEnum de = null;

            for (int i = 0; i < 10050; i++)
            {
                //string expected = df.format(i);
                string expected = i.ToString("00000", CultureInfo.InvariantCulture);
                te.MoveNext();
                assertEquals(expected, te.Term.Utf8ToString());
                de = TestUtil.Docs(Random, te, null, de, DocsFlags.NONE);
                assertTrue(de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                assertEquals(DocIdSetIterator.NO_MORE_DOCS, de.NextDoc());
            }
            ir.Dispose();

            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }

        /// <summary>
        /// a variant, that uses pulsing, but uses a high TF to force pass thru to the underlying codec
        /// </summary>
        [Test]
        public virtual void Test10kNotPulsed()
        {
            // we always run this test with pulsing codec.
            int freqCutoff = TestUtil.NextInt32(Random, 1, 10);
            Codec cp = TestUtil.AlwaysPostingsFormat(new Pulsing41PostingsFormat(freqCutoff));

            DirectoryInfo f = CreateTempDir("10knotpulsed");
            BaseDirectoryWrapper dir = NewFSDirectory(f);
            dir.CheckIndexOnDispose = false; // we do this ourselves explicitly
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));

            Document document = new Document();
            FieldType ft = new FieldType(TextField.TYPE_STORED);

            switch (TestUtil.NextInt32(Random, 0, 2))
            {
                case 0:
                    ft.IndexOptions = IndexOptions.DOCS_ONLY;
                    break;
                case 1:
                    ft.IndexOptions = IndexOptions.DOCS_AND_FREQS;
                    break;
                default:
                    ft.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    break;
            }

            Field field = NewField("field", "", ft);
            document.Add(field);

            //NumberFormat df = new DecimalFormat("00000", new DecimalFormatSymbols(Locale.ROOT));

            int freq = freqCutoff + 1;

            for (int i = 0; i < 10050; i++)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < freq; j++)
                {
                    //sb.Append(df.format(i));
                    sb.Append(i.ToString("00000", CultureInfo.InvariantCulture));
                    sb.Append(' '); // whitespace
                }
                field.SetStringValue(sb.ToString());
                iw.AddDocument(document);
            }

            IndexReader ir = iw.GetReader();
            iw.Dispose();

            TermsEnum te = MultiFields.GetTerms(ir, "field").GetEnumerator();
            DocsEnum de = null;

            for (int i = 0; i < 10050; i++)
            {
                //string expected = df.format(i);
                string expected = i.ToString("00000", CultureInfo.InvariantCulture);
                assertTrue(te.MoveNext());
                assertEquals(expected, te.Term.Utf8ToString());
                de = TestUtil.Docs(Random, te, null, de, DocsFlags.NONE);
                assertTrue(de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                assertEquals(DocIdSetIterator.NO_MORE_DOCS, de.NextDoc());
            }
            ir.Dispose();

            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }
    }
}