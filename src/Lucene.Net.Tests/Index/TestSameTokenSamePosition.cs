using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using NUnit.Framework;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TextField = TextField;
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
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir);
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
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir);
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
        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly int tokenCount = 4;
        private int nextTokenIndex = 0;
        private readonly string[] terms = new string[] { "six", "six", "drunken", "drunken" };
        private readonly int[] starts = new int[] { 0, 0, 4, 4 };
        private readonly int[] ends = new int[] { 3, 3, 11, 11 };
        private readonly int[] incs = new int[] { 1, 0, 1, 0 };

        public BugReproTokenStream()
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (nextTokenIndex < tokenCount)
            {
                termAtt.SetEmpty().Append(terms[nextTokenIndex]);
                offsetAtt.SetOffset(starts[nextTokenIndex], ends[nextTokenIndex]);
                posIncAtt.PositionIncrement = incs[nextTokenIndex];
                nextTokenIndex++;
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
            this.nextTokenIndex = 0;
        }
    }
}