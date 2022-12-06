using System;
using System.Globalization;

namespace Lucene.Net.Util
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
    /// A LUCENENET specific class that represents a numeric format. This class
    /// mimicks the design of Java's NumberFormat class, which unlike the
    /// <see cref="NumberFormatInfo"/> class in .NET, can be subclassed.
    /// </summary>
    // LUCENENET NOTE: Ideally, the design of Lucene.NET would be changed to accept a
    // NumberFormatInfo object instead of using this, or better yet be changed to use IFormatProvider
    // and/or ICustomFormatter, but since Lucene is using inheritance
    // and passing this class around to different methods, that would require some major refactoring.
    // We should probably look into doing that in vNext. We should also look into supporting all of .NET's numeric
    // types instead of just the ones that Java supports, as well.
    public abstract class NumberFormat
    {
        private readonly IFormatProvider formatProvider;

        //private int maximumIntegerDigits;
        //private int minimumIntegerDigits;
        //private int maximumFractionDigits;
        //private int minimumFractionDigits;

        protected NumberFormat(IFormatProvider formatProvider) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.formatProvider = formatProvider;
        }

        public IFormatProvider FormatProvider => formatProvider;

        public virtual string Format(object number)
        {
            string format = GetNumberFormat();

            if (number is J2N.Numerics.Number num)
            {
                return num.ToString(format, formatProvider);
            }
            else if (number is int i)
            {
                return i.ToString(format, formatProvider);
            }
            else if (number is long l)
            {
                return l.ToString(format, formatProvider);
            }
            else if (number is short s)
            {
                return s.ToString(format, formatProvider);
            }
            else if (number is float f)
            {
                return J2N.Numerics.Single.ToString(f, format, formatProvider);
            }
            else if (number is double d)
            {
                return J2N.Numerics.Double.ToString(d, format, formatProvider);
            }
            else if (number is decimal dec)
            {
                return dec.ToString(format, formatProvider);
            }

            throw new ArgumentException("Cannot format given object as a Number");
        }

        public virtual string Format(double number)
        {
            string format = GetNumberFormat();
            return J2N.Numerics.Double.ToString(number, format, formatProvider);
        }

        public virtual string Format(long number)
        {
            string format = GetNumberFormat();
            return number.ToString(format, formatProvider);
        }

        /// <summary>
        /// When overridden in a subclass, provides the numeric format as a <see cref="string"/>.
        /// Generally, this is the same format that is passed into the <see cref="M:string.Format(IFormatProvider, string, object)"/>
        /// method.
        /// </summary>
        /// <returns>A numeric format string.</returns>
        protected virtual string GetNumberFormat()
        {
            return null;
        }

        public abstract J2N.Numerics.Number Parse(string source);

        public override string ToString()
        {
            return base.ToString() + " - " + GetNumberFormat() + " - " + formatProvider.ToString();
        }

        // LUCENENET TODO: Add additional functionality to edit the NumberFormatInfo
        // properties, which provides somewhat similar functionality to the below Java
        // getters and setters.

        //public virtual int MaximumIntegerDigits
        //{
        //    get { return this.maximumIntegerDigits; }
        //}

        //public virtual void SetMaximumIntegerDigits(int newValue)
        //{
        //    this.maximumIntegerDigits = Math.Max(0, newValue);
        //    if (maximumIntegerDigits < minimumIntegerDigits)
        //    {
        //        minimumIntegerDigits = maximumIntegerDigits;
        //    }
        //}

        //public virtual int MinimumIntegerDigits
        //{
        //    get { return this.minimumIntegerDigits; }
        //}

        //public virtual void SetMinimumIntegerDigits(int newValue)
        //{
        //    this.minimumIntegerDigits = Math.Max(0, newValue);
        //    if (minimumIntegerDigits > maximumIntegerDigits)
        //    {
        //        maximumIntegerDigits = minimumIntegerDigits;
        //    }
        //}

        //public virtual int MaximumFractionDigits
        //{
        //    get { return this.maximumFractionDigits; }
        //}

        //public virtual void SetMaximumFractionDigits(int newValue)
        //{
        //    maximumFractionDigits = Math.Max(0, newValue);
        //    if (maximumFractionDigits < minimumFractionDigits)
        //    {
        //        minimumFractionDigits = maximumFractionDigits;
        //    }
        //}

        //public virtual int MinimumFractionDigits
        //{
        //    get { return this.minimumFractionDigits; }
        //}

        //public void SetMinimumFractionDigits(int newValue)
        //{
        //    minimumFractionDigits = Math.Max(0, newValue);
        //    if (maximumFractionDigits < minimumFractionDigits)
        //    {
        //        maximumFractionDigits = minimumFractionDigits;
        //    }
        //}
    }
}
