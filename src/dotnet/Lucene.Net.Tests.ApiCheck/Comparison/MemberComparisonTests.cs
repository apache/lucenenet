using Lucene.Net.ApiCheck.Comparison;
using Lucene.Net.ApiCheck.Models.JavaApi;
using System.Reflection;

namespace Lucene.Net.Tests.ApiCheck.Comparison;

public class MemberComparisonTests
{
    public class Example
    {
        protected int foo1 = 1;
        protected int _foo2 = 2;
        protected int m_foo3 = 3;
        protected static int s_foo4 = 4;
    }

    [InlineData("foo1", "protected", "foo1")]
    [InlineData("_foo2", "protected", "foo2")]
    [InlineData("m_foo3", "protected", "foo3")]
    [InlineData("s_foo4", "protected static", "foo4")]
    [Theory]
    public void FieldsMatchTests_Name(string dotNetFieldName, string javaModifiers, string javaFieldName)
    {
        var dotNetField = typeof(Example).GetField(dotNetFieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Field not found");
        var javaField = new FieldMetadata(
            Name: javaFieldName,
            Type: dotNetField.FieldType.Name, // ignoring this for now
            Modifiers: javaModifiers.Split(" ").ToList(),
            IsStatic: javaModifiers.Contains("static"));
        Assert.True(MemberComparison.FieldNamesMatch(dotNetField, javaField));
    }
}
