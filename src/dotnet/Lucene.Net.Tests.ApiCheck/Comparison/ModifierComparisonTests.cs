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
    [InlineData(ModifierUsage.Type, "public abstract", "public static", false)]
    // Members
    [InlineData(ModifierUsage.Member, "public", "public", true)]
    [InlineData(ModifierUsage.Member, "public abstract", "public abstract", true)]
    [InlineData(ModifierUsage.Member, "public static", "public static", true)]
    [InlineData(ModifierUsage.Member, "public final", "public sealed", true)]
    [InlineData(ModifierUsage.Member, "public final", "public sealed override", true)]
    [InlineData(ModifierUsage.Member, "public", "public virtual", true)] // Java methods are virtual by default
    [InlineData(ModifierUsage.Member, "public", "public override", true)] // override is the .NET default-virtual continuation
    [InlineData(ModifierUsage.Member, "public synchronized", "public", true)] // 'synchronized' has no .NET analogue
    [InlineData(ModifierUsage.Member, "public", "private", false)] // visibility actually differs
    [InlineData(ModifierUsage.Member, "public abstract", "public", false)] // abstract differs
    [InlineData(ModifierUsage.Member, "public static", "public", false)] // static differs (members)
    [Theory]
    public void ModifiersAreEquivalent(ModifierUsage usage, string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(usage, javaModifiers.Split(" "), dotnetModifiers.Split(" ")));
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
            javaModifiers.Split(" "),
            dotnetModifiers.Split(" "),
            javaTypeKind: "enum",
            isDotNetEnum: true));
    }

    [Fact]
    public void ModifiersAreEquivalent_EnumKindMismatch_FallsThroughToDefault()
    {
        // Java side claims enum but .NET side isn't an enum: skip the enum normalization
        // path so we don't accidentally accept a kind-mismatched pair.
        Assert.False(ModifierComparison.ModifiersAreEquivalent(
            ModifierUsage.Type,
            "public abstract static".Split(" "),
            "public".Split(" "),
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
            javaModifiers.Split(" "),
            dotnetModifiers.Split(" ")));
    }
}
