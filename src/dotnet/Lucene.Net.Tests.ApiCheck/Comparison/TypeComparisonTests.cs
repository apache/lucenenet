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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Morfologik.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.ApiCheck.Comparison;
using Lucene.Net.ApiCheck.Extensions;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Benchmarks.ByTask;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Codecs.IntBlock;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Range;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Index.Sorter;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Tests.ApiCheck.Comparison;

public class TypeComparisonTests
{
    [InlineData(typeof(Analyzer), "class", "org.apache.lucene.analysis", "Analyzer")]
    [InlineData(typeof(TokenStream), "class", "org.apache.lucene.analysis", "TokenStream")]
    [InlineData(typeof(ICharTermAttribute), "interface", "org.apache.lucene.analysis.tokenattributes", "CharTermAttribute")]
    [InlineData(typeof(Document), "class", "org.apache.lucene.document", "Document")]
    [InlineData(typeof(SingleDocValuesField), "class", "org.apache.lucene.document", "FloatDocValuesField")]
    [InlineData(typeof(BlockTreeTermsReader<int>), "class", "org.apache.lucene.codecs", "BlockTreeTermsReader")]
    // Test for having LuceneEquivalentAttribute
    [InlineData(typeof(IMorphosyntacticTagsAttribute), "interface", "org.apache.lucene.analysis.morfologik", "MorphosyntacticTagsAttribute")]
    [InlineData(typeof(MorphosyntacticTagsAttribute), "class", "org.apache.lucene.analysis.morfologik", "MorphosyntacticTagsAttributeImpl")]
    // Test for nested types in Java where not nested in .NET, with LuceneEquivalentAttribute
    [InlineData(typeof(AbstractAllGroupHeadsCollector_GroupHead), "class", "org.apache.lucene.search.grouping", "AbstractAllGroupHeadsCollector$GroupHead")]
    // Test for nested types in Java where nested in .NET, note: the Java Name does not include the nested parent name, must include FullName
    [InlineData(typeof(FieldCache.CacheEntry), "class", "org.apache.lucene.search", "CacheEntry", "org.apache.lucene.search.FieldCache$CacheEntry")]
    // Test for interface/class naming convention differences
    [InlineData(typeof(IKeywordAttribute), "interface", "org.apache.lucene.analysis.tokenattributes", "KeywordAttribute")]
    [InlineData(typeof(KeywordAttribute), "class", "org.apache.lucene.analysis.tokenattributes", "KeywordAttributeImpl")]
    // Test for LuceneNamespaceMappingAttribute
    [InlineData(typeof(PerfRunData), "class", "org.apache.lucene.benchmark.byTask", "PerfRunData")]
    // Test for primitive-word renames in type names: Int → Int32
    [InlineData(typeof(PackedInt32s), "class", "org.apache.lucene.util.packed", "PackedInts")]
    [InlineData(typeof(Int32BlockPool), "class", "org.apache.lucene.util", "IntBlockPool")]
    // Test for primitive-word renames on a Java-nested type (flattened to Outer.Inner)
    [InlineData(typeof(PackedInt32s.Header), "class", "org.apache.lucene.util.packed", "PackedInts.Header", "org.apache.lucene.util.packed.PackedInts$Header")]
    [InlineData(typeof(Int32BlockPool.Allocator), "class", "org.apache.lucene.util", "IntBlockPool.Allocator", "org.apache.lucene.util.IntBlockPool$Allocator")]
    // Auto-derivable primitive-word renames across the various namespaces — these previously
    // carried a [LuceneType] attribute that has since been removed because the inferred lookup
    // now handles Int→Int32 / Long→Int64 / Short→Int16 / Float→Single uniformly.
    [InlineData(typeof(Int32sRef), "class", "org.apache.lucene.util", "IntsRef")]
    [InlineData(typeof(Int64sRef), "class", "org.apache.lucene.util", "LongsRef")]
    [InlineData(typeof(Int64BitSet), "class", "org.apache.lucene.util", "LongBitSet")]
    [InlineData(typeof(Int64Values), "class", "org.apache.lucene.util", "LongValues")]
    [InlineData(typeof(Int32SequenceOutputs), "class", "org.apache.lucene.util.fst", "IntSequenceOutputs")]
    [InlineData(typeof(PositiveInt32Outputs), "class", "org.apache.lucene.util.fst", "PositiveIntOutputs")]
    [InlineData(typeof(Int32sRefFSTEnum<object>), "class", "org.apache.lucene.util.fst", "IntsRefFSTEnum")]
    [InlineData(typeof(Int32Field), "class", "org.apache.lucene.document", "IntField")]
    [InlineData(typeof(Int64Field), "class", "org.apache.lucene.document", "LongField")]
    [InlineData(typeof(SingleField), "class", "org.apache.lucene.document", "FloatField")]
    [InlineData(typeof(Int32DocValuesField), "class", "org.apache.lucene.document", "IntDocValuesField")]
    [InlineData(typeof(Int64DocValuesField), "class", "org.apache.lucene.document", "LongDocValuesField")]
    [InlineData(typeof(Int16DocValuesField), "class", "org.apache.lucene.document", "ShortDocValuesField")]
    [InlineData(typeof(PackedInt64DocValuesField), "class", "org.apache.lucene.document", "PackedLongDocValuesField")]
    [InlineData(typeof(BlockTreeTermsWriter<int>), "class", "org.apache.lucene.codecs", "BlockTreeTermsWriter")]
    [InlineData(typeof(Int32IndexInput), "class", "org.apache.lucene.codecs.sep", "IntIndexInput")]
    [InlineData(typeof(Int32IndexOutput), "class", "org.apache.lucene.codecs.sep", "IntIndexOutput")]
    [InlineData(typeof(Int32StreamFactory), "class", "org.apache.lucene.codecs.sep", "IntStreamFactory")]
    [InlineData(typeof(FixedInt32BlockIndexInput), "class", "org.apache.lucene.codecs.intblock", "FixedIntBlockIndexInput")]
    [InlineData(typeof(FixedInt32BlockIndexOutput), "class", "org.apache.lucene.codecs.intblock", "FixedIntBlockIndexOutput")]
    [InlineData(typeof(VariableInt32BlockIndexInput), "class", "org.apache.lucene.codecs.intblock", "VariableIntBlockIndexInput")]
    [InlineData(typeof(VariableInt32BlockIndexOutput), "class", "org.apache.lucene.codecs.intblock", "VariableIntBlockIndexOutput")]
    [InlineData(typeof(Int64Range), "class", "org.apache.lucene.facet.range", "LongRange")]
    [InlineData(typeof(Int64RangeFacetCounts), "class", "org.apache.lucene.facet.range", "LongRangeFacetCounts")]
    [InlineData(typeof(SingleAssociationFacetField), "class", "org.apache.lucene.facet.taxonomy", "FloatAssociationFacetField")]
    [InlineData(typeof(SingleTaxonomyFacets), "class", "org.apache.lucene.facet.taxonomy", "FloatTaxonomyFacets")]
    [InlineData(typeof(Int32AssociationFacetField), "class", "org.apache.lucene.facet.taxonomy", "IntAssociationFacetField")]
    [InlineData(typeof(Int32TaxonomyFacets), "class", "org.apache.lucene.facet.taxonomy", "IntTaxonomyFacets")]
    [InlineData(typeof(TaxonomyFacetSumSingleAssociations), "class", "org.apache.lucene.facet.taxonomy", "TaxonomyFacetSumFloatAssociations")]
    [InlineData(typeof(TaxonomyFacetSumInt32Associations), "class", "org.apache.lucene.facet.taxonomy", "TaxonomyFacetSumIntAssociations")]
    [InlineData(typeof(TopOrdAndSingleQueue), "class", "org.apache.lucene.facet", "TopOrdAndFloatQueue")]
    [InlineData(typeof(TopOrdAndInt32Queue), "class", "org.apache.lucene.facet", "TopOrdAndIntQueue")]
    [InlineData(typeof(BlockJoinComparerSource), "class", "org.apache.lucene.index.sorter", "BlockJoinComparatorSource")]
    [InlineData(typeof(SingleDocValues), "class", "org.apache.lucene.queries.function.docvalues", "FloatDocValues")]
    [InlineData(typeof(Int32DocValues), "class", "org.apache.lucene.queries.function.docvalues", "IntDocValues")]
    [InlineData(typeof(Int64DocValues), "class", "org.apache.lucene.queries.function.docvalues", "LongDocValues")]
    [InlineData(typeof(DivSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "DivFloatFunction")]
    [InlineData(typeof(DualSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "DualFloatFunction")]
    [InlineData(typeof(SingleFieldSource), "class", "org.apache.lucene.queries.function.valuesource", "FloatFieldSource")]
    [InlineData(typeof(Int32FieldSource), "class", "org.apache.lucene.queries.function.valuesource", "IntFieldSource")]
    [InlineData(typeof(LinearSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "LinearFloatFunction")]
    [InlineData(typeof(Int64FieldSource), "class", "org.apache.lucene.queries.function.valuesource", "LongFieldSource")]
    [InlineData(typeof(MaxSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "MaxFloatFunction")]
    [InlineData(typeof(MinSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "MinFloatFunction")]
    [InlineData(typeof(MultiSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "MultiFloatFunction")]
    [InlineData(typeof(PowSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "PowFloatFunction")]
    [InlineData(typeof(ProductSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "ProductFloatFunction")]
    [InlineData(typeof(RangeMapSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "RangeMapFloatFunction")]
    [InlineData(typeof(ReciprocalSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "ReciprocalFloatFunction")]
    [InlineData(typeof(ScaleSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "ScaleFloatFunction")]
    [InlineData(typeof(Int16FieldSource), "class", "org.apache.lucene.queries.function.valuesource", "ShortFieldSource")]
    [InlineData(typeof(SimpleSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "SimpleFloatFunction")]
    [InlineData(typeof(SumSingleFunction), "class", "org.apache.lucene.queries.function.valuesource", "SumFloatFunction")]
    // FieldComparer / FieldComparerSource: 'Comparator' → 'Comparer' rename
    [InlineData(typeof(FieldComparerSource), "class", "org.apache.lucene.search", "FieldComparatorSource")]
    [InlineData(typeof(FieldComparer<object>), "class", "org.apache.lucene.search", "FieldComparator")]
    // H-1: de-nested enums (Java Outer$Inner -> .NET top-level Inner, no [LuceneType] attribute).
    [InlineData(typeof(Occur), "enum", "org.apache.lucene.search", "Occur", "org.apache.lucene.search.BooleanClause$Occur")]
    [InlineData(typeof(Lucene.Net.Index.IndexOptions), "enum", "org.apache.lucene.index", "IndexOptions", "org.apache.lucene.index.FieldInfo$IndexOptions")]
    // BUG-5: a type kept as 'FooImpl' identically on both sides (the Impl strip is one-directional).
    [InlineData(typeof(Lucene.Net.Analysis.MockUTF16TermAttributeImpl), "class", "org.apache.lucene.analysis", "MockUTF16TermAttributeImpl")]
    [Theory]
    public void TypesMatchTests(Type dotNetType, string javaTypeKind, string javaPackage, string javaTypeName, string? javaFullName = null)
    {
        var javaType = new TypeMetadata(javaPackage, javaTypeKind, javaTypeName, javaFullName ?? $"{javaPackage}.{javaTypeName}", null, [], [], [], []);
        Assert.True(TypeComparison.TypesMatch(dotNetType, javaType));
    }

    // Test for interface/class naming convention differences
    [InlineData(typeof(IKeywordAttribute), "class", "org.apache.lucene.analysis.tokenattributes", "KeywordAttributeImpl")]
    [InlineData(typeof(KeywordAttribute), "interface", "org.apache.lucene.analysis.tokenattributes", "KeywordAttribute")]
    [Theory]
    public void NegativeTypesMatchTests(Type dotNetType, string javaTypeKind, string javaPackage, string javaTypeName, string? javaFullName = null)
    {
        var javaType = new TypeMetadata(javaPackage, javaTypeKind, javaTypeName, javaFullName ?? $"{javaPackage}.{javaTypeName}", null, [], [], [], []);
        Assert.False(TypeComparison.TypesMatch(dotNetType, javaType));
    }

    // H-4: well-known type equivalences, including the closed->open generic reduction
    // (IComparable<Term> must match via the IComparable<> key) and the non-generic IDictionary.
    [InlineData(typeof(System.Collections.IDictionary), "java.util.Map")]
    [InlineData(typeof(IDictionary<string, int>), "java.util.Map")]
    [InlineData(typeof(System.Text.StringBuilder), "java.lang.StringBuilder")]
    [InlineData(typeof(System.Globalization.CultureInfo), "java.util.Locale")]
    [InlineData(typeof(System.Xml.XmlElement), "org.w3c.dom.Element")]
    [InlineData(typeof(IComparable<Term>), "java.lang.Comparable")]
    [InlineData(typeof(J2N.Numerics.Int64), "java.lang.Long")]
    [InlineData(typeof(J2N.Threading.Atomic.AtomicInt32), "java.util.concurrent.atomic.AtomicInteger")]
    [Theory]
    public void WellKnownEquivalentTypes_Match(Type dotNetType, string javaTypeName)
    {
        Assert.True(TypeComparison.TypeMatchesFullNameAnyKind(dotNetType, javaTypeName));
    }

    // H-4: Spatial4n / ICU4N equivalences keyed by .NET full name (assemblies the ApiCheck
    // project does not reference at compile time). These can't be expressed as typeof in the
    // production map, so verify via the name-derived TypesMatch path using a synthetic .NET type
    // proxy is not possible; instead assert the negative cases below are not affected. The
    // positive Spatial4n cases are exercised by the integration diff. Here we cover the
    // closed->open reduction does not over-match an unrelated Java type.
    [InlineData(typeof(IComparable<Term>), "java.lang.Runnable")]
    [InlineData(typeof(System.Collections.IDictionary), "java.util.List")]
    [InlineData(typeof(System.Text.StringBuilder), "java.lang.String")]
    [Theory]
    public void WellKnownEquivalentTypes_DoNotOverMatch(Type dotNetType, string javaTypeName)
    {
        Assert.False(TypeComparison.TypeMatchesFullNameAnyKind(dotNetType, javaTypeName));
    }

    // H-6: generic/non-generic same-name base split. FieldComparer<T> derives from the
    // non-generic FieldComparer, while the Java FieldComparator derives from java.lang.Object.
    [Fact]
    public void BaseTypesMatch_GenericNonGenericSplit()
    {
        Assert.True(TypeComparison.BaseTypesMatch(typeof(FieldComparer<object>), "java.lang.Object"));
    }

    // H-6: a direct base-type match still works through BaseTypesMatch.
    [Fact]
    public void BaseTypesMatch_DirectObjectBase()
    {
        // A type whose .NET base is object and whose Java base is java.lang.Object.
        Assert.True(TypeComparison.BaseTypesMatch(typeof(object), null));
        Assert.True(TypeComparison.BaseTypesMatch(typeof(Document), "java.lang.Object"));
    }

    // H-6: not every object-based .NET type should match an arbitrary Java base; a genuine
    // re-rooting (no matching interface, not a generic split) must still be reported.
    [Fact]
    public void BaseTypesMatch_GenuineDifference_DoesNotMatch()
    {
        Assert.False(TypeComparison.BaseTypesMatch(typeof(Document), "org.apache.lucene.search.Collector"));
    }

    // BUG-1 / H-4: InterfacesMatch is subset-based (every Java interface represented on .NET),
    // tolerates .NET-only additions, and filters the Cloneable/Serializable JVM markers.
    [Fact]
    public void InterfacesMatch_SubsetWithDotNetAdditions()
    {
        // Java declares Comparable; .NET adds IComparable<T> plus an extra IEquatable<T>.
        Assert.True(TypeComparison.InterfacesMatch(
            [typeof(IComparable<Term>), typeof(IEquatable<Term>)],
            ["java.lang.Comparable"]));
    }

    [Fact]
    public void InterfacesMatch_MarkerInterfacesFilteredOut()
    {
        // Cloneable and Serializable have no .NET counterpart; they must not count as missing.
        Assert.True(TypeComparison.InterfacesMatch(
            [],
            ["java.lang.Cloneable", "java.io.Serializable"]));
    }

    [Fact]
    public void InterfacesMatch_GenuinelyMissingJavaInterface_DoesNotMatch()
    {
        // A non-marker Java interface with no .NET representation is still reported.
        Assert.False(TypeComparison.InterfacesMatch(
            [typeof(IComparable<Term>)],
            ["java.lang.Comparable", "java.io.DataInput"]));
    }
}
