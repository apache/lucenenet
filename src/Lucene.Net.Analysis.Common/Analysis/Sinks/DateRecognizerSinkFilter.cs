// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Sinks
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
    /// Attempts to parse the <see cref="ICharTermAttribute.Buffer"/> as a Date using either the 
    /// <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/> or 
    /// <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/> methods.
    /// If a format is passed, <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/> 
    /// will be used, and the format must strictly match one of the specified formats as specified in the MSDN documentation.
    /// If the value is a Date, it will add it to the sink.
    /// </summary>
    public class DateRecognizerSinkFilter : TeeSinkTokenFilter.SinkFilter
    {
        protected DateTimeStyles m_style;
        protected ICharTermAttribute m_termAtt;
        protected IFormatProvider m_culture;
        protected string[] m_formats;

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/> using the current culture and <see cref="DateTimeStyles.None"/>.
        /// Loosely matches standard DateTime formats using <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        public DateRecognizerSinkFilter()
            : this((string[])null, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/> using the supplied culture and <see cref="DateTimeStyles.None"/>.
        /// Loosely matches standard DateTime formats using <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        public DateRecognizerSinkFilter(IFormatProvider culture)
            : this((string[])null, culture, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/> using the current culture and <see cref="DateTimeStyles.None"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="format">The allowable format of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, it must match the format of the date exactly to get a match.</param>
        public DateRecognizerSinkFilter(string format)
           : this(new string[] { format }, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/>  using the current culture and <see cref="DateTimeStyles.None"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="formats">An array of allowable formats of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, one of them must match the format of the date exactly to get a match.</param>
        public DateRecognizerSinkFilter(string[] formats)
            : this(formats, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/>  using the supplied culture and <see cref="DateTimeStyles"/>.
        /// Loosely matches standard DateTime formats using <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. 
        /// A typical value to specify is <see cref="DateTimeStyles.None"/></param>
        public DateRecognizerSinkFilter(IFormatProvider culture, DateTimeStyles style)
            : this((string[])null, culture, style)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/>  using the supplied format, culture and <see cref="DateTimeStyles.None"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="format">The allowable format of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, it must match the format of the date exactly to get a match.</param>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        public DateRecognizerSinkFilter(string format, IFormatProvider culture)
           : this(new string[] { format }, culture, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/>  using the supplied formats, culture and <see cref="DateTimeStyles.None"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="formats">An array of allowable formats of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, one of them must match the format of the date exactly to get a match.</param>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        public DateRecognizerSinkFilter(string[] formats, IFormatProvider culture)
            : this(formats, culture, DateTimeStyles.None)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/> using the supplied format, culture and <see cref="DateTimeStyles"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="format">The allowable format of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, it must match the format of the date exactly to get a match.</param>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. 
        /// A typical value to specify is <see cref="DateTimeStyles.None"/></param>
        public DateRecognizerSinkFilter(string format, IFormatProvider culture, DateTimeStyles style)
            : this(new string[] { format }, culture, style)
        { }

        /// <summary>
        /// Creates a new instance of <see cref="DateRecognizerSinkFilter"/> using the supplied formats, culture and <see cref="DateTimeStyles"/>.
        /// Strictly matches the supplied DateTime formats using <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// </summary>
        /// <param name="formats">An array of allowable formats of the <see cref="ICharTermAttribute.Buffer"/>.
        /// If supplied, one of them must match the format of the date exactly to get a match.</param>
        /// <param name="culture">An object that supplies culture-specific format information</param>
        /// <param name="style">A bitwise combination of enumeration values that indicates the permitted format of s. 
        /// A typical value to specify is <see cref="DateTimeStyles.None"/></param>
        public DateRecognizerSinkFilter(string[] formats, IFormatProvider culture, DateTimeStyles style)
        {
            this.m_culture = culture;
            this.m_style = style;
            this.m_formats = formats;
        }

        public override bool Accept(AttributeSource source)
        {
            if (m_termAtt is null)
            {
                m_termAtt = source.AddAttribute<ICharTermAttribute>();
            }

            //We don't care about the date, just that we can parse it as a date
#if FEATURE_NUMBER_PARSE_READONLYSPAN
            if (m_formats is null)
            {
                return DateTime.TryParse(new ReadOnlySpan<char>(m_termAtt.Buffer, 0, m_termAtt.Length), m_culture, m_style, out _);
            }
            else
            {
                return DateTime.TryParseExact(new ReadOnlySpan<char>(m_termAtt.Buffer, 0, m_termAtt.Length), m_formats, m_culture, m_style, out _);
            }
#else
            if (m_formats is null)
            {
                return DateTime.TryParse(m_termAtt.ToString(), m_culture, m_style, out _);
            }
            else
            {
                return DateTime.TryParseExact(m_termAtt.ToString(), m_formats, m_culture, m_style, out _);
            }
#endif
        }
    }
}