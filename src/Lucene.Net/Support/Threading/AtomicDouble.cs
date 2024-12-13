#region Copyright 2010 by Apache Harmony, Licensed under the Apache License, Version 2.0
/*  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */
#endregion

using J2N;
using J2N.Numerics;
using System;
using System.Diagnostics;
using System.Threading;

#nullable enable

namespace Lucene.Net.Support.Threading
{
    /// <summary>
    /// A <see cref="double"/> value that may be updated atomically.
    /// An <see cref="AtomicDouble"/> is used in applications such as atomically
    /// stored and retrieved values, and cannot be used as a replacement
    /// for a <see cref="System.Double"/>. However, this class does
    /// implement implicit conversion to <see cref="double"/>, so it can
    /// be utilized with language features, tools and utilities that deal
    /// with numerical operations.
    /// <para/>
    /// NOTE: This is a modified version of <see cref="J2N.Threading.Atomic.AtomicInt64"/> to support <see cref="double"/> values.
    /// It does not have the increment, decrement, and add methods because those operations are not atomic
    /// due to the conversion to/from <see cref="long"/>.
    /// </summary>
    /// <remarks>
    /// Note that this class is set up to mimic <c>double</c> in Java, rather than the J2N <see cref="J2N.Numerics.Double"/> class.
    /// This may cause differences in comparing NaN values.
    /// </remarks>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    [DebuggerDisplay("{Value}")]
    internal class AtomicDouble : Number, IEquatable<AtomicDouble>, IEquatable<double>, IFormattable, IConvertible
    {
        private long value;

        /// <summary>
        /// Creates a new <see cref="AtomicDouble"/> with the default initial value, <c>0</c>.
        /// </summary>
        public AtomicDouble()
            : this(0d)
        { }

        /// <summary>
        /// Creates a new <see cref="AtomicDouble"/> with the given initial <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public AtomicDouble(double value)
        {
            this.value = BitConversion.DoubleToRawInt64Bits(value);
        }

        /// <summary>
        /// Gets or sets the current value. Note that these operations can be done
        /// implicitly by setting the <see cref="AtomicDouble"/> to a <see cref="double"/>.
        /// <code>
        /// AtomicDouble aDouble = new AtomicDouble(4.0);
        /// double x = aDouble;
        /// </code>
        /// </summary>
        /// <remarks>
        /// Properties are inherently not atomic. Operators such as += and -= should not
        /// be used on <see cref="Value"/> because they perform both a separate get and a set operation.
        /// </remarks>
        public double Value
        {
            get => BitConversion.Int64BitsToDouble(Interlocked.Read(ref this.value));
            set => Interlocked.Exchange(ref this.value, BitConversion.DoubleToRawInt64Bits(value));
        }

        /// <summary>
        /// Atomically sets to the given value and returns the old value.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        /// <returns>The previous value.</returns>
        public double GetAndSet(double newValue)
        {
            return BitConversion.Int64BitsToDouble(Interlocked.Exchange(ref value, BitConversion.DoubleToRawInt64Bits(newValue)));
        }

        /// <summary>
        /// Atomically sets the value to the given updated value
        /// if the current value equals the expected value.
        /// </summary>
        /// <param name="expect">The expected value (the comparand).</param>
        /// <param name="update">The new value that will be set if the current value equals the expected value.</param>
        /// <returns><c>true</c> if successful. A <c>false</c> return value indicates that the actual value
        /// was not equal to the expected value.</returns>
        public bool CompareAndSet(double expect, double update)
        {
            long expectLong = BitConversion.DoubleToRawInt64Bits(expect);
            long updateLong = BitConversion.DoubleToRawInt64Bits(update);
            long rc = Interlocked.CompareExchange(ref value, updateLong, expectLong);
            return rc == expectLong;
        }

        /// <summary>
        /// Determines whether the specified <see cref="AtomicDouble"/> is equal to the current <see cref="AtomicDouble"/>.
        /// </summary>
        /// <param name="other">The <see cref="AtomicDouble"/> to compare with the current <see cref="AtomicDouble"/>.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is equal to the current <see cref="AtomicDouble"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(AtomicDouble? other)
        {
            if (other is null)
                return false;

            // NOTE: comparing long values rather than floating point comparison
            return Interlocked.Read(ref value) == Interlocked.Read(ref other.value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="double"/> is equal to the current <see cref="AtomicDouble"/>.
        /// </summary>
        /// <param name="other">The <see cref="double"/> to compare with the current <see cref="AtomicDouble"/>.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is equal to the current <see cref="AtomicDouble"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(double other)
        {
            // NOTE: comparing long values rather than floating point comparison
            return Interlocked.Read(ref value) == BitConversion.DoubleToRawInt64Bits(other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="AtomicDouble"/>.
        /// <para/>
        /// If <paramref name="other"/> is a <see cref="AtomicDouble"/>, the comparison is not done atomically.
        /// </summary>
        /// <param name="other">The <see cref="object"/> to compare with the current <see cref="AtomicDouble"/>.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is equal to the current <see cref="AtomicDouble"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? other)
        {
            if (other is AtomicDouble ai)
                return Equals(ai);
            if (other is double i)
                return Equals(i);
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance, consisting of
        /// a negative sign if the value is negative,
        /// and a sequence of digits ranging from 0 to 9 with no leading zeroes.</returns>
        public override string ToString()
        {
            return J2N.Numerics.Double.ToString(Value);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation,
        /// using the specified <paramref name="format"/>.
        /// </summary>
        /// <param name="format">A standard or custom numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified
        /// by <paramref name="format"/>.</returns>
        public override string ToString(string? format)
        {
            return J2N.Numerics.Double.ToString(Value, format);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation
        /// using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="provider"/>.</returns>
        public override string ToString(IFormatProvider? provider)
        {
            return J2N.Numerics.Double.ToString(Value, provider);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation using the
        /// specified format and culture-specific format information.
        /// </summary>
        /// <param name="format">A standard or custom numeric format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by
        /// <paramref name="format"/> and <paramref name="provider"/>.</returns>
        public override string ToString(string? format, IFormatProvider? provider)
        {
            return J2N.Numerics.Double.ToString(Value, format, provider);
        }

        #region IConvertible Members

        /// <inheritdoc/>
        public override byte ToByte()
        {
            return (byte)Value;
        }

        /// <inheritdoc/>
        public override sbyte ToSByte()
        {
            return (sbyte)Value;
        }

        /// <inheritdoc/>
        public override double ToDouble()
        {
            return Value;
        }

        /// <inheritdoc/>
        public override float ToSingle()
        {
            return (float)Value;
        }

        /// <inheritdoc/>
        public override int ToInt32()
        {
            return (int)Value;
        }

        /// <inheritdoc/>
        public override long ToInt64()
        {
            return (long)Value;
        }

        /// <inheritdoc/>
        public override short ToInt16()
        {
            return (short)Value;
        }

        /// <summary>
        /// Returns the <see cref="TypeCode"/> for value type <see cref="int"/>.
        /// </summary>
        /// <returns></returns>
        public TypeCode GetTypeCode() => ((IConvertible)Value).GetTypeCode();

        bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(Value);

        byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(Value);

        char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(Value);

        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(Value);

        decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(Value);

        double IConvertible.ToDouble(IFormatProvider? provider) => Value;

        short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(Value);

        int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(Value);

        long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(Value);

        sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(Value);

        float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(Value);

        object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)Value).ToType(conversionType, provider);

        ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(Value);

        uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(Value);

        ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(Value);

        #endregion IConvertible Members

        #region Operator Overrides

        /// <summary>
        /// Implicitly converts an <see cref="AtomicDouble"/> to a <see cref="double"/>.
        /// </summary>
        /// <param name="atomicInt64">The <see cref="AtomicDouble"/> to convert.</param>
        public static implicit operator double(AtomicDouble atomicInt64)
        {
            return atomicInt64.Value;
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value equality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(AtomicDouble? a1, AtomicDouble? a2)
        {
            if (a1 is null)
                return a2 is null;
            if (a2 is null)
                return false;

            return a1.Equals(a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value inequality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(AtomicDouble? a1, AtomicDouble? a2)
        {
            return !(a1 == a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value equality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(AtomicDouble? a1, double a2)
        {
            if (a1 is null)
                return false;

            return a1.Value.Equals(a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value inequality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(AtomicDouble? a1, double a2)
        {
            return !(a1 == a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value equality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(double a1, AtomicDouble? a2)
        {
            if (a2 is null)
                return false;

            return a2.Equals(a1);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value inequality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(double a1, AtomicDouble? a2)
        {
            return !(a1 == a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value equality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(AtomicDouble? a1, double? a2)
        {
            if (a1 is null)
                return a2 is null;
            if (a2 is null)
                return false;

            return a1.Value.Equals(a2.Value);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value inequality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(AtomicDouble? a1, double? a2)
        {
            return !(a1 == a2);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value equality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(double? a1, AtomicDouble? a2)
        {
            if (a1 is null)
                return a2 is null;
            if (a2 is null)
                return false;

            return a2.Equals(a1.Value);
        }

        /// <summary>
        /// Compares <paramref name="a1"/> and <paramref name="a2"/> for value inequality.
        /// </summary>
        /// <param name="a1">The first number.</param>
        /// <param name="a2">The second number.</param>
        /// <returns><c>true</c> if the given numbers are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(double? a1, AtomicDouble? a2)
        {
            return !(a1 == a2);
        }

        #endregion Operator Overrides
    }
}
