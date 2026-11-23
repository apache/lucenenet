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
using ModifierUsage = Lucene.Net.ApiCheck.Comparison.ModifierComparison.ModifierUsage;

namespace Lucene.Net.Tests.ApiCheck.Comparison;

public class ModifierComparisonTests
{
    [InlineData(ModifierUsage.Type, "public", "public", true)]
    [InlineData(ModifierUsage.Type, "public abstract", "public abstract", true)]
    [InlineData(ModifierUsage.Type, "public static", "public", true)] // for Java inner types
    [InlineData(ModifierUsage.Type, "public", "public static", true)] // for .NET static types
    [InlineData(ModifierUsage.Type, "public final", "public static", true)] // for .NET static types, this works. static == abstract sealed, so this is close enough for our purposes
    [InlineData(ModifierUsage.Type, "public final", "public sealed", true)]
    [InlineData(ModifierUsage.Type, "public final", "public", false)]
    [InlineData(ModifierUsage.Type, "public", "public sealed", false)]
    [InlineData(ModifierUsage.Type, "public", "public abstract", false)]
    [InlineData(ModifierUsage.Type, "public abstract", "public sealed", false)]
    // H-7: a Java 'public abstract' holder class (all-static members) is ported to a
    // .NET 'public static' class. GetModifiers emits 'static' for a .NET static class.
    [InlineData(ModifierUsage.Type, "public abstract", "public static", true)]
    // Members
    [InlineData(ModifierUsage.Member, "public", "public", true)]
    [InlineData(ModifierUsage.Member, "public abstract", "public abstract", true)]
    [InlineData(ModifierUsage.Member, "public static", "public static", true)]
    [InlineData(ModifierUsage.Member, "public final", "public sealed", true)]
    [InlineData(ModifierUsage.Member, "public final", "public sealed override", true)]
    [InlineData(ModifierUsage.Member, "public", "public virtual", true)] // Java methods are virtual by default
    [InlineData(ModifierUsage.Member, "public", "public override", true)] // override is the .NET default-virtual continuation
    [InlineData(ModifierUsage.Member, "public synchronized", "public", true)] // 'synchronized' has no .NET analogue
    // Java 'static final' methods: 'final' on a static method only blocks subclass
    // hiding (not virtual override, since statics aren't virtual), so it has no .NET
    // analogue and should not register as a diff.
    [InlineData(ModifierUsage.Member, "public static final", "public static", true)]
    [InlineData(ModifierUsage.Member, "protected static final", "protected static", true)]
    [InlineData(ModifierUsage.Member, "public final", "public", false)] // instance: 'final' is meaningful (blocks override)
    [InlineData(ModifierUsage.Member, "public final", "public static", false)] // static differs (instance vs static)
    [InlineData(ModifierUsage.Member, "public static final", "public", false)] // static differs (one is instance)
    // Lucene.NET widens Java 'protected' members to 'protected internal' so the test
    // framework (separate assembly) can reach them. Treat the two as equivalent.
    [InlineData(ModifierUsage.Member, "protected", "protected internal", true)]
    [InlineData(ModifierUsage.Member, "protected abstract", "protected internal abstract", true)]
    [InlineData(ModifierUsage.Member, "protected", "protected internal virtual", true)]
    [InlineData(ModifierUsage.Member, "protected final", "protected internal sealed", true)]
    [InlineData(ModifierUsage.Member, "public", "protected internal", false)] // visibility still differs vs public
    [InlineData(ModifierUsage.Member, "public", "private", false)] // visibility actually differs
    [InlineData(ModifierUsage.Member, "public abstract", "public", false)] // abstract differs
    [InlineData(ModifierUsage.Member, "public static", "public", false)] // static differs (members)
    [Theory]
    public void ModifiersAreEquivalent(ModifierUsage usage, string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(usage, ParseModifiers(javaModifiers), ParseModifiers(dotnetModifiers)));
    }

    // Multi-word .NET modifiers (`protected internal`, `private protected`, `sealed override`)
    // are emitted by the production extractors as a single string with an embedded space —
    // see ConstructorInfoExtensions / MethodInfoExtensions / FieldInfoExtensions. The test
    // inputs need to mirror that shape, so re-glue these compounds before splitting.
    private static string[] ParseModifiers(string s)
    {
        s = s
            .Replace("protected internal", "protected_internal")
            .Replace("private protected", "private_protected")
            .Replace("sealed override", "sealed_override");
        return s.Split(' ').Select(m => m.Replace('_', ' ')).ToArray();
    }

    // Enum cases: a Java enum's static/final/abstract are not applicable to a .NET enum
    // (.NET enums are implicitly sealed and cannot carry static or abstract). When both
    // sides are enums, those Java modifiers should be dropped before comparing.
    [InlineData("public static final", "public", true)] // nested Java enum (e.g., Field.Store)
    [InlineData("public final", "public", true)]        // top-level Java enum (e.g., MergeTrigger)
    [InlineData("public abstract static", "public", true)] // Java enum with abstract method (e.g., Field.Index)
    [InlineData("public abstract", "public", true)]
    [InlineData("public static", "public", true)]
    [InlineData("public", "public", true)]
    [Theory]
    public void ModifiersAreEquivalent_Enum(string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Type,
            ParseModifiers(javaModifiers),
            ParseModifiers(dotnetModifiers),
            javaTypeKind: "enum",
            isDotNetEnum: true));
    }

    // H-7: a .NET struct is the value-type port of a Java final / static-final (nested) class.
    // GetModifiers does not emit 'sealed' for a struct, so the Java-side final/static (which have
    // no struct analogue) should be dropped when isDotNetStruct is set.
    [InlineData("public static final", "public", true)] // nested Java final class -> .NET struct
    [InlineData("public final", "public", true)]        // top-level Java final class -> .NET struct
    [InlineData("public", "public", true)]
    [Theory]
    public void ModifiersAreEquivalent_Type_DotNetStruct(string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Type,
            ParseModifiers(javaModifiers),
            ParseModifiers(dotnetModifiers),
            javaTypeKind: "class",
            isDotNetEnum: false,
            dotNetDeclaringTypeIsSealed: false,
            isDotNetStruct: true));
    }

    // Without the struct flag, the same Java final/static-final modifiers are a real diff
    // against a bare 'public' .NET type (the modifiers don't simply vanish).
    [InlineData("public static final", "public", false)]
    [InlineData("public final", "public", false)]
    [Theory]
    public void ModifiersAreEquivalent_Type_NonStruct_FinalIsDiff(string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Type,
            ParseModifiers(javaModifiers),
            ParseModifiers(dotnetModifiers),
            javaTypeKind: "class",
            isDotNetEnum: false));
    }

    // When the .NET declaring type is sealed (which includes .NET static classes),
    // member-level final/sealed/override designators are redundant — no member there can
    // be further overridden through the type. Java 'final' and .NET 'override' (or no
    // designator at all) all collapse to the same shape.
    [InlineData("public final", "public override", true)]
    [InlineData("public final", "public", true)]
    [InlineData("public final", "public sealed override", true)]
    [InlineData("public", "public override", true)] // already true without sealed, kept for clarity
    [InlineData("protected final", "protected internal override", true)]
    [InlineData("public abstract", "public", false)] // abstract is still a real diff
    [InlineData("public static", "public", false)] // static is still a real diff
    [Theory]
    public void ModifiersAreEquivalent_Member_DotNetSealedType(string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Member,
            ParseModifiers(javaModifiers),
            ParseModifiers(dotnetModifiers),
            dotNetDeclaringTypeIsSealed: true));
    }

    [Fact]
    public void ModifiersAreEquivalent_EnumKindMismatch_FallsThroughToDefault()
    {
        // Java side claims enum but .NET side isn't an enum: skip the enum normalization
        // path so we don't accidentally accept a kind-mismatched pair.
        Assert.False(ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Type,
            ParseModifiers("public abstract static"),
            ParseModifiers("public"),
            javaTypeKind: "enum",
            isDotNetEnum: false));
    }

    // Fields: Java 'final' ↔ .NET 'readonly' (not 'sealed' as for methods). A Java
    // 'public static final' constant is often ported to a .NET 'public const', which
    // is implicitly static and not init-only — treat 'const' as 'static readonly'.
    [InlineData("public static final", "public static readonly", true)]
    [InlineData("public final", "public readonly", true)]
    [InlineData("public static final", "public const", true)] // Java constant ported to .NET const
    [InlineData("public", "public", true)]
    [InlineData("protected static final", "protected static readonly", true)]
    // Java 'protected' field ↔ .NET 'protected internal' (see Member tests for rationale)
    [InlineData("protected", "protected internal", true)]
    [InlineData("protected static final", "protected internal static readonly", true)]
    [InlineData("public transient", "public", true)] // 'transient' has no .NET analogue
    [InlineData("public volatile", "public", true)] // 'volatile' has no .NET analogue
    [InlineData("public static final", "public static", false)] // missing readonly on .NET side
    [InlineData("public", "public readonly", false)] // .NET-only readonly
    [InlineData("public static", "public", false)] // static differs
    [InlineData("public", "private", false)] // visibility differs
    [Theory]
    public void ModifiersAreEquivalent_Field(string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Field,
            ParseModifiers(javaModifiers),
            ParseModifiers(dotnetModifiers)));
    }
}
