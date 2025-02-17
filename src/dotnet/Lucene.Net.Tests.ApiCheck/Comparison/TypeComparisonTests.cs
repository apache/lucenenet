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
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.ApiCheck.Comparison;
using Lucene.Net.ApiCheck.Models.Config;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;

namespace Lucene.Net.Tests.ApiCheck.Comparison;

public class TypeComparisonTests
{
    private static readonly LibraryConfig _coreLibraryConfig = new LibraryConfig("core", "Lucene.Net",
        PackageNameMappings: new Dictionary<string, string>
        {
            ["org.apache.lucene.document"] = "Lucene.Net.Documents",
        },
        MavenDependencies: new List<string>(),
        TypeOverrides: new List<TypeOverride>
        {
            new TypeOverride(
                Justification: "Uses .NET numeric naming conventions",
                JavaToDotNetTypes: new Dictionary<string, string>
                {
                    ["org.apache.lucene.document.FloatDocValuesField"] = "Lucene.Net.Documents.SingleDocValuesField",
                }
            )
        });

    [InlineData(typeof(Analyzer), "class", "org.apache.lucene.analysis", "Analyzer")]
    [InlineData(typeof(TokenStream), "class", "org.apache.lucene.analysis", "TokenStream")]
    [InlineData(typeof(ICharTermAttribute), "interface", "org.apache.lucene.analysis.tokenattributes", "CharTermAttribute")]
    [InlineData(typeof(Document), "class", "org.apache.lucene.document", "Document")]
    [InlineData(typeof(SingleDocValuesField), "class", "org.apache.lucene.document", "FloatDocValuesField")]
    [InlineData(typeof(BlockTreeTermsReader<int>), "class", "org.apache.lucene.codecs", "BlockTreeTermsReader")]
    [Theory]
    public void TypesMatchTests(Type dotNetType, string javaTypeKind, string javaPackage, string javaTypeName)
    {
        var javaType = new TypeMetadata(javaPackage, javaTypeKind, javaTypeName, $"{javaPackage}.{javaTypeName}", null, [], [], [], []);
        Assert.True(TypeComparison.TypesMatch(_coreLibraryConfig, dotNetType, javaType));
    }

    [InlineData("org.apache.lucene", "Lucene.Net")]
    [InlineData("org.apache.lucene.analysis", "Lucene.Net.Analysis")]
    [InlineData("org.apache.lucene.test-framework", "Lucene.Net.TestFramework")]
    [Theory]
    public void GetExpectedDotNetNamespaceTests(string javaPackage, string dotNetNamespace)
    {
        Assert.Equal(dotNetNamespace, TypeComparison.GetExpectedDotNetNamespace(javaPackage));
    }
}
