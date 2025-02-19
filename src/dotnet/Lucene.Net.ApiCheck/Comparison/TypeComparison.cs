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
using Lucene.Net.Reflection;
using System.Reflection;

namespace Lucene.Net.ApiCheck.Comparison;

public static class TypeComparison
{
    public static bool TypesMatch(Type dotNetType, TypeMetadata javaType)
    {
        if (dotNetType.GetCustomAttribute<LuceneTypeAttribute>() is { } luceneEquivalent
            && luceneEquivalent.PackageName == javaType.PackageName
            && luceneEquivalent.TypeName == javaType.Name.Replace("$", "."))
        {
            return true;
        }

        var luceneTypeInfo = dotNetType.GetLuceneTypeInfo();

        if (luceneTypeInfo is null)
        {
            return false;
        }

        var expectedPackage = luceneTypeInfo.PackageName;
        var cleanJavaName = javaType.Name.EndsWith("Impl") ? javaType.Name[..^4] : javaType.Name;
        cleanJavaName = cleanJavaName.Replace("$", ".");

        var expectedDotNetName = javaType.Kind == "interface"
            ? $"I{cleanJavaName}"
            : cleanJavaName;

        var cleanDotNetName = dotNetType.Name.IndexOf('`') is > 0 and var genericIndex
            ? dotNetType.Name[..genericIndex]
            : dotNetType.Name;

        return string.Equals(javaType.PackageName, expectedPackage, StringComparison.OrdinalIgnoreCase)
            && cleanDotNetName == expectedDotNetName;
    }
}
