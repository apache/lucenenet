using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;

namespace Lucene.Net.Index
{
    using System.IO;

    /*
        /// Copyright 2006 The Apache Software Foundation
        ///
        /// Licensed under the Apache License, Version 2.0 (the "License");
        /// you may not use this file except in compliance with the License.
        /// You may obtain a copy of the License at
        ///
        ///     http://www.apache.org/licenses/LICENSE-2.0
        ///
        /// Unless required by applicable law or agreed to in writing, software
        /// distributed under the License is distributed on an "AS IS" BASIS,
        /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        /// See the License for the specific language governing permissions and
        /// limitations under the License.
        */

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;

    internal class RepeatingTokenizer : Tokenizer
    {
        private readonly Random Random;
        private readonly float PercentDocs;
        private readonly int MaxTF;
        private int Num;
        internal ICharTermAttribute TermAtt;
        internal string Value;

        public RepeatingTokenizer(TextReader reader, string val, Random random, float percentDocs, int maxTF)
            : base(reader)
        {
            this.Value = val;
            this.Random = random;
            this.PercentDocs = percentDocs;
            this.MaxTF = maxTF;
            this.TermAtt = AddAttribute<ICharTermAttribute>();
        }

        public sealed override bool IncrementToken()
        {
            Num--;
            if (Num >= 0)
            {
                ClearAttributes();
                TermAtt.Append(Value);
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            if (Random.NextDouble() < PercentDocs)
            {
                Num = Random.Next(MaxTF) + 1;
            }
            else
            {
                Num = 0;
            }
        }
    }

    [TestFixture]
    public class TestTermdocPerf : LuceneTestCase
    {
        internal virtual void AddDocs(Random random, Directory dir, int ndocs, string field, string val, int maxTF, float percentDocs)
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(random, val, maxTF, percentDocs);

            Document doc = new Document();

            doc.Add(NewStringField(field, val, Field.Store.NO));
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode_e.CREATE).SetMaxBufferedDocs(100).SetMergePolicy(NewLogMergePolicy(100)));

            for (int i = 0; i < ndocs; i++)
            {
                writer.AddDocument(doc);
            }

            writer.ForceMerge(1);
            writer.Dispose();
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private Random Random;
            private string Val;
            private int MaxTF;
            private float PercentDocs;

            public AnalyzerAnonymousInnerClassHelper(Random random, string val, int maxTF, float percentDocs)
            {
                this.Random = random;
                this.Val = val;
                this.MaxTF = maxTF;
                this.PercentDocs = percentDocs;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new RepeatingTokenizer(reader, Val, Random, PercentDocs, MaxTF));
            }
        }

        public virtual int DoTest(int iter, int ndocs, int maxTF, float percentDocs)
        {
            Directory dir = NewDirectory();

            long start = Environment.TickCount;
            AddDocs(Random(), dir, ndocs, "foo", "val", maxTF, percentDocs);
            long end = Environment.TickCount;
            if (VERBOSE)
            {
                Console.WriteLine("milliseconds for creation of " + ndocs + " docs = " + (end - start));
            }

            IndexReader reader = DirectoryReader.Open(dir);

            TermsEnum tenum = MultiFields.GetTerms(reader, "foo").Iterator(null);

            start = Environment.TickCount;

            int ret = 0;
            DocsEnum tdocs = null;
            Random random = new Random(Random().Next());
            for (int i = 0; i < iter; i++)
            {
                tenum.SeekCeil(new BytesRef("val"));
                tdocs = TestUtil.Docs(random, tenum, MultiFields.GetLiveDocs(reader), tdocs, DocsEnum.FLAG_NONE);
                while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    ret += tdocs.DocID();
                }
            }

            end = Environment.TickCount;
            if (VERBOSE)
            {
                Console.WriteLine("milliseconds for " + iter + " TermDocs iteration: " + (end - start));
            }

            return ret;
        }

        [Test, LongRunningTest, MaxTime(120000)]
        public virtual void TestTermDocPerf()
        {
            // performance test for 10% of documents containing a term
            DoTest(100000, 10000, 3, .1f);
        }
    }
}