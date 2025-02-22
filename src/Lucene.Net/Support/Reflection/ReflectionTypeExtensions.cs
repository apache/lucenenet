using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#nullable enable

namespace Lucene.Net.Reflection
{
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

    /// <summary>
    /// Extensions on <see cref="Type"/> to provide functionality for reflection.
    /// </summary>
    [NoLuceneEquivalent]
    public static class ReflectionTypeExtensions
    {
        private static readonly Regex LuceneNetNamespaceRegex = new("^Lucene.Net.", RegexOptions.Compiled);

        /// <summary>
        /// Gets the <see cref="LuceneTypeInfo"/> for the specified type.
        /// </summary>
        /// <param name="type">The type to get the Lucene type information for.</param>
        /// <returns>The Lucene type information for the specified type.</returns>
        /// <exception cref="ArgumentException">The namespace of the type is null.</exception>
        public static LuceneTypeInfo? GetLuceneTypeInfo(this Type type)
        {
            var equivalentAttribute = type.GetCustomAttribute<LuceneTypeAttribute>();
            var noEquivalentAttribute = type.GetCustomAttribute<NoLuceneEquivalentAttribute>();

            if (equivalentAttribute != null)
            {
                if (noEquivalentAttribute != null)
                {
                    throw new InvalidOperationException($"Type '{type.FullName}' cannot have both a LuceneTypeAttribute and a NoLuceneEquivalentAttribute.");
                }

                return new LuceneTypeInfo(equivalentAttribute.PackageName, equivalentAttribute.TypeName, type.BaseType?.GetLuceneTypeInfo());
            }

            if (noEquivalentAttribute != null)
            {
                return null;
            }

            string? packageName = type.GetLucenePackageName();

            if (packageName == null)
            {
                // happens for some special types like <PrivateImplementationDetails>
                return null;
            }

            string typeName = type.GetInferredLuceneTypeName();

            return new LuceneTypeInfo(packageName, typeName, type.BaseType?.GetLuceneTypeInfo());
        }

        /// <summary>
        /// Gets the Lucene package name for the specified type.
        /// </summary>
        /// <param name="type">The type to get the Lucene package name for.</param>
        /// <returns>The Lucene package name for the specified type, or null if the package name cannot be determined.</returns>
        /// <exception cref="ArgumentException">The namespace of the type is null.</exception>
        public static string? GetLucenePackageName(this Type type)
        {
            if (type.Namespace == null)
            {
                return null;
            }

            string? packageName = null;

            var nsMappings = type.Assembly.GetLucenePackageMappings();

            foreach (var mapping in nsMappings.OrderByDescending(i => i.DotNetNamespace.Length))
            {
                if (type.Namespace.StartsWith(mapping.DotNetNamespace, StringComparison.Ordinal))
                {
                    packageName = $"{mapping.JavaPackage}{type.Namespace.Substring(mapping.DotNetNamespace.Length).ToLowerInvariant()}";
                    break;
                }
            }

            if (packageName == null)
            {
                // assume all lower-case, any casing will have to be mapped manually
                packageName = LuceneNetNamespaceRegex.Replace(type.Namespace, "org.apache.lucene.").ToLowerInvariant();
            }

            var packageNameParts = packageName.Split('.');
            return string.Join(".", packageNameParts);
        }

        /// <summary>
        /// Gets the inferred Lucene type name for the specified type.
        /// </summary>
        /// <param name="type">The type to get the Lucene type name for.</param>
        /// <returns>The Lucene type name for the specified type.</returns>
        public static string GetInferredLuceneTypeName(this Type type)
        {
            var typeName = type.Name;

            if (typeName.Contains('`'))
            {
                // Java Generic types are erased, so we don't include the type parameters
                typeName = typeName.Substring(0, type.Name.IndexOf('`'));
            }

            if (type.IsInterface && typeName.Length > 1 && typeName[0] == 'I' && char.IsUpper(typeName[1]))
            {
                typeName = typeName.Substring(1);
            }

            Type? declaringType = type.DeclaringType;

            while (declaringType != null)
            {
                typeName = $"{declaringType.Name}.{typeName}";
                declaringType = declaringType.DeclaringType;
            }

            return typeName;
        }

        /// <summary>
        /// Gets whether the type has a known modifier difference between .NET and Java.
        /// </summary>
        /// <param name="type">The type to check for a known modifier difference.</param>
        /// <returns>true if the type has a known modifier difference; otherwise, false.</returns>
        public static bool HasKnownModifierDifference(this Type type)
            => type.GetCustomAttribute<LuceneModifierDifferenceAttribute>() != null;

        /// <summary>
        /// Gets whether the type has a known base type difference between .NET and Java.
        /// </summary>
        /// <param name="type">The type to check for a known base type difference.</param>
        /// <returns>true if the type has a known base type difference; otherwise, false.</returns>
        public static bool HasKnownBaseTypeDifference(this Type type)
            => type.GetCustomAttribute<LuceneBaseTypeDifferenceAttribute>() != null;

        /// <summary>
        /// Gets whether the type has a known interface difference between .NET and Java.
        /// </summary>
        /// <param name="type">The type to check for a known interface difference.</param>
        /// <returns>true if the type has a known interface difference; otherwise, false.</returns>
        public static bool HasKnownInterfaceDifference(this Type type)
            => type.GetCustomAttribute<LuceneInterfaceDifferenceAttribute>() != null;

        /// <summary>
        /// Gets whether the type has no Lucene equivalent.
        /// </summary>
        /// <param name="type">The type to check for a Lucene equivalent.</param>
        /// <returns>true if the type has no Lucene equivalent; otherwise, false.</returns>
        public static bool HasNoLuceneEquivalent(this Type type)
            => type.GetCustomAttribute<NoLuceneEquivalentAttribute>() != null;
    }
}
