using System.Diagnostics;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Reflection;

namespace Lucene.Net.Analysis
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
    /// <b>Expert:</b> this class provides a <seealso cref="TokenStream"/>
    /// for indexing numeric values that can be used by {@link
    /// NumericRangeQuery} or <seealso cref="NumericRangeFilter"/>.
    ///
    /// <p>Note that for simple usage, <seealso cref="IntField"/>, {@link
    /// LongField}, <seealso cref="FloatField"/> or <seealso cref="DoubleField"/> is
    /// recommended.  These fields disable norms and
    /// term freqs, as they are not usually needed during
    /// searching.  If you need to change these settings, you
    /// should use this class.
    ///
    /// <p>Here's an example usage, for an <code>int</code> field:
    ///
    /// <pre class="prettyprint">
    ///  FieldType fieldType = new FieldType(TextField.TYPE_NOT_STORED);
    ///  fieldType.setOmitNorms(true);
    ///  fieldType.setIndexOptions(IndexOptions.DOCS_ONLY);
    ///  Field field = new Field(name, new NumericTokenStream(precisionStep).setIntValue(value), fieldType);
    ///  document.add(field);
    /// </pre>
    ///
    /// <p>For optimal performance, re-use the TokenStream and Field instance
    /// for more than one document:
    ///
    /// <pre class="prettyprint">
    ///  NumericTokenStream stream = new NumericTokenStream(precisionStep);
    ///  FieldType fieldType = new FieldType(TextField.TYPE_NOT_STORED);
    ///  fieldType.setOmitNorms(true);
    ///  fieldType.setIndexOptions(IndexOptions.DOCS_ONLY);
    ///  Field field = new Field(name, stream, fieldType);
    ///  Document document = new Document();
    ///  document.add(field);
    ///
    ///  for(all documents) {
    ///    stream.setIntValue(value)
    ///    writer.addDocument(document);
    ///  }
    /// </pre>
    ///
    /// <p>this stream is not intended to be used in analyzers;
    /// it's more for iterating the different precisions during
    /// indexing a specific numeric value.</p>
    ///
    /// <p><b>NOTE</b>: as token streams are only consumed once
    /// the document is added to the index, if you index more
    /// than one numeric field, use a separate <code>NumericTokenStream</code>
    /// instance for each.</p>
    ///
    /// <p>See <seealso cref="NumericRangeQuery"/> for more details on the
    /// <a
    /// href="../search/NumericRangeQuery.html#precisionStepDesc"><code>precisionStep</code></a>
    /// parameter as well as how numeric fields work under the hood.</p>
    ///
    /// @since 2.9
    /// </summary>
    public sealed class NumericTokenStream : TokenStream
    {
        private bool InstanceFieldsInitialized = false;

        private void InitializeInstanceFields()
        {
            NumericAtt = AddAttribute<INumericTermAttribute>();
            TypeAtt = AddAttribute<ITypeAttribute>();
            PosIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        /// <summary>
        /// The full precision token gets this token type assigned. </summary>
        public const string TOKEN_TYPE_FULL_PREC = "fullPrecNumeric";

        /// <summary>
        /// The lower precision tokens gets this token type assigned. </summary>
        public const string TOKEN_TYPE_LOWER_PREC = "lowerPrecNumeric";

        /// <summary>
        /// <b>Expert:</b> Use this attribute to get the details of the currently generated token.
        /// @lucene.experimental
        /// @since 4.0
        /// </summary>
        public interface INumericTermAttribute : IAttribute
        {
            /// <summary>
            /// Returns current shift value, undefined before first token </summary>
            int Shift { get; set; }

            /// <summary>
            /// Returns current token's raw value as {@code long} with all <seealso cref="#getShift"/> applied, undefined before first token </summary>
            long RawValue { get; }

            /// <summary>
            /// Returns value size in bits (32 for {@code float}, {@code int}; 64 for {@code double}, {@code long}) </summary>
            int ValueSize { get; }

            /// <summary>
            /// <em>Don't call this method!</em>
            /// @lucene.internal
            /// </summary>
            void Init(long value, int valSize, int precisionStep, int shift);

            /// <summary>
            /// <em>Don't call this method!</em>
            /// @lucene.internal
            /// </summary>
            int IncShift();
        }

        // just a wrapper to prevent adding CTA
        private sealed class NumericAttributeFactory : AttributeSource.AttributeFactory
        {
            private readonly AttributeSource.AttributeFactory @delegate;

            internal NumericAttributeFactory(AttributeSource.AttributeFactory @delegate)
            {
                this.@delegate = @delegate;
            }

            public override Util.Attribute CreateAttributeInstance<T>()
            {
                var attClass = typeof(T);
                if (typeof(ICharTermAttribute).GetTypeInfo().IsAssignableFrom(attClass.GetTypeInfo()))
                {
                    throw new System.ArgumentException("NumericTokenStream does not support CharTermAttribute.");
                }
                return @delegate.CreateAttributeInstance<T>();
            }
        }

        /// <summary>
        /// Implementation of <seealso cref="NumericTermAttribute"/>.
        /// @lucene.internal
        /// @since 4.0
        /// </summary>
        public sealed class NumericTermAttribute : Util.Attribute, INumericTermAttribute, ITermToBytesRefAttribute
        {
            private long _value = 0L;
            private int _precisionStep = 0;
            private readonly BytesRef _bytes = new BytesRef();

            public NumericTermAttribute()
            {
                ValueSize = 0;
            }

            public BytesRef BytesRef
            {
                get
                {
                    return _bytes;
                }
            }

            public void FillBytesRef()
            {
                Debug.Assert(ValueSize == 64 || ValueSize == 32);
                if (ValueSize == 64)
                {
                    NumericUtils.LongToPrefixCoded(_value, Shift, _bytes);
                }
                else
                {
                    NumericUtils.IntToPrefixCoded((int)_value, Shift, _bytes);
                }
            }

            public int Shift { get; set; }

            public int IncShift()
            {
                return (Shift += _precisionStep);
            }

            public long RawValue
            {
                get
                {
                    return _value & ~((1L << Shift) - 1L);
                }
            }

            public int ValueSize { get; private set; }

            public void Init(long value, int valueSize, int precisionStep, int shift)
            {
                this._value = value;
                this.ValueSize = valueSize;
                this._precisionStep = precisionStep;
                this.Shift = shift;
            }

            public override void Clear()
            {
                // this attribute has no contents to clear!
                // we keep it untouched as it's fully controlled by outer class.
            }

            public override void ReflectWith(IAttributeReflector reflector)
            {
                FillBytesRef();
                reflector.Reflect(typeof(ITermToBytesRefAttribute), "bytes", BytesRef.DeepCopyOf(_bytes));
                reflector.Reflect(typeof(INumericTermAttribute), "shift", Shift);
                reflector.Reflect(typeof(INumericTermAttribute), "rawValue", RawValue);
                reflector.Reflect(typeof(INumericTermAttribute), "valueSize", ValueSize);
            }

            public override void CopyTo(Util.Attribute target)
            {
                var a = (NumericTermAttribute)target;
                a.Init(_value, ValueSize, _precisionStep, Shift);
            }
        }

        /// <summary>
        /// Creates a token stream for numeric values using the default <code>precisionStep</code>
        /// <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4). The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream()
            : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, NumericUtils.PRECISION_STEP_DEFAULT)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
        }

        /// <summary>
        /// Creates a token stream for numeric values with the specified
        /// <code>precisionStep</code>. The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(int precisionStep)
            : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, precisionStep)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
        }

        /// <summary>
        /// Expert: Creates a token stream for numeric values with the specified
        /// <code>precisionStep</code> using the given
        /// <seealso cref="Lucene.Net.Util.AttributeSource.AttributeFactory"/>.
        /// The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(AttributeSource.AttributeFactory factory, int precisionStep)
            : base(new NumericAttributeFactory(factory))
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
            if (precisionStep < 1)
            {
                throw new System.ArgumentException("precisionStep must be >=1");
            }
            this.PrecisionStep_Renamed = precisionStep;
            NumericAtt.Shift = -precisionStep;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <code>long</code> value. </summary>
        /// <param name="value"> the value, for which this TokenStream should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).setLongValue(value))</code> </returns>
        public NumericTokenStream SetLongValue(long value)
        {
            NumericAtt.Init(value, ValSize = 64, PrecisionStep_Renamed, -PrecisionStep_Renamed);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <code>int</code> value. </summary>
        /// <param name="value"> the value, for which this TokenStream should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).setIntValue(value))</code> </returns>
        public NumericTokenStream SetIntValue(int value)
        {
            NumericAtt.Init(value, ValSize = 32, PrecisionStep_Renamed, -PrecisionStep_Renamed);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <code>double</code> value. </summary>
        /// <param name="value"> the value, for which this TokenStream should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).setDoubleValue(value))</code> </returns>
        public NumericTokenStream SetDoubleValue(double value)
        {
            NumericAtt.Init(NumericUtils.DoubleToSortableLong(value), ValSize = 64, PrecisionStep_Renamed, -PrecisionStep_Renamed);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <code>float</code> value. </summary>
        /// <param name="value"> the value, for which this TokenStream should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).setFloatValue(value))</code> </returns>
        public NumericTokenStream SetFloatValue(float value)
        {
            NumericAtt.Init(NumericUtils.FloatToSortableInt(value), ValSize = 32, PrecisionStep_Renamed, -PrecisionStep_Renamed);
            return this;
        }

        public override void Reset()
        {
            if (ValSize == 0)
            {
                throw new Exception("call set???Value() before usage");
            }
            NumericAtt.Shift = -PrecisionStep_Renamed;
        }

        public override bool IncrementToken()
        {
            if (ValSize == 0)
            {
                throw new Exception("call set???Value() before usage");
            }

            // this will only clear all other attributes in this TokenStream
            ClearAttributes();

            int shift = NumericAtt.IncShift();
            TypeAtt.Type = (shift == 0) ? TOKEN_TYPE_FULL_PREC : TOKEN_TYPE_LOWER_PREC;
            PosIncrAtt.PositionIncrement = (shift == 0) ? 1 : 0;
            return (shift < ValSize);
        }

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep
        {
            get
            {
                return PrecisionStep_Renamed;
            }
        }

        // members
        private INumericTermAttribute NumericAtt;

        private ITypeAttribute TypeAtt;
        private IPositionIncrementAttribute PosIncrAtt;

        private int ValSize = 0; // valSize==0 means not initialized
        private readonly int PrecisionStep_Renamed;
    }
}