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

    public class CtorExample
    {
        public CtorExample() { }
        public CtorExample(int x) { }
        public CtorExample(int x, string y) { }
        public CtorExample(int[] xs) { }
    }

    private static ConstructorMetadata JavaCtor(params (string Name, string Type)[] parameters)
        => new(
            Parameters: parameters
                .Select(p => new ParameterMetadata(p.Name, p.Type))
                .ToList(),
            Modifiers: new List<string> { "public" },
            IsVarArgs: false);

    [Fact]
    public void ConstructorsMatch_NoArgs_Matches()
    {
        var ctor = typeof(CtorExample).GetConstructor(Type.EmptyTypes)!;
        Assert.True(MemberComparison.ConstructorsMatch(ctor, JavaCtor()));
    }

    [Fact]
    public void ConstructorsMatch_DifferentParamCount_DoesNotMatch()
    {
        var ctor = typeof(CtorExample).GetConstructor(Type.EmptyTypes)!;
        Assert.False(MemberComparison.ConstructorsMatch(ctor, JavaCtor(("x", "int"))));
    }

    [Fact]
    public void ConstructorsMatch_PrimitiveInt_Matches()
    {
        var ctor = typeof(CtorExample).GetConstructor(new[] { typeof(int) })!;
        Assert.True(MemberComparison.ConstructorsMatch(ctor, JavaCtor(("x", "int"))));
    }

    [Fact]
    public void ConstructorsMatch_StringAndInt_Matches()
    {
        var ctor = typeof(CtorExample).GetConstructor(new[] { typeof(int), typeof(string) })!;
        Assert.True(MemberComparison.ConstructorsMatch(ctor, JavaCtor(("x", "int"), ("y", "java.lang.String"))));
    }

    [Fact]
    public void ConstructorsMatch_PrimitiveArray_Matches()
    {
        var ctor = typeof(CtorExample).GetConstructor(new[] { typeof(int[]) })!;
        Assert.True(MemberComparison.ConstructorsMatch(ctor, JavaCtor(("xs", "int[]"))));
    }

    [Fact]
    public void ConstructorsMatch_MismatchedTypes_DoesNotMatch()
    {
        var ctor = typeof(CtorExample).GetConstructor(new[] { typeof(int) })!;
        Assert.False(MemberComparison.ConstructorsMatch(ctor, JavaCtor(("x", "long"))));
    }
}
