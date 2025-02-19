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

namespace Lucene.Net.ApiCheck.Extensions;

public static class TypeExtensions
{
    public static string FormatDisplayName(this Type type)
    {
        var fullName = type.FullName ?? type.Name;
        fullName = fullName.Replace("+", "."); // format nested types

        if (type.IsGenericType)
        {
            var genericArguments = type.GetGenericArguments();
            var genericArgumentsDisplay = string.Join(", ", genericArguments.Select(a => a.Name));
            // LUCENENET TODO: this is likely buggy in some cases like nested types
            return $"{fullName.Split('`')[0]}<{genericArgumentsDisplay}>";
        }

        return fullName;
    }

    public static IReadOnlyList<string> GetModifiers(this Type type)
    {
        var modifiers = new List<string>();

        if (type.IsAbstract)
        {
            modifiers.Add(type.IsSealed ? "static" : "abstract");
        }

        if (type.IsSealed && !type.IsAbstract && !type.IsValueType)
        {
            modifiers.Add("sealed");
        }

        if (type.IsPublic || type.IsNestedPublic)
        {
            modifiers.Add("public");
        }

        // TODO: other modifiers?

        return modifiers;
    }

    public static string GetTypeKind(this Type t)
    {
        if (t.IsInterface)
        {
            return "interface";
        }

        if (t.IsEnum)
        {
            return "enum";
        }

        if (t.IsValueType)
        {
            return "struct";
        }

        return t.IsClass ? "class" : "unknown";
    }
}
