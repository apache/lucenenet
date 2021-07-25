using Lucene.Net.Index;
using System;
using Double = J2N.Numerics.Double;

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
    /// Field that indexes <see cref="double"/> values
    /// for efficient range filtering and sorting. Here's an example usage:
    ///
    /// <code>
    /// document.Add(new DoubleField(name, 6.0, Field.Store.NO));
    /// </code>
    ///
    /// For optimal performance, re-use the <see cref="DoubleField"/> and
    /// <see cref="Document"/> instance for more than one document:
    ///
    /// <code>
    ///     DoubleField field = new DoubleField(name, 0.0, Field.Store.NO);
    ///     Document document = new Document();
    ///     document.Add(field);
    ///
    ///     for (all documents)
    ///     {
    ///         ...
    ///         field.SetDoubleValue(value)
    ///         writer.AddDocument(document);
    ///         ...
    ///     }
    /// </code>
    ///
    /// See also <seealso cref="Int32Field"/>, <seealso cref="Int64Field"/>, 
    /// <see cref="SingleField"/>.</para>
    ///
    /// <para>To perform range querying or filtering against a
    /// <see cref="DoubleField"/>, use <see cref="Search.NumericRangeQuery"/> or 
    /// <see cref="Search.NumericRangeFilter{T}"/>.  To sort according to a
    /// <see cref="DoubleField"/>, use the normal numeric sort types, eg
    /// <see cref="Lucene.Net.Search.SortFieldType.DOUBLE"/>. <see cref="DoubleField"/>
    /// values can also be loaded directly from <see cref="Search.IFieldCache"/>.</para>
    ///
    /// <para>You may add the same field name as an <see cref="DoubleField"/> to
    /// the same document more than once.  Range querying and
    /// filtering will be the logical OR of all values; so a range query
    /// will hit all documents that have at least one value in
    /// the range. However sort behavior is not defined.  If you need to sort,
    /// you should separately index a single-valued <see cref="DoubleField"/>.</para>
    ///
    /// <para>A <see cref="DoubleField"/> will consume somewhat more disk space
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
    /// indexing, including the <see cref="Search.NumericRangeQuery{T}.PrecisionStep"/> (<a
    /// href="../search/NumericRangeQuery.html#precisionStepDesc"><c>precisionStep</c></a>)
    /// configuration, see <see cref="Search.NumericRangeQuery{T}"/>. The format of
    /// indexed values is described in <see cref="Util.NumericUtils"/>.</para>
    ///
    /// <para>If you only need to sort by numeric value, and never
    /// run range querying/filtering, you can index using a
    /// <c>precisionStep</c> of <see cref="int.MaxValue"/>.
    /// this will minimize disk space consumed. </para>
    ///
    /// <para>More advanced users can instead use 
    /// <see cref="Analysis.NumericTokenStream"/> directly, when indexing numbers. This
    /// class is a wrapper around this token stream type for
    /// easier, more intuitive usage.</para>
    ///
    /// @since 2.9
    /// </summary>
    public sealed class DoubleField : Field
    {
        /// <summary>
        /// Type for a <see cref="DoubleField"/> that is not stored:
        /// normalization factors, frequencies, and positions are omitted.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_NOT_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            OmitNorms = true,
            IndexOptions = IndexOptions.DOCS_ONLY,
            NumericType = Documents.NumericType.DOUBLE
        }.Freeze();

        /// <summary>
        /// Type for a stored <see cref="DoubleField"/>:
        /// normalization factors, frequencies, and positions are omitted.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE_STORED = new FieldType
        {
            IsIndexed = true,
            IsTokenized = true,
            OmitNorms = true,
            IndexOptions = IndexOptions.DOCS_ONLY,
            NumericType = Documents.NumericType.DOUBLE,
            IsStored = true
        }.Freeze();

        /// <summary>
        /// Creates a stored or un-stored <see cref="DoubleField"/> with the provided value
        /// and default <c>precisionStep</c> 
        /// <see cref="Util.NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit <see cref="double"/> value </param>
        /// <param name="stored"> <see cref="Field.Store.YES"/> if the content should also be stored </param>
        /// <exception cref="ArgumentNullException"> if the field name is <c>null</c>.  </exception>
        public DoubleField(string name, double value, Store stored)
            : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            FieldsData = Double.GetInstance(value);
        }

        /// <summary>
        /// Expert: allows you to customize the <see cref="FieldType"/>. 
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> 64-bit double value </param>
        /// <param name="type"> customized field type: must have <see cref="FieldType.NumericType"/>
        ///         of <see cref="NumericType.DOUBLE"/>. </param>
        /// <exception cref="ArgumentNullException"> if the field name or type is <c>null</c>, or
        ///          if the field type does not have a <see cref="NumericType.DOUBLE"/> <see cref="FieldType.NumericType"/> </exception>
        public DoubleField(string name, double value, FieldType type)
            : base(name, type)
        {
            if (type.NumericType != Documents.NumericType.DOUBLE)
            {
                throw new ArgumentException("type.NumericType must be NumericType.DOUBLE but got " + type.NumericType);
            }
            FieldsData = Double.GetInstance(value);
        }
    }
}