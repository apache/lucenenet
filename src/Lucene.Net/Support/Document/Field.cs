using System;

namespace Lucene.Net.Documents
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

    public partial class Field
    {

        // LUCENENET NOTE: The following classes were duplicated from Apache Harmony
        // because nullable types in .NET are not reference types, therefore storing
        // them in a field type object will require boxing/unboxing.

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected abstract class Number
        {
            /// <summary>
            /// Returns th is object's value as a <see cref="byte"/>. Might involve rounding and/or
            /// truncating the value, so it fits into a <see cref="byte"/>.
            /// </summary>
            /// <returns>the primitive <see cref="byte"/> value of th is object.</returns>
            public virtual byte GetByteValue()
            {
                return (byte)GetInt32Value();
            }

            /// <summary>
            /// Returns th is object's value as a <see cref="double"/>. Might involve rounding.
            /// </summary>
            /// <returns>the primitive <see cref="double"/> value of th is object.</returns>
            public abstract double GetDoubleValue();

            /// <summary>
            /// Returns th is object's value as a <see cref="float"/>. Might involve rounding.
            /// </summary>
            /// <returns>the primitive <see cref="float"/> value of th is object.</returns>
            public abstract float GetSingleValue();

            /// <summary>
            /// Returns th is object's value as an <see cref="int"/>. Might involve rounding and/or
            /// truncating the value, so it fits into an <see cref="int"/>.
            /// </summary>
            /// <returns>the primitive <see cref="int"/> value of th is object.</returns>
            public abstract int GetInt32Value();

            /// <summary>
            /// Returns th is object's value as a <see cref="long"/>. Might involve rounding and/or
            /// truncating the value, so it fits into a <see cref="long"/>.
            /// </summary>
            /// <returns>the primitive <see cref="long"/> value of th is object.</returns>
            public abstract long GetInt64Value();

            /// <summary>
            /// Returns th is object's value as a <see cref="short"/>. Might involve rounding and/or
            /// truncating the value, so it fits into a <see cref="short"/>.
            /// </summary>
            /// <returns>the primitive <see cref="short"/> value of th is object.</returns>
            public virtual short GetInt16Value()
            {
                return (short)GetInt32Value();
            }

            public abstract override string ToString(); 

            public abstract string ToString(string format);

            public abstract string ToString(IFormatProvider provider);

            public abstract string ToString(string format, IFormatProvider provider);
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Byte : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly byte value;

            public Byte(byte value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return value;
            }

            public override int GetInt32Value()
            {
                return value;
            }

            public override long GetInt64Value()
            {
                return value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Int16 : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly short value;

            public Int16(short value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return value;
            }

            public override int GetInt32Value()
            {
                return value;
            }

            public override long GetInt64Value()
            {
                return value;
            }

            public override short GetInt16Value()
            {
                return value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Int32 : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly int value;

            public Int32(int value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return value;
            }

            public override int GetInt32Value()
            {
                return value;
            }

            public override long GetInt64Value()
            {
                return value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Int64 : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly long value;

            public Int64(long value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return value;
            }

            public override int GetInt32Value()
            {
                return (int)value;
            }

            public override long GetInt64Value()
            {
                return value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Double : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly double value;

            public Double(double value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return (float)value;
            }

            public override int GetInt32Value()
            {
                return (int)value;
            }

            public override long GetInt64Value()
            {
                return (long)value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }

#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        protected sealed class Single : Number
        {
            /// <summary>
            /// The value which the receiver represents.
            /// </summary>
            private readonly float value;

            public Single(float value)
            {
                this.value = value;
            }

            public override double GetDoubleValue()
            {
                return value;
            }

            public override float GetSingleValue()
            {
                return value;
            }

            public override int GetInt32Value()
            {
                return (int)value;
            }

            public override long GetInt64Value()
            {
                return (long)value;
            }

            public override string ToString()
            {
                return value.ToString();
            }

            public override string ToString(string format)
            {
                return value.ToString(format);
            }

            public override string ToString(IFormatProvider provider)
            {
                return value.ToString(provider);
            }

            public override string ToString(string format, IFormatProvider provider)
            {
                return value.ToString(format, provider);
            }
        }
    }
}
