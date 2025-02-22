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
using Lucene.Net.ApiCheck.Extensions;
using Lucene.Net.ApiCheck.Models;
using Lucene.Net.ApiCheck.Models.Config;
using Lucene.Net.ApiCheck.Models.Diff;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Reflection;
using System.Reflection;

namespace Lucene.Net.ApiCheck.Utilities;

public static class DiffUtility
{
    private record LibraryToDiff(LibraryConfig LibraryConfig, MavenCoordinates MavenCoordinates, Assembly Assembly);

    private record MatchingType(TypeMetadata JavaType, Type DotNetType);

    public static async Task<ApiDiffResult> GenerateDiff(ApiCheckConfig config,
        FileInfo extractorJarPath,
        DirectoryInfo outputPath)
    {
        Console.WriteLine("Generating diff...");

        var libraries = config.Libraries
            .Select(i =>
            {
                var assembly = Assembly.Load(i.LuceneNetName)
                               ?? throw new InvalidOperationException($"Assembly {i.LuceneNetName} not found.");

                var mavenMapping = assembly.GetCustomAttribute<LuceneMavenMappingAttribute>()
                                   ?? throw new InvalidOperationException(
                                       $"Assembly {assembly.GetName().Name} is missing {nameof(LuceneMavenMappingAttribute)}.");

                return new LibraryToDiff(i,
                    new MavenCoordinates(mavenMapping.GroupId, mavenMapping.ArtifactId, mavenMapping.Version),
                    assembly);
            })
            .ToList();

        var mavenDependencies = libraries
            .Select(i => i.LibraryConfig.MavenDependencies)
            .OfType<IReadOnlyList<string>>() // filter out nulls
            .SelectMany(i => i)
            .Select(MavenCoordinates.Parse)
            .Distinct()
            .ToList();

        var javaLibraries = await JarToolIntegration.ExtractApi(extractorJarPath,
            new FileInfo(Path.Combine(outputPath.FullName, "lucene-api.json")),
            libraries.Select(i => i.MavenCoordinates).ToList(),
            mavenDependencies);

        var assemblyDiffs = new List<AssemblyDiff>();

        foreach (var javaLib in javaLibraries)
        {
            Console.WriteLine($"Processing {javaLib.Library}...");

            var libraryToDiff = libraries.FirstOrDefault(i => i.MavenCoordinates == javaLib.Library)
                                ?? throw new InvalidOperationException(
                                    $"Library {javaLib.Library.ArtifactId} not found in config.");

            var diff = DiffAssembly(javaLib, libraryToDiff);

            assemblyDiffs.Add(diff);
        }

        return new ApiDiffResult
        {
            Assemblies = assemblyDiffs.OrderBy(i => i.LuceneNetName).ToList()
        };
    }

    private static AssemblyDiff DiffAssembly(LibraryResult javaLib, LibraryToDiff libraryToDiff)
    {
        // strategy:
        // 1. get types in .NET that are not in Java library
        // 2. get types in Java library that are not in .NET
        // 3. get types that are in both, but have different modifiers or base types
        // 4. get types that are in both, but have different members

        var assemblyTypes = GetAssemblyTypesForComparison(libraryToDiff.Assembly);
        var libraryConfig = libraryToDiff.LibraryConfig;

        var netTypesNotInJava = GetDotNetTypesNotInJava(javaLib, assemblyTypes);
        var javaTypesNotInNet = GetJavaTypesNotInDotNet(javaLib, assemblyTypes);
        var matchingTypes = GetMatchingTypes(javaLib, assemblyTypes);

        var mismatchedModifiers = GetMismatchedModifiers(matchingTypes);
        var mismatchedBaseTypes = GetMismatchedBaseTypes(matchingTypes);

        var diff = new AssemblyDiff
        {
            LuceneName = javaLib.Library.ArtifactId.Replace("lucene-", ""),
            LuceneMavenCoordinates = libraryToDiff.MavenCoordinates,
            LuceneNetName = libraryConfig.LuceneNetName,
            LuceneNetVersion = libraryToDiff.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                   ?.InformationalVersion
                               ?? libraryToDiff.Assembly.GetName().Version?.ToString()
                               ?? "unknown",
            LibraryConfig = libraryConfig,
            LuceneNetTypesNotInLucene = netTypesNotInJava,
            LuceneTypesNotInLuceneNet = javaTypesNotInNet,
            MismatchedModifiers = mismatchedModifiers,
            MismatchedBaseTypes = mismatchedBaseTypes,
        };

        return diff;
    }

