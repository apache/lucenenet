using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Queries.Function.DocValues
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
    /// Serves as base class for <see cref="FunctionValues"/> based on DocTermsIndex.
    /// @lucene.internal
    /// </summary>
    public abstract class DocTermsIndexDocValues : FunctionValues
    {
        protected readonly SortedDocValues m_termsIndex;
        protected readonly ValueSource m_vs;
        protected readonly MutableValueStr m_val = new MutableValueStr();
        protected readonly BytesRef m_spare = new BytesRef();
        protected readonly CharsRef m_spareChars = new CharsRef();

        public DocTermsIndexDocValues(ValueSource vs, AtomicReaderContext context, string field)
        {
            try
            {
                m_termsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, field);
            }
            catch (Exception e)
            {
                throw new DocTermsIndexException(field, e);
            }
            this.m_vs = vs;
        }

        protected abstract string ToTerm(string readableValue);

        public override bool Exists(int doc)
        {
            return OrdVal(doc) >= 0;
        }

        public override int OrdVal(int doc)
        {
            return m_termsIndex.GetOrd(doc);
        }

        public override int NumOrd
        {
            get { return m_termsIndex.ValueCount; }
        }

        public override bool BytesVal(int doc, BytesRef target)
        {
            m_termsIndex.Get(doc, target);
            return target.Length > 0;
        }

        public override string StrVal(int doc)
        {
            m_termsIndex.Get(doc, m_spare);
            if (m_spare.Length == 0)
            {
                return null;
            }
            UnicodeUtil.UTF8toUTF16(m_spare, m_spareChars);
            return m_spareChars.ToString();
        }

        public override bool BoolVal(int doc)
        {
            return Exists(doc);
        }

        public override abstract object ObjectVal(int doc); // force subclasses to override

        public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
        {
            // TODO: are lowerVal and upperVal in indexed form or not?
            lowerVal = lowerVal == null ? null : ToTerm(lowerVal);
            upperVal = upperVal == null ? null : ToTerm(upperVal);

            int lower = int.MinValue;
            if (lowerVal != null)
            {
                lower = m_termsIndex.LookupTerm(new BytesRef(lowerVal));
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
                upper = m_termsIndex.LookupTerm(new BytesRef(upperVal));
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
                int ord = outerInstance.m_termsIndex.GetOrd(doc);
                return ord >= ll && ord <= uu;
            }
        }

        public override string ToString(int doc)
        {
            return m_vs.GetDescription() + '=' + StrVal(doc);
        }

        public override ValueFiller GetValueFiller()
        {
            return new ValueFillerAnonymousInnerClassHelper(this);
        }

        private class ValueFillerAnonymousInnerClassHelper : ValueFiller
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
                int ord = outerInstance.m_termsIndex.GetOrd(doc);
                if (ord == -1)
                {
                    mval.Value.Bytes = BytesRef.EMPTY_BYTES;
                    mval.Value.Offset = 0;
                    mval.Value.Length = 0;
                    mval.Exists = false;
                }
                else
                {
                    outerInstance.m_termsIndex.LookupOrd(ord, mval.Value);
                    mval.Exists = true;
                }
            }
        }

        /// <summary>
        /// Custom <see cref="Exception"/> to be thrown when the DocTermsIndex for a field cannot be generated
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
        public sealed class DocTermsIndexException : Exception
        {
            public DocTermsIndexException(string fieldName, Exception cause)
                : base("Can't initialize DocTermsIndex to generate (function) FunctionValues for field: " + fieldName, cause)
            {
            }  

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            // For testing
            public DocTermsIndexException(string message)
                : base(message)
            {
            }

            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            public DocTermsIndexException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }
    }
}