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
}
