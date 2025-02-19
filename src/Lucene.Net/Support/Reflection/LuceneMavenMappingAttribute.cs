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
    /// Specifies the Maven coordinates for the Lucene package that this assembly is equivalent to.
    /// </summary>
    [NoLuceneEquivalent]
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class LuceneMavenMappingAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="LuceneMavenMappingAttribute"/>.
        /// </summary>
        /// <param name="groupId">The Maven group ID.</param>
        /// <param name="artifactId">The Maven artifact ID.</param>
        /// <param name="version">The Maven version.</param>
        public LuceneMavenMappingAttribute(string groupId, string artifactId, string version)
        {
            GroupId = groupId;
            ArtifactId = artifactId;
            Version = version;
        }

        /// <summary>
        /// Gets the Maven group ID.
        /// </summary>
        public string GroupId { get; }

        /// <summary>
        /// Gets the Maven artifact ID.
        /// </summary>
        public string ArtifactId { get; }

        /// <summary>
        /// Gets the Maven version.
        /// </summary>
        public string Version { get; }
    }
}
