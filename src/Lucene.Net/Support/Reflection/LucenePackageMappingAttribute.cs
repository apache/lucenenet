using System;
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
    /// Attribute to map a .NET namespace in Lucene.NET to a Lucene Java package for the purposes of reflection.
    /// </summary>
    /// <remarks>
    /// This attribute should only be used when the package name cannot be correctly inferred from the namespace.
    /// Note that <c>org.apache.lucene</c> can always be inferred from the namespace <c>Lucene.Net</c>,
    /// and any sub-packages can be inferred from the sub-namespaces of <c>Lucene.Net</c> by capitalizing the first
    /// letter. For example, <c>Lucene.Net.Codecs</c> maps to <c>org.apache.lucene.codecs</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [assembly: LucenePackageMapping("Lucene.Net.Documents", "org.apache.lucene.document")]
    /// </code>
    /// </example>
    [NoLuceneEquivalent]
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class LucenePackageMappingAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="LucenePackageMappingAttribute"/>.
        /// </summary>
        /// <param name="dotNetNamespace">The .NET namespace to map.</param>
        /// <param name="javaPackage">The Java package to map to.</param>
        public LucenePackageMappingAttribute(string dotNetNamespace, string javaPackage)
        {
            DotNetNamespace = dotNetNamespace;
            JavaPackage = javaPackage;
        }

        /// <summary>
        /// Gets the .NET namespace to map.
        /// </summary>
        public string DotNetNamespace { get; }

        /// <summary>
        /// Gets the Java package to map to.
        /// </summary>
        public string JavaPackage { get; }

        /// <summary>
        /// Gets or sets a justification for why the discrepancy exists.
        /// </summary>
        public string? Justification { get; set; }
    }
}
