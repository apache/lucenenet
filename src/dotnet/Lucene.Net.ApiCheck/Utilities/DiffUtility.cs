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

    private sealed class DiffState
    {
        public List<MemberReference> JavaOnly { get; } = new();
        public List<MemberReference> DotNetOnly { get; } = new();
        public List<MemberDiff> MatchedWithDifferences { get; } = new();

        public HashSet<FieldMetadata> ConsumedJavaFields { get; } = new();
        public HashSet<PropertyInfo> ConsumedDotNetProperties { get; } = new();
        public HashSet<MethodMetadata> ConsumedJavaMethods { get; } = new();
    }

    private static MemberDiffResult ComputeMemberDiffs(MatchingType matchingType)
    {
        var state = new DiffState();

        DiffFields(matchingType, state);
        DiffConstructors(matchingType, state);
        DiffMethodsAndProperties(matchingType, state);

        return new MemberDiffResult(state.JavaOnly, state.DotNetOnly, state.MatchedWithDifferences);
    }

    private static void DiffFields(MatchingType matchingType, DiffState state)
    {
        var dotNetFields = matchingType.DotNetType.GetApiFields();
        var dotNetProperties = matchingType.DotNetType.GetApiProperties();

        foreach (var dotNetField in dotNetFields)
        {
            var javaField = matchingType.JavaType.Fields
                .FirstOrDefault(j => !state.ConsumedJavaFields.Contains(j) && MemberComparison.FieldNamesMatch(dotNetField, j));

            if (javaField is null)
            {
                state.DotNetOnly.Add(BuildDotNetFieldReference(dotNetField));
                continue;
            }

            state.ConsumedJavaFields.Add(javaField);

            var hasTypeMismatch = !MemberComparison.FieldTypesMatch(dotNetField, javaField);
            var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                ModifierComparison.ModifierUsage.Field,
                javaField.Modifiers,
                dotNetField.GetModifiers(),
                dotNetDeclaringTypeIsSealed: matchingType.DotNetType.IsSealed);

            if (hasTypeMismatch || hasModifierMismatch)
            {
                state.MatchedWithDifferences.Add(new MemberDiff
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

        // Java fields not matched by a .NET field: try matching against .NET properties
        // (Lucene exposes many public mutable fields that the .NET ports promoted to
        // properties).
        foreach (var javaField in matchingType.JavaType.Fields.Where(j => !state.ConsumedJavaFields.Contains(j)).ToList())
        {
            var matchingProperty = dotNetProperties
                .FirstOrDefault(p => !state.ConsumedDotNetProperties.Contains(p)
                                     && MemberComparison.PropertyMatchesJavaField(p, javaField));

            if (matchingProperty is not null)
            {
                state.ConsumedJavaFields.Add(javaField);
                state.ConsumedDotNetProperties.Add(matchingProperty);

                // Java public field ↔ .NET property of the same name and compatible
                // value type is the canonical Lucene.NET porting idiom (the field was
                // promoted to a property to satisfy CA1051 / encapsulation). Treat as
                // equivalent — no member difference recorded.
                continue;
            }

            // Fallback: name-only match → record as type mismatch.
            var nameMatchingProperty = dotNetProperties
                .FirstOrDefault(p => !state.ConsumedDotNetProperties.Contains(p)
                                     && MemberComparison.PropertyNameMatchesJavaField(p, javaField));

            if (nameMatchingProperty is not null)
            {
                state.ConsumedJavaFields.Add(javaField);
                state.ConsumedDotNetProperties.Add(nameMatchingProperty);

                state.MatchedWithDifferences.Add(new MemberDiff
                {
                    MatchedMember = new ComparisonPair<MemberReference>
                    {
                        Java = BuildJavaFieldReference(javaField),
                        DotNet = BuildDotNetPropertyReference(nameMatchingProperty),
                    },
                    HasTypeMismatch = true,
                    HasModifierMismatch = false,
                });
            }
        }

        foreach (var javaField in matchingType.JavaType.Fields.Where(j => !state.ConsumedJavaFields.Contains(j)))
        {
            state.JavaOnly.Add(BuildJavaFieldReference(javaField));
        }
    }

    private static void DiffConstructors(MatchingType matchingType, DiffState state)
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
                    dotNetCtor.GetModifiers(),
                    dotNetDeclaringTypeIsSealed: matchingType.DotNetType.IsSealed);

                if (hasModifierMismatch)
                {
                    state.MatchedWithDifferences.Add(new MemberDiff
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

                    // Paired by arity alone because the strict ConstructorsMatch above failed.
                    // Re-check the parameter types rather than assuming a mismatch: the strict
                    // match can fail for reasons unrelated to this ctor's own parameters, in
                    // which case the signatures are actually compatible.
                    var hasTypeMismatch = !MemberComparison.ConstructorSignaturesMatch(dotNetCtor, pairedJava);

                    var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                        ModifierComparison.ModifierUsage.Member,
                        pairedJava.Modifiers,
                        dotNetCtor.GetModifiers(),
                        dotNetDeclaringTypeIsSealed: matchingType.DotNetType.IsSealed);

                    if (hasTypeMismatch || hasModifierMismatch)
                    {
                        state.MatchedWithDifferences.Add(new MemberDiff
                        {
                            MatchedMember = new ComparisonPair<MemberReference>
                            {
                                Java = BuildJavaConstructorReference(matchingType, pairedJava),
                                DotNet = BuildDotNetConstructorReference(matchingType, dotNetCtor),
                            },
                            HasTypeMismatch = hasTypeMismatch,
                            HasModifierMismatch = hasModifierMismatch,
                        });
                    }

                    continue;
                }
            }

            state.DotNetOnly.Add(BuildDotNetConstructorReference(matchingType, dotNetCtor));
        }

        foreach (var javaCtor in javaCtors.Where(j => !consumedJavaCtors.Contains(j)))
        {
            state.JavaOnly.Add(BuildJavaConstructorReference(matchingType, javaCtor));
        }
    }

    private static void DiffMethodsAndProperties(MatchingType matchingType, DiffState state)
    {
        var javaMethods = matchingType.JavaType.Methods;
        var dotNetMethods = matchingType.DotNetType.GetApiMethods();
        var dotNetProperties = matchingType.DotNetType.GetApiProperties();

        // Properties first: a Java getter/setter/is method matched (even loosely by
        // name) by a .NET property is consumed.
        foreach (var prop in dotNetProperties.Where(p => !state.ConsumedDotNetProperties.Contains(p)))
        {
            var matchedJavaMethods = javaMethods
                .Where(j => !state.ConsumedJavaMethods.Contains(j) && MemberComparison.PropertyMatchesJavaAccessor(prop, j))
                .ToList();

            if (matchedJavaMethods.Count > 0)
            {
                // Don't let the property greedily consume a Java accessor that a standalone .NET
                // method will pair with. Lucene.NET sometimes exposes BOTH a property X and a
                // method SetX/GetX for ONE Java get/set PAIR (e.g. RateLimiter.MbPerSec property +
                // SetMbPerSec method <- getMbPerSec + setMbPerSec). If the property swallows both
                // Java accessors, the .NET method is left orphaned.
                //
                // Only divert an accessor to a .NET method when doing so still leaves the property
                // at least one accessor to pair with. Otherwise (a single Java accessor with both a
                // .NET property AND method, e.g. BooleanQuery.getClauses <- Clauses + GetClauses)
                // the property keeps it and the extra .NET method is genuine enrichment.
                var divertable = matchedJavaMethods
                    .Where(j => dotNetMethods.Any(d => MemberComparison.MethodNamesAndArityMatch(d, j)))
                    .ToList();

                var accessorsToConsume = divertable.Count > 0 && matchedJavaMethods.Count - divertable.Count >= 1
                    ? matchedJavaMethods.Except(divertable).ToList()
                    : matchedJavaMethods;

                state.ConsumedDotNetProperties.Add(prop);
                foreach (var m in accessorsToConsume)
                {
                    state.ConsumedJavaMethods.Add(m);
                }
                continue;
            }

            // Fallback: Java accessor whose name matches but type doesn't → type mismatch.
            var nameOnlyMatch = javaMethods
                .FirstOrDefault(j => !state.ConsumedJavaMethods.Contains(j) && MemberComparison.PropertyNameMatchesJavaAccessor(prop, j));

            if (nameOnlyMatch is not null)
            {
                state.ConsumedDotNetProperties.Add(prop);
                state.ConsumedJavaMethods.Add(nameOnlyMatch);
                state.MatchedWithDifferences.Add(new MemberDiff
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

            state.ConsumedDotNetProperties.Add(prop);
            state.DotNetOnly.Add(BuildDotNetPropertyReference(prop));
        }

        // Methods: pair by name+arity, then check signatures and modifiers.
        foreach (var dotNetMethod in dotNetMethods)
        {
            var fullMatch = javaMethods
                .FirstOrDefault(j => !state.ConsumedJavaMethods.Contains(j) && MemberComparison.MethodsMatch(dotNetMethod, j));

            if (fullMatch is not null)
            {
                state.ConsumedJavaMethods.Add(fullMatch);

                // Dispose-pattern matches against Java close() (whether to .NET Dispose()
                // or protected Dispose(bool disposing)) intentionally diverge in modifiers:
                // Java close() is typically abstract, but Lucene.NET moves abstractness onto
                // the Dispose(bool) overload while Dispose() is concrete and forwards. Don't
                // surface those as member differences.
                if (fullMatch.Name == "close"
                    && fullMatch.Parameters.Count == 0
                    && MemberComparison.IsDotNetDisposePatternMethod(dotNetMethod))
                {
                    continue;
                }

                var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                    ModifierComparison.ModifierUsage.Member,
                    fullMatch.Modifiers,
                    dotNetMethod.GetModifiers(),
                    dotNetDeclaringTypeIsSealed: matchingType.DotNetType.IsSealed);

                if (hasModifierMismatch)
                {
                    state.MatchedWithDifferences.Add(new MemberDiff
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
                .Where(j => !state.ConsumedJavaMethods.Contains(j) && MemberComparison.MethodNamesAndArityMatch(dotNetMethod, j))
                .ToList();

            if (arityCandidates.Count == 1)
            {
                var sameSideOverloads = dotNetMethods
                    .Count(d => MemberComparison.MethodNamesMatch(d.Name, arityCandidates[0].Name)
                                && MemberComparison.EffectiveParameterCount(d.GetParameters()) == arityCandidates[0].Parameters.Count);
                if (sameSideOverloads == 1)
                {
                    var pairedJava = arityCandidates[0];
                    state.ConsumedJavaMethods.Add(pairedJava);

                    // The pair matched by name+arity only because the strict MethodsMatch above
                    // failed. Re-check the signature here rather than assuming a type mismatch:
                    // the strict match can fail for reasons unrelated to the parameter/return
                    // types (e.g. a single de-nested type elsewhere), in which case the
                    // signatures are actually compatible and there is no type mismatch to report.
                    var hasTypeMismatch = !MemberComparison.MethodSignaturesMatch(dotNetMethod, pairedJava);

                    var hasModifierMismatch = !ModifierComparison.ModifiersAreEquivalent(
                        ModifierComparison.ModifierUsage.Member,
                        pairedJava.Modifiers,
                        dotNetMethod.GetModifiers(),
                        dotNetDeclaringTypeIsSealed: matchingType.DotNetType.IsSealed);

                    if (hasTypeMismatch || hasModifierMismatch)
                    {
                        state.MatchedWithDifferences.Add(new MemberDiff
                        {
                            MatchedMember = new ComparisonPair<MemberReference>
                            {
                                Java = BuildJavaMethodReference(pairedJava),
                                DotNet = BuildDotNetMethodReference(dotNetMethod),
                            },
                            HasTypeMismatch = hasTypeMismatch,
                            HasModifierMismatch = hasModifierMismatch,
                        });
                    }

                    continue;
                }
            }

            // Lucene.NET typically exposes both Dispose() and protected Dispose(bool)
            // for a single Java close(). Only one can pair with close() above; the
            // sibling lands here with no Java match. Drop it silently rather than
            // reporting it as .NET-only. The Java close() may be declared on the type
            // itself or inherited from java.io.Closeable / java.lang.AutoCloseable,
            // so check both declared methods and the implements list.
            if (MemberComparison.IsDotNetDisposePatternMethod(dotNetMethod)
                && (matchingType.JavaType.Methods.Any(j => j.Name == "close" && j.Parameters.Count == 0)
                    || matchingType.JavaType.Interfaces.Any(i =>
                        i == "java.io.Closeable" || i == "java.lang.AutoCloseable")))
            {
                continue;
            }

            state.DotNetOnly.Add(BuildDotNetMethodReference(dotNetMethod));
        }

        foreach (var javaMethod in javaMethods.Where(j => !state.ConsumedJavaMethods.Contains(j)))
        {
            state.JavaOnly.Add(BuildJavaMethodReference(javaMethod));
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
            || TypeComparison.BaseTypesMatch(matchingType.DotNetType, matchingType.JavaType.BaseType))
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
                matchingType.DotNetType.GetModifiers(),
                javaTypeKind: matchingType.JavaType.Kind,
                isDotNetEnum: matchingType.DotNetType.IsEnum,
                isDotNetStruct: matchingType.DotNetType is { IsValueType: true, IsEnum: false }))
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
