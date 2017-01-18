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
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Queries.Function.DocValues
{
    /// <summary>
    /// Serves as base class for FunctionValues based on DocTermsIndex.
    /// @lucene.internal
    /// </summary>
    public abstract class DocTermsIndexDocValues : FunctionValues
    {
        protected internal readonly SortedDocValues termsIndex;
        protected internal readonly ValueSource vs;
        protected internal readonly MutableValueStr val = new MutableValueStr();
        protected internal readonly BytesRef spare = new BytesRef();
        protected internal readonly CharsRef spareChars = new CharsRef();

        protected DocTermsIndexDocValues(ValueSource vs, AtomicReaderContext context, string field)
        {
            try
            {
                termsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
            }
            catch (Exception e)
            {
                throw new DocTermsIndexException(field, e);
            }
            this.vs = vs;
        }

        protected internal abstract string toTerm(string readableValue);

        public override bool Exists(int doc)
        {
            return OrdVal(doc) >= 0;
        }

        public override int OrdVal(int doc)
        {
            return termsIndex.GetOrd(doc);
        }

        public override int NumOrd()
        {
            return termsIndex.ValueCount;
        }

        public override bool BytesVal(int doc, BytesRef target)
        {
            termsIndex.Get(doc, target);
            return target.Length > 0;
        }

        public override string StrVal(int doc)
        {
            termsIndex.Get(doc, spare);
            if (spare.Length == 0)
            {
                return null;
            }
            UnicodeUtil.UTF8toUTF16(spare, spareChars);
            return spareChars.ToString();
        }

        public override bool BoolVal(int doc)
        {
            return Exists(doc);
        }

        public override abstract object ObjectVal(int doc); // force subclasses to override

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            // TODO: are lowerVal and upperVal in indexed form or not?
            lowerVal = lowerVal == null ? null : toTerm(lowerVal);
            upperVal = upperVal == null ? null : toTerm(upperVal);

            int lower = int.MinValue;
            if (lowerVal != null)
            {
                lower = termsIndex.LookupTerm(new BytesRef(lowerVal));
                if (lower < 0)
                {
                    lower = -lower - 1;
                }
                else if (!includeLower)
                {
                    lower++;
                }
            }

            int upper = int.MaxValue;
            if (upperVal != null)
            {
                upper = termsIndex.LookupTerm(new BytesRef(upperVal));
                if (upper < 0)
                {
                    upper = -upper - 2;
                }
                else if (!includeUpper)
                {
                    upper--;
                }
            }
            int ll = lower;
            int uu = upper;

            return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, this, ll, uu);
        }

        private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
        {
            private readonly DocTermsIndexDocValues outerInstance;

            private int ll;
            private int uu;

            public ValueSourceScorerAnonymousInnerClassHelper(DocTermsIndexDocValues outerInstance, IndexReader reader,
                DocTermsIndexDocValues @this, int ll, int uu)
                : base(reader, @this)
            {
                this.outerInstance = outerInstance;
                this.ll = ll;
                this.uu = uu;
            }

            public override bool MatchesValue(int doc)
            {
                int ord = outerInstance.termsIndex.GetOrd(doc);
                return ord >= ll && ord <= uu;
            }
        }

        public override string ToString(int doc)
        {
            return vs.GetDescription() + '=' + StrVal(doc);
        }

        public override AbstractValueFiller ValueFiller
        {
            get
            {
                return new ValueFillerAnonymousInnerClassHelper(this);
            }
        }

        private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
        {
            private readonly DocTermsIndexDocValues outerInstance;

            public ValueFillerAnonymousInnerClassHelper(DocTermsIndexDocValues outerInstance)
            {
                this.outerInstance = outerInstance;
                mval = new MutableValueStr();
            }

            private readonly MutableValueStr mval;

            public override MutableValue Value
            {
                get
                {
                    return mval;
                }
            }

            public override void FillValue(int doc)
            {
                int ord = outerInstance.termsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    mval.Value.Bytes = BytesRef.EMPTY_BYTES;
                    mval.Value.Offset = 0;
                    mval.Value.Length = 0;
                    mval.Exists = false;
                }
                else
                {
                    outerInstance.termsIndex.LookupOrd(ord, mval.Value);
                    mval.Exists = true;
                }
            }
        }

        /// <summary>
        /// Custom Exception to be thrown when the DocTermsIndex for a field cannot be generated
        /// </summary>
        public sealed class DocTermsIndexException : Exception
        {
            public DocTermsIndexException(string fieldName, Exception cause)
                : base("Can't initialize DocTermsIndex to generate (function) FunctionValues for field: " + fieldName, cause)
            {
            }
        }
    }
}