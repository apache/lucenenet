﻿using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Codecs.Compressing
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using BaseTermVectorsFormatTestCase = Lucene.Net.Index.BaseTermVectorsFormatTestCase;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TextField = TextField;

    [TestFixture]
    public class TestCompressingTermVectorsFormat : BaseTermVectorsFormatTestCase
    {
        protected override Codec GetCodec()
        {
            return CompressingCodec.RandomInstance(Random);
        }

        // https://issues.apache.org/jira/browse/LUCENE-5156
        [Test]
        public virtual void TestNoOrds()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.StoreTermVectors = true;
            doc.Add(new Field("foo", "this is a test", ft));
            iw.AddDocument(doc);
            AtomicReader ir = GetOnlySegmentReader(iw.GetReader());
            Terms terms = ir.GetTermVector(0, "foo");
            Assert.IsNotNull(terms);
            TermsEnum termsEnum = terms.GetEnumerator();
            Assert.AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("this")));
            try
            {
                var _ = termsEnum.Ord;
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsUnsupportedOperationException())
            {
                // expected exception
            }

            try
            {
                termsEnum.SeekExact(0);
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsUnsupportedOperationException())
            {
                // expected exception
            }
            ir.Dispose();
            iw.Dispose();
            dir.Dispose();
        }
    }
}