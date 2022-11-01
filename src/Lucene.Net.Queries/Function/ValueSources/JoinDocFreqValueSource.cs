// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
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
    /// Use a field value and find the Document Frequency within another field.
    /// 
    /// @since solr 4.0
    /// </summary>
    public class JoinDocFreqValueSource : FieldCacheSource
    {
        public const string NAME = "joindf";

        protected readonly string m_qfield;

        public JoinDocFreqValueSource(string field, string qfield)
            : base(field)
        {
            this.m_qfield = qfield;
        }

        public override string GetDescription()
        {
            return NAME + "(" + m_field + ":(" + m_qfield + "))";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            BinaryDocValues terms = m_cache.GetTerms(readerContext.AtomicReader, m_field, false, PackedInt32s.FAST);
            IndexReader top = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            Terms t = MultiFields.GetTerms(top, m_qfield);
            TermsEnum termsEnum = t is null ? TermsEnum.EMPTY : t.GetEnumerator();

            return new Int32DocValuesAnonymousClass(this, this, terms, termsEnum);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly JoinDocFreqValueSource outerInstance;

            private readonly BinaryDocValues terms;
            private readonly TermsEnum termsEnum;

            public Int32DocValuesAnonymousClass(JoinDocFreqValueSource outerInstance, JoinDocFreqValueSource @this, BinaryDocValues terms, TermsEnum termsEnum)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.terms = terms;
                this.termsEnum = termsEnum;
                @ref = new BytesRef();
            }

            private readonly BytesRef @ref;

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                try
                {
                    terms.Get(doc, @ref);
                    if (termsEnum.SeekExact(@ref))
                    {
                        return termsEnum.DocFreq;
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create("caught exception in function " + outerInstance.GetDescription() + " : doc=" + doc, e);
                }
            }
        }

        public override bool Equals(object o)
        {
            if (!(o is JoinDocFreqValueSource other))
                return false;
            if (!m_qfield.Equals(other.m_qfield, StringComparison.Ordinal))
            {
                return false;
            }
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return m_qfield.GetHashCode() + base.GetHashCode();
        }
    }
}