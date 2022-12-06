using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Diagnostics;
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
    /// <b>Expert:</b> this class provides a <see cref="TokenStream"/>
    /// for indexing numeric values that can be used by <see cref="Search.NumericRangeQuery"/>
    /// or <see cref="Search.NumericRangeFilter"/>.
    ///
    /// <para/>Note that for simple usage, <see cref="Documents.Int32Field"/>, <see cref="Documents.Int64Field"/>, 
    /// <see cref="Documents.SingleField"/> or <see cref="Documents.DoubleField"/> is
    /// recommended.  These fields disable norms and
    /// term freqs, as they are not usually needed during
    /// searching.  If you need to change these settings, you
    /// should use this class.
    ///
    /// <para/>Here's an example usage, for an <see cref="int"/> field:
    ///
    /// <code>
    ///     IndexableFieldType fieldType = new IndexableFieldType(TextField.TYPE_NOT_STORED)
    ///     {
    ///         OmitNorms = true,
    ///         IndexOptions = IndexOptions.DOCS_ONLY
    ///     };
    ///     Field field = new Field(name, new NumericTokenStream(precisionStep).SetInt32Value(value), fieldType);
    ///     document.Add(field);
    /// </code>
    ///
    /// <para/>For optimal performance, re-use the <see cref="TokenStream"/> and <see cref="Documents.Field"/> instance
    /// for more than one document:
    ///
    /// <code>
    ///     NumericTokenStream stream = new NumericTokenStream(precisionStep);
    ///     IndexableFieldType fieldType = new IndexableFieldType(TextField.TYPE_NOT_STORED)
    ///     {
    ///         OmitNorms = true,
    ///         IndexOptions = IndexOptions.DOCS_ONLY
    ///     };
    ///     Field field = new Field(name, stream, fieldType);
    ///     Document document = new Document();
    ///     document.Add(field);
    ///
    ///     for(all documents) 
    ///     {
    ///         stream.SetInt32Value(value)
    ///         writer.AddDocument(document);
    ///     }
    /// </code>
    ///
    /// <para>this stream is not intended to be used in analyzers;
    /// it's more for iterating the different precisions during
    /// indexing a specific numeric value.</para>
    ///
    /// <para><b>NOTE</b>: as token streams are only consumed once
    /// the document is added to the index, if you index more
    /// than one numeric field, use a separate <see cref="NumericTokenStream"/>
    /// instance for each.</para>
    ///
    /// <para>See <see cref="Search.NumericRangeQuery"/> for more details on the
    /// <c>precisionStep</c> parameter as well as how numeric fields work under the hood.
    /// </para>
    ///
    /// @since 2.9
    /// </summary>
    public sealed class NumericTokenStream : TokenStream
    {
        private void InitializeInstanceFields()
        {
            numericAtt = AddAttribute<INumericTermAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
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
            /// Returns current token's raw value as <see cref="long"/> with all <see cref="Shift"/> applied, undefined before first token </summary>
            long RawValue { get; }

            /// <summary>
            /// Returns value size in bits (32 for <see cref="float"/>, <see cref="int"/>; 64 for <see cref="double"/>, <see cref="long"/>) </summary>
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
                if (typeof(ICharTermAttribute).IsAssignableFrom(attClass))
                {
                    throw new ArgumentException("NumericTokenStream does not support ICharTermAttribute.");
                }
                return @delegate.CreateAttributeInstance<T>();
            }
        }

        /// <summary>
        /// Implementation of <see cref="INumericTermAttribute"/>.
        /// @lucene.internal
        /// @since 4.0
        /// </summary>
        public sealed class NumericTermAttribute : Util.Attribute, INumericTermAttribute, ITermToBytesRefAttribute
        {
            private long _value = 0L;
            private int _precisionStep = 0;
            private readonly BytesRef _bytes = new BytesRef();

            /// <summary>
            /// Creates, but does not yet initialize this attribute instance
            /// </summary>
            /// <seealso cref="Init(long, int, int, int)"/>
            public NumericTermAttribute()
            {
                ValueSize = 0;
            }

            public BytesRef BytesRef => _bytes;

            public void FillBytesRef()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(ValueSize == 64 || ValueSize == 32);
                if (ValueSize == 64)
                {
                    NumericUtils.Int64ToPrefixCoded(_value, Shift, _bytes);
                }
                else
                {
                    NumericUtils.Int32ToPrefixCoded((int)_value, Shift, _bytes);
                }
            }

            public int Shift { get; set; }

            public int IncShift()
            {
                return (Shift += _precisionStep);
            }

            public long RawValue => _value & ~((1L << Shift) - 1L);

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
                // LUCENENET: Added guard clause
                if (reflector is null)
                    throw new ArgumentNullException(nameof(reflector));

                FillBytesRef();
                reflector.Reflect(typeof(ITermToBytesRefAttribute), "bytes", BytesRef.DeepCopyOf(_bytes));
                reflector.Reflect(typeof(INumericTermAttribute), "shift", Shift);
                reflector.Reflect(typeof(INumericTermAttribute), "rawValue", RawValue);
                reflector.Reflect(typeof(INumericTermAttribute), "valueSize", ValueSize);
            }

            public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
            {
                // LUCENENET: Added guard clauses
                if (target is null)
                    throw new ArgumentNullException(nameof(target));
                if (target is not INumericTermAttribute a)
                    throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(INumericTermAttribute)}", nameof(target));
                a.Init(_value, ValueSize, _precisionStep, Shift);
            }
        }

        /// <summary>
        /// Creates a token stream for numeric values using the default <seealso cref="precisionStep"/>
        /// <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/> (4). The stream is not yet initialized,
        /// before using set a value using the various Set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream()
            : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, NumericUtils.PRECISION_STEP_DEFAULT)
        {
            InitializeInstanceFields();
        }

        /// <summary>
        /// Creates a token stream for numeric values with the specified
        /// <paramref name="precisionStep"/>. The stream is not yet initialized,
        /// before using set a value using the various Set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(int precisionStep)
            : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, precisionStep)
        {
            InitializeInstanceFields();
        }

        /// <summary>
        /// Expert: Creates a token stream for numeric values with the specified
        /// <paramref name="precisionStep"/> using the given
        /// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory"/>.
        /// The stream is not yet initialized,
        /// before using set a value using the various Set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(AttributeSource.AttributeFactory factory, int precisionStep)
            : base(new NumericAttributeFactory(factory))
        {
            InitializeInstanceFields();
            if (precisionStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(precisionStep), "precisionStep must be >=1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.precisionStep = precisionStep;
            numericAtt.Shift = -precisionStep;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <see cref="long"/> value. 
        /// <para/>
        /// NOTE: This was setLongValue() in Lucene
        /// </summary>
        /// <param name="value"> the value, for which this <see cref="TokenStream"/> should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).SetInt64Value(value))</code> </returns>
        public NumericTokenStream SetInt64Value(long value)
        {
            numericAtt.Init(value, valSize = 64, precisionStep, -precisionStep);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <see cref="int"/> value.
        /// <para/>
        /// NOTE: This was setIntValue() in Lucene
        /// </summary>
        /// <param name="value"> the value, for which this <see cref="TokenStream"/> should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).SetInt32Value(value))</code> </returns>
        public NumericTokenStream SetInt32Value(int value)
        {
            numericAtt.Init(value, valSize = 32, precisionStep, -precisionStep);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <see cref="double"/> value. </summary>
        /// <param name="value"> the value, for which this <see cref="TokenStream"/> should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).SetDoubleValue(value))</code> </returns>
        public NumericTokenStream SetDoubleValue(double value)
        {
            numericAtt.Init(NumericUtils.DoubleToSortableInt64(value), valSize = 64, precisionStep, -precisionStep);
            return this;
        }

        /// <summary>
        /// Initializes the token stream with the supplied <see cref="float"/> value. 
        /// <para/>
        /// NOTE: This was setFloatValue() in Lucene
        /// </summary>
        /// <param name="value"> the value, for which this <see cref="TokenStream"/> should enumerate tokens. </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <code>new Field(name, new NumericTokenStream(precisionStep).SetSingleValue(value))</code> </returns>
        public NumericTokenStream SetSingleValue(float value)
        {
            numericAtt.Init(NumericUtils.SingleToSortableInt32(value), valSize = 32, precisionStep, -precisionStep);
            return this;
        }

        public override void Reset()
        {
            if (valSize == 0)
            {
                throw IllegalStateException.Create("call Set???Value() before usage");
            }
            numericAtt.Shift = -precisionStep;
        }

        public override bool IncrementToken()
        {
            if (valSize == 0)
            {
                throw IllegalStateException.Create("call Set???Value() before usage");
            }

            // this will only clear all other attributes in this TokenStream
            ClearAttributes();

            int shift = numericAtt.IncShift();
            typeAtt.Type = (shift == 0) ? TOKEN_TYPE_FULL_PREC : TOKEN_TYPE_LOWER_PREC;
            posIncrAtt.PositionIncrement = (shift == 0) ? 1 : 0;
            return (shift < valSize);
        }

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep => precisionStep;

        // members
        private INumericTermAttribute numericAtt;

        private ITypeAttribute typeAtt;
        private IPositionIncrementAttribute posIncrAtt;

        private int valSize = 0; // valSize==0 means not initialized
        private readonly int precisionStep;
    }
}