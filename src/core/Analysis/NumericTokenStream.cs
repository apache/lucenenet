/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Search;
using Lucene.Net.Util;
using AttributeSource = Lucene.Net.Util.AttributeSource;
using NumericUtils = Lucene.Net.Util.NumericUtils;
using NumericField = Lucene.Net.Documents.NumericField;
using System;
// javadocs

namespace Lucene.Net.Analysis
{

    /// <summary> <b>Expert:</b> This class provides a <see cref="TokenStream" />
    /// for indexing numeric values that can be used by <see cref="NumericRangeQuery{T}" />
    /// or <see cref="NumericRangeFilter{T}" />.
    /// 
    /// <p/>Note that for simple usage, <see cref="NumericField" /> is
    /// recommended.  <see cref="NumericField" /> disables norms and
    /// term freqs, as they are not usually needed during
    /// searching.  If you need to change these settings, you
    /// should use this class.
    /// 
    /// <p/>See <see cref="NumericField" /> for capabilities of fields
    /// indexed numerically.<p/>
    /// 
    /// <p/>Here's an example usage, for an <c>int</c> field:
    /// 
    /// <code>
    ///  Field field = new Field(name, new NumericTokenStream(precisionStep).setIntValue(value));
    ///  field.setOmitNorms(true);
    ///  field.setOmitTermFreqAndPositions(true);
    ///  document.add(field);
    /// </code>
    /// 
    /// <p/>For optimal performance, re-use the TokenStream and Field instance
    /// for more than one document:
    /// 
    /// <code>
    ///  NumericTokenStream stream = new NumericTokenStream(precisionStep);
    ///  Field field = new Field(name, stream);
    ///  field.setOmitNorms(true);
    ///  field.setOmitTermFreqAndPositions(true);
    ///  Document document = new Document();
    ///  document.add(field);
    /// 
    ///  for(all documents) {
    ///    stream.setIntValue(value)
    ///    writer.addDocument(document);
    ///  }
    /// </code>
    /// 
    /// <p/>This stream is not intended to be used in analyzers;
    /// it's more for iterating the different precisions during
    /// indexing a specific numeric value.<p/>
    /// 
    /// <p/><b>NOTE</b>: as token streams are only consumed once
    /// the document is added to the index, if you index more
    /// than one numeric field, use a separate <c>NumericTokenStream</c>
    /// instance for each.<p/>
    /// 
    /// <p/>See <see cref="NumericRangeQuery{T}" /> for more details on the
    /// <a href="../search/NumericRangeQuery.html#precisionStepDesc"><c>precisionStep</c></a>
    /// parameter as well as how numeric fields work under the hood.<p/>
    /// 
    /// <p/><font color="red"><b>NOTE:</b> This API is experimental and
    /// might change in incompatible ways in the next release.</font>
    ///   Since 2.9
    /// </summary>
    public sealed class NumericTokenStream : TokenStream
    {
        /// <summary>The full precision token gets this token type assigned. </summary>
        public const System.String TOKEN_TYPE_FULL_PREC = "fullPrecNumeric";

        /// <summary>The lower precision tokens gets this token type assigned. </summary>
        public const System.String TOKEN_TYPE_LOWER_PREC = "lowerPrecNumeric";

        public interface INumericTermAttribute : IAttribute
        {
            int Shift { get; set; }

            long RawValue { get; }

            int ValueSize { get; }

            void Init(long value, int valSize, int precisionStep, int shift);

            int IncShift();
        }

        private class NumericAttributeFactory : AttributeFactory
        {
            private readonly AttributeFactory @delegate;

            public NumericAttributeFactory(AttributeFactory @delegate)
            {
                this.@delegate = @delegate;
            }

            public override Lucene.Net.Util.Attribute CreateAttributeInstance<T>()
            {
                if (typeof(CharTermAttribute).IsAssignableFrom(typeof(T)))
                    throw new ArgumentException("NumericTokenStream does not support CharTermAttribute.");

                return @delegate.CreateAttributeInstance<T>();
            }
        }

        public sealed class NumericTermAttribute : Lucene.Net.Util.Attribute, INumericTermAttribute, ITermToBytesRefAttribute
        {
            private long value = 0L;
            private int valueSize = 0, shift = 0, precisionStep = 0;
            private BytesRef bytes = new BytesRef();

            public NumericTermAttribute()
            {
            }

            public BytesRef BytesRef
            {
                get
                {
                    return bytes;
                }
            }

            public int FillBytesRef()
            {
                try
                {
                    //assert valueSize == 64 || valueSize == 32;
                    return (valueSize == 64) ?
                      NumericUtils.LongToPrefixCoded(value, shift, bytes) :
                      NumericUtils.IntToPrefixCoded((int)value, shift, bytes);
                }
                catch (ArgumentException iae)
                {
                    // return empty token before first or after last
                    bytes.length = 0;
                    return 0;
                }
            }

            public int Shift
            {
                get
                {
                    return shift;
                }
                set
                {
                    this.shift = value;
                }
            }

            public int IncShift()
            {
                return (shift += precisionStep);
            }

            public long RawValue
            {
                get { return value & ~((1L << shift) - 1L); }
            }

            public int ValueSize
            {
                get { return valueSize; }
            }

            public void Init(long value, int valSize, int precisionStep, int shift)
            {
                this.value = value;
                this.valueSize = valSize;
                this.precisionStep = precisionStep;
                this.shift = shift;
            }

            public override void Clear()
            {
                // this attribute has no contents to clear!
                // we keep it untouched as it's fully controlled by outer class.
            }

            public override void ReflectWith(IAttributeReflector reflector)
            {
                FillBytesRef();
                reflector.Reflect<ITermToBytesRefAttribute>("bytes", BytesRef.DeepCopyOf(bytes));
                reflector.Reflect<INumericTermAttribute>("shift", shift);
                reflector.Reflect<INumericTermAttribute>("rawValue", RawValue);
                reflector.Reflect<INumericTermAttribute>("valueSize", valueSize);
            }

            public override void CopyTo(Util.Attribute target)
            {
                INumericTermAttribute a = (INumericTermAttribute)target;
                a.Init(value, valueSize, precisionStep, shift);
            }
        }


        private void InitBlock()
        {
            termAtt = AddAttribute<INumericTermAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }



        /// <summary> Creates a token stream for numeric values using the default <c>precisionStep</c>
        /// <see cref="NumericUtils.PRECISION_STEP_DEFAULT" /> (4). The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream()
            : this(NumericUtils.PRECISION_STEP_DEFAULT)
        {
        }

        /// <summary> Creates a token stream for numeric values with the specified
        /// <c>precisionStep</c>. The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(int precisionStep)
            : base()
        {
            InitBlock();
            this.precisionStep = precisionStep;
            if (precisionStep < 1)
                throw new System.ArgumentException("precisionStep must be >=1");
        }

        /// <summary> Expert: Creates a token stream for numeric values with the specified
        /// <c>precisionStep</c> using the given <see cref="AttributeSource" />.
        /// The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(AttributeSource source, int precisionStep)
            : base(source)
        {
            InitBlock();
            this.precisionStep = precisionStep;
            if (precisionStep < 1)
                throw new System.ArgumentException("precisionStep must be >=1");
        }

        /// <summary> Expert: Creates a token stream for numeric values with the specified
        /// <c>precisionStep</c> using the given
        /// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory" />.
        /// The stream is not yet initialized,
        /// before using set a value using the various set<em>???</em>Value() methods.
        /// </summary>
        public NumericTokenStream(AttributeFactory factory, int precisionStep)
            : base(factory)
        {
            InitBlock();
            this.precisionStep = precisionStep;
            if (precisionStep < 1)
                throw new System.ArgumentException("precisionStep must be >=1");
        }

