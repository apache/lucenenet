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
    /// Specifies the full name of the equivalent Java type in Lucene, if the name or namespace/package cannot be inferred.
    /// </summary>
    /// <remarks>
    /// Avoid adding this attribute eagerly. Instead, only add it when the name or namespace/package of the equivalent
    /// Java type in Lucene cannot be inferred from the namespace and name of the .NET type.
    /// <para />
    /// For example, if the .NET type is <c>Lucene.Net.Analysis.Analyzer</c>, the equivalent Java type in Lucene is
    /// <c>org.apache.lucene.analysis.Analyzer</c>. In this case, the attribute is not needed because the namespace and
    /// name of the .NET type can be used to infer the equivalent Java type in Lucene.
    /// <para />
    /// An optional <see cref="Justification"/> can be added to communicate why the discrepancy exists.
    /// Example:
    /// <code>
    /// [LuceneEquivalent("org.apache.lucene.document", "FloatField", Justification = ".NET numeric type naming conventions")]
    /// </code>
    /// <para />
    /// For nested/inner classes, specify the full hierarchy of the class using dot notation.
    /// For example, if the inner class is named <c>MyInnerClass</c> and is nested inside <c>MyOuterClass</c>, the
    /// <see cref="TypeName"/> value should be <c>MyOuterClass.MyInnerClass</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// [LuceneEquivalent("org.apache.lucene.document", "FloatField")]
    /// public sealed class SingleField : Field { ... }
    /// </code>
    /// </example>
    [NoLuceneEquivalent] // This attribute does not exist in Lucene
    [AttributeUsage(
        AttributeTargets.Class
        | AttributeTargets.Enum
        | AttributeTargets.Interface
        | AttributeTargets.Struct,
        Inherited = false, AllowMultiple = false)]
    public class LuceneTypeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="LuceneTypeAttribute"/>.
        /// </summary>
        /// <param name="packageName">The Java package name of the Lucene type.</param>
        /// <param name="typeName">The name of the Lucene type.</param>
        public LuceneTypeAttribute(string packageName, string typeName)
        {
            PackageName = packageName;
            TypeName = typeName;
        }

        /// <summary>
        /// Gets the Lucene type's package name.
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// Gets the name of the Lucene type.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets a justification for why the discrepancy exists.
        /// </summary>
        public string? Justification { get; set; }
    }
}
