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

    private static readonly HashSet<string> DotNetPublicStaticSpecialCase = new HashSet<string>
    {
        "public",
        "static"
    };

    public enum ModifierUsage
    {
        Type,
        Member,
    }

    public static bool ModifiersAreEquivalent(ModifierUsage usage, IReadOnlyList<string> javaModifiers, IReadOnlyList<string> dotnetModifiers)
        => ModifiersAreEquivalent(usage, javaModifiers, dotnetModifiers, javaTypeKind: null, isDotNetEnum: false);

    public static bool ModifiersAreEquivalent(ModifierUsage usage,
        IReadOnlyList<string> javaModifiers,
        IReadOnlyList<string> dotnetModifiers,
        string? javaTypeKind,
        bool isDotNetEnum)
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

            // .NET virtual/override ↔ Java's default open-by-default behavior (no
            // explicit modifier on the Java side). Drop these so they don't count
            // as differences against an unannotated Java method.
            applicableDotNetModifiers.Remove("virtual");
            applicableDotNetModifiers.Remove("override");

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
