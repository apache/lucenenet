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
using Lucene.Net.ApiCheck.Models.Diff;

namespace Lucene.Net.ApiCheck.Utilities;

public static class DiffUtility
{
    public static async Task<ApiDiffResult> GenerateDiff(ApiCheckConfig config,
        FileInfo extractorJarPath,
        DirectoryInfo outputPath)
    {
        Console.WriteLine("Generating diff...");

        var libraries = await JarToolIntegration.ExtractApi(extractorJarPath,
            new FileInfo(Path.Combine(outputPath.FullName, "lucene-api.json")),
            config.LuceneVersion,
            config.Libraries.Select(i => i.LuceneName).ToList());

        foreach (var lib in libraries)
        {
            Console.WriteLine($"Processing {lib.Library.JarName}...");
        }

        return new ApiDiffResult();
    }
}
