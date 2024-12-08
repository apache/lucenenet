using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.ApiCheck.Comparison;
using Lucene.Net.ApiCheck.Models.Config;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Documents;

namespace Lucene.Net.Tests.ApiCheck.Comparison;

public class TypeComparisonTests
{
    private static readonly LibraryConfig _coreLibraryConfig = new LibraryConfig("core", "Lucene.Net",
        PackageNameMappings: new Dictionary<string, string>
        {
            ["org.apache.lucene.document"] = "Lucene.Net.Documents",
        });

    [InlineData(typeof(Analyzer), "class", "org.apache.lucene.analysis", "Analyzer")]
    [InlineData(typeof(TokenStream), "class", "org.apache.lucene.analysis", "TokenStream")]
    [InlineData(typeof(ICharTermAttribute), "interface", "org.apache.lucene.analysis.tokenattributes", "CharTermAttribute")]
    [InlineData(typeof(Document), "class", "org.apache.lucene.document", "Document")]
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
