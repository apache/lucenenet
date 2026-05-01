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

namespace Lucene.Net.ApiCheck.Comparison;

public class MemberComparison
{
    public static bool FieldNamesMatch(FieldInfo dotNetField, FieldMetadata javaField)
    {
        return CleanFieldName(dotNetField.Name) == CleanFieldName(javaField.Name);
    }

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

        return ParameterListsMatch(dotNetMethod.GetParameters(), javaMethod.Parameters);
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

        return string.Equals(dotNetName, javaName, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Dictionary<string, string> KnownMethodNameEquivalents = new(StringComparer.Ordinal)
    {
        ["equals"] = "Equals",
        ["hashCode"] = "GetHashCode",
        ["toString"] = "ToString",
        ["clone"] = "Clone",
        ["compareTo"] = "CompareTo",
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
        // Match Java primitives to .NET equivalents (Java parameter types are erased
        // and unboxed primitives appear as 'int', 'long', etc.).
        if (TryMatchJavaPrimitive(dotNetType, javaTypeName))
        {
            return true;
        }

        // Strip array brackets if present on both sides and recurse
        if (javaTypeName.EndsWith("[]") && dotNetType.IsArray)
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
        if (name.StartsWith("m_") || name.StartsWith("s_"))
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
