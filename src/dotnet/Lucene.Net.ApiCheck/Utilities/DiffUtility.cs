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
// ReSharper disable LocalizableElement

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
        var assemblyTypes = GetAssemblyTypesForComparison(libraryToDiff.Assembly);
        var libraryConfig = libraryToDiff.LibraryConfig;

        var netTypesNotInJava = GetDotNetTypesNotInJava(javaLib, assemblyTypes);
        var javaTypesNotInNet = GetJavaTypesNotInDotNet(javaLib, assemblyTypes);
        var typeDiffs = GetMatchingTypes(javaLib, assemblyTypes)
            .Select(i => new TypeDiff
            {
                MatchingType = new ComparisonPair<TypeDeclaration>
                {
                    Java = new TypeDeclaration
                    {
                        Type = i.JavaType.ToTypeReference(),
                        Modifiers = new ModifierSet(i.JavaType.Modifiers.SortJavaModifiers().ToList()),
                    },
                    DotNet = new TypeDeclaration
                    {
                        Type = i.DotNetType.ToTypeReference(),
                        Modifiers = new ModifierSet(i.DotNetType.GetModifiers().SortDotNetModifiers().ToList()),
                    },
                },
                MismatchedModifiers = GetTypeMismatchedModifiers(i),
                MismatchedBaseType = GetTypeMismatchedBaseType(i),
                MismatchedInterfaces = GetTypeMismatchedInterfaces(i),
                LuceneNetMembersNotInLucene = GetDotNetMembersNotInJava(i),
                LuceneMembersNotInLuceneNet = GetJavaMembersNotInDotNet(i),
            })
            .ToList();

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
            MatchingTypes = typeDiffs,
        };

        return diff;
    }

    private static IReadOnlyList<MemberReference> GetJavaMembersNotInDotNet(MatchingType matchingType)
    {
        return matchingType.JavaType.Fields
            .Where(j => !matchingType.DotNetType.GetFields().Any(d => MemberComparison.MembersMatch(d, j)))
            .Select(j => new FieldReference
            {
                Name = j.Name,
                Modifiers = new ModifierSet(j.Modifiers.SortJavaModifiers().ToList()),
                IsStatic = j.IsStatic,
                FieldType = j.Type.ToJavaTypeReference(kind: null)
            })
            .ToList();
    }

    private static IReadOnlyList<MemberReference> GetDotNetMembersNotInJava(MatchingType matchingType)
    {
        return matchingType.DotNetType.GetFields()
            .Where(d => !matchingType.JavaType.Fields.Any(j => MemberComparison.MembersMatch(d, j)))
            .Select(d => new FieldReference
            {
                Name = d.Name,
                Modifiers = new ModifierSet(d.GetModifiers().SortDotNetModifiers().ToList()),
                IsStatic = d.IsStatic,
                FieldType = d.FieldType.ToTypeReference(),
            })
            .ToList();
    }

    private static ComparisonPair<IReadOnlyList<TypeReference>>? GetTypeMismatchedInterfaces(MatchingType matchingType)
    {
        if (matchingType.DotNetType.HasKnownInterfaceDifference()
            || TypeComparison.InterfacesMatch(matchingType.DotNetType.GetImplementedInterfaces(),
                matchingType.JavaType.Interfaces))
        {
            return null;
        }

        return new ComparisonPair<IReadOnlyList<TypeReference>>
        {
            Java = matchingType.JavaType.Interfaces.Select(j => j.ToJavaTypeReference("interface")).ToList(),
            DotNet = matchingType.DotNetType.GetImplementedInterfaces().Select(d => d.ToTypeReference()).ToList(),
        };
    }

    private static ComparisonPair<TypeReference?>? GetTypeMismatchedBaseType(MatchingType matchingType)
    {
        if (matchingType.DotNetType.BaseType == null && matchingType.JavaType.BaseType == null)
        {
            return null;
        }

        if (matchingType.DotNetType.HasKnownBaseTypeDifference()
            || TypeComparison.TypeMatchesFullName(matchingType.DotNetType.BaseType, matchingType.JavaType.BaseType,
                "class"))
        {
            return null;
        }

        return new ComparisonPair<TypeReference?>
        {
            Java = matchingType.JavaType.BaseType?.ToJavaTypeReference("class"),
            DotNet = matchingType.DotNetType.BaseType?.ToTypeReference(),
        };
    }

    private static ComparisonPair<ModifierSet>? GetTypeMismatchedModifiers(MatchingType matchingType)
    {
        if (matchingType.DotNetType.HasKnownModifierDifference()
            || ModifierComparison.ModifiersAreEquivalent(ModifierComparison.ModifierUsage.Type,
                matchingType.JavaType.Modifiers,
                matchingType.DotNetType.GetModifiers()))
        {
            return null;
        }

        return new ComparisonPair<ModifierSet>
        {
            Java = new ModifierSet(matchingType.JavaType.Modifiers.SortJavaModifiers().ToList()),
            DotNet = new ModifierSet(matchingType.DotNetType.GetModifiers().SortDotNetModifiers().ToList()),
        };
    }

    private static IReadOnlyList<MatchingType> GetMatchingTypes(LibraryResult javaLib,
        IReadOnlyList<Type> assemblyTypes)
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

    private static IReadOnlyList<TypeDeclaration> GetJavaTypesNotInDotNet(LibraryResult javaLib,
        IReadOnlyList<Type> assemblyTypes)
    {
        return javaLib.Types
            .Where(jt => jt.Modifiers.Contains("public"))
            .Where(jt => !assemblyTypes.Any(t => TypeComparison.TypesMatch(t, jt)))
            .Select(jt => new TypeDeclaration
            {
                Type = jt.ToTypeReference(),
                Modifiers = new ModifierSet(jt.Modifiers.SortJavaModifiers().ToList()),
            })
            .OrderBy(jt => jt.Type.DisplayName)
            .ToList();
    }

    private static IReadOnlyList<TypeDeclaration> GetDotNetTypesNotInJava(LibraryResult javaLib,
        IReadOnlyList<Type> assemblyTypes)
    {
        return assemblyTypes
            .Where(t => !javaLib.Types.Any(jt => TypeComparison.TypesMatch(t, jt)))
            .Select(t => new TypeDeclaration
            {
                Type = t.ToTypeReference(),
                Modifiers = new ModifierSet(t.GetModifiers().SortDotNetModifiers().ToList()),
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
