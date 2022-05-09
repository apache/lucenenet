﻿using Lucene.Net.Index;
using System;
using Int32 = J2N.Numerics.Int32;

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
    /// <para>
    /// Field that indexes <see cref="int"/> values
    /// for efficient range filtering and sorting. Here's an example usage:
    ///
    /// <code>
    ///     document.Add(new Int32Field(name, 6, Field.Store.NO));
    /// </code>
    ///
    /// For optimal performance, re-use the <see cref="Int32Field"/> and
    /// <see cref="Document"/> instance for more than one document:
    ///
    /// <code>
    ///     Int32Field field = new Int32Field(name, 6, Field.Store.NO);
    ///     Document document = new Document();
    ///     document.Add(field);
    ///
    ///     for (all documents) 
    ///     {
    ///         ...
    ///         field.SetInt32Value(value)
    ///         writer.AddDocument(document);
    ///         ...
    ///     }
    /// </code>
    ///
    /// See also <see cref="Int64Field"/>, <see cref="SingleField"/>, 
    /// <see cref="DoubleField"/>.</para>
    ///
    /// <para>To perform range querying or filtering against a
    /// <see cref="Int32Field"/>, use <see cref="Search.NumericRangeQuery{T}"/> or 
    /// <see cref="Search.NumericRangeFilter{T}"/>.  To sort according to a
    /// <see cref="Int32Field"/>, use the normal numeric sort types, eg
    /// <see cref="Lucene.Net.Search.SortFieldType.INT32"/>. <see cref="Int32Field"/>
    /// values can also be loaded directly from <see cref="Search.IFieldCache"/>.</para>
    ///
    /// <para>You may add the same field name as an <see cref="Int32Field"/> to
    /// the same document more than once.  Range querying and
    /// filtering will be the logical OR of all values; so a range query
    /// will hit all documents that have at least one value in
    /// the range. However sort behavior is not defined.  If you need to sort,
    /// you should separately index a single-valued <see cref="Int32Field"/>.</para>
    ///
    /// <para>An <see cref="Int32Field"/> will consume somewhat more disk space
    /// in the index than an ordinary single-valued field.
    /// However, for a typical index that includes substantial
    /// textual content per document, this increase will likely
    /// be in the noise. </para>
    ///
    /// <para>Within Lucene, each numeric value is indexed as a
    /// <em>trie</em> structure, where each term is logically
    /// assigned to larger and larger pre-defined brackets (which
    /// are simply lower-precision representations of the value).
    /// The step size between each successive bracket is called the
    /// <c>precisionStep</c>, measured in bits.  Smaller
    /// <c>precisionStep</c> values result in larger number
    /// of brackets, which consumes more disk space in the index
    /// but may result in faster range search performance.  The
    /// default value, 4, was selected for a reasonable tradeoff
    /// of disk space consumption versus performance.  You can
    /// create a custom <see cref="FieldType"/> and invoke the 
    /// <see cref="FieldType.NumericPrecisionStep"/> setter if you'd
    /// like to change the value.  Note that you must also
    /// specify a congruent value when creating 
    /// <see cref="Search.NumericRangeQuery{T}"/> or <see cref="Search.NumericRangeFilter{T}"/>.
    /// For low cardinality fields larger precision steps are good.
    /// If the cardinality is &lt; 100, it is fair
    /// to use <see cref="int.MaxValue"/>, which produces one
    /// term per value.</para>
    ///
    /// <para>For more information on the internals of numeric trie
    /// indexing, including the <see cref="Search.NumericRangeQuery{T}.PrecisionStep"/> <a
    /// href="../search/NumericRangeQuery.html#precisionStepDesc"><c>precisionStep</c></a>
    /// configuration, see <see cref="Search.NumericRangeQuery{T}"/>. The format of
    /// indexed values is described in <see cref="Util.NumericUtils"/>.</para>
    ///
    /// <para>If you only need to sort by numeric value, and never
    /// run range querying/filtering, you can index using a
    /// <c>precisionStep</c> of <see cref="int.MaxValue"/>.
    /// this will minimize disk space consumed. </para>
    ///
    /// <para>More advanced users can instead use 
    /// <see cref="Analysis.NumericTokenStream"/> directly, 
    /// when indexing numbers. this
    /// class is a wrapper around this token stream type for
    /// easier, more intuitive usage.</para>
    /// <para>
    /// NOTE: This was IntField in Lucene
    /// </para>
    /// @since 2.9
    /// </summary>
    public sealed class Int32Field : Field
    {
        /// <summary>
        /// Type for an <see cref="Int32Field"/> that is not stored:
        /// normalization factors, frequencies, and positions are omitted.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_NOT_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            OmitNorms = true,
            IndexOptions = IndexOptions.DOCS_ONLY,
            NumericType = Documents.NumericType.INT32
        }.Freeze();

        /// <summary>
        /// Type for a stored <see cref="Int32Field"/>:
        /// normalization factors, frequencies, and positions are omitted.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            OmitNorms = true,
            IndexOptions = IndexOptions.DOCS_ONLY,
            NumericType = Documents.NumericType.INT32,
            IsStored = true
        }.Freeze();

        /// <summary>
        /// Creates a stored or un-stored <see cref="Int32Field"/> with the provided value
        /// and default <c>precisionStep</c> 
        /// <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4). 
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="int"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public Int32Field(string name, int value, Store stored)
            : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            FieldsData = Int32.GetInstance(value);
        }

        /// <summary>
        /// Expert: allows you to customize the 
        /// <see cref="FieldType"/>.
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 32-bit <see cref="int"/> value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.INT32"/>. </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> or <paramref name="type"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException">if the field type does not have a 
        ///         <see cref="FieldType.NumericType"/> of <see cref="NumericType.INT32"/> </exception>
        public Int32Field(string name, int value, FieldType type)
            : base(name, type)
        {
            if (type.NumericType != Documents.NumericType.INT32)
            {
                throw new ArgumentException("type.NumericType must be NumericType.INT32 but got " + type.NumericType);
            }
            FieldsData = Int32.GetInstance(value);
        }
    }
}