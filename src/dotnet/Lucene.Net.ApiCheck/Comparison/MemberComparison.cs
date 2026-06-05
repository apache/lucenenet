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

using Lucene.Net.ApiCheck.Models.JavaApi;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Lucene.Net.ApiCheck.Comparison;

public class MemberComparison
{
    public static bool FieldNamesMatch(FieldInfo dotNetField, FieldMetadata javaField)
    {
        var dotNet = CleanFieldName(dotNetField.Name);
        var java = CleanFieldName(javaField.Name);
        if (dotNet == java)
        {
            return true;
        }

        if (KnownFieldNameRenames.TryGetValue(java, out var renamed)
            && string.Equals(dotNet, renamed, StringComparison.Ordinal))
        {
            return true;
        }

        var normalized = NormalizeJavaTypeWordsToDotNet(java);
        return !ReferenceEquals(normalized, java) && dotNet == normalized;
    }

    // Lucene.NET applies a few well-known field renames that aren't reachable via
    // mechanical string normalization. These are the Java field names (cleaned of
    // .NET-side prefixes like m_/s_/_) and their .NET cleaned equivalents.
    private static readonly Dictionary<string, string> KnownFieldNameRenames = new(StringComparer.Ordinal)
    {
        // Filter pattern: java.io.FilterReader.in / FilterWriter.out are exposed in
        // Lucene.NET as the more descriptive m_input / m_output protected fields.
        ["in"] = "input",
        ["out"] = "output",
    };

    /// <summary>
    /// Determines whether the field types of a name-matched .NET field and Java
    /// field are compatible.
    /// </summary>
    public static bool FieldTypesMatch(FieldInfo dotNetField, FieldMetadata javaField)
    {
        return ParameterTypesMatch(dotNetField.FieldType, javaField.Type);
    }

    /// <summary>
    /// Determines whether a .NET method matches a Java method by name and arity
    /// only, ignoring parameter types. Useful for detecting type mismatches on
    /// otherwise-corresponding methods.
    /// </summary>
    public static bool MethodNamesAndArityMatch(MethodInfo dotNetMethod, MethodMetadata javaMethod)
    {
        return MethodNamesMatch(dotNetMethod.Name, javaMethod.Name)
               && dotNetMethod.GetParameters().Length == javaMethod.Parameters.Count;
    }

    /// <summary>
    /// Determines whether the parameter and return types of two name+arity-matched
    /// methods are compatible.
    /// </summary>
    public static bool MethodSignaturesMatch(MethodInfo dotNetMethod, MethodMetadata javaMethod)
    {
        return ParameterListsMatch(dotNetMethod.GetParameters(), javaMethod.Parameters)
               && ParameterTypesMatch(dotNetMethod.ReturnType, javaMethod.ReturnType);
    }

    /// <summary>
    /// Determines whether the parameter types of two arity-matched constructors
    /// are compatible.
    /// </summary>
    public static bool ConstructorSignaturesMatch(ConstructorInfo dotNetCtor, ConstructorMetadata javaCtor)
    {
        return ParameterListsMatch(dotNetCtor.GetParameters(), javaCtor.Parameters);
    }

    /// <summary>
    /// Determines whether a .NET property corresponds to a Java public field of
    /// the same (case-insensitive) name and a compatible type. Lucene exposes
    /// many public mutable fields that the .NET ports promoted to properties.
    /// </summary>
    public static bool PropertyMatchesJavaField(PropertyInfo dotNetProperty, FieldMetadata javaField)
    {
        return PropertyNameMatchesJavaField(dotNetProperty, javaField)
               && ParameterTypesMatch(dotNetProperty.PropertyType, javaField.Type);
    }

