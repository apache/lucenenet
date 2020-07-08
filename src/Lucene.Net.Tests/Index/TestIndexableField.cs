using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
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
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using Field = Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using Occur = Lucene.Net.Search.Occur;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TokenStream = Lucene.Net.Analysis.TokenStream;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestIndexableField : LuceneTestCase
    {
        private class MyField : IIndexableField
        {
            private readonly TestIndexableField outerInstance;

            internal readonly int counter;
            internal readonly IIndexableFieldType fieldType;

            public MyField()
            {
                fieldType = new IndexableFieldTypeAnonymousInnerClassHelper(this);
            }

            private class IndexableFieldTypeAnonymousInnerClassHelper : IIndexableFieldType
            {
                private MyField outerInstance;

                public IndexableFieldTypeAnonymousInnerClassHelper(MyField outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public bool IsIndexed
                {
                    get { return (outerInstance.counter % 10) != 3; }
                }

                public bool IsStored
                {
                    get { return (outerInstance.counter & 1) == 0 || (outerInstance.counter % 10) == 3; }
                }

                public bool IsTokenized
                {
                    get { return true; }
                }

                public bool StoreTermVectors
                {
                    get { return IsIndexed && outerInstance.counter % 2 == 1 && outerInstance.counter % 10 != 9; }
                }

                public bool StoreTermVectorOffsets
                {
                    get { return StoreTermVectors && outerInstance.counter % 10 != 9; }
                }

                public bool StoreTermVectorPositions
                {
                    get { return StoreTermVectors && outerInstance.counter % 10 != 9; }
                }

                public bool StoreTermVectorPayloads
                {
                    get
                    {
#pragma warning disable 612, 618
                        if (Codec.Default is Lucene3xCodec)
#pragma warning restore 612, 618
                        {
                            return false; // 3.x doesnt support
                        }
                        else
                        {
                            return StoreTermVectors && outerInstance.counter % 10 != 9;
                        }
                    }
                }

                public bool OmitNorms
                {
                    get { return false; }
                }

                public IndexOptions IndexOptions
                {
                    get { return Index.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; }
                }

                public DocValuesType DocValueType
                {
                    get { return DocValuesType.NONE; }
                }
            }

            public MyField(TestIndexableField outerInstance, int counter)
                : this()
            {
                this.outerInstance = outerInstance;
                this.counter = counter;
            }

            public string Name
            {
                get { return "f" + counter; }
            }

            public float Boost
            {
                get { return 1.0f + (float)Random.NextDouble(); }
            }

            public BytesRef GetBinaryValue()
            {
                if ((counter % 10) == 3)
                {
                    var bytes = new byte[10];
                    for (int idx = 0; idx < bytes.Length; idx++)
                    {
                        bytes[idx] = (byte)(counter + idx);
                    }
                    return new BytesRef(bytes, 0, bytes.Length);
                }
                else
                {
                    return null;
                }
            }

            public string GetStringValue()
            {
                int fieldID = counter % 10;
                if (fieldID != 3 && fieldID != 7)
                {
                    return "text " + counter;
                }
                else
                {
                    return null;
                }
            }

            // LUCENENET specific - created overload so we can format an underlying numeric type using specified provider
            public virtual string GetStringValue(IFormatProvider provider)
            {
                return GetStringValue();
            }

            // LUCENENET specific - created overload so we can format an underlying numeric type using specified format
            public virtual string GetStringValue(string format)
            {
                return GetStringValue();
            }

            // LUCENENET specific - created overload so we can format an underlying numeric type using specified format and provider
            public virtual string GetStringValue(string format, IFormatProvider provider)
            {
                return GetStringValue();
            }

            public TextReader GetReaderValue()
            {
                if (counter % 10 == 7)
                {
                    return new StringReader("text " + counter);
                }
                else
                {
                    return null;
                }
            }

            public object GetNumericValue()
            {
                return null;
            }

            // LUCENENET specific - Since we have no numeric reference types in .NET, this method was added to check
            // the numeric type of the inner field without boxing/unboxing.
            public virtual NumericFieldType NumericType
            {
                get { return NumericFieldType.NONE; }
            }

            // LUCENENET specific - created overload for Byte, since we have no Number class in .NET
            public virtual byte? GetByteValue()
            {
                return null;
            }

            // LUCENENET specific - created overload for Short, since we have no Number class in .NET
            public virtual short? GetInt16Value()
            {
                return null;
            }

            // LUCENENET specific - created overload for Int32, since we have no Number class in .NET
            public virtual int? GetInt32Value()
            {
                return null;
            }

            // LUCENENET specific - created overload for Int64, since we have no Number class in .NET
            public virtual long? GetInt64Value()
            {
                return null;
            }

            // LUCENENET specific - created overload for Single, since we have no Number class in .NET
            public virtual float? GetSingleValue()
            {
                return null;
            }

            // LUCENENET specific - created overload for Double, since we have no Number class in .NET
            public virtual double? GetDoubleValue()
            {
                return null;
            }

            public IIndexableFieldType IndexableFieldType
            {
                get { return fieldType; }
            }

            public TokenStream GetTokenStream(Analyzer analyzer)
            {
                return GetReaderValue() != null ? analyzer.GetTokenStream(Name, GetReaderValue()) : analyzer.GetTokenStream(Name, new StringReader(GetStringValue()));
            }
        }

        // Silly test showing how to index documents w/o using Lucene's core
        // Document nor Field class
        [Test]
        public virtual void TestArbitraryFields()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
#if FEATURE_INSTANCE_TESTDATA_INITIALIZATION
                this,
#endif
                Random, dir);

            int NUM_DOCS = AtLeast(27);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: " + NUM_DOCS + " docs");
            }
            int[] fieldsPerDoc = new int[NUM_DOCS];
            int baseCount = 0;

            for (int docCount = 0; docCount < NUM_DOCS; docCount++)
            {
                int fieldCount = TestUtil.NextInt32(Random, 1, 17);
                fieldsPerDoc[docCount] = fieldCount - 1;

                int finalDocCount = docCount;
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: " + fieldCount + " fields in doc " + docCount);
                }

                int finalBaseCount = baseCount;
                baseCount += fieldCount - 1;

                w.AddDocument(new IterableAnonymousInnerClassHelper(this, fieldCount, finalDocCount, finalBaseCount));
            }

            IndexReader r = w.GetReader();
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            int counter = 0;
            for (int id = 0; id < NUM_DOCS; id++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: verify doc id=" + id + " (" + fieldsPerDoc[id] + " fields) counter=" + counter);
                }
                TopDocs hits = s.Search(new TermQuery(new Term("id", "" + id)), 1);
                Assert.AreEqual(1, hits.TotalHits);
                int docID = hits.ScoreDocs[0].Doc;
                Document doc = s.Doc(docID);
                int endCounter = counter + fieldsPerDoc[id];
                while (counter < endCounter)
                {
                    string name = "f" + counter;
                    int fieldID = counter % 10;

                    bool stored = (counter & 1) == 0 || fieldID == 3;
                    bool binary = fieldID == 3;
                    bool indexed = fieldID != 3;

                    string stringValue;
                    if (fieldID != 3 && fieldID != 9)
                    {
                        stringValue = "text " + counter;
                    }
                    else
                    {
                        stringValue = null;
                    }

                    // stored:
                    if (stored)
                    {
                        IIndexableField f = doc.GetField(name);
                        Assert.IsNotNull(f, "doc " + id + " doesn't have field f" + counter);
                        if (binary)
                        {
                            Assert.IsNotNull(f, "doc " + id + " doesn't have field f" + counter);
                            BytesRef b = f.GetBinaryValue();
                            Assert.IsNotNull(b);
                            Assert.AreEqual(10, b.Length);
                            for (int idx = 0; idx < 10; idx++)
                            {
                                Assert.AreEqual((byte)(idx + counter), b.Bytes[b.Offset + idx]);
                            }
                        }
                        else
                        {
                            Debug.Assert(stringValue != null);
                            Assert.AreEqual(stringValue, f.GetStringValue());
                        }
                    }

                    if (indexed)
                    {
                        bool tv = counter % 2 == 1 && fieldID != 9;
                        if (tv)
                        {
                            Terms tfv = r.GetTermVectors(docID).GetTerms(name);
                            Assert.IsNotNull(tfv);
                            TermsEnum termsEnum = tfv.GetIterator(null);
                            Assert.AreEqual(new BytesRef("" + counter), termsEnum.Next());
                            Assert.AreEqual(1, termsEnum.TotalTermFreq);
                            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
                            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                            Assert.AreEqual(1, dpEnum.Freq);
                            Assert.AreEqual(1, dpEnum.NextPosition());

                            Assert.AreEqual(new BytesRef("text"), termsEnum.Next());
                            Assert.AreEqual(1, termsEnum.TotalTermFreq);
                            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
                            Assert.AreEqual(1, dpEnum.Freq);
                            Assert.AreEqual(0, dpEnum.NextPosition());

                            Assert.IsNull(termsEnum.Next());

                            // TODO: offsets
                        }
                        else
                        {
                            Fields vectors = r.GetTermVectors(docID);
                            Assert.IsTrue(vectors == null || vectors.GetTerms(name) == null);
                        }

                        BooleanQuery bq = new BooleanQuery();
                        bq.Add(new TermQuery(new Term("id", "" + id)), Occur.MUST);
                        bq.Add(new TermQuery(new Term(name, "text")), Occur.MUST);
                        TopDocs hits2 = s.Search(bq, 1);
                        Assert.AreEqual(1, hits2.TotalHits);
                        Assert.AreEqual(docID, hits2.ScoreDocs[0].Doc);

                        bq = new BooleanQuery();
                        bq.Add(new TermQuery(new Term("id", "" + id)), Occur.MUST);
                        bq.Add(new TermQuery(new Term(name, "" + counter)), Occur.MUST);
                        TopDocs hits3 = s.Search(bq, 1);
                        Assert.AreEqual(1, hits3.TotalHits);
                        Assert.AreEqual(docID, hits3.ScoreDocs[0].Doc);
                    }

                    counter++;
                }
            }

            r.Dispose();
            dir.Dispose();
        }

        private class IterableAnonymousInnerClassHelper : IEnumerable<IIndexableField>
        {
            private readonly TestIndexableField outerInstance;

            private int fieldCount;
            private int finalDocCount;
            private int finalBaseCount;

            public IterableAnonymousInnerClassHelper(TestIndexableField outerInstance, int fieldCount, int finalDocCount, int finalBaseCount)
            {
                this.outerInstance = outerInstance;
                this.fieldCount = fieldCount;
                this.finalDocCount = finalDocCount;
                this.finalBaseCount = finalBaseCount;
            }

            public virtual IEnumerator<IIndexableField> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this, outerInstance);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<IIndexableField>
            {
                private readonly IterableAnonymousInnerClassHelper outerInstance;
                private readonly TestIndexableField outerTextIndexableField;

                public IteratorAnonymousInnerClassHelper(IterableAnonymousInnerClassHelper outerInstance, TestIndexableField outerTextIndexableField)
                {
                    this.outerInstance = outerInstance;
                    this.outerTextIndexableField = outerTextIndexableField;
                }

                internal int fieldUpto;
                private IIndexableField current;

                public bool MoveNext()
                {
                    if (fieldUpto >= outerInstance.fieldCount)
                    {
                        return false;
                    }

                    Debug.Assert(fieldUpto < outerInstance.fieldCount);
                    if (fieldUpto == 0)
                    {
                        fieldUpto = 1;
                        current = NewStringField("id", "" + outerInstance.finalDocCount, Field.Store.YES);
                    }
                    else
                    {
                        current = new MyField(outerTextIndexableField, outerInstance.finalBaseCount + (fieldUpto++ - 1));
                    }

                    return true;
                }

                public IIndexableField Current
                {
                    get { return current; }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public void Dispose()
                {
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}