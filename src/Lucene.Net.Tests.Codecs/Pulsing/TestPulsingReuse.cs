using J2N.Runtime.CompilerServices;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.NestedPulsing;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Tests that pulsing codec reuses its enums and wrapped enums
    /// </summary>
    public class TestPulsingReuse : LuceneTestCase
    {
        // TODO: this is a basic test. this thing is complicated, add more
        [Test]
        public virtual void TestSophisticatedReuse()
        {
            // we always run this test with pulsing codec.
            Codec cp = TestUtil.AlwaysPostingsFormat(new Pulsing41PostingsFormat(1));
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));
            Document doc = new Document();
            doc.Add(new TextField("foo", "a b b c c c d e f g g h i i j j k", Field.Store.NO));
            iw.AddDocument(doc);
            DirectoryReader ir = iw.GetReader();
            iw.Dispose();

            AtomicReader segment = GetOnlySegmentReader(ir);
            DocsEnum reuse = null;
            IDictionary<DocsEnum, bool> allEnums = new JCG.Dictionary<DocsEnum, bool>(IdentityEqualityComparer<DocsEnum>.Default);
            TermsEnum te = segment.GetTerms("foo").GetEnumerator();
            while (te.MoveNext())
            {
                reuse = te.Docs(null, reuse, DocsFlags.NONE);
                allEnums[reuse] = true;
            }

            assertEquals(2, allEnums.Count);

            allEnums.Clear();
            DocsAndPositionsEnum posReuse = null;
            te = segment.GetTerms("foo").GetEnumerator();
            while (te.MoveNext())
            {
                posReuse = te.DocsAndPositions(null, posReuse);
                allEnums[posReuse] = true;
            }

            assertEquals(2, allEnums.Count);

            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// tests reuse with Pulsing1(Pulsing2(Standard)) </summary>
        [Test]
        public virtual void TestNestedPulsing()
        {
            // we always run this test with pulsing codec.
            Codec cp = TestUtil.AlwaysPostingsFormat(new NestedPulsingPostingsFormat());
            BaseDirectoryWrapper dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(cp));
            Document doc = new Document();
            doc.Add(new TextField("foo", "a b b c c c d e f g g g h i i j j k l l m m m", Field.Store.NO));
            // note: the reuse is imperfect, here we would have 4 enums (lost reuse when we get an enum for 'm')
            // this is because we only track the 'last' enum we reused (not all).
            // but this seems 'good enough' for now.
            iw.AddDocument(doc);
            DirectoryReader ir = iw.GetReader();
            iw.Dispose();

            AtomicReader segment = GetOnlySegmentReader(ir);
            DocsEnum reuse = null;
            IDictionary<DocsEnum, bool> allEnums = new JCG.Dictionary<DocsEnum, bool>(IdentityEqualityComparer<DocsEnum>.Default);
            TermsEnum te = segment.GetTerms("foo").GetEnumerator();
            while (te.MoveNext())
            {
                reuse = te.Docs(null, reuse, DocsFlags.NONE);
                allEnums[reuse] = true;
            }

            assertEquals(4, allEnums.Count);

            allEnums.Clear();
            DocsAndPositionsEnum posReuse = null;
            te = segment.GetTerms("foo").GetEnumerator();
            while (te.MoveNext())
            {
                posReuse = te.DocsAndPositions(null, posReuse);
                allEnums[posReuse] = true;
            }

            assertEquals(4, allEnums.Count);

            ir.Dispose();
            dir.Dispose();
        }
    }
}