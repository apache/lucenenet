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

    public string LuceneMavenUrl => LuceneMavenCoordinates.MvnRepositoryUrl;

    public required MavenCoordinates LuceneMavenCoordinates { get; set; }

    public required string LuceneNetName { get; set; }

    public required string LuceneNetVersion { get; set; }

    public required LibraryConfig LibraryConfig { get; set; }

    public required IReadOnlyList<TypeDeclaration> LuceneNetTypesNotInLucene { get; set; }

    public required IReadOnlyList<TypeDeclaration> LuceneTypesNotInLuceneNet { get; set; }

    public required IReadOnlyList<TypeDiff> MatchingTypes { get; set; }

    public IEnumerable<TypeDiff> TypesWithMismatchedModifiers => MatchingTypes.Where(t => t.MismatchedModifiers != null);

    public int TypesWithMismatchedModifiersCount => TypesWithMismatchedModifiers.Count();

    public IEnumerable<TypeDiff> TypesWithMismatchedBaseTypes => MatchingTypes.Where(t => t.MismatchedBaseType != null);

    public int TypesWithMismatchedBaseTypesCount => TypesWithMismatchedBaseTypes.Count();

    public IEnumerable<TypeDiff> TypesWithMismatchedInterfaces => MatchingTypes.Where(t => t.MismatchedInterfaces != null);

    public int TypesWithMismatchedInterfacesCount => TypesWithMismatchedInterfaces.Count();

    public IEnumerable<TypeDiff> TypesWithMembersNotInLuceneNet => MatchingTypes.Where(t => t.LuceneMembersNotInLuceneNet.Count > 0);

    public int TypesWithMembersNotInLuceneNetCount => TypesWithMembersNotInLuceneNet.Count();

    public IEnumerable<TypeDiff> TypesWithMembersNotInLucene => MatchingTypes.Where(t => t.LuceneNetMembersNotInLucene.Count > 0);

    public int TypesWithMembersNotInLuceneCount => TypesWithMembersNotInLucene.Count();
}
