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
    [Theory]
    public void ModifiersAreEquivalent(ModifierUsage usage, string javaModifiers, string dotnetModifiers, bool expected)
    {
        Assert.Equal(expected, ModifierComparison.ModifiersAreEquivalent(usage, javaModifiers.Split(" "), dotnetModifiers.Split(" ")));
    }
}
