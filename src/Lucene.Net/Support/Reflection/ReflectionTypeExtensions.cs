using System;
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

            if (equivalentAttribute != null)
            {
                return equivalentAttribute.ToLuceneTypeInfo();
            }

            var noEquivalentAttribute = type.GetCustomAttribute<NoLuceneEquivalentAttribute>();

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

            string typeName = type.GetLuceneTypeName();

            return new LuceneTypeInfo(packageName, typeName);
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
                    packageName = $"{mapping.JavaPackage}{type.Namespace.Substring(mapping.DotNetNamespace.Length)}";
                    break;
                }
            }

            if (packageName == null)
            {
                packageName = LuceneNetNamespaceRegex.Replace(type.Namespace, "org.apache.lucene.");
            }

            var packageNameParts = packageName.Split('.');
            return string.Join(".", packageNameParts.Select(i => $"{char.ToLowerInvariant(i[0])}{i.Substring(1)}"));
        }

        public static string GetLuceneTypeName(this Type type)
        {
            var typeName = type.Name;

            if (typeName.Contains("`"))
            {
                // Java Generic types are erased, so we don't include the type parameters
                typeName = typeName.Substring(0, type.Name.IndexOf('`'));
            }

            return typeName;
        }
    }
}
