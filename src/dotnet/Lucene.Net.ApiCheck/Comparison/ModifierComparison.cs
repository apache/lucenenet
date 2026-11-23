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

namespace Lucene.Net.ApiCheck.Comparison;

/// <summary>
/// Compares Java and .NET modifiers to determine if they are equivalent.
/// </summary>
/// <remarks>
/// Java modifier specs:
/// <list type="bullet">
/// <item>Classes: https://docs.oracle.com/javase/specs/jls/se11/html/jls-8.html#jls-8.1.1</item>
/// <item>Interfaces: https://docs.oracle.com/javase/specs/jls/se11/html/jls-9.html#jls-9.1.1</item>
/// <item>Enums: (see Classes)</item>
/// </list>
/// <para />
/// C# modifier specs:
/// <list type="bullet">
/// <item>Classes: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/classes#15221-general</item>
/// <item>Interfaces: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/interfaces#1822-interface-modifiers</item>
/// <item>Structs: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/structs#1622-struct-modifiers</item>
/// <item>Enums: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/enums#193-enum-modifiers</item>
/// </list>
/// </remarks>
public static class ModifierComparison
{
    private static readonly HashSet<string> JavaPublicFinalSpecialCase = new HashSet<string>
    {
        "public",
        "final"
    };

    // A Java 'public abstract' class whose members are all static is ported to a .NET
    // 'public static' class (e.g. StringHelper, PriorityQueue). Java has no static-class
    // construct, so the idiomatic Java holder is an abstract class that can't be instantiated.
    private static readonly HashSet<string> JavaPublicAbstractSpecialCase = new HashSet<string>
    {
        "public",
        "abstract"
    };

    private static readonly HashSet<string> DotNetPublicStaticSpecialCase = new HashSet<string>
    {
        "public",
        "static"
    };

    public enum ModifierUsage
    {
        Type,
        Member,
        Field,
    }

    public static bool ModifiersAreEquivalent(ModifierUsage usage, IReadOnlyList<string> javaModifiers, IReadOnlyList<string> dotnetModifiers)
        => ModifiersAreEquivalent(usage, javaModifiers, dotnetModifiers, javaTypeKind: null, isDotNetEnum: false, dotNetDeclaringTypeIsSealed: false);

    public static bool ModifiersAreEquivalent(ModifierUsage usage,
        IReadOnlyList<string> javaModifiers,
        IReadOnlyList<string> dotnetModifiers,
        bool dotNetDeclaringTypeIsSealed)
        => ModifiersAreEquivalent(usage, javaModifiers, dotnetModifiers, javaTypeKind: null, isDotNetEnum: false, dotNetDeclaringTypeIsSealed: dotNetDeclaringTypeIsSealed);

