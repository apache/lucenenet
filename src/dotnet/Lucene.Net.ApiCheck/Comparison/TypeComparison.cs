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

using Lucene.Net.ApiCheck.Extensions;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Reflection;
using System.Reflection;

namespace Lucene.Net.ApiCheck.Comparison;

public static class TypeComparison
{
    private static readonly Dictionary<Type, HashSet<string>> WellKnownEquivalentTypes = new()
    {
        [typeof(object)] = ["java.lang.Object"],
        [typeof(ValueType)] = ["java.lang.Object"],
        [typeof(Enum)] = ["java.lang.Enum"],
        [typeof(IOException)] = ["java.io.IOException"],
        [typeof(FileNotFoundException)] = ["java.io.FileNotFoundException"],
        [typeof(Exception)] = ["java.lang.RuntimeException", "java.lang.Exception"],
    };

    public static bool BaseTypesMatch(Type? dotNetType, string? javaTypeName)
    {
        if (dotNetType is null && javaTypeName is null)
        {
            return true;
        }

        // handle classes that are mapped to interfaces in .NET, which don't have base types
        if (dotNetType == null && javaTypeName != null && javaTypeName.Equals("java.lang.Object", StringComparison.Ordinal))
        {
            return true;
        }

        if (dotNetType is null || javaTypeName is null)
        {
            return false;
        }

        var nameParts = javaTypeName.Split(".");

        var packageName = string.Join(".", nameParts[..^1]);

        var javaType = new TypeMetadata(packageName, "class", nameParts[^1], javaTypeName, null, [], [], [], []);

        return TypesMatch(dotNetType, javaType);
    }

    public static bool TypesMatch(Type dotNetType, TypeMetadata javaType)
    {
        if (WellKnownEquivalentTypes.TryGetValue(dotNetType, out HashSet<string>? wellKnownEquivalentTypes))
        {
            return wellKnownEquivalentTypes.Contains(javaType.FullName);
        }

        var javaFullNameParts = javaType.FullName.Split(".");
        var javaName = javaFullNameParts[^1].Replace("$", ".");

        if (dotNetType.GetCustomAttribute<LuceneTypeAttribute>() is { } luceneEquivalent
            && luceneEquivalent.PackageName.Equals(javaType.PackageName, StringComparison.Ordinal)
            && luceneEquivalent.TypeName.Equals(javaName, StringComparison.Ordinal))
        {
            return true;
        }

        var luceneTypeInfo = dotNetType.GetLuceneTypeInfo();

        if (luceneTypeInfo is null)
        {
            return false;
        }

        var cleanJavaName = javaType.Kind.Equals("class", StringComparison.Ordinal)
                            && javaName.EndsWith("Impl", StringComparison.Ordinal)
            ? javaName[..^4]
            : javaName;

        var equivalentJavaKind = dotNetType.GetTypeKind() switch
        {
            "interface" => "interface",
            "class" => "class",
            "enum" => "enum",
            "struct" => "class",
            _ => null
        };

        return string.Equals(equivalentJavaKind, javaType.Kind, StringComparison.Ordinal)
            && string.Equals(luceneTypeInfo.PackageName, javaType.PackageName, StringComparison.Ordinal)
            && string.Equals(luceneTypeInfo.TypeName, cleanJavaName, StringComparison.Ordinal);
    }
}
