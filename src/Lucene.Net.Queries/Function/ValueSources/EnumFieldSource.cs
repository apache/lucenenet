using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;

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
    /// Obtains int field values from <seealso cref="IFieldCache#getInts"/> and makes
    /// those values available as other numeric types, casting as needed.
    /// StrVal of the value is not the int value, but its str (displayed) value
    /// </summary>
    public class EnumFieldSource : FieldCacheSource
    {
        internal const int DEFAULT_VALUE = -1;

        internal readonly FieldCache.IIntParser parser;
        internal readonly IDictionary<int?, string> enumIntToStringMap;
        internal readonly IDictionary<string, int?> enumStringToIntMap;

        public EnumFieldSource(string field, FieldCache.IIntParser parser, IDictionary<int?, string> enumIntToStringMap, IDictionary<string, int?> enumStringToIntMap)
            : base(field)
        {
            this.parser = parser;
            this.enumIntToStringMap = enumIntToStringMap;
            this.enumStringToIntMap = enumStringToIntMap;
        }

        private static int? TryParseInt(string valueStr)
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

        private string IntValueToStringValue(int? intVal)
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

        private int? StringValueToIntValue(string stringVal)
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
            intValue = TryParseInt(stringVal);
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

        public override string Description
        {
            get { return "enum(" + field + ')'; }
        }


        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var arr = cache.GetInts(readerContext.AtomicReader, field, parser, true);
            var valid = cache.GetDocsWithField(readerContext.AtomicReader, field);

            return new IntDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
        }

        private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
        {
            private readonly EnumFieldSource outerInstance;

            private readonly FieldCache.Ints arr;
            private readonly IBits valid;

            public IntDocValuesAnonymousInnerClassHelper(EnumFieldSource outerInstance, EnumFieldSource @this, FieldCache.Ints arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
                val = new MutableValueInt();
            }

            private readonly MutableValueInt val;

            public override float FloatVal(int doc)
            {
                return (float)arr.Get(doc);
            }

            public override int IntVal(int doc)
            {
                return arr.Get(doc);
            }

            public override long LongVal(int doc)
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
                return outerInstance.IntValueToStringValue(intValue);
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
                return outerInstance.Description + '=' + StrVal(doc);
            }


            public override ValueSourceScorer GetRangeScorer(IndexReader reader, string lowerVal, string upperVal, bool includeLower, bool includeUpper)
            {
                int? lower = outerInstance.StringValueToIntValue(lowerVal);
                int? upper = outerInstance.StringValueToIntValue(upperVal);

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
                private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

                private readonly int ll;
                private readonly int uu;

                public ValueSourceScorerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance, IndexReader reader, EnumFieldSource @this, int ll, int uu)
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

            public override AbstractValueFiller ValueFiller
            {
                get
                {
                    return new ValueFillerAnonymousInnerClassHelper(this);
                }
            }

            private class ValueFillerAnonymousInnerClassHelper : AbstractValueFiller
            {
                private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

                public ValueFillerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                    mval = new MutableValueInt();
                }

                private readonly MutableValueInt mval;

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

            if (!enumIntToStringMap.Equals(that.enumIntToStringMap))
            {
                return false;
            }
            if (!enumStringToIntMap.Equals(that.enumStringToIntMap))
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
            result = 31 * result + enumIntToStringMap.GetHashCode();
            result = 31 * result + enumStringToIntMap.GetHashCode();
            return result;
        }
    }


}