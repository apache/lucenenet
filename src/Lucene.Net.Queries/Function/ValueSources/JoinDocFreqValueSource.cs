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
using System;
using System.Collections;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Use a field value and find the Document Frequency within another field.
    /// 
    /// @since solr 4.0
    /// </summary>
    public class JoinDocFreqValueSource : FieldCacheSource
    {

        public const string NAME = "joindf";

        protected internal readonly string qfield;

        public JoinDocFreqValueSource(string field, string qfield)
            : base(field)
        {
            this.qfield = qfield;
        }

        public override string Description
        {
            get { return NAME + "(" + field + ":(" + qfield + "))"; }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            BinaryDocValues terms = cache.GetTerms(readerContext.AtomicReader, field, false, PackedInts.FAST);
            IndexReader top = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            Terms t = MultiFields.GetTerms(top, qfield);
            TermsEnum termsEnum = t == null ? TermsEnum.EMPTY : t.Iterator(null);

            return new IntDocValuesAnonymousInnerClassHelper(this, this, terms, termsEnum);
        }

        private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
        {
            private readonly JoinDocFreqValueSource outerInstance;

            private readonly BinaryDocValues terms;
            private readonly TermsEnum termsEnum;

            public IntDocValuesAnonymousInnerClassHelper(JoinDocFreqValueSource outerInstance, JoinDocFreqValueSource @this, BinaryDocValues terms, TermsEnum termsEnum)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.terms = terms;
                this.termsEnum = termsEnum;
                @ref = new BytesRef();
            }

            private readonly BytesRef @ref;

            public override int IntVal(int doc)
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
                catch (IOException e)
                {
                    throw new Exception("caught exception in function " + outerInstance.Description + " : doc=" + doc, e);
                }
            }
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(JoinDocFreqValueSource))
            {
                return false;
            }
            var other = (JoinDocFreqValueSource)o;
            if (!qfield.Equals(other.qfield))
            {
                return false;
            }
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return qfield.GetHashCode() + base.GetHashCode();
        }
    }
}