using System;
using System.Linq;
using System.Reflection;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestFilterAtomicReader : LuceneTestCase
    {
        private class TestReader : FilterAtomicReader
        {
            /// <summary>
            /// Filter that only permits terms containing 'e'. </summary>
            private class TestFields : FilterFields
            {
                internal TestFields(Fields @in)
                    : base(@in)
                {
                }

                public override Terms Terms(string field)
                {
                    return new TestTerms(base.Terms(field));
                }
            }

            private class TestTerms : FilterTerms
            {
                internal TestTerms(Terms @in)
                    : base(@in)
                {
                }

                public override TermsEnum Iterator(TermsEnum reuse)
                {
                    return new TestTermsEnum(base.Iterator(reuse));
                }
            }

            private class TestTermsEnum : FilterTermsEnum
            {
                public TestTermsEnum(TermsEnum @in)
                    : base(@in)
                {
                }

                /// <summary>
                /// Scan for terms containing the letter 'e'. </summary>
                public override BytesRef Next()
                {
                    BytesRef text;
                    while ((text = @in.Next()) != null)
                    {
                        if (text.Utf8ToString().IndexOf('e') != -1)
                        {
                            return text;
                        }
                    }
                    return null;
                }

                public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
                {
                    return new TestPositions(base.DocsAndPositions(liveDocs, reuse == null ? null : ((FilterDocsAndPositionsEnum)reuse).DocsEnumIn_Nunit(), flags));
                }
            }

            /// <summary>
            /// Filter that only returns odd numbered documents. </summary>
            private class TestPositions : FilterDocsAndPositionsEnum
            {
                public TestPositions(DocsAndPositionsEnum @in)
                    : base(@in)
                {
                }

                /// <summary>
                /// Scan for odd numbered documents. </summary>
                public override int NextDoc()
                {
                    int doc;
                    while ((doc = @in.NextDoc()) != NO_MORE_DOCS)
                    {
                        if ((doc % 2) == 1)
                        {
                            return doc;
                        }
                    }
                    return NO_MORE_DOCS;
                }
            }

            public TestReader(IndexReader reader)
                : base(SlowCompositeReaderWrapper.Wrap(reader))
            {
            }

            public override Fields Fields
            {
                get { return new TestFields(base.Fields); }
            }
        }

        /// <summary>
        /// Tests the IndexReader.getFieldNames implementation </summary>
        /// <exception cref="Exception"> on error </exception>
        [Test]
        public virtual void TestFilterIndexReader()
        {
            Directory directory = NewDirectory();

            IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));

            Document d1 = new Document();
            d1.Add(NewTextField("default", "one two", Field.Store.YES));
            writer.AddDocument(d1);

            Document d2 = new Document();
            d2.Add(NewTextField("default", "one three", Field.Store.YES));
            writer.AddDocument(d2);

            Document d3 = new Document();
            d3.Add(NewTextField("default", "two four", Field.Store.YES));
            writer.AddDocument(d3);

            writer.Dispose();

            Directory target = NewDirectory();

            // We mess with the postings so this can fail:
            ((BaseDirectoryWrapper)target).CrossCheckTermVectorsOnClose = false;

            writer = new IndexWriter(target, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            IndexReader reader = new TestReader(DirectoryReader.Open(directory));
            writer.AddIndexes(reader);
            writer.Dispose();
            reader.Dispose();
            reader = DirectoryReader.Open(target);

            TermsEnum terms = MultiFields.GetTerms(reader, "default").Iterator(null);
            while (terms.Next() != null)
            {
                Assert.IsTrue(terms.Term().Utf8ToString().IndexOf('e') != -1);
            }

            Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(new BytesRef("one")));

            DocsAndPositionsEnum positions = terms.DocsAndPositions(MultiFields.GetLiveDocs(reader), null);
            while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                Assert.IsTrue((positions.DocID() % 2) == 1);
            }

            reader.Dispose();
            directory.Dispose();
            target.Dispose();
        }

        private static void CheckOverrideMethods(Type clazz)
        {
            Type superClazz = clazz.GetTypeInfo().BaseType;
            foreach (MethodInfo m in superClazz.GetMethods())
            {
                if (m.IsStatic || m.IsAbstract || m.IsFinal || /*m.Synthetic ||*/ m.Name.Equals("Attributes"))
                {
                    continue;
                }
                // The point of these checks is to ensure that methods that have a default
                // impl through other methods are not overridden. this makes the number of
                // methods to override to have a working impl minimal and prevents from some
                // traps: for example, think about having getCoreCacheKey delegate to the
                // filtered impl by default
                MethodInfo subM = clazz.GetMethod(m.Name, m.GetParameters().Select(p => p.ParameterType).ToArray());
                if (subM.DeclaringType == clazz && m.DeclaringType != typeof(object) && m.DeclaringType != subM.DeclaringType)
                {
                    Assert.Fail(clazz + " overrides " + m + " although it has a default impl");
                }
            }
        }

        [Test]
        public virtual void TestOverrideMethods()
        {
            CheckOverrideMethods(typeof(FilterAtomicReader));
            CheckOverrideMethods(typeof(FilterAtomicReader.FilterFields));
            CheckOverrideMethods(typeof(FilterAtomicReader.FilterTerms));
            CheckOverrideMethods(typeof(FilterAtomicReader.FilterTermsEnum));
            CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsEnum));
            CheckOverrideMethods(typeof(FilterAtomicReader.FilterDocsAndPositionsEnum));
        }
    }
}