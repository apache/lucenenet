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

using Lucene.Net.ApiCheck.Comparison;
using Lucene.Net.ApiCheck.Models.Config;
using Lucene.Net.ApiCheck.Models.Diff;
using Lucene.Net.ApiCheck.Models.JavaApi;
using System.Reflection;

namespace Lucene.Net.ApiCheck.Utilities;

public static class DiffUtility
{
    public static async Task<ApiDiffResult> GenerateDiff(ApiCheckConfig config,
        FileInfo extractorJarPath,
        DirectoryInfo outputPath)
    {
        Console.WriteLine("Generating diff...");

        var javaLibraries = await JarToolIntegration.ExtractApi(extractorJarPath,
            new FileInfo(Path.Combine(outputPath.FullName, "lucene-api.json")),
            config.LuceneVersion,
            config.Libraries.ToList());

        var assemblyDiffs = new List<AssemblyDiff>();

        foreach (var javaLib in javaLibraries)
        {
            Console.WriteLine($"Processing {javaLib.Library.JarName}...");

            var libraryConfig = config.Libraries.FirstOrDefault(i => i.LuceneName == javaLib.Library.LibraryName)
                ?? throw new InvalidOperationException($"Library {javaLib.Library.LibraryName} not found in config.");

            var assembly = Assembly.Load(libraryConfig.LuceneNetName)
                ?? throw new InvalidOperationException($"Assembly {libraryConfig.LuceneNetName} not found.");

            var diff = DiffAssembly(javaLib, libraryConfig, assembly);

            assemblyDiffs.Add(diff);
        }

        return new ApiDiffResult
        {
            Assemblies = assemblyDiffs
        };
    }

    private static AssemblyDiff DiffAssembly(LibraryResult javaLib, LibraryConfig libraryConfig, Assembly assembly)
    {
        // strategy:
        // 1. get types in .NET that are not in Java library
        // 2. get types in Java library that are not in .NET
        // 3. get types that are in both, but have different members

        var netTypesNotInJava = assembly.GetTypes()
            .Where(t => t.IsPublic)
            .Where(t => !javaLib.Types.Any(jt => TypeComparison.TypesMatch(libraryConfig, t, jt)))
            .Select(t => new MissingTypeDiff
            {
                TypeKind = GetKindForType(t),
                TypeName = t.FullName ?? t.Name,
                Modifiers = GetModifiersForType(t),
            })
            .ToList();

        var javaTypesNotInNet = javaLib.Types
            .Where(jt => jt.Modifiers.Contains("public"))
            .Where(jt => !assembly.GetTypes().Any(t => TypeComparison.TypesMatch(libraryConfig, t, jt)))
            .Select(jt => new MissingTypeDiff
            {
                TypeKind = jt.Kind,
                TypeName = jt.FullName,
                Modifiers = jt.Modifiers,
            })
            .ToList();

        // TODO: compare types that are in both assemblies

        var diff = new AssemblyDiff
        {
            LuceneName = javaLib.Library.LibraryName,
            LuceneVersion = javaLib.Library.Version,
            LuceneNetName = libraryConfig.LuceneNetName,
            LuceneNetVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                               ?? assembly.GetName().Version?.ToString()
                               ?? "unknown",
            LibraryConfig = libraryConfig,
            LuceneNetTypesNotInLucene = netTypesNotInJava,
            LuceneTypesNotInLuceneNet = javaTypesNotInNet,
        };

        return diff;
    }

    private static IReadOnlyList<string> GetModifiersForType(Type type)
    {
        var modifiers = new List<string>();

        if (type.IsAbstract)
        {
            modifiers.Add("abstract");
        }

        if (type.IsSealed)
        {
            modifiers.Add("sealed");
        }

        if (type.IsPublic)
        {
            modifiers.Add("public");
        }

        // TODO: other modifiers

        return modifiers;
    }

    private static string GetKindForType(Type t)
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
}