    /// <summary>
    /// Determines whether a .NET property name matches a Java public field name
    /// (case-insensitively), ignoring whether the type aligns.
    /// </summary>
    public static bool PropertyNameMatchesJavaField(PropertyInfo dotNetProperty, FieldMetadata javaField)
    {
        if (string.Equals(dotNetProperty.Name, javaField.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = NormalizeJavaTypeWordsToDotNet(javaField.Name);
        return !ReferenceEquals(normalized, javaField.Name)
               && string.Equals(dotNetProperty.Name, normalized, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a .NET property's name matches a Java getter/setter/is
    /// method's bare name (ignoring whether the type aligns).
    /// </summary>
    public static bool PropertyNameMatchesJavaAccessor(PropertyInfo dotNetProperty, MethodMetadata javaMethod)
    {
        var javaName = javaMethod.Name;
        var paramCount = javaMethod.Parameters.Count;

        if (javaName.StartsWith("get", StringComparison.Ordinal) && javaName.Length > 3 && paramCount == 0)
        {
            return PropertyNameMatches(dotNetProperty.Name, javaName[3..], allowIsPrefix: false);
        }

        if (javaName.StartsWith("set", StringComparison.Ordinal) && javaName.Length > 3 && paramCount == 1
            && javaMethod.ReturnType.Equals("void", StringComparison.Ordinal))
        {
            return PropertyNameMatches(dotNetProperty.Name, javaName[3..], allowIsPrefix: false);
        }

        if (javaName.StartsWith("is", StringComparison.Ordinal) && javaName.Length > 2 && paramCount == 0
            && javaMethod.ReturnType.Equals("boolean", StringComparison.Ordinal))
        {
            return PropertyNameMatches(dotNetProperty.Name, javaName[2..], allowIsPrefix: true);
        }

        if (paramCount == 0
            && !javaMethod.ReturnType.Equals("void", StringComparison.Ordinal)
            && string.Equals(dotNetProperty.Name, javaName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a .NET constructor matches a Java constructor by parameter
    /// count and parameter types. Constructors have no name to compare on, so the
    /// signature is the only signal.
    /// </summary>
    public static bool ConstructorsMatch(ConstructorInfo dotNetCtor, ConstructorMetadata javaCtor)
    {
        return ParameterListsMatch(dotNetCtor.GetParameters(), javaCtor.Parameters);
    }

    /// <summary>
    /// Determines whether a .NET method matches a Java method by name and parameter
    /// signature. Names are normalized to handle casing and the well-known
    /// equals/hashCode/toString equivalents.
    /// </summary>
    public static bool MethodsMatch(MethodInfo dotNetMethod, MethodMetadata javaMethod)
    {
        if (!MethodNamesMatch(dotNetMethod.Name, javaMethod.Name))
        {
            return false;
        }

        if (ParameterListsMatch(dotNetMethod.GetParameters(), javaMethod.Parameters))
        {
            return true;
        }

        // Lucene.NET implements the dispose pattern: Java's zero-arg close() is exposed
        // as protected Dispose(bool disposing) on the .NET side, with a public Dispose()
        // inherited from IDisposable. The bool overload would otherwise look like an
        // unmatched .NET-only method.
        return IsCloseToDisposeBoolMatch(dotNetMethod, javaMethod);
    }

    /// <summary>
    /// Returns true when the pair is the canonical Lucene.NET dispose-pattern idiom:
    /// Java <c>close()</c> ↔ .NET <c>Dispose(bool disposing)</c>. The .NET overload is
    /// protected by convention, so callers can use this to suppress the expected
    /// visibility/parameter divergence from being reported as a member difference.
    /// </summary>
    public static bool IsCloseToDisposeBoolMatch(MethodInfo dotNetMethod, MethodMetadata javaMethod)
    {
        if (!javaMethod.Name.Equals("close", StringComparison.Ordinal)
            || javaMethod.Parameters.Count != 0)
        {
            return false;
        }

        if (!dotNetMethod.Name.Equals("Dispose", StringComparison.Ordinal))
        {
            return false;
        }

        var parameters = dotNetMethod.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
    }

    /// <summary>
    /// Returns true when the .NET method is one of the two dispose-pattern shapes —
    /// public <c>Dispose()</c> or protected <c>Dispose(bool disposing)</c>. Lucene.NET
    /// commonly exposes both on the same type for one Java <c>close()</c>, so the
    /// loser of the name+arity match should not be reported as a .NET-only method.
    /// </summary>
    public static bool IsDotNetDisposePatternMethod(MethodInfo dotNetMethod)
    {
        if (!dotNetMethod.Name.Equals("Dispose", StringComparison.Ordinal))
        {
            return false;
        }

        var parameters = dotNetMethod.GetParameters();
        return parameters.Length switch
        {
            0 => true,
            1 => parameters[0].ParameterType == typeof(bool),
            _ => false,
        };
    }

    /// <summary>
    /// Compares a .NET method name to a Java method name allowing for the standard
    /// .NET PascalCase / Java camelCase difference and the well-known
    /// equals/hashCode/toString equivalents.
    /// </summary>
    public static bool MethodNamesMatch(string dotNetName, string javaName)
    {
        if (KnownMethodNameEquivalents.TryGetValue(javaName, out var dotNetEquivalent)
            && dotNetEquivalent.Equals(dotNetName, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(dotNetName, javaName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Common porting idiom: Java 'description()' ↔ .NET 'GetDescription()',
        // Java 'getDescription()' ↔ .NET 'Description()' (rare, but possible).
        // Match when either side adds a 'Get' or 'Is' prefix that the other lacks.
        if (TryStripVerbPrefix(dotNetName, out var strippedDotNet)
            && string.Equals(strippedDotNet, javaName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (TryStripVerbPrefix(javaName, out var strippedJava)
            && string.Equals(dotNetName, strippedJava, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Lucene.NET systematically substitutes BCL type names where the Java name embeds
        // the Java primitive: Long→Int64, Short→Int16, Int→Int32, Float→Single. This shows
        // up in many method/field names (readLong→ReadInt64, getInts→GetInt32s, etc.).
        var normalizedJava = NormalizeJavaTypeWordsToDotNet(javaName);
        if (!ReferenceEquals(normalizedJava, javaName)
            && string.Equals(dotNetName, normalizedJava, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // Substitution rules for Java→.NET BCL type-word renames. The PascalCase/camelCase
    // boundary is: matched word followed by end-of-string, an uppercase letter (next
    // syllable), or 's' (plural) before such a boundary. The leading lowercase 'i' in
    // 'int', 's' in 'short', etc. is permitted only at start-of-string so we don't
    // re-match inside longer words like 'interesting' or 'shorthand'. SCREAMING_SNAKE
    // uses _ as the boundary since regex \b treats _ as a word char. The Int rules use
    // (?!\d) to avoid re-matching the digit-suffixed BCL types we just produced.
    // Downstream comparisons are case-insensitive, so we don't preserve input case.
    private const string PascalAfter = @"(?=$|[A-Z]|s(?=$|[A-Z]))";
    private const string ScreamAfter = @"(?=$|_|S(?=$|_))";

    private static readonly (Regex Pattern, string Replacement)[] PrimitiveWordRenames =
    [
        (new Regex(@"(?:(?<=^)long|Long)" + PascalAfter, RegexOptions.Compiled), "Int64"),
        (new Regex(@"(?:(?<=^)short|Short)" + PascalAfter, RegexOptions.Compiled), "Int16"),
        (new Regex(@"(?:(?<=^)float|Float)" + PascalAfter, RegexOptions.Compiled), "Single"),
        (new Regex(@"(?<=^|_)LONG" + ScreamAfter, RegexOptions.Compiled), "INT64"),
        (new Regex(@"(?<=^|_)SHORT" + ScreamAfter, RegexOptions.Compiled), "INT16"),
        (new Regex(@"(?<=^|_)FLOAT" + ScreamAfter, RegexOptions.Compiled), "SINGLE"),
        (new Regex(@"(?:Comparator|comparator)", RegexOptions.Compiled), "Comparer"),
        (new Regex(@"(?:Iterator|iterator)", RegexOptions.Compiled), "Enumerator"),
        (new Regex(@"(?<=^|_)COMPARATOR(?=$|_)", RegexOptions.Compiled), "COMPARER"),
        (new Regex(@"(?:(?<=^)int|Int)(?!\d)" + PascalAfter, RegexOptions.Compiled), "Int32"),
        (new Regex(@"(?<=^|_)INT(?!\d)" + ScreamAfter, RegexOptions.Compiled), "INT32"),
    ];

    /// <summary>
    /// Substitutes Java primitive/BCL type words inside a member name with their .NET
    /// BCL equivalents (Long→Int64, Short→Int16, Float→Single, Int→Int32, Comparator→Comparer,
    /// Iterator→Enumerator). Handles PascalCase, camelCase (at name start), and
    /// SCREAMING_SNAKE_CASE forms. Downstream comparisons are case-insensitive so the
    /// replacement always uses the canonical .NET casing. Returns the original reference
    /// when no substitution applies, so callers can detect a no-op cheaply.
    /// </summary>
    internal static string NormalizeJavaTypeWordsToDotNet(string name)
    {
        var result = name;
        foreach (var (pattern, replacement) in PrimitiveWordRenames)
        {
            result = pattern.Replace(result, replacement);
        }

        return string.Equals(result, name, StringComparison.Ordinal) ? name : result;
    }

    private static bool TryStripVerbPrefix(string name, out string stripped)
    {
        if (name.Length > 3 && (name.StartsWith("Get", StringComparison.Ordinal)
                                || name.StartsWith("get", StringComparison.Ordinal)
                                || name.StartsWith("Set", StringComparison.Ordinal)
                                || name.StartsWith("set", StringComparison.Ordinal)))
        {
            stripped = name[3..];
            return true;
        }

        if (name.Length > 2 && (name.StartsWith("Is", StringComparison.Ordinal)
                                || name.StartsWith("is", StringComparison.Ordinal)))
        {
            stripped = name[2..];
            return true;
        }

        stripped = name;
        return false;
    }

    private static readonly Dictionary<string, string> KnownMethodNameEquivalents = new(StringComparer.Ordinal)
    {
        ["equals"] = "Equals",
        ["hashCode"] = "GetHashCode",
        ["toString"] = "ToString",
        ["clone"] = "Clone",
        ["compareTo"] = "CompareTo",
        // Lucene.NET maps Java close() to .NET Dispose() via IDisposable.
        ["close"] = "Dispose",
        // IEnumerable/IEnumerator convention: Java iterator() returns an Iterator;
        // .NET exposes GetEnumerator() returning an IEnumerator.
        ["iterator"] = "GetEnumerator",
    };

    /// <summary>
    /// Determines whether a .NET property corresponds to a Java getter, setter, or
    /// boolean-style accessor method. Recognized Java patterns:
    ///   <list type="bullet">
    ///     <item><c>getX()</c> with non-void return matches a property named <c>X</c>.</item>
    ///     <item><c>setX(T)</c> returning void matches a property named <c>X</c> whose type matches the parameter.</item>
    ///     <item><c>isX()</c> returning boolean matches a property named <c>X</c> or <c>IsX</c> of type <see cref="bool"/>.</item>
    ///   </list>
    /// </summary>
    public static bool PropertyMatchesJavaAccessor(PropertyInfo dotNetProperty, MethodMetadata javaMethod)
    {
        var javaName = javaMethod.Name;
        var paramCount = javaMethod.Parameters.Count;

        if (javaName.StartsWith("get", StringComparison.Ordinal) && javaName.Length > 3 && paramCount == 0)
        {
            var bareName = javaName[3..];
            return PropertyNameMatches(dotNetProperty.Name, bareName, allowIsPrefix: false)
                   && ParameterTypesMatch(dotNetProperty.PropertyType, javaMethod.ReturnType);
        }

        if (javaName.StartsWith("set", StringComparison.Ordinal) && javaName.Length > 3 && paramCount == 1
            && javaMethod.ReturnType.Equals("void", StringComparison.Ordinal))
        {
            var bareName = javaName[3..];
            return PropertyNameMatches(dotNetProperty.Name, bareName, allowIsPrefix: false)
                   && ParameterTypesMatch(dotNetProperty.PropertyType, javaMethod.Parameters[0].Type);
        }

        if (javaName.StartsWith("is", StringComparison.Ordinal) && javaName.Length > 2 && paramCount == 0
            && javaMethod.ReturnType.Equals("boolean", StringComparison.Ordinal))
        {
            var bareName = javaName[2..];
            return dotNetProperty.PropertyType == typeof(bool)
                   && PropertyNameMatches(dotNetProperty.Name, bareName, allowIsPrefix: true);
        }

        // Bare-name getter: a Java zero-arg, non-void method whose name matches
        // the property name (e.g., Java 'startOffset()' ↔ .NET 'StartOffset' property).
        // Strict to avoid consuming legitimate methods: name must equal the property
        // name case-insensitively and return type must match.
        if (paramCount == 0
            && !javaMethod.ReturnType.Equals("void", StringComparison.Ordinal)
            && string.Equals(dotNetProperty.Name, javaName, StringComparison.OrdinalIgnoreCase)
            && ParameterTypesMatch(dotNetProperty.PropertyType, javaMethod.ReturnType))
        {
            return true;
        }

        return false;
    }

    private static bool PropertyNameMatches(string dotNetPropertyName, string javaBareName, bool allowIsPrefix)
    {
        if (string.Equals(dotNetPropertyName, javaBareName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Allow .NET 'IsFoo' to correspond to Java 'isFoo' (already stripped to 'Foo').
        if (allowIsPrefix
            && dotNetPropertyName.StartsWith("Is", StringComparison.Ordinal)
            && string.Equals(dotNetPropertyName[2..], javaBareName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = NormalizeJavaTypeWordsToDotNet(javaBareName);
        if (!ReferenceEquals(normalized, javaBareName))
        {
            if (string.Equals(dotNetPropertyName, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (allowIsPrefix
                && dotNetPropertyName.StartsWith("Is", StringComparison.Ordinal)
                && string.Equals(dotNetPropertyName[2..], normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ParameterListsMatch(ParameterInfo[] dotNetParams, IReadOnlyList<ParameterMetadata> javaParams)
    {
        if (dotNetParams.Length != javaParams.Count)
        {
            return false;
        }

        for (int i = 0; i < dotNetParams.Length; i++)
        {
            if (!ParameterTypesMatch(dotNetParams[i].ParameterType, javaParams[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParameterTypesMatch(Type dotNetType, string javaTypeName)
    {
        // Unwrap Nullable<T> on the .NET side. The Java equivalent of int? is either
        // 'int' (primitive) or 'java.lang.Integer' (boxed); try the unwrapped type.
        var underlying = Nullable.GetUnderlyingType(dotNetType);
        if (underlying is not null)
        {
            return ParameterTypesMatch(underlying, javaTypeName);
        }

        // Match Java primitives to .NET equivalents (Java parameter types are erased
        // and unboxed primitives appear as 'int', 'long', etc.).
        if (TryMatchJavaPrimitive(dotNetType, javaTypeName))
        {
            return true;
        }

        // Strip array brackets if present on both sides and recurse
        if (javaTypeName.EndsWith("[]", StringComparison.Ordinal) && dotNetType.IsArray)
        {
            return ParameterTypesMatch(
                dotNetType.GetElementType()!,
                javaTypeName[..^2]);
        }

        // .NET generic method/type parameters (T, TKey, etc.) erase to their bound
        // on the Java side; in practice that's java.lang.Object unless a constraint
        // applies. Treat them as matching java.lang.Object for v1.
        if (dotNetType.IsGenericParameter)
        {
            return javaTypeName.Equals("java.lang.Object", StringComparison.Ordinal);
        }

        // For generic .NET types, compare against the open generic definition since
        // Java parameter types are type-erased.
        var typeForCompare = dotNetType.IsGenericType
            ? dotNetType.GetGenericTypeDefinition()
            : dotNetType;

        return TypeComparison.TypeMatchesFullNameAnyKind(typeForCompare, javaTypeName);
    }

    private static readonly Dictionary<Type, string> JavaPrimitiveMappings = new()
    {
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(short)] = "short",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "byte",
        [typeof(bool)] = "boolean",
        [typeof(char)] = "char",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(void)] = "void",
    };

    private static bool TryMatchJavaPrimitive(Type dotNetType, string javaTypeName)
    {
        return JavaPrimitiveMappings.TryGetValue(dotNetType, out var javaPrimitive)
               && javaPrimitive.Equals(javaTypeName, StringComparison.Ordinal);
    }

    private static string CleanFieldName(string name)
    {
        if (name.StartsWith("m_", StringComparison.Ordinal) || name.StartsWith("s_", StringComparison.Ordinal))
        {
            return name[2..];
        }

        if (name.StartsWith('_'))
        {
            return name[1..];
        }

        return name;
    }
}
