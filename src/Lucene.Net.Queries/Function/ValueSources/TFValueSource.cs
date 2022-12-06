// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.IO;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Function that returns <see cref="TFIDFSimilarity.Tf(float)"/>
    /// for every document.
    /// <para/>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <see cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </summary>
    public class TFValueSource : TermFreqValueSource
    {
        public TFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name => "tf";

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var fields = readerContext.AtomicReader.Fields;
            var terms = fields.GetTerms(m_indexedField);
            var searcher = (IndexSearcher)context["searcher"];
            var similarity = IDFValueSource.AsTFIDF(searcher.Similarity, m_indexedField);
            if (similarity is null)
            {
                throw UnsupportedOperationException.Create("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            return new SingleDocValuesAnonymousClass(this, this, terms, similarity);
        }

        private sealed class SingleDocValuesAnonymousClass : SingleDocValues
        {
            private readonly TFValueSource outerInstance;

            private readonly Terms terms;
            private readonly TFIDFSimilarity similarity;

            public SingleDocValuesAnonymousClass(TFValueSource outerInstance, TFValueSource @this, Terms terms, TFIDFSimilarity similarity)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.terms = terms;
                this.similarity = similarity;
                lastDocRequested = -1;
                Reset();
            }

            private DocsEnum docs;
            private int atDoc;
            private int lastDocRequested;

            public void Reset()
            {
                // no one should call us for deleted docs?

                if (terms != null)
                {
                    TermsEnum termsEnum = terms.GetEnumerator();
                    if (termsEnum.SeekExact(outerInstance.m_indexedBytes))
                    {
                        docs = termsEnum.Docs(null, null);
                    }
                    else
                    {
                        docs = null;
                    }
                }
                else
                {
                    docs = null;
                }

                if (docs is null)
                {
                    docs = new DocsEnumAnonymousClass();
                }
                atDoc = -1;
            }

            private sealed class DocsEnumAnonymousClass : DocsEnum
            {
                public override int Freq => 0;

                public override int DocID => DocIdSetIterator.NO_MORE_DOCS;

                public override int NextDoc()
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override int Advance(int target)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override long GetCost()
                {
                    return 0;
                }
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                try
                {
                    if (doc < lastDocRequested)
                    {
                        // out-of-order access.... reset
                        Reset();
                    }
                    lastDocRequested = doc;

                    if (atDoc < doc)
                    {
                        atDoc = docs.Advance(doc);
                    }

                    if (atDoc > doc)
                    {
                        // term doesn't match this document... either because we hit the
                        // end, or because the next doc is after this doc.
                        return similarity.Tf(0);
                    }

                    // a match!
                    return similarity.Tf(docs.Freq);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create("caught exception in function " + outerInstance.GetDescription() + " : doc=" + doc, e);
                }
            }
        }
    }
}