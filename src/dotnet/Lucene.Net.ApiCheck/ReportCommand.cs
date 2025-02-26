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

using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using System.Reflection;

namespace Lucene.Net.ApiCheck;

public static class ReportCommand
{
    private static readonly Assembly _assembly = typeof(ReportCommand).Assembly;

    public static async Task GenerateReport(GlobalOptions globalOptions, DirectoryInfo outputPath)
    {
        var diff = await DiffCommand.GenerateDiff(globalOptions, outputPath);

        string[] filesToCopy =
        [
            "index.css",
            "index.js",
        ];

        foreach (var file in filesToCopy)
        {
            Stream resourceStream = GetEmbeddedResourceStream(file);

            var filePath = Path.Combine(outputPath.FullName, file);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await resourceStream.CopyToAsync(fileStream);
        }

        var indexHtmlPath = Path.Combine(outputPath.FullName, "index.html");

        if (File.Exists(indexHtmlPath))
        {
            File.Delete(indexHtmlPath);
        }

        var handlebarsStream = GetEmbeddedResourceStream("index.handlebars");
        using var indexHtmlReader = new StreamReader(handlebarsStream);

        var handlebarsContext = Handlebars.Create();
        HandlebarsHelpers.Register(handlebarsContext);
        var template = handlebarsContext.Compile(await indexHtmlReader.ReadToEndAsync());
        var htmlOutput = template(diff);

        await File.WriteAllTextAsync(indexHtmlPath, htmlOutput);

        Console.WriteLine($"Report generated at {indexHtmlPath}");
    }

    private static Stream GetEmbeddedResourceStream(string file)
    {
        string resourceName = $"Lucene.Net.ApiCheck.Report.{file}";
        return _assembly.GetManifestResourceStream(resourceName)
               ?? throw new InvalidOperationException($"Could not find embedded resource '{resourceName}'");
    }
}
