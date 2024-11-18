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

var extractorJarPath = new Option<FileInfo>(
    aliases: ["-j", "--jar"],
    description: "Path to the lucene-api-extractor JAR file");

var extractorDownloadPath = new Option<DirectoryInfo>(
    aliases: ["-d", "--download"],
    description: "Path to store the files downloaded by the extractor");

var outputPath = new Option<DirectoryInfo>(
    aliases: ["-o", "--output"],
    description: "Path to save the output files to");

var configFilePath = new Option<FileInfo>(
    aliases: ["-c", "--config"],
    description: "Path to the configuration file");

var diffCommand = new Command("diff", "Generates a raw diff JSON data file of the API differences between Lucene and Lucene.NET")
{
    outputPath,
};

diffCommand.SetHandler(async (j, d, c, o) =>
{
    var globalOptions = new GlobalOptions(j, d, c);
    await DiffCommand.GenerateDiff(globalOptions, o);
}, extractorJarPath, extractorDownloadPath, configFilePath, outputPath);

var rootCommand = new RootCommand
{
    diffCommand,
};

rootCommand.AddGlobalOption(extractorJarPath);
rootCommand.AddGlobalOption(extractorDownloadPath);
rootCommand.AddGlobalOption(configFilePath);

return await rootCommand.InvokeAsync(args);
