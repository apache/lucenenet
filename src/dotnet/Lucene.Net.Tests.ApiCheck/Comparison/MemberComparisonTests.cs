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

    public class FilterFieldExample
    {
        protected int m_input = 0;
        protected int m_output = 0;
    }

    [InlineData("m_input", "in", true)]
    [InlineData("m_output", "out", true)]
    [InlineData("m_input", "out", false)]
    [Theory]
    public void FieldNamesMatch_FilterRename(string dotNetFieldName, string javaFieldName, bool expected)
    {
        var dotNetField = typeof(FilterFieldExample).GetField(dotNetFieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var javaField = new FieldMetadata(javaFieldName, "java.io.Reader", new List<string> { "protected" }, IsStatic: false);
        Assert.Equal(expected, MemberComparison.FieldNamesMatch(dotNetField, javaField));
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

    [InlineData("Foo", "foo", true)]
    [InlineData("Foo", "Foo", true)]
    [InlineData("DoSomething", "doSomething", true)]
    [InlineData("Equals", "equals", true)]
    [InlineData("GetHashCode", "hashCode", true)]
    [InlineData("ToString", "toString", true)]
    [InlineData("CompareTo", "compareTo", true)]
    [InlineData("Foo", "bar", false)]
    [InlineData("DoSomething", "doSomethingElse", false)]
    // Primitive type-name renames: Java Long/Short/Int/Float -> .NET Int64/Int16/Int32/Single.
    [InlineData("ReadInt64", "readLong", true)]
    [InlineData("ReadInt32", "readInt", true)]
    [InlineData("ReadInt16", "readShort", true)]
    [InlineData("ReadVInt32", "readVInt", true)]
    [InlineData("ReadVInt64", "readVLong", true)]
    [InlineData("WriteInt64", "writeLong", true)]
    [InlineData("SetInt64Value", "setLongValue", true)]
    [InlineData("SetSingleValue", "setFloatValue", true)]
    [InlineData("SetInt16Value", "setShortValue", true)]
    [InlineData("NewInt64Range", "newLongRange", true)]
    [InlineData("NewSingleRange", "newFloatRange", true)]
    [InlineData("Int32Val", "intVal", true)]
    [InlineData("Int64Val", "longVal", true)]
    [InlineData("SingleVal", "floatVal", true)]
    [InlineData("ToInt32sRef", "toIntsRef", true)]
    // Comparator -> Comparer (BCL terminology).
    [InlineData("GetComparer", "getComparator", true)]
    // Iterator -> Enumerator (substring rule), and standalone iterator -> GetEnumerator.
    [InlineData("GetEnumerator", "iterator", true)]
    [InlineData("GetEntryEnumerator", "getEntryIterator", true)]
    // Don't confuse Int with the Int32 we just produced.
    [InlineData("Int32", "int32", true)]
    [Theory]
    public void MethodNamesMatch_Tests(string dotNetName, string javaName, bool expected)
    {
        Assert.Equal(expected, MemberComparison.MethodNamesMatch(dotNetName, javaName));
    }

    public class PropExample
    {
        public int EMPTY_INT64S { get; set; }
        public int NUM_BYTES_SINGLE { get; set; }
        public int NUM_BYTES_INT64 { get; set; }
        public int NUM_BYTES_INT16 { get; set; }
        public int DEFAULT_COMPARER { get; set; }
        public int BUF_SIZE_INT64 { get; set; }
        public int Comparer { get; set; }
    }

    [InlineData("EMPTY_INT64S", "EMPTY_LONGS", true)]
    [InlineData("NUM_BYTES_SINGLE", "NUM_BYTES_FLOAT", true)]
    [InlineData("NUM_BYTES_INT64", "NUM_BYTES_LONG", true)]
    [InlineData("NUM_BYTES_INT16", "NUM_BYTES_SHORT", true)]
    [InlineData("DEFAULT_COMPARER", "DEFAULT_COMPARATOR", true)]
    [InlineData("BUF_SIZE_INT64", "BUF_SIZE_LONG", true)]
    [Theory]
    public void PropertyNameMatchesJavaField_TypeWordRenames(string propertyName, string javaFieldName, bool expected)
    {
        var prop = typeof(PropExample).GetProperty(propertyName)!;
        var javaField = new FieldMetadata(javaFieldName, "int", new List<string> { "public" }, IsStatic: false);
        Assert.Equal(expected, MemberComparison.PropertyNameMatchesJavaField(prop, javaField));
    }

    public class MethodExample
    {
        public void DoSomething() { }
        public int Add(int x, int y) => x + y;
        public string Format(string s) => s;
        public T Identity<T>(T value) => value;
        public override string ToString() => string.Empty;

        // H-8: a method with a trailing CancellationToken added on the .NET side only.
        public int Search(string query, int count, System.Threading.CancellationToken token) => count;
        // A method whose only parameter is a CancellationToken (the Java method has zero params).
        public void Collect(System.Threading.CancellationToken token) { }
    }

    [Fact]
    public void MethodsMatch_TrailingCancellationToken_Matches()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Search))!;
        // Java side has no CancellationToken: search(String, int).
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("search", "int", ("query", "java.lang.String"), ("count", "int"))));
    }

    [Fact]
    public void MethodsMatch_OnlyCancellationToken_MatchesZeroArgJava()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Collect))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("collect", "void")));
    }

    [Fact]
    public void MethodNamesAndArityMatch_TrailingCancellationToken_IgnoredForArity()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Search))!;
        // Effective arity is 2 (the CancellationToken does not count).
        Assert.True(MemberComparison.MethodNamesAndArityMatch(method, JavaMethod("search", "int", ("query", "java.lang.String"), ("count", "int"))));
    }

    [Fact]
    public void MethodsMatch_TrailingCancellationToken_DifferentLeadingParams_DoesNotMatch()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Search))!;
        // Leading params still must match: here the Java arity-2 signature differs in type.
        Assert.False(MemberComparison.MethodsMatch(method, JavaMethod("search", "int", ("query", "java.lang.String"), ("count", "java.lang.String"))));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_SizeMethod_MatchesCount()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("size", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_GetSizeMethod_MatchesCount()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getSize", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_GetFilePointer_MatchesPosition()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Position))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getFilePointer", "long")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_SizeMethod_WrongProperty_DoesNotMatch()
    {
        // size() maps to Count, not to an arbitrary same-typed property.
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Position))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("size", "long")));
    }

    private static MethodMetadata JavaMethod(string name, string returnType, params (string Name, string Type)[] parameters)
        => new(
            Name: name,
            ReturnType: returnType,
            Parameters: parameters
                .Select(p => new ParameterMetadata(p.Name, p.Type))
                .ToList(),
            Modifiers: new List<string> { "public" },
            IsVarArgs: false);

    [Fact]
    public void MethodsMatch_NoArgs_Matches()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.DoSomething))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("doSomething", "void")));
    }

    [Fact]
    public void MethodsMatch_TwoIntParams_Matches()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Add))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("add", "int", ("x", "int"), ("y", "int"))));
    }

    [Fact]
    public void MethodsMatch_DifferentName_DoesNotMatch()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.DoSomething))!;
        Assert.False(MemberComparison.MethodsMatch(method, JavaMethod("doSomethingElse", "void")));
    }

    [Fact]
    public void MethodsMatch_DifferentParamCount_DoesNotMatch()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Add))!;
        Assert.False(MemberComparison.MethodsMatch(method, JavaMethod("add", "int", ("x", "int"))));
    }

    [Fact]
    public void MethodsMatch_StringParam_Matches()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Format))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("format", "java.lang.String", ("s", "java.lang.String"))));
    }

    [Fact]
    public void MethodsMatch_GenericParameter_MatchesJavaLangObject()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Identity))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("identity", "java.lang.Object", ("value", "java.lang.Object"))));
    }

    [Fact]
    public void MethodsMatch_OverriddenToString_MatchesJavaToString()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.ToString))!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("toString", "java.lang.String")));
    }

    public class PropertyExample
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool Empty { get; set; }
        public bool IsClosed { get; set; }
        public long Position { get; set; }
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_Getter_Matches()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getName", "java.lang.String")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_Setter_Matches()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("setName", "void", ("name", "java.lang.String"))));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_PrimitiveGetter_Matches()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getCount", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BoolIsAccessor_MatchesBareName()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Empty))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("isEmpty", "boolean")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BoolIsAccessor_MatchesIsPrefixedName()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.IsClosed))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("isClosed", "boolean")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_NameMismatch_DoesNotMatch()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getOther", "java.lang.String")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_GetterTypeMismatch_DoesNotMatch()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("getName", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_SetterReturnsNonVoid_DoesNotMatch()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("setName", "java.lang.String", ("name", "java.lang.String"))));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_IsAccessorOnNonBool_DoesNotMatch()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("isCount", "boolean")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BareNameGetter_Matches()
    {
        // Java 'count()' ↔ .NET 'Count' property.
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.True(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("count", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BareNameGetter_VoidReturn_DoesNotMatch()
    {
        // Java 'name()' returning void is not a getter.
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("name", "void")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BareNameGetter_TypeMismatch_DoesNotMatch()
    {
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Name))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("name", "int")));
    }

    [Fact]
    public void PropertyMatchesJavaAccessor_BareNameGetter_NameMismatch_DoesNotMatch()
    {
        // 'capacity()' is an unrelated zero-arg accessor that should not match the Count property.
        // (Note: 'size()' DOES map to Count via the known-accessor rename table, so it isn't used here.)
        var prop = typeof(PropertyExample).GetProperty(nameof(PropertyExample.Count))!;
        Assert.False(MemberComparison.PropertyMatchesJavaAccessor(prop, JavaMethod("capacity", "int")));
    }

    public class FieldTypeExample
    {
        protected int foo = 0;
        protected string bar = string.Empty;
    }

    [Fact]
    public void FieldTypesMatch_PrimitiveInt_Matches()
    {
        var dotNetField = typeof(FieldTypeExample).GetField("foo", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var javaField = new FieldMetadata("foo", "int", new List<string> { "protected" }, IsStatic: false);
        Assert.True(MemberComparison.FieldTypesMatch(dotNetField, javaField));
    }

    [Fact]
    public void FieldTypesMatch_StringMismatch_DoesNotMatch()
    {
        var dotNetField = typeof(FieldTypeExample).GetField("foo", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var javaField = new FieldMetadata("foo", "java.lang.String", new List<string> { "protected" }, IsStatic: false);
        Assert.False(MemberComparison.FieldTypesMatch(dotNetField, javaField));
    }

    [Fact]
    public void MethodNamesAndArityMatch_DifferentParamTypes_StillMatches()
    {
        // Method matches by name + arity even when parameter types differ.
        // This is the looser pairing used to detect type-mismatched matched methods.
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Format))!;
        var javaMethod = JavaMethod("format", "java.lang.String", ("s", "int"));
        Assert.True(MemberComparison.MethodNamesAndArityMatch(method, javaMethod));
        Assert.False(MemberComparison.MethodSignaturesMatch(method, javaMethod));
    }

    [Fact]
    public void MethodSignaturesMatch_FullMatch_True()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Add))!;
        var javaMethod = JavaMethod("add", "int", ("x", "int"), ("y", "int"));
        Assert.True(MemberComparison.MethodSignaturesMatch(method, javaMethod));
    }

    [Fact]
    public void MethodSignaturesMatch_ReturnTypeDiffers_False()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.Add))!;
        var javaMethod = JavaMethod("add", "long", ("x", "int"), ("y", "int"));
        Assert.False(MemberComparison.MethodSignaturesMatch(method, javaMethod));
    }

    public class DisposeExample
    {
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void Dispose(int unrelated) { }
    }

    [Fact]
    public void MethodsMatch_CloseToDisposeBool_Matches()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose),
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(bool) })!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void MethodsMatch_CloseToDisposeZeroArg_Matches()
    {
        // The pre-existing zero-arg path: Java close() ↔ .NET Dispose() via KnownMethodNameEquivalents.
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose), Type.EmptyTypes)!;
        Assert.True(MemberComparison.MethodsMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void MethodsMatch_CloseToDisposeNonBoolArg_DoesNotMatch()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose), new[] { typeof(int) })!;
        Assert.False(MemberComparison.MethodsMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void IsCloseToDisposeBoolMatch_True()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose),
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(bool) })!;
        Assert.True(MemberComparison.IsCloseToDisposeBoolMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void IsCloseToDisposeBoolMatch_DisposeZeroArg_False()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose), Type.EmptyTypes)!;
        Assert.False(MemberComparison.IsCloseToDisposeBoolMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void IsCloseToDisposeBoolMatch_JavaCloseWithArg_False()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose),
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(bool) })!;
        Assert.False(MemberComparison.IsCloseToDisposeBoolMatch(method, JavaMethod("close", "void", ("x", "int"))));
    }

    [Fact]
    public void IsCloseToDisposeBoolMatch_NotDispose_False()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.DoSomething))!;
        Assert.False(MemberComparison.IsCloseToDisposeBoolMatch(method, JavaMethod("close", "void")));
    }

    [Fact]
    public void IsDotNetDisposePatternMethod_DisposeZeroArg_True()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose), Type.EmptyTypes)!;
        Assert.True(MemberComparison.IsDotNetDisposePatternMethod(method));
    }

    [Fact]
    public void IsDotNetDisposePatternMethod_DisposeBool_True()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose),
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(bool) })!;
        Assert.True(MemberComparison.IsDotNetDisposePatternMethod(method));
    }

    [Fact]
    public void IsDotNetDisposePatternMethod_DisposeNonBoolArg_False()
    {
        var method = typeof(DisposeExample).GetMethod(nameof(DisposeExample.Dispose), new[] { typeof(int) })!;
        Assert.False(MemberComparison.IsDotNetDisposePatternMethod(method));
    }

    [Fact]
    public void IsDotNetDisposePatternMethod_NotDispose_False()
    {
        var method = typeof(MethodExample).GetMethod(nameof(MethodExample.DoSomething))!;
        Assert.False(MemberComparison.IsDotNetDisposePatternMethod(method));
    }
}
