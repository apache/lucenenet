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
    /// Determines whether a .NET constructor matches a Java constructor by parameter
    /// count and parameter types. Constructors have no name to compare on, so the
    /// signature is the only signal.
    /// </summary>
    public static bool ConstructorsMatch(ConstructorInfo dotNetCtor, ConstructorMetadata javaCtor)
    {
        var dotNetParams = dotNetCtor.GetParameters();
        if (dotNetParams.Length != javaCtor.Parameters.Count)
        {
            return false;
        }

        for (int i = 0; i < dotNetParams.Length; i++)
        {
            if (!ParameterTypesMatch(dotNetParams[i].ParameterType, javaCtor.Parameters[i].Type))
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