    public static bool ModifiersAreEquivalent(ModifierUsage usage,
        IReadOnlyList<string> javaModifiers,
        IReadOnlyList<string> dotnetModifiers,
        string? javaTypeKind,
        bool isDotNetEnum,
        bool dotNetDeclaringTypeIsSealed = false,
        bool isDotNetStruct = false)
    {
        var applicableJavaModifiers = new HashSet<string>(javaModifiers);
        var applicableDotNetModifiers = new HashSet<string>(dotnetModifiers);

        // for our purposes, we expect all types to be public
        if (usage == ModifierUsage.Type)
        {
            if (!applicableJavaModifiers.Contains("public"))
            {
                throw new ArgumentException("All Java types are expected to be public.");
            }

            if (!applicableDotNetModifiers.Contains("public"))
            {
                throw new ArgumentException("All .NET types are expected to be public.");
            }

            // Enums on the .NET side are implicitly sealed and can't carry static/abstract
            // (those are compile errors). Java enums always carry static (when nested) and
            // either 'final' (plain enum) or 'abstract' (enum with abstract methods, which
            // Lucene.NET ports as a different shape). Drop these Java-only modifiers when
            // both sides are enums so the comparison degenerates to a public-only match.
            if (string.Equals(javaTypeKind, "enum", StringComparison.Ordinal) && isDotNetEnum)
            {
                applicableJavaModifiers.Remove("static");
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Remove("abstract");
                return applicableJavaModifiers.SetEquals(applicableDotNetModifiers);
            }

            // special case where we consider public final in Java to be equivalent to public static in .NET
            if (applicableJavaModifiers.SetEquals(JavaPublicFinalSpecialCase)
                && applicableDotNetModifiers.SetEquals(DotNetPublicStaticSpecialCase))
            {
                return true;
            }

            // public abstract (Java holder class) ↔ public static (.NET static class)
            if (applicableJavaModifiers.SetEquals(JavaPublicAbstractSpecialCase)
                && applicableDotNetModifiers.SetEquals(DotNetPublicStaticSpecialCase))
            {
                return true;
            }

            // A .NET struct is implicitly sealed and is the value-type port of a Java final
            // (often 'public static final' for a nested) class. GetModifiers does not emit
            // 'sealed' for a struct, so drop the Java-side 'final'/'static' that have no .NET
            // struct analogue before comparing.
            if (isDotNetStruct)
            {
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Remove("static");
            }

            // static has different meanings in Java and .NET
            applicableJavaModifiers.Remove("static");
            applicableDotNetModifiers.Remove("static");

            if (applicableJavaModifiers.Contains("final"))
            {
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Add("sealed"); // normalize to C# naming conventions
            }
        }
        else if (usage == ModifierUsage.Member)
        {
            // Java 'final' on a method/field ↔ .NET 'sealed' (for sealed override)
            // or no virtual modifier at all. Normalize 'final' → 'sealed', and treat
            // 'sealed override' on the .NET side as 'sealed' for comparison.
            if (applicableJavaModifiers.Contains("final"))
            {
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Add("sealed");
            }

            if (applicableDotNetModifiers.Contains("sealed override"))
            {
                applicableDotNetModifiers.Remove("sealed override");
                applicableDotNetModifiers.Add("sealed");
            }

            // When the .NET declaring type is sealed, no member can be further overridden
            // through it — so Java 'final' (now 'sealed') is redundant. Likewise a .NET
            // 'override' on a sealed type is effectively a sealed override. Drop the
            // 'sealed' designator on both sides so the member-level final/override
            // bookkeeping doesn't surface as a diff.
            if (dotNetDeclaringTypeIsSealed)
            {
                applicableJavaModifiers.Remove("sealed");
                applicableDotNetModifiers.Remove("sealed");
            }

            // Java 'static final' methods are idiomatic-but-redundant: 'final' on a static
            // method only blocks subclass *hiding* (declaring a same-signature static in a
            // subclass), not virtual override (statics aren't dispatched virtually). .NET
            // resolves static method calls on the declaring type, so the Java 'final' has
            // no .NET analogue. Drop the normalized 'sealed' on the Java side when both
            // sides agree the member is static.
            if (applicableJavaModifiers.Contains("static") && applicableDotNetModifiers.Contains("static"))
            {
                applicableJavaModifiers.Remove("sealed");
            }

            // .NET virtual/override ↔ Java's default open-by-default behavior (no
            // explicit modifier on the Java side). Drop these so they don't count
            // as differences against an unannotated Java method.
            applicableDotNetModifiers.Remove("virtual");
            applicableDotNetModifiers.Remove("override");

            // Java 'protected' ↔ .NET 'protected internal'. Lucene.NET frequently widens
            // Java 'protected' members to 'protected internal' so the test framework
            // (which lives in a separate assembly) can reach them. Treat the two as
            // equivalent for the API comparison.
            if (applicableDotNetModifiers.Contains("protected internal"))
            {
                applicableDotNetModifiers.Remove("protected internal");
                applicableDotNetModifiers.Add("protected");
            }

            // Java 'abstract' is meaningful on both sides and should match.
            // Java 'static' is meaningful on both sides and should match (for members,
            // static has the same meaning unlike for types).
            // Java 'synchronized', 'native', 'strictfp', 'transient', 'volatile' have
            // no .NET equivalent on members; ignore them.
            foreach (var javaOnly in JavaMemberOnlyModifiers)
            {
                applicableJavaModifiers.Remove(javaOnly);
            }
        }
        else if (usage == ModifierUsage.Field)
        {
            // Java 'final' on a field ↔ .NET 'readonly' (init-only). This is distinct
            // from the method case where 'final' maps to 'sealed'. Note: a Java
            // 'public static final' constant is often ported to a .NET 'const', which
            // shows up here as 'public const' (no 'static'/'readonly' on the .NET side
            // because const is implicitly static and not init-only). Treat .NET 'const'
            // as 'static readonly' for the comparison so the two forms match.
            if (applicableJavaModifiers.Contains("final"))
            {
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Add("readonly");
            }

            if (applicableDotNetModifiers.Contains("const"))
            {
                applicableDotNetModifiers.Remove("const");
                applicableDotNetModifiers.Add("static");
                applicableDotNetModifiers.Add("readonly");
            }

            // Java 'protected' ↔ .NET 'protected internal' (see Member branch for rationale).
            if (applicableDotNetModifiers.Contains("protected internal"))
            {
                applicableDotNetModifiers.Remove("protected internal");
                applicableDotNetModifiers.Add("protected");
            }

            // 'transient'/'volatile' are Java-only field modifiers with no .NET analogue.
            applicableJavaModifiers.Remove("transient");
            applicableJavaModifiers.Remove("volatile");
        }

        return applicableJavaModifiers.SetEquals(applicableDotNetModifiers);
    }

    private static readonly HashSet<string> JavaMemberOnlyModifiers = new()
    {
        "synchronized",
        "native",
        "strictfp",
        "transient",
        "volatile",
    };
}
