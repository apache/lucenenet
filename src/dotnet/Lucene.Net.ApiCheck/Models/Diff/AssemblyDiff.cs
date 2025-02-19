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

using Lucene.Net.ApiCheck.Models.Config;

namespace Lucene.Net.ApiCheck.Models.Diff;

public class AssemblyDiff
{
    public required string LuceneName { get; set; }

    public string LuceneVersion => LuceneMavenCoordinates.Version;

    public required MavenCoordinates LuceneMavenCoordinates { get; set; }

    public required string LuceneNetName { get; set; }

    public required string LuceneNetVersion { get; set; }

    public required LibraryConfig LibraryConfig { get; set; }

    public required IReadOnlyList<MissingTypeDiff> LuceneNetTypesNotInLucene { get; set; }

    public required IReadOnlyList<MissingTypeDiff> LuceneTypesNotInLuceneNet { get; set; }

    public required IReadOnlyList<MismatchedModifierDiff> MismatchedModifiers { get; set; }

    public string LuceneMavenUrl => $"https://mvnrepository.com/artifact/org.apache.lucene/{LuceneMavenCoordinates.ArtifactId}/{LuceneMavenCoordinates.Version}";
}
