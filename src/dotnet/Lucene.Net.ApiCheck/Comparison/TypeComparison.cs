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

using J2N;
using J2N.Text;
using Lucene.Net.ApiCheck.Extensions;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Reflection;
using System.Collections;
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
        [typeof(IDisposable)] = ["java.lang.AutoCloseable", "java.io.Closeable"], // TODO: map ICloseable once #271 is done
        [typeof(ICharSequence)] = ["java.lang.CharSequence"],
        [typeof(IAppendable)] = ["java.lang.Appendable"],
        [typeof(string)] = ["java.lang.String", "java.lang.CharSequence"],
        [typeof(TextReader)] = ["java.io.Reader"],
        [typeof(TextWriter)] = ["java.io.Writer"],
        [typeof(Stream)] = ["java.io.InputStream", "java.io.OutputStream"],
        [typeof(FileInfo)] = ["java.io.File"],
        [typeof(Random)] = ["java.util.Random"],
        [typeof(Randomizer)] = ["java.util.Random"],
        // Open-generic collection mappings: a Java erased List/Set/Map type can correspond
        // to several .NET collection types depending on how the porter chose to expose it.
        [typeof(IEnumerable<>)] = ["java.util.List", "java.util.Collection", "java.lang.Iterable", "java.util.Set"],
        [typeof(IEnumerable)] = ["java.util.List", "java.util.Collection", "java.lang.Iterable", "java.util.Set"],
        [typeof(ICollection<>)] = ["java.util.List", "java.util.Collection", "java.util.Set"],
        [typeof(IList<>)] = ["java.util.List"],
        [typeof(IReadOnlyCollection<>)] = ["java.util.List", "java.util.Collection", "java.util.Set"],
        [typeof(IReadOnlyList<>)] = ["java.util.List"],
        [typeof(List<>)] = ["java.util.List"],
        [typeof(ISet<>)] = ["java.util.Set"],
        [typeof(HashSet<>)] = ["java.util.HashSet", "java.util.Set"],
        [typeof(SortedSet<>)] = ["java.util.TreeSet", "java.util.SortedSet", "java.util.NavigableSet"],
        [typeof(IDictionary<,>)] = ["java.util.Map"],
        [typeof(IReadOnlyDictionary<,>)] = ["java.util.Map"],
        [typeof(Dictionary<,>)] = ["java.util.HashMap", "java.util.Map"],
        [typeof(SortedDictionary<,>)] = ["java.util.TreeMap", "java.util.SortedMap", "java.util.NavigableMap"],
        [typeof(IEnumerator<>)] = ["java.util.Iterator"],
        [typeof(IEnumerator)] = ["java.util.Iterator"],
        [typeof(KeyValuePair<,>)] = ["java.util.Map$Entry"],
    };

    public static bool TypeMatchesFullName(Type? dotNetType, string? javaTypeName, string? javaTypeKind)
    {
        if (dotNetType is null && javaTypeName is null)
        {
            return true;
        }

        // handle classes that are mapped to interfaces in .NET, which don't have base types
        if (dotNetType is null && javaTypeName is not null && javaTypeName.Equals("java.lang.Object", StringComparison.Ordinal))
        {
            return true;
        }

        if (dotNetType is null || javaTypeName is null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(javaTypeKind))
        {
            throw new ArgumentNullException(nameof(javaTypeKind));
        }

        var nameParts = javaTypeName.Split(".");

        var packageName = string.Join(".", nameParts[..^1]);

        var javaType = new TypeMetadata(packageName, javaTypeKind, nameParts[^1], javaTypeName, null, [], [], [], []);

        return TypesMatch(dotNetType, javaType);
    }

    /// <summary>
    /// Checks whether a .NET type matches a Java type name when the Java kind is not known
    /// (e.g., when comparing parameter types). This tries the common kinds and is more lenient.
    /// </summary>
    public static bool TypeMatchesFullNameAnyKind(Type? dotNetType, string? javaTypeName)
    {
        if (dotNetType is null && javaTypeName is null)
        {
            return true;
        }

        if (dotNetType is null || javaTypeName is null)
        {
            return false;
        }

        // Derive a likely Java kind from the .NET type so TypesMatch's kind check passes
        // for inferred LuceneType info matches.
        var likelyKind = dotNetType.GetTypeKind() switch
        {
            "interface" => "interface",
            "enum" => "enum",
            "struct" => "class",
            _ => "class"
        };

        return TypeMatchesFullName(dotNetType, javaTypeName, likelyKind);
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

    public static bool InterfacesMatch(IReadOnlyList<Type> dotNetInterfaces, IReadOnlyList<string> javaInterfaces)
    {
        if (dotNetInterfaces.Count != javaInterfaces.Count)
        {
            return false;
        }

        return dotNetInterfaces.All(i => javaInterfaces.Any(j => TypeMatchesFullName(i, j, "interface")));
    }
}
