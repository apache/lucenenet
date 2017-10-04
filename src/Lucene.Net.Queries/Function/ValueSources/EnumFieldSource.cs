using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

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
    /// Obtains <see cref="int"/> field values from <see cref="IFieldCache.GetInt32s(AtomicReader, string, FieldCache.IInt32Parser, bool)"/> and makes
    /// those values available as other numeric types, casting as needed.
    /// StrVal of the value is not the <see cref="int"/> value, but its <see cref="string"/> (displayed) value
    /// </summary>
    public class EnumFieldSource : FieldCacheSource
    {
        private const int DEFAULT_VALUE = -1;

        private readonly FieldCache.IInt32Parser parser;
        private readonly IDictionary<int?, string> enumIntToStringMap;
        private readonly IDictionary<string, int?> enumStringToIntMap;

        public EnumFieldSource(string field, FieldCache.IInt32Parser parser, IDictionary<int?, string> enumIntToStringMap, IDictionary<string, int?> enumStringToIntMap)
            : base(field)
        {
            this.parser = parser;
            this.enumIntToStringMap = enumIntToStringMap;
            this.enumStringToIntMap = enumStringToIntMap;
        }

        /// <summary>
        /// NOTE: This was tryParseInt() in Lucene
        /// </summary>
        private static int? TryParseInt32(string valueStr) 
        {
            int? intValue = null;
            try
            {
                intValue = Convert.ToInt32(valueStr);
            }
            catch (FormatException)
            {
            }
            return intValue;
        }

        /// <summary>
        /// NOTE: This was intValueToStringValue() in Lucene
        /// </summary>
        private string Int32ValueToStringValue(int? intVal)
        {
            if (intVal == null)
            {
                return null;
            }

            string enumString = enumIntToStringMap[intVal];
            if (enumString != null)
            {
                return enumString;
            }
            // can't find matching enum name - return DEFAULT_VALUE.toString()
            return DEFAULT_VALUE.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// NOTE: This was stringValueToIntValue() in Lucene
        /// </summary>
        private int? StringValueToInt32Value(string stringVal)
        {
            if (stringVal == null)
            {
                return null;
            }

            int? intValue;
            int? enumInt = enumStringToIntMap[stringVal];
            if (enumInt != null) //enum int found for str
            {
                return enumInt;
            }

            //enum int not found for str
            intValue = TryParseInt32(stringVal);
            if (intValue == null) //not Integer
            {
                intValue = DEFAULT_VALUE;
            }
            string enumString = enumIntToStringMap[intValue];
            if (enumString != null) //has matching str
            {
                return intValue;
            }

            return DEFAULT_VALUE;
        }

        public override string GetDescription()
        {
            return "enum(" + m_field + ')';
        }


        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = m_cache.GetInt32s(readerContext.AtomicReader, m_field, parser, true);
            var valid = m_cache.GetDocsWithField(readerContext.AtomicReader, m_field);

            return new Int32DocValuesAnonymousInnerClassHelper(this, this, arr, valid);
        }

        private class Int32DocValuesAnonymousInnerClassHelper : Int32DocValues
        {
            private readonly EnumFieldSource outerInstance;

            private readonly FieldCache.Int32s arr;
            private readonly IBits valid;

            public Int32DocValuesAnonymousInnerClassHelper(EnumFieldSource outerInstance, EnumFieldSource @this, FieldCache.Int32s arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
                val = new MutableValueInt32();
            }

            private readonly MutableValueInt32 val;

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was intVal() in Lucene
            /// </summary>
            public override int Int32Val(int doc)
            {
                return arr.Get(doc);
            }

            /// <summary>
            /// NOTE: This was longVal() in Lucene
            /// </summary>
            public override long Int64Val(int doc)
            {
                return (long)arr.Get(doc);
            }

            public override double DoubleVal(int doc)
            {
                return (double)arr.Get(doc);
            }

            public override string StrVal(int doc)
            {
                int? intValue = arr.Get(doc);
                return outerInstance.Int32ValueToStringValue(intValue);
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? (object)arr.Get(doc) : null;
            }

            public override bool Exists(int doc)
            {
                return valid.Get(doc);
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + '=' + StrVal(doc);
            }


            public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
            {
                int? lower = outerInstance.StringValueToInt32Value(lowerVal);
                int? upper = outerInstance.StringValueToInt32Value(upperVal);

                // instead of using separate comparison functions, adjust the endpoints.

                if (lower == null)
                {
                    lower = int.MinValue;
                }
                else
                {
                    if (!includeLower && lower < int.MaxValue)
                    {
                        lower++;
                    }
                }

                if (upper == null)
                {
                    upper = int.MaxValue;
                }
                else
                {
                    if (!includeUpper && upper > int.MinValue)
                    {
                        upper--;
                    }
                }

                int ll = lower.Value;
                int uu = upper.Value;

                return new ValueSourceScorerAnonymousInnerClassHelper(this, reader, outerInstance, ll, uu);
            }

            private class ValueSourceScorerAnonymousInnerClassHelper : ValueSourceScorer
            {
                private readonly Int32DocValuesAnonymousInnerClassHelper outerInstance;

                private readonly int ll;
                private readonly int uu;

                public ValueSourceScorerAnonymousInnerClassHelper(Int32DocValuesAnonymousInnerClassHelper outerInstance, IndexReader reader, EnumFieldSource @this, int ll, int uu)
                    : base(reader, outerInstance)
                {
                    this.outerInstance = outerInstance;
                    this.ll = ll;
                    this.uu = uu;
                }

                public override bool MatchesValue(int doc)
                {
                    int val = outerInstance.arr.Get(doc);
                    // only check for deleted if it's the default value
                    // if (val==0 && reader.isDeleted(doc)) return false;
                    return val >= ll && val <= uu;
                }
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFillerAnonymousInnerClassHelper(this);
            }

            private class ValueFillerAnonymousInnerClassHelper : ValueFiller
            {
                private readonly Int32DocValuesAnonymousInnerClassHelper outerInstance;

                public ValueFillerAnonymousInnerClassHelper(Int32DocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                    mval = new MutableValueInt32();
                }

                private readonly MutableValueInt32 mval;

                public override MutableValue Value
                {
                    get
                    {
                        return mval;
                    }
                }

                public override void FillValue(int doc)
                {
                    mval.Value = outerInstance.arr.Get(doc);
                    mval.Exists = outerInstance.valid.Get(doc);
                }
            }
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o == null || this.GetType() != o.GetType())
            {
                return false;
            }
            if (!base.Equals(o))
            {
                return false;
            }

            EnumFieldSource that = (EnumFieldSource)o;

            // LUCENENET specific: must use Collections.Equals() to ensure values
            // contained within the dictionaries are compared for equality
            if (!Collections.Equals(enumIntToStringMap, that.enumIntToStringMap))
            {
                return false;
            }
            if (!Collections.Equals(enumStringToIntMap, that.enumStringToIntMap))
            {
                return false;
            }
            if (!parser.Equals(that.parser))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + parser.GetHashCode();
            // LUCENENET specific: must use Collections.GetHashCode() to ensure values
            // contained within the dictionaries are compared for equality
            result = 31 * result + Collections.GetHashCode(enumIntToStringMap);
            result = 31 * result + Collections.GetHashCode(enumStringToIntMap);
            return result;
        }
    }


}