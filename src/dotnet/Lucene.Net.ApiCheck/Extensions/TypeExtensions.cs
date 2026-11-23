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

using Lucene.Net.ApiCheck.Models.Diff;
using System.Reflection;

namespace Lucene.Net.ApiCheck.Extensions;

public static class TypeExtensions
{
    public static string FormatDisplayName(this Type type)
    {
        // Recurse through array, by-ref, and pointer wrappers so the inner element
        // benefits from the keyword/generic formatting below (e.g. 'string[]' rather
        // than 'System.String[]', 'string&' rather than 'System.String&').
        if (type.IsArray)
        {
            return $"{type.GetElementType()!.FormatDisplayName()}[]";
        }

        if (type.IsByRef)
        {
            return $"{type.GetElementType()!.FormatDisplayName()}&";
        }

        if (type.IsPointer)
        {
            return $"{type.GetElementType()!.FormatDisplayName()}*";
        }

        // Render BCL primitives (and System.Void) as their C# keyword equivalents.
        // System.Void is an internal sentinel for "no return type" and is always shown
        // as 'void' in C# source; the other primitives are shown as keywords by convention.
        if (BclTypeKeywords.TryGetValue(type, out var keyword))
        {
            return keyword;
        }

        var fullName = type.FullName ?? type.Name;
        fullName = fullName.Replace("+", "."); // format nested types

        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            var genericArgumentsDisplay = string.Join(", ", genericArguments.Select(a => a.FormatDisplayName()));
            // LUCENENET TODO: this is likely buggy in some cases like nested types
            return $"{fullName.Split('`')[0]}<{genericArgumentsDisplay}>";
        }

        return fullName;
    }

    private static readonly Dictionary<Type, string> BclTypeKeywords = new()
    {
        [typeof(void)] = "void",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
    };

    public static IReadOnlyList<string> GetModifiers(this Type type)
    {
        var modifiers = new List<string>();

        if (type is { IsAbstract: true, IsInterface: false })
        {
            modifiers.Add(type.IsSealed ? "static" : "abstract");
        }

        if (type is { IsSealed: true, IsAbstract: false, IsValueType: false })
        {
            modifiers.Add("sealed");
        }

        if (type.IsPublic || type.IsNestedPublic)
        {
            modifiers.Add("public");
        }

        // TODO: other modifiers?

        return modifiers;
    }

    public static string GetTypeKind(this Type t)
    {
        if (t.IsInterface)
        {
            return "interface";
        }

        if (t.IsEnum)
        {
            return "enum";
        }

        if (t.IsValueType)
        {
            return "struct";
        }

        return t.IsClass ? "class" : "unknown";
    }

    public static TypeReference ToTypeReference(this Type type)
        => new()
        {
            TypeKind = type.GetTypeKind(),
            TypeName = type.FullName ?? type.Name,
            DisplayName = type.FormatDisplayName()
        };

    /// <summary>
    /// Gets the interfaces implemented by the specified type that are not inherited from the base type.
    /// </summary>
    /// <param name="type">The type to get the interfaces for.</param>
    /// <returns>The interfaces implemented by the specified type that are not inherited from the base type.</returns>
    public static IReadOnlyList<Type> GetImplementedInterfaces(this Type type)
        => type.BaseType is not null
        ? type.GetInterfaces().Except(type.BaseType.GetInterfaces()).ToList()
        : type.GetInterfaces();

    public static IReadOnlyList<FieldInfo> GetApiFields(this Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f is { IsPrivate: false, IsAssembly: false, IsFamilyAndAssembly: false }
                        && !f.IsSpecialName) // excludes the synthetic enum 'value__' field
            .ToList();

    public static IReadOnlyList<ConstructorInfo> GetApiConstructors(this Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(c => c is { IsPrivate: false, IsAssembly: false, IsFamilyAndAssembly: false, IsStatic: false })
            .ToList();

    /// <summary>
    /// Gets the API-visible methods declared on the type, excluding property
    /// accessors, event accessors, operators, and other special-name methods.
    /// </summary>
    public static IReadOnlyList<MethodInfo> GetApiMethods(this Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m is { IsPrivate: false, IsAssembly: false, IsFamilyAndAssembly: false }
                        && !m.IsSpecialName)
            .ToList();

    public static IReadOnlyList<PropertyInfo> GetApiProperties(this Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(p => p.IsApiVisible())
            .ToList();
}
