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
            .Select(i =>
            {
                var memberResult = ComputeMemberDiffs(i);
                return new TypeDiff
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
                    LuceneNetMembersNotInLucene = memberResult.DotNetOnly,
                    LuceneMembersNotInLuceneNet = memberResult.JavaOnly,
                    MatchedMembersWithDifferences = memberResult.MatchedWithDifferences,
                };
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

    private record MemberDiffResult(
        IReadOnlyList<MemberReference> JavaOnly,
        IReadOnlyList<MemberReference> DotNetOnly,
        IReadOnlyList<MemberDiff> MatchedWithDifferences);

    private static MemberDiffResult ComputeMemberDiffs(MatchingType matchingType)
    {
        var javaOnly = new List<MemberReference>();
        var dotNetOnly = new List<MemberReference>();
        var matchedWithDifferences = new List<MemberDiff>();

        DiffFields(matchingType, javaOnly, dotNetOnly, matchedWithDifferences);
        DiffConstructors(matchingType, javaOnly, dotNetOnly, matchedWithDifferences);
        DiffMethodsAndProperties(matchingType, javaOnly, dotNetOnly, matchedWithDifferences);

        return new MemberDiffResult(javaOnly, dotNetOnly, matchedWithDifferences);
    }

    private static void DiffFields(MatchingType matchingType,
        List<MemberReference> javaOnly,
        List<MemberReference> dotNetOnly,
        List<MemberDiff> matchedWithDifferences)
    {
        var dotNetFields = matchingType.DotNetType.GetApiFields();
        var consumedJavaFields = new HashSet<FieldMetadata>();

        foreach (var dotNetField in dotNetFields)
        {
            var javaField = matchingType.JavaType.Fields
                .FirstOrDefault(j => !consumedJavaFields.Contains(j) && MemberComparison.FieldNamesMatch(dotNetField, j));

            if (javaField is null)
            {
                dotNetOnly.Add(BuildDotNetFieldReference(dotNetField));
                continue;
            }

            consumedJavaFields.Add(javaField);

            var hasTypeMismatch = !MemberComparison.FieldTypesMatch(dotNetField, javaField);
            var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                ModifierComparison.ModifierUsage.Member,
                javaField.Modifiers,
                dotNetField.GetModifiers());

            if (hasTypeMismatch || hasModifierMismatch)
            {
                matchedWithDifferences.Add(new MemberDiff
                {
                    MatchedMember = new ComparisonPair<MemberReference>
                    {
                        Java = BuildJavaFieldReference(javaField),
                        DotNet = BuildDotNetFieldReference(dotNetField),
                    },
                    HasTypeMismatch = hasTypeMismatch,
                    HasModifierMismatch = hasModifierMismatch,
                });
            }
        }

        foreach (var javaField in matchingType.JavaType.Fields.Where(j => !consumedJavaFields.Contains(j)))
        {
            javaOnly.Add(BuildJavaFieldReference(javaField));
        }
    }

    private static void DiffConstructors(MatchingType matchingType,
        List<MemberReference> javaOnly,
        List<MemberReference> dotNetOnly,
        List<MemberDiff> matchedWithDifferences)
    {
        var javaCtors = matchingType.JavaType.Constructors ?? new List<ConstructorMetadata>();
        var dotNetCtors = matchingType.DotNetType.GetApiConstructors();
        var consumedJavaCtors = new HashSet<ConstructorMetadata>();

        foreach (var dotNetCtor in dotNetCtors)
        {
            // Prefer a fully-matched ctor (parameter types match exactly).
            var javaCtor = javaCtors
                .FirstOrDefault(j => !consumedJavaCtors.Contains(j) && MemberComparison.ConstructorsMatch(dotNetCtor, j));

            if (javaCtor is not null)
            {
                consumedJavaCtors.Add(javaCtor);

                var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                    ModifierComparison.ModifierUsage.Member,
                    javaCtor.Modifiers,
                    dotNetCtor.GetModifiers());

                if (hasModifierMismatch)
                {
                    matchedWithDifferences.Add(new MemberDiff
                    {
                        MatchedMember = new ComparisonPair<MemberReference>
                        {
                            Java = BuildJavaConstructorReference(matchingType, javaCtor),
                            DotNet = BuildDotNetConstructorReference(matchingType, dotNetCtor),
                        },
                        HasTypeMismatch = false,
                        HasModifierMismatch = true,
                    });
                }

                continue;
            }

            // Try to pair by arity alone, but only when the arity is unique on both
            // sides to avoid mis-pairing overloads.
            var dotNetArity = dotNetCtor.GetParameters().Length;
            if (dotNetCtors.Count(c => c.GetParameters().Length == dotNetArity) == 1)
            {
                var arityCandidates = javaCtors
                    .Where(j => !consumedJavaCtors.Contains(j) && j.Parameters.Count == dotNetArity)
                    .ToList();
                if (arityCandidates.Count == 1)
                {
                    var pairedJava = arityCandidates[0];
                    consumedJavaCtors.Add(pairedJava);

                    var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                        ModifierComparison.ModifierUsage.Member,
                        pairedJava.Modifiers,
                        dotNetCtor.GetModifiers());

                    matchedWithDifferences.Add(new MemberDiff
                    {
                        MatchedMember = new ComparisonPair<MemberReference>
                        {
                            Java = BuildJavaConstructorReference(matchingType, pairedJava),
                            DotNet = BuildDotNetConstructorReference(matchingType, dotNetCtor),
                        },
                        HasTypeMismatch = true,
                        HasModifierMismatch = hasModifierMismatch,
                    });
                    continue;
                }
            }

            dotNetOnly.Add(BuildDotNetConstructorReference(matchingType, dotNetCtor));
        }

        foreach (var javaCtor in javaCtors.Where(j => !consumedJavaCtors.Contains(j)))
        {
            javaOnly.Add(BuildJavaConstructorReference(matchingType, javaCtor));
        }
    }

    private static void DiffMethodsAndProperties(MatchingType matchingType,
        List<MemberReference> javaOnly,
        List<MemberReference> dotNetOnly,
        List<MemberDiff> matchedWithDifferences)
    {
        var javaMethods = matchingType.JavaType.Methods;
        var dotNetMethods = matchingType.DotNetType.GetApiMethods();
        var dotNetProperties = matchingType.DotNetType.GetApiProperties();

        var consumedJavaMethods = new HashSet<MethodMetadata>();

        // Properties first: a Java getter/setter/is method matched (even loosely by
        // name) by a .NET property is consumed.
        foreach (var prop in dotNetProperties)
        {
            var matchedJavaMethods = javaMethods
                .Where(j => !consumedJavaMethods.Contains(j) && MemberComparison.PropertyMatchesJavaAccessor(prop, j))
                .ToList();

            if (matchedJavaMethods.Count > 0)
            {
                foreach (var m in matchedJavaMethods)
                {
                    consumedJavaMethods.Add(m);
                }
                continue;
            }

            // Fallback: Java accessor whose name matches but type doesn't → type mismatch.
            var nameOnlyMatch = javaMethods
                .FirstOrDefault(j => !consumedJavaMethods.Contains(j) && MemberComparison.PropertyNameMatchesJavaAccessor(prop, j));

            if (nameOnlyMatch is not null)
            {
                consumedJavaMethods.Add(nameOnlyMatch);
                matchedWithDifferences.Add(new MemberDiff
                {
                    MatchedMember = new ComparisonPair<MemberReference>
                    {
                        Java = BuildJavaMethodReference(nameOnlyMatch),
                        DotNet = BuildDotNetPropertyReference(prop),
                    },
                    HasTypeMismatch = true,
                    HasModifierMismatch = false,
                });
                continue;
            }

            dotNetOnly.Add(BuildDotNetPropertyReference(prop));
        }

        // Methods: pair by name+arity, then check signatures and modifiers.
        foreach (var dotNetMethod in dotNetMethods)
        {
            var fullMatch = javaMethods
                .FirstOrDefault(j => !consumedJavaMethods.Contains(j) && MemberComparison.MethodsMatch(dotNetMethod, j));

            if (fullMatch is not null)
            {
                consumedJavaMethods.Add(fullMatch);

                var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                    ModifierComparison.ModifierUsage.Member,
                    fullMatch.Modifiers,
                    dotNetMethod.GetModifiers());

                if (hasModifierMismatch)
                {
                    matchedWithDifferences.Add(new MemberDiff
                    {
                        MatchedMember = new ComparisonPair<MemberReference>
                        {
                            Java = BuildJavaMethodReference(fullMatch),
                            DotNet = BuildDotNetMethodReference(dotNetMethod),
                        },
                        HasTypeMismatch = false,
                        HasModifierMismatch = true,
                    });
                }

                continue;
            }

            // Try name+arity pairing. For methods, name+arity uniqueness on both
            // sides keeps overload pairing safe.
            var arityCandidates = javaMethods
                .Where(j => !consumedJavaMethods.Contains(j) && MemberComparison.MethodNamesAndArityMatch(dotNetMethod, j))
                .ToList();

            if (arityCandidates.Count == 1)
            {
                var sameSideOverloads = dotNetMethods
                    .Count(d => MemberComparison.MethodNamesMatch(d.Name, arityCandidates[0].Name)
                                && d.GetParameters().Length == arityCandidates[0].Parameters.Count);
                if (sameSideOverloads == 1)
                {
                    var pairedJava = arityCandidates[0];
                    consumedJavaMethods.Add(pairedJava);

                    var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                        ModifierComparison.ModifierUsage.Member,
                        pairedJava.Modifiers,
                        dotNetMethod.GetModifiers());

                    matchedWithDifferences.Add(new MemberDiff
                    {
                        MatchedMember = new ComparisonPair<MemberReference>
                        {
                            Java = BuildJavaMethodReference(pairedJava),
                            DotNet = BuildDotNetMethodReference(dotNetMethod),
                        },
                        HasTypeMismatch = true,
                        HasModifierMismatch = hasModifierMismatch,
                    });
                    continue;
                }
            }

            dotNetOnly.Add(BuildDotNetMethodReference(dotNetMethod));
        }

        foreach (var javaMethod in javaMethods.Where(j => !consumedJavaMethods.Contains(j)))
        {
            javaOnly.Add(BuildJavaMethodReference(javaMethod));
        }
    }

    private static FieldReference BuildJavaFieldReference(FieldMetadata javaField)
        => new()
        {
            Name = javaField.Name,
            Modifiers = new ModifierSet(javaField.Modifiers.SortJavaModifiers().ToList()),
            IsStatic = javaField.IsStatic,
            FieldType = javaField.Type.ToJavaTypeReference(kind: null),
        };

    private static FieldReference BuildDotNetFieldReference(FieldInfo dotNetField)
        => new()
        {
            Name = dotNetField.Name,
            Modifiers = new ModifierSet(dotNetField.GetModifiers().SortDotNetModifiers().ToList()),
            IsStatic = dotNetField.IsStatic,
            FieldType = dotNetField.FieldType.ToTypeReference(),
        };

    private static ConstructorReference BuildJavaConstructorReference(MatchingType matchingType, ConstructorMetadata javaCtor)
        => new()
        {
            Name = matchingType.JavaType.Name.Replace("$", "."),
            Modifiers = new ModifierSet(javaCtor.Modifiers.SortJavaModifiers().ToList()),
            IsStatic = false,
            Parameters = javaCtor.Parameters
                .Select(p => new Parameter { Name = p.Name, Type = p.Type.ToJavaTypeReference(kind: null) })
                .ToList(),
        };

    private static ConstructorReference BuildDotNetConstructorReference(MatchingType matchingType, ConstructorInfo dotNetCtor)
        => new()
        {
            Name = matchingType.DotNetType.Name,
            Modifiers = new ModifierSet(dotNetCtor.GetModifiers().SortDotNetModifiers().ToList()),
            IsStatic = false,
            Parameters = dotNetCtor.GetParameters()
                .Select(p => new Parameter { Name = p.Name ?? string.Empty, Type = p.ParameterType.ToTypeReference() })
                .ToList(),
        };

    private static MethodReference BuildJavaMethodReference(MethodMetadata javaMethod)
        => new()
        {
            Name = javaMethod.Name,
            Modifiers = new ModifierSet(javaMethod.Modifiers.SortJavaModifiers().ToList()),
            IsStatic = javaMethod.Modifiers.Contains("static"),
            ReturnType = javaMethod.ReturnType.ToJavaTypeReference(kind: null),
            Parameters = javaMethod.Parameters
                .Select(p => new Parameter { Name = p.Name, Type = p.Type.ToJavaTypeReference(kind: null) })
                .ToList(),
        };

    private static MethodReference BuildDotNetMethodReference(MethodInfo dotNetMethod)
        => new()
        {
            Name = dotNetMethod.Name,
            Modifiers = new ModifierSet(dotNetMethod.GetModifiers().SortDotNetModifiers().ToList()),
            IsStatic = dotNetMethod.IsStatic,
            ReturnType = dotNetMethod.ReturnType.ToTypeReference(),
            Parameters = dotNetMethod.GetParameters()
                .Select(p => new Parameter { Name = p.Name ?? string.Empty, Type = p.ParameterType.ToTypeReference() })
                .ToList(),
        };

    private static PropertyReference BuildDotNetPropertyReference(PropertyInfo prop)
        => new()
        {
            Name = prop.Name,
            Modifiers = new ModifierSet(prop.GetModifiers().SortDotNetModifiers().ToList()),
            IsStatic = prop.GetMostVisibleAccessor()?.IsStatic ?? false,
            PropertyType = prop.PropertyType.ToTypeReference(),
            HasGetter = prop.GetMethod is not null,
            HasSetter = prop.SetMethod is not null,
            IsIndexer = prop.GetIndexParameters().Length > 0,
        };

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
