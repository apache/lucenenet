// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

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
    /// StrVal of the value is not the <see cref="int"/> value, but its <see cref="string"/> (displayed) value.
    /// </summary>
    public class EnumFieldSource : FieldCacheSource
    {
        private const int DEFAULT_VALUE = -1;

        private readonly FieldCache.IInt32Parser parser;
        private readonly IDictionary<int, string> enumIntToStringMap;
        private readonly IDictionary<string, int> enumStringToIntMap;

        public EnumFieldSource(string field, FieldCache.IInt32Parser parser, IDictionary<int, string> enumIntToStringMap, IDictionary<string, int> enumStringToIntMap)
            : base(field)
        {
            this.parser = parser;
            this.enumIntToStringMap = enumIntToStringMap;
            this.enumStringToIntMap = enumStringToIntMap;
        }

        // LUCENENET specific - removed TryParseInt in favor of int.TryParse()

        /// <summary>
        /// NOTE: This was intValueToStringValue() in Lucene
        /// </summary>
        private string Int32ValueToStringValue(int intVal)
        {
            // LUCENENET: null value not applicable for value types (it defaults to 0 anyway)

            if (enumIntToStringMap.TryGetValue(intVal, out string enumString))
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
            if (stringVal is null)
            {
                return null;
            }

            if (enumStringToIntMap.TryGetValue(stringVal, out int enumInt)) //enum int found for str
            {
                return enumInt;
            }

            //enum int not found for str
            if (!int.TryParse(stringVal, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue)) //not Integer
            {
                intValue = DEFAULT_VALUE;
            }
            if (enumIntToStringMap.ContainsKey(intValue)) //has matching str
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

            return new Int32DocValuesAnonymousClass(this, this, arr, valid);
        }

        private sealed class Int32DocValuesAnonymousClass : Int32DocValues
        {
            private readonly EnumFieldSource outerInstance;

            private readonly FieldCache.Int32s arr;
            private readonly IBits valid;

            public Int32DocValuesAnonymousClass(EnumFieldSource outerInstance, EnumFieldSource @this, FieldCache.Int32s arr, IBits valid)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.arr = arr;
                this.valid = valid;
                //val = new MutableValueInt32(); // LUCENENET: Never read
            }

            //private readonly MutableValueInt32 val; // LUCENENET: Never read

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
                int intValue = arr.Get(doc);
                return outerInstance.Int32ValueToStringValue(intValue);
            }

            public override object ObjectVal(int doc)
            {
                return valid.Get(doc) ? J2N.Numerics.Int32.GetInstance(arr.Get(doc)) : null; // LUCENENET: In Java, the conversion to instance of java.util.Integer is implicit, but we need to do an explicit conversion
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

                if (lower is null)
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

                if (upper is null)
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

                return new ValueSourceScorer.AnonymousValueSourceScorer(reader, this, matchesValue: (doc) =>
                {
                    int val = arr.Get(doc);
                    // only check for deleted if it's the default value
                    // if (val==0 && reader.isDeleted(doc)) return false;
                    return val >= ll && val <= uu;
                });
            }

            public override ValueFiller GetValueFiller()
            {
                return new ValueFiller.AnonymousValueFiller<MutableValueInt32>(new MutableValueInt32(), fillValue: (doc, mutableValue) =>
                {
                    mutableValue.Value = arr.Get(doc);
                    mutableValue.Exists = valid.Get(doc);
                });
            }
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o is null || this.GetType() != o.GetType())
            {
                return false;
            }
            if (!base.Equals(o))
            {
                return false;
            }

            EnumFieldSource that = (EnumFieldSource)o;

            // LUCENENET specific: must use DictionaryEqualityComparer.Equals() to ensure values
            // contained within the dictionaries are compared for equality
            if (!JCG.DictionaryEqualityComparer<int, string>.Default.Equals(enumIntToStringMap, that.enumIntToStringMap))
            {
                return false;
            }
            if (!JCG.DictionaryEqualityComparer<string, int>.Default.Equals(enumStringToIntMap, that.enumStringToIntMap))
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
            // LUCENENET specific: must use DictionaryEqualityComparer.GetHashCode() to ensure values
            // contained within the dictionaries are compared for equality
            result = 31 * result + JCG.DictionaryEqualityComparer<int, string>.Default.GetHashCode(enumIntToStringMap);
            result = 31 * result + JCG.DictionaryEqualityComparer<string, int>.Default.GetHashCode(enumStringToIntMap);
            return result;
        }
    }
}