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

namespace Lucene.Net.ApiCheck.Models.Diff;

public class ApiDiffResult
{
    public required IList<AssemblyDiff> Assemblies { get; set; }

    public int TotalLuceneTypesNotInLuceneNet => Assemblies.Sum(a => a.LuceneTypesNotInLuceneNet.Count);

    public int TotalLuceneNetTypesNotInLucene => Assemblies.Sum(a => a.LuceneNetTypesNotInLucene.Count);

    public int TotalMismatchedModifiers => Assemblies.Sum(a => a.TypesWithMismatchedModifiers.Count());

    public int TotalMismatchedBaseTypes => Assemblies.Sum(a => a.TypesWithMismatchedBaseTypes.Count());

    public int TotalMismatchedInterfaces => Assemblies.Sum(a => a.TypesWithMismatchedInterfaces.Count());

    public int TotalLuceneMembersNotInLuceneNet => Assemblies.Sum(a => a.TypesWithMembersNotInLuceneNet.Sum(b => b.LuceneMembersNotInLuceneNet.Count));

    public int TotalLuceneNetMembersNotInLucene => Assemblies.Sum(a => a.TypesWithMembersNotInLucene.Sum(b => b.LuceneNetMembersNotInLucene.Count));
}
