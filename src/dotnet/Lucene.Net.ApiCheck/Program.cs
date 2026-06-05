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

using Lucene.Net.ApiCheck;
using System.CommandLine;

var extractorJarPath = new Option<FileInfo>("-j", "--jar")
{
    Description = "Path to the lucene-api-extractor JAR file",
    Recursive = true,
};

var extractorDownloadPath = new Option<DirectoryInfo>("-d", "--download")
{
    Description = "Path to store the files downloaded by the extractor",
    Recursive = true,
};

var outputPath = new Option<DirectoryInfo>("-o", "--output")
{
    Description = "Path to save the output files to",
};

var configFilePath = new Option<FileInfo>("-c", "--config")
{
    Description = "Path to the configuration file",
    Recursive = true,
};

var diffCommand = new Command("diff", "Generates a raw diff JSON data file of the API differences between Lucene and Lucene.NET")
{
    outputPath,
};

diffCommand.SetAction(async parseResult =>
{
    var o = parseResult.GetValue(outputPath)!;
    if (!Directory.Exists(o.FullName))
    {
        Directory.CreateDirectory(o.FullName);
    }

    var globalOptions = new GlobalOptions(
        parseResult.GetValue(extractorJarPath)!,
        parseResult.GetValue(extractorDownloadPath)!,
        parseResult.GetValue(configFilePath)!);
    await DiffCommand.GenerateDiff(globalOptions, o);
});

var reportCommand = new Command("report", "Generates an HTML report of the API differences between Lucene and Lucene.NET")
{
    outputPath,
};

reportCommand.SetAction(async parseResult =>
{
    var o = parseResult.GetValue(outputPath)!;
    if (!Directory.Exists(o.FullName))
    {
        Directory.CreateDirectory(o.FullName);
    }

    var globalOptions = new GlobalOptions(
        parseResult.GetValue(extractorJarPath)!,
        parseResult.GetValue(extractorDownloadPath)!,
        parseResult.GetValue(configFilePath)!);
    await ReportCommand.GenerateReport(globalOptions, o);
});

var rootCommand = new RootCommand
{
    diffCommand,
    reportCommand,
    extractorJarPath,
    extractorDownloadPath,
    configFilePath,
};

return await rootCommand.Parse(args).InvokeAsync();
