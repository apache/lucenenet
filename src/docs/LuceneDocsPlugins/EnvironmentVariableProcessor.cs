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
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace LuceneDocsPlugins;

public class EnvironmentVariableProcessor : IPostProcessor
{
    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        foreach (var manifestItem in manifest.Files.Where(x => x.Type == "Conceptual"))
        {
            foreach (var manifestItemOutputFile in manifestItem.Output)
            {
                var outputPath = Path.Combine(outputFolder, manifestItemOutputFile.Value.RelativePath);

                var content = File.ReadAllText(outputPath);

                Logger.LogInfo($"Replacing environment variables in {outputPath}");

                var newContent = EnvironmentVariableUtil.ReplaceEnvironmentVariables(content);

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
}
