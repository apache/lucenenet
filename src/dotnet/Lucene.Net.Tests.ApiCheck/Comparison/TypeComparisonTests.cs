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
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;

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
    [InlineData(typeof(PerfRunData), "class", "org.apache.lucene.benchmarks.byTask", "PerfRunData")]
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
