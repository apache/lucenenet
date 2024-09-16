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

using Docfx.Plugins;
using System;
using System.Collections.Immutable;
using System.Composition;

namespace LuceneDocsPlugins;

[Export(nameof(AggregatePostProcessor), typeof(IPostProcessor))]
public class AggregatePostProcessor : IPostProcessor
{
    private readonly IPostProcessor[] _postProcessors =
    [
        new LuceneNoteProcessor(),
        new EnvironmentVariableProcessor(),
    ];

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        foreach (var postProcessor in _postProcessors)
        {
            metadata = postProcessor.PrepareMetadata(metadata);
        }

        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        foreach (var postProcessor in _postProcessors)
        {
            manifest = postProcessor.Process(manifest, outputFolder);
        }

        return manifest;
    }
}
