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

using Lucene.Net.ApiCheck.Models.Config;
using Lucene.Net.ApiCheck.Models.JavaApi;
using System.Text;

namespace Lucene.Net.ApiCheck.Comparison;

internal static class TypeComparison
{
    public static bool TypesMatch(LibraryConfig libraryConfig, Type dotNetType, TypeMetadata javaType)
    {
        var expectedNamespace = libraryConfig.PackageNameMappings.TryGetValue(javaType.PackageName, out var mappedNamespace)
            ? mappedNamespace
            : GetExpectedDotNetNamespace(javaType.PackageName);
        var cleanJavaName = javaType.Name.EndsWith("Impl") ? javaType.Name[..^4] : javaType.Name;

        var expectedDotNetName = javaType.Kind == "interface"
            ? $"I{cleanJavaName}"
            : cleanJavaName;

        return string.Equals(dotNetType.Namespace, expectedNamespace, StringComparison.OrdinalIgnoreCase)
            && dotNetType.Name == expectedDotNetName;
    }

    public static string? GetExpectedDotNetNamespace(string packageName)
    {
        if (!packageName.StartsWith("org.apache.lucene"))
        {
            return null;
        }

        var parts = packageName.Split('.');
        var sb = new StringBuilder();

        sb.Append("Lucene.Net");

        for (int i = 3; i < parts.Length; i++)
        {
            sb.Append('.');

            var dashParts = parts[i].Split('-');
            foreach (var dashPart in dashParts)
            {
                sb.Append(char.ToUpper(dashPart[0]));
                sb.Append(dashPart[1..]);
            }
        }

        return sb.ToString();
    }
}
