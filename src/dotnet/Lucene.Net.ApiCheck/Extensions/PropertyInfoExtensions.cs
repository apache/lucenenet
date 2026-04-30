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

using System.Reflection;

namespace Lucene.Net.ApiCheck.Extensions;

public static class PropertyInfoExtensions
{
    /// <summary>
    /// Returns the most visible accessor of the property, used to derive the
    /// property's "effective" visibility (matching how C# reports it).
    /// </summary>
    public static MethodInfo? GetMostVisibleAccessor(this PropertyInfo property)
    {
        var getter = property.GetMethod;
        var setter = property.SetMethod;

        if (getter is null)
        {
            return setter;
        }

        if (setter is null)
        {
            return getter;
        }

        // Lower numeric values of MethodAttributes.MemberAccessMask correspond to
        // less-visible access; pick the higher one.
        var getterAccess = (int)(getter.Attributes & MethodAttributes.MemberAccessMask);
        var setterAccess = (int)(setter.Attributes & MethodAttributes.MemberAccessMask);
        return getterAccess >= setterAccess ? getter : setter;
    }

    public static IReadOnlyList<string> GetModifiers(this PropertyInfo property)
    {
        var accessor = property.GetMostVisibleAccessor();
        return accessor?.GetModifiers() ?? new List<string>();
    }

    public static bool IsApiVisible(this PropertyInfo property)
    {
        var accessor = property.GetMostVisibleAccessor();
        return accessor is not null
               && accessor is { IsPrivate: false, IsAssembly: false, IsFamilyAndAssembly: false };
    }
}
