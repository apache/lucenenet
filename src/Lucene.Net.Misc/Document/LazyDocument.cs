using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// Defers actually loading a field's value until you ask
    ///  for it.  You must not use the returned Field instances
    ///  after the provided reader has been closed. </summary>
    /// <seealso cref="GetField(FieldInfo)"/>
    public class LazyDocument
    {
        private readonly IndexReader reader;
        private readonly int docID;

        // null until first field is loaded
        private Document doc;

        private readonly IDictionary<int, IList<LazyField>> fields = new Dictionary<int, IList<LazyField>>(); // LUCENENET: marked readonly
        private readonly ISet<string> fieldNames = new JCG.HashSet<string>(); // LUCENENET: marked readonly

        public LazyDocument(IndexReader reader, int docID)
        {
            this.reader = reader;
            this.docID = docID;
        }

        /// <summary>
        /// Creates an IndexableField whose value will be lazy loaded if and 
        /// when it is used. 
        /// <para>
        /// <b>NOTE:</b> This method must be called once for each value of the field 
        /// name specified in sequence that the values exist.  This method may not be 
        /// used to generate multiple, lazy, IndexableField instances refering to 
        /// the same underlying IndexableField instance.
        /// </para>
        /// <para>
        /// The lazy loading of field values from all instances of IndexableField 
        /// objects returned by this method are all backed by a single Document 
        /// per LazyDocument instance.
        /// </para>
        /// </summary>
        public virtual IIndexableField GetField(FieldInfo fieldInfo)
        {
            fieldNames.Add(fieldInfo.Name);
            if (!fields.TryGetValue(fieldInfo.Number, out IList<LazyField> values) || null == values)
            {
                values = new JCG.List<LazyField>();
                fields[fieldInfo.Number] = values;
            }

            LazyField value = new LazyField(this, fieldInfo.Name, fieldInfo.Number);
            values.Add(value);

            UninterruptableMonitor.Enter(this);
            try
            {
                // edge case: if someone asks this LazyDoc for more LazyFields
                // after other LazyFields from the same LazyDoc have been
                // actuallized, we need to force the doc to be re-fetched
                // so the new LazyFields are also populated.
                doc = null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
            return value;
        }

        /// <summary>
        /// non-private for test only access
        /// @lucene.internal 
        /// </summary>
        internal virtual Document GetDocument()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (doc is null)
                {
                    try
                    {
                        doc = reader.Document(docID, fieldNames);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        throw IllegalStateException.Create("unable to load document", ioe);
                    }
                }
                return doc;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // :TODO: synchronize to prevent redundent copying? (sync per field name?)
        private void FetchRealValues(string name, int fieldNum)
        {
            Document d = GetDocument();

            fields.TryGetValue(fieldNum, out IList<LazyField> lazyValues);
            IIndexableField[] realValues = d.GetFields(name);

            if (Debugging.AssertsEnabled) Debugging.Assert(realValues.Length <= lazyValues.Count,
                "More lazy values then real values for field: {0}", name);

            for (int i = 0; i < lazyValues.Count; i++)
            {
                LazyField f = lazyValues[i];
                if (null != f)
                {
                    f.realValue = realValues[i];
                }
            }
        }


        /// <summary>
        /// @lucene.internal 
        /// </summary>
        public class LazyField : IIndexableField, IFormattable
        {
            private readonly LazyDocument outerInstance;

            internal string name;
            internal int fieldNum;
            internal volatile IIndexableField realValue = null;

            internal LazyField(LazyDocument outerInstance, string name, int fieldNum)
            {
                this.outerInstance = outerInstance;
                this.name = name;
                this.fieldNum = fieldNum;
            }

            /// <summary>
            /// non-private for test only access
            /// @lucene.internal 
            /// </summary>
            public virtual bool HasBeenLoaded => null != realValue;

            internal virtual IIndexableField GetRealValue()
            {
                if (null == realValue)
                {
                    outerInstance.FetchRealValues(name, fieldNum);
                }
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(HasBeenLoaded, "field value was not lazy loaded");
                    Debugging.Assert(realValue.Name.Equals(Name, StringComparison.Ordinal), "realvalue name != name: {0} != {1}", realValue.Name, Name);
                }

                return realValue;
            }

            /// <summary>
            /// The field's name
            /// </summary>
            public virtual string Name => name;

            /// <summary>
            /// Gets the boost factor on this field.
            /// </summary>
            public virtual float Boost => 1.0f;

            /// <summary>
            /// Non-null if this field has a binary value. </summary>
            public virtual BytesRef GetBinaryValue()
            {
                return GetRealValue().GetBinaryValue();
            }

            /// <summary>
            /// The value of the field as a <see cref="string"/>, or <c>null</c>. If <c>null</c>, the <see cref="TextReader"/> value or
            /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
            /// <see cref="GetBinaryValue()"/> must be set.
            /// </summary>
            /// <returns>The string representation of the value if it is either a <see cref="string"/> or numeric type.</returns>
            public virtual string GetStringValue()
            {
                return GetRealValue().GetStringValue();
            }

            /// <summary>
            /// The value of the field as a <see cref="string"/>, or <c>null</c>. If <c>null</c>, the <see cref="TextReader"/> value or
            /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
            /// <see cref="GetBinaryValue()"/> must be set.
            /// </summary>
            /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
            /// <returns>The string representation of the value if it is either a <see cref="string"/> or numeric type.</returns>
            // LUCENENET specific - created overload so we can format an underlying numeric type using specified provider
            public virtual string GetStringValue(IFormatProvider provider) 
            {
                return GetRealValue().GetStringValue(provider);
            }

            /// <summary>
            /// The value of the field as a <see cref="string"/>, or <c>null</c>. If <c>null</c>, the <see cref="TextReader"/> value or
            /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
            /// <see cref="GetBinaryValue()"/> must be set.
            /// </summary>
            /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
            /// <returns>The string representation of the value if it is either a <see cref="string"/> or numeric type.</returns>
            // LUCENENET specific - created overload so we can format an underlying numeric type using specified format
            public virtual string GetStringValue(string format) 
            {
                return GetRealValue().GetStringValue(format);
            }

            /// <summary>
            /// The value of the field as a <see cref="string"/>, or <c>null</c>. If <c>null</c>, the <see cref="TextReader"/> value or
            /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
            /// <see cref="GetBinaryValue()"/> must be set.
            /// </summary>
            /// <param name="format">A standard or custom numeric format string. This parameter has no effect if this field is non-numeric.</param>
            /// <param name="provider">An object that supplies culture-specific formatting information. This parameter has no effect if this field is non-numeric.</param>
            /// <returns>The string representation of the value if it is either a <see cref="string"/> or numeric type.</returns>
            // LUCENENET specific - created overload so we can format an underlying numeric type using specified format and provider
            public virtual string GetStringValue(string format, IFormatProvider provider) 
            {
                return GetRealValue().GetStringValue(format, provider);
            }

            /// <summary>
            /// The value of the field as a <see cref="TextReader"/>, or <c>null</c>. If <c>null</c>, the <see cref="string"/> value or
            /// binary value is used. Exactly one of <see cref="GetStringValue()"/>, <see cref="GetReaderValue()"/>, and
            /// <see cref="GetBinaryValue()"/> must be set.
            /// </summary>
            public virtual TextReader GetReaderValue()
            {
                return GetRealValue().GetReaderValue();
            }

            [Obsolete("In .NET, use of this method will cause boxing/unboxing. Instead, use the NumericType property to check the underlying type and call the appropriate GetXXXValue() method to retrieve the value.")]
            public virtual object GetNumericValue()
            {
                return GetRealValue().GetNumericValue();
            }

            /// <summary>
            /// Gets the <see cref="NumericFieldType"/> of the underlying value, or <see cref="NumericFieldType.NONE"/> if the value is not set or non-numeric.
            /// <para/>
            /// Expert: The difference between this property and <see cref="FieldType.NumericType"/> is 
            /// this is represents the current state of the field (whether being written or read) and the
            /// <see cref="FieldType"/> property represents instructions on how the field will be written,
            /// but does not re-populate when reading back from an index (it is write-only).
            /// <para/>
            /// In Java, the numeric type was determined by checking the type of  
            /// <see cref="GetNumericValue()"/>. However, since there are no reference number
            /// types in .NET, using <see cref="GetNumericValue()"/> so will cause boxing/unboxing. It is
            /// therefore recommended to use this property to check the underlying type and the corresponding 
            /// <c>Get*Value()</c> method to retrieve the value.
            /// <para/>
            /// NOTE: Since Lucene codecs do not support <see cref="NumericFieldType.BYTE"/> or <see cref="NumericFieldType.INT16"/>,
            /// fields created with these types will always be <see cref="NumericFieldType.INT32"/> when read back from the index.
            /// </summary>
            // LUCENENET specific - Since we have no numeric reference types in .NET, this method was added to check
            // the numeric type of the inner field without boxing/unboxing.
            public virtual NumericFieldType NumericType => GetRealValue().NumericType;

            /// <summary>
            /// Returns the field value as <see cref="byte"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Byte, since we have no Number class in .NET
            public virtual byte? GetByteValue()
            {
                return GetRealValue().GetByteValue();
            }

            /// <summary>
            /// Returns the field value as <see cref="short"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Short, since we have no Number class in .NET
            public virtual short? GetInt16Value()
            {
                return GetRealValue().GetInt16Value();
            }

            /// <summary>
            /// Returns the field value as <see cref="int"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Int32, since we have no Number class in .NET
            public virtual int? GetInt32Value()
            {
                return GetRealValue().GetInt32Value();
            }

            /// <summary>
            /// Returns the field value as <see cref="long"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Int64, since we have no Number class in .NET
            public virtual long? GetInt64Value()
            {
                return GetRealValue().GetInt64Value();
            }

            /// <summary>
            /// Returns the field value as <see cref="float"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Single, since we have no Number class in .NET
            public virtual float? GetSingleValue()
            {
                return GetRealValue().GetSingleValue();
            }

            /// <summary>
            /// Returns the field value as <see cref="double"/> or <c>null</c> if the type
            /// is non-numeric.
            /// </summary>
            /// <returns>The field value or <c>null</c> if the type is non-numeric.</returns>
            // LUCENENET specific - created overload for Double, since we have no Number class in .NET
            public virtual double? GetDoubleValue()
            {
                return GetRealValue().GetDoubleValue();
            }

            /// <summary>
            /// Returns the <see cref="Documents.FieldType"/> for this field as type <see cref="Documents.FieldType"/>. </summary>
            public virtual FieldType FieldType => GetRealValue().IndexableFieldType as FieldType;

            /// <summary>
            /// Returns the <see cref="Documents.FieldType"/> for this field as type <see cref="IIndexableFieldType"/>. </summary>
            public virtual IIndexableFieldType IndexableFieldType => GetRealValue().IndexableFieldType;

            public virtual TokenStream GetTokenStream(Analyzer analyzer)
            {
                return GetRealValue().GetTokenStream(analyzer);
            }

            // LUCENENET specific - method added for better .NET compatibility
            public override string ToString()
            {
                return ToString(null, J2N.Text.StringFormatter.CurrentCulture);
            }

            // LUCENENET specific - method added for better .NET compatibility
            public virtual string ToString(string format)
            {
                return ToString(format, J2N.Text.StringFormatter.CurrentCulture);
            }

            // LUCENENET specific - method added for better .NET compatibility
            public virtual string ToString(IFormatProvider provider)
            {
                return ToString(null, provider);
            }

            // LUCENENET specific - method added for better .NET compatibility
            public virtual string ToString(string format,IFormatProvider provider)
            {
                IIndexableField realValue = GetRealValue();
                if(realValue is IFormattable formattable)
                {
                    return formattable.ToString(format, provider);
                }
                else
                {
                    return realValue.ToString();
                }
            }
        }
    }
}