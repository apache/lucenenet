using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Index
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;

    internal class RepeatingTokenizer : Tokenizer
    {
        private readonly Random random;
        private readonly float percentDocs;
        private readonly int maxTf;
        private int num;
        internal ICharTermAttribute termAtt;
        internal string value;

        public RepeatingTokenizer(TextReader reader, string val, Random random, float percentDocs, int maxTF)
            : base(reader)
        {
            this.value = val;
            this.random = random;
            this.percentDocs = percentDocs;
            this.maxTf = maxTF;
            this.termAtt = AddAttribute<ICharTermAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            num--;
            if (num >= 0)
            {
                ClearAttributes();
                termAtt.Append(value);
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            if (random.NextDouble() < percentDocs)
            {
                num = random.Next(maxTf) + 1;
            }
            else
            {
                num = 0;
            }
        }
    }

    [TestFixture]
    public class TestTermdocPerf : LuceneTestCase
    {
        internal virtual void AddDocs(Random random, Directory dir, int ndocs, string field, string val, int maxTF, float percentDocs)
        {
            Analyzer analyzer = new AnalyzerAnonymousClass(random, val, maxTF, percentDocs);

            Document doc = new Document();

            doc.Add(NewStringField(field, val, Field.Store.NO));
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(100).SetMergePolicy(NewLogMergePolicy(100)));

            for (int i = 0; i < ndocs; i++)
            {
                writer.AddDocument(doc);
            }

            writer.ForceMerge(1);
            writer.Dispose();
        }

        private sealed class AnalyzerAnonymousClass : Analyzer
        {
            private readonly Random random;
            private readonly string val;
            private readonly int maxTf;
            private readonly float percentDocs;

            public AnalyzerAnonymousClass(Random random, string val, int maxTF, float percentDocs)
            {
                this.random = random;
                this.val = val;
                this.maxTf = maxTF;
                this.percentDocs = percentDocs;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new RepeatingTokenizer(reader, val, random, percentDocs, maxTf));
            }
        }

        public virtual int DoTest(int iter, int ndocs, int maxTF, float percentDocs)
        {
            Directory dir = NewDirectory();

            long start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            AddDocs(LuceneTestCase.Random, dir, ndocs, "foo", "val", maxTF, percentDocs);
            long end = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            if (Verbose)
            {
                Console.WriteLine("milliseconds for creation of " + ndocs + " docs = " + (end - start));
            }

            IndexReader reader = DirectoryReader.Open(dir);

            TermsEnum tenum = MultiFields.GetTerms(reader, "foo").GetEnumerator();

            start = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

            int ret = 0;
            DocsEnum tdocs = null;
            Random random = new J2N.Randomizer(Random.NextInt64());
            for (int i = 0; i < iter; i++)
            {
                tenum.SeekCeil(new BytesRef("val"));
                tdocs = TestUtil.Docs(random, tenum, MultiFields.GetLiveDocs(reader), tdocs, DocsFlags.NONE);
                while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    ret += tdocs.DocID;
                }
            }

            end = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            if (Verbose)
            {
                Console.WriteLine("milliseconds for " + iter + " TermDocs iteration: " + (end - start));
            }

            return ret;
        }

        [Test]
        [Slow]
        [Nightly] // LUCENENET: Since this is more of a benchmark than a test, moving to Nightly to keep us from buring testing time on it
        public virtual void TestTermDocPerf()
        {
            // performance test for 10% of documents containing a term
            DoTest(100000, 10000, 3, .1f);
        }
    }
}