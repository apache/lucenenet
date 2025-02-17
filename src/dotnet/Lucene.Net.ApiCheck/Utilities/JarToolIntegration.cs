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
using Lucene.Net.ApiCheck.Models.JavaApi;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.ApiCheck.Utilities;

public static class JarToolIntegration
{
    public static async Task<IReadOnlyList<LibraryResult>> ExtractApi(FileInfo jarToolPath,
        FileInfo outputPath,
        string luceneVersion,
        IReadOnlyList<LibraryConfig> luceneLibraries)
    {
        var arguments = new List<string>
        {
            "-jar",
            jarToolPath.FullName,
            "extract",
            "-lv", // lucene version
            luceneVersion,
            "-libs",
            string.Join(",", luceneLibraries.Select(i => i.LuceneName)),
            "-o",
            outputPath.FullName,
        };

        var mavenDependencies = luceneLibraries
            .SelectMany(i => i.MavenDependencies ?? Array.Empty<string>())
            .Distinct()
            .ToList();

        if (mavenDependencies.Count > 0)
        {
            arguments.Add("-dep");

            foreach (string mavenDependency in mavenDependencies)
            {
                arguments.Add(mavenDependency);
            }
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = string.Join(" ", arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
        };

        process.Start();

        string? line;
        StringBuilder output = new();
        while ((line = await process.StandardOutput.ReadLineAsync()) != null)
        {
            output.AppendLine(line);
        }

        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"lucene-api-extractor failed with exit code {process.ExitCode}.{Environment.NewLine}Output: {output}{Environment.NewLine}Error: {error}");
        }

        var apiJson = await File.ReadAllTextAsync(outputPath.FullName);

        var libraries = Json.Deserialize<IReadOnlyList<LibraryResult>>(apiJson)
            ?? throw new InvalidOperationException("Failed to deserialize output from lucene-api-extractor");

        return libraries;
    }
}
