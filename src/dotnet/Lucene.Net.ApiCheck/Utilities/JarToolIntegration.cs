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

using Lucene.Net.ApiCheck.Models;
using Lucene.Net.ApiCheck.Models.JavaApi;
using Lucene.Net.Util;
using System.Diagnostics;

namespace Lucene.Net.ApiCheck.Utilities;

public static class JarToolIntegration
{
    public static async Task<IReadOnlyList<LibraryResult>> ExtractApi(FileInfo jarToolPath,
        DirectoryInfo extractorDownloadPath,
        FileInfo outputPath,
        IReadOnlyList<MavenCoordinates> luceneLibraries,
        IReadOnlyList<MavenCoordinates> mavenDependencies)
    {
        var arguments = new List<string>
        {
            "-Danalysis.data.dir=./analysis", // to bypass smartcn static init error
            "-jar",
            jarToolPath.FullName,
            "extract",
            "-o",
            outputPath.FullName,
            "-dd",
            extractorDownloadPath.FullName,
            "-lib",
        };

        arguments.AddRange(luceneLibraries.Select(i => i.ToString()));

        if (mavenDependencies.Count > 0)
        {
            arguments.Add("-dep");
            arguments.AddRange(mavenDependencies.Select(mavenDependency => mavenDependency.ToString()));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "java",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.AddRange(arguments);

        using var process = new Process
        {
            StartInfo = startInfo,
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await stdoutTask;
        var error = await stderrTask;

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
