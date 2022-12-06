// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// Function that returns <see cref="DocsEnum.Freq"/> for the
    /// supplied term in every document.
    /// <para/>
    /// If the term does not exist in the document, returns 0.
    /// If frequencies are omitted, returns 1.
    /// </summary>
    public class TermFreqValueSource : DocFreqValueSource
    {
        public TermFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name => "termfreq";

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            Fields fields = readerContext.AtomicReader.Fields;
            Terms terms = fields.GetTerms(m_indexedField);

            return new Int32DocValuesAnonymousClass(this, this, terms);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly TermFreqValueSource outerInstance;

            private readonly Terms terms;

            public Int32DocValuesAnonymousClass(TermFreqValueSource outerInstance, TermFreqValueSource @this, Terms terms)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.terms = terms;
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
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
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
                        return 0;
                    }

                    // a match!
                    return docs.Freq;
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create("caught exception in function " + outerInstance.GetDescription() + " : doc=" + doc, e);
                }
            }
        }
    }
}