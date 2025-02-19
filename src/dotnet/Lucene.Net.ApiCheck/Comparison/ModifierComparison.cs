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
    public enum ModifierUsage
    {
        Type,
    }

    public static bool ModifiersAreEquivalent(ModifierUsage usage, IReadOnlyList<string> javaModifiers, IReadOnlyList<string> dotnetModifiers)
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

            // static has different meanings in Java and .NET
            applicableJavaModifiers.Remove("static");
            applicableDotNetModifiers.Remove("static");

            if (applicableJavaModifiers.Contains("final"))
            {
                applicableJavaModifiers.Remove("final");
                applicableJavaModifiers.Add("sealed"); // normalize to C# naming conventions
            }
        }

        return applicableJavaModifiers.SetEquals(applicableDotNetModifiers);
    }
}