    private static IReadOnlyList<MismatchedBaseTypeDiff> GetMismatchedBaseTypes(IReadOnlyList<MatchingType> matchingTypes)
    {
        return matchingTypes
            .Where(i => (i.JavaType.BaseType != null || i.DotNetType.BaseType != null)
                        && !TypeComparison.BaseTypesMatch(i.DotNetType.BaseType, i.JavaType.BaseType)
                        && !i.DotNetType.HasKnownBaseTypeDifference())
            .Select(i => new MismatchedBaseTypeDiff
            {
                JavaType = new TypeReference
                {
                    TypeKind = i.JavaType.Kind,
                    TypeName = i.JavaType.FullName,
                    DisplayName = i.JavaType.FullName.Replace("$", "."),
                },
                DotNetType = new TypeReference
                {
                    TypeKind = i.DotNetType.GetTypeKind(),
                    TypeName = i.DotNetType.FullName ?? i.DotNetType.Name,
                    DisplayName = i.DotNetType.FormatDisplayName(),
                },
                JavaBaseType = i.JavaType.BaseType != null ? new TypeReference
                {
                    TypeKind = "class", // assume Java base types are always classes
                    TypeName = i.JavaType.BaseType,
                    DisplayName = i.JavaType.BaseType.Replace("$", "."),
                } : null,
                DotNetBaseType = i.DotNetType.BaseType != null ? new TypeReference
                {
                    TypeKind = i.DotNetType.BaseType?.GetTypeKind() ?? "unknown",
                    TypeName = i.DotNetType.BaseType?.FullName ?? i.DotNetType.BaseType?.Name ?? "unknown",
                    DisplayName = i.DotNetType.BaseType?.FormatDisplayName() ?? "unknown",
                } : null,
            })
            .OrderBy(i => i.DotNetType.DisplayName)
            .ToList();
    }

    private static IReadOnlyList<MismatchedModifierDiff> GetMismatchedModifiers(IReadOnlyList<MatchingType> matchingTypes)
    {
        return matchingTypes
            .Where(i => !ModifierComparison.ModifiersAreEquivalent(ModifierComparison.ModifierUsage.Type,
                i.JavaType.Modifiers,
                i.DotNetType.GetModifiers())
                && !i.DotNetType.HasKnownModifierDifference())
            .Select(i => new MismatchedModifierDiff
            {
                JavaType = new TypeReference
                {
                    TypeKind = i.JavaType.Kind,
                    TypeName = i.JavaType.FullName,
                    DisplayName = i.JavaType.FullName.Replace("$", "."),
                },
                DotNetType = new TypeReference
                {
                    TypeKind = i.DotNetType.GetTypeKind(),
                    TypeName = i.DotNetType.FullName ?? i.DotNetType.Name,
                    DisplayName = i.DotNetType.FormatDisplayName(),
                },
                JavaModifiers = i.JavaType.Modifiers.SortJavaModifiers().ToList(),
                DotNetModifiers = i.DotNetType.GetModifiers().SortDotNetModifiers().ToList(),
            })
            .OrderBy(i => i.DotNetType.DisplayName)
            .ToList();
    }

    private static IReadOnlyList<MatchingType> GetMatchingTypes(LibraryResult javaLib, IReadOnlyList<Type> assemblyTypes)
    {
        return javaLib.Types
            .Where(jt => jt.Modifiers.Contains("public"))
            .Select(t => new
            {
                JavaType = t,
                DotNetType = assemblyTypes.FirstOrDefault(d => TypeComparison.TypesMatch(d, t))
            })
            .Where(i => i.DotNetType != null)
            .Select(i => new MatchingType(i.JavaType, i.DotNetType!))
            .ToList();
    }

    private static IReadOnlyList<MissingTypeDiff> GetJavaTypesNotInDotNet(LibraryResult javaLib, IReadOnlyList<Type> assemblyTypes)
    {
        return javaLib.Types
            .Where(jt => jt.Modifiers.Contains("public"))
            .Where(jt => !assemblyTypes.Any(t => TypeComparison.TypesMatch(t, jt)))
            .Select(jt => new MissingTypeDiff
            {
                Type = new TypeReference
                {
                    TypeKind = jt.Kind,
                    TypeName = jt.FullName,
                    DisplayName = jt.FullName.Replace("$", "."),
                },
                Modifiers = jt.Modifiers.SortJavaModifiers().ToList(),
            })
            .OrderBy(jt => jt.Type.DisplayName)
            .ToList();
    }

    private static IReadOnlyList<MissingTypeDiff> GetDotNetTypesNotInJava(LibraryResult javaLib, IReadOnlyList<Type> assemblyTypes)
    {
        return assemblyTypes
            .Where(t => !javaLib.Types.Any(jt => TypeComparison.TypesMatch(t, jt)))
            .Select(t => new MissingTypeDiff
            {
                Type = new TypeReference
                {
                    TypeKind = t.GetTypeKind(),
                    TypeName = t.FullName ?? t.Name,
                    DisplayName = t.FormatDisplayName(),
                },
                Modifiers = t.GetModifiers().SortDotNetModifiers().ToList(),
            })
            .OrderBy(t => t.Type.DisplayName)
            .ToList();
    }

    internal static IReadOnlyList<Type> GetAssemblyTypesForComparison(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => (t.IsPublic || t.IsNestedPublic) && !t.HasNoLuceneEquivalent())
            .ToList();
    }
}