        /// <summary> Initializes the token stream with the supplied <c>long</c> value.</summary>
        /// <param name="value_Renamed">the value, for which this TokenStream should enumerate tokens.
        /// </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <c>new Field(name, new NumericTokenStream(precisionStep).SetLongValue(value))</c>
        /// </returns>
        public NumericTokenStream SetLongValue(long value_Renamed)
        {
            this.value_Renamed = value_Renamed;
            valSize = 64;
            shift = 0;
            return this;
        }

        /// <summary> Initializes the token stream with the supplied <c>int</c> value.</summary>
        /// <param name="value_Renamed">the value, for which this TokenStream should enumerate tokens.
        /// </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <c>new Field(name, new NumericTokenStream(precisionStep).SetIntValue(value))</c>
        /// </returns>
        public NumericTokenStream SetIntValue(int value_Renamed)
        {
            this.value_Renamed = (long)value_Renamed;
            valSize = 32;
            shift = 0;
            return this;
        }

        /// <summary> Initializes the token stream with the supplied <c>double</c> value.</summary>
        /// <param name="value_Renamed">the value, for which this TokenStream should enumerate tokens.
        /// </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <c>new Field(name, new NumericTokenStream(precisionStep).SetDoubleValue(value))</c>
        /// </returns>
        public NumericTokenStream SetDoubleValue(double value_Renamed)
        {
            this.value_Renamed = NumericUtils.DoubleToSortableLong(value_Renamed);
            valSize = 64;
            shift = 0;
            return this;
        }

        /// <summary> Initializes the token stream with the supplied <c>float</c> value.</summary>
        /// <param name="value_Renamed">the value, for which this TokenStream should enumerate tokens.
        /// </param>
        /// <returns> this instance, because of this you can use it the following way:
        /// <c>new Field(name, new NumericTokenStream(precisionStep).SetFloatValue(value))</c>
        /// </returns>
        public NumericTokenStream SetFloatValue(float value_Renamed)
        {
            this.value_Renamed = (long)NumericUtils.FloatToSortableInt(value_Renamed);
            valSize = 32;
            shift = 0;
            return this;
        }

        // @Override
        public override void Reset()
        {
            if (valSize == 0)
                throw new System.SystemException("call set???Value() before usage");
            shift = 0;
        }

        protected override void Dispose(bool disposing)
        {
            // Do nothing.
        }

        // @Override
        public override bool IncrementToken()
        {
            if (valSize == 0)
                throw new System.SystemException("call set???Value() before usage");
            if (shift >= valSize)
                return false;

            ClearAttributes();
            char[] buffer;
            switch (valSize)
            {

                case 64:
                    buffer = termAtt.ResizeTermBuffer(NumericUtils.BUF_SIZE_LONG);
                    termAtt.SetTermLength(NumericUtils.LongToPrefixCoded(value_Renamed, shift, buffer));
                    break;


                case 32:
                    buffer = termAtt.ResizeTermBuffer(NumericUtils.BUF_SIZE_INT);
                    termAtt.SetTermLength(NumericUtils.IntToPrefixCoded((int)value_Renamed, shift, buffer));
                    break;


                default:
                    // should not happen
                    throw new System.ArgumentException("valSize must be 32 or 64");

            }

            typeAtt.Type = (shift == 0) ? TOKEN_TYPE_FULL_PREC : TOKEN_TYPE_LOWER_PREC;
            posIncrAtt.PositionIncrement = (shift == 0) ? 1 : 0;
            shift += precisionStep;
            return true;
        }

        // @Override
        public override System.String ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("(numeric,valSize=").Append(valSize);
            sb.Append(",precisionStep=").Append(precisionStep).Append(')');
            return sb.ToString();
        }

        // members
        private INumericTermAttribute termAtt;
        private ITypeAttribute typeAtt;
        private IPositionIncrementAttribute posIncrAtt;

        private int valSize = 0; // valSize==0 means not initialized
        private readonly int precisionStep;
    }
}