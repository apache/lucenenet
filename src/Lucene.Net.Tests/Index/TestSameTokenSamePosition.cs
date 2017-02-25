using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Index
{
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TextField = TextField;

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

    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [TestFixture]
    public class TestSameTokenSamePosition : LuceneTestCase
    {
        /// <summary>
        /// Attempt to reproduce an assertion error that happens
        /// only with the trunk version around April 2011.
        /// </summary>
        [Test]
        public virtual void Test()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(new TextField("eng", new BugReproTokenStream()));
            riw.AddDocument(doc);
            riw.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// Same as the above, but with more docs
        /// </summary>
        [Test]
        public virtual void TestMoreDocs()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                doc.Add(new TextField("eng", new BugReproTokenStream()));
                riw.AddDocument(doc);
            }
            riw.Dispose();
            dir.Dispose();
        }
    }

    internal sealed class BugReproTokenStream : TokenStream
    {
        private readonly ICharTermAttribute TermAtt;
        private readonly IOffsetAttribute OffsetAtt;
        private readonly IPositionIncrementAttribute PosIncAtt;
        private readonly int TokenCount = 4;
        private int NextTokenIndex = 0;
        private readonly string[] Terms = new string[] { "six", "six", "drunken", "drunken" };
        private readonly int[] Starts = new int[] { 0, 0, 4, 4 };
        private readonly int[] Ends = new int[] { 3, 3, 11, 11 };
        private readonly int[] Incs = new int[] { 1, 0, 1, 0 };

        public BugReproTokenStream()
        {
            TermAtt = AddAttribute<ICharTermAttribute>();
            OffsetAtt = AddAttribute<IOffsetAttribute>();
            PosIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (NextTokenIndex < TokenCount)
            {
                TermAtt.SetEmpty().Append(Terms[NextTokenIndex]);
                OffsetAtt.SetOffset(Starts[NextTokenIndex], Ends[NextTokenIndex]);
                PosIncAtt.PositionIncrement = Incs[NextTokenIndex];
                NextTokenIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            this.NextTokenIndex = 0;
        }
    }
}