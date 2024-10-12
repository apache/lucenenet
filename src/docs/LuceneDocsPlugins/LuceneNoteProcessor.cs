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

using Docfx.Common;
using Docfx.Plugins;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LuceneDocsPlugins;

public partial class LuceneNoteProcessor : IPostProcessor
{
    private const string ExperimentalMessage =
        "This API is experimental and might change in incompatible ways in the next release.";

    private const string InternalMessage =
        "This API is for internal purposes only and might change in incompatible ways in the next release.";

    [GeneratedRegex("@lucene\\.(?<notetype>(experimental|internal))")]
    private static partial Regex LuceneNoteRegex();

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        foreach (var manifestItem in manifest.Files)
        {
            foreach (var manifestItemOutputFile in manifestItem.Output)
            {
                var outputPath = Path.Combine(outputFolder, manifestItemOutputFile.Value.RelativePath);

                var content = File.ReadAllText(outputPath);

                Logger.LogInfo($"Replacing @lucene notes in {outputPath}");

                var newContent = ReplaceLuceneNotes(content);

                if (content == newContent)
                {
                    continue;
                }

                Logger.LogInfo($"Writing new content to {outputPath}");

                File.WriteAllText(outputPath, newContent);
            }
        }

        return manifest;
    }

    private static string ReplaceLuceneNotes(string sourceText)
    {
        var matches = LuceneNoteRegex().Matches(sourceText);

        if (matches.Count > 0)
        {
            var sb = new StringBuilder(sourceText.Length);

            int lastMatchEnd = 0;

            foreach (Match match in matches)
            {
                var noteType = match.Groups["notetype"].Value;

                // Append the prefix that didn't match the regex (or text since last match)
                sb.Append(sourceText.AsSpan(lastMatchEnd, match.Index - lastMatchEnd));

                // Do the replacement of the regex match
                string noteValue = GetLuceneNoteValue(noteType);
                sb.Append(noteValue);

                Logger.LogInfo($"Replaced @lucene note '{noteType}'");
                lastMatchEnd = match.Index + match.Length;
            }

            // Append the suffix that didn't match the regex
            sb.Append(sourceText.AsSpan(lastMatchEnd));

            return sb.ToString();
        }

        return sourceText;
    }

    private static string GetLuceneNoteValue(string noteType)
    {
        string message = noteType == "internal" ? InternalMessage : ExperimentalMessage;

        var builder = new StringBuilder();
        builder.AppendLine("<div class=\"NOTE\">");
        builder.AppendLine("<h5>Note</h5>");
        builder.Append("<p>").Append(message).AppendLine("</p>");
        builder.AppendLine("</div>");
        return builder.ToString();
    }
}
