---
uid: Lucene.Net.Analysis.OpenNlp
summary: *content
---


<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

OpenNLP Library Integration

This module exposes functionality from Apache OpenNLP to Apache Lucene.NET. The Apache OpenNLP library is a machine learning based toolkit for the processing of natural language text.

For an introduction to Lucene's analysis API, see the [Lucene.Net.Analysis](../core/Lucene.Net.Analysis.html) namespace documentation.

The OpenNLP Tokenizer behavior is similar to the <xref:Lucene.Net.Analysis.Core.WhiteSpaceTokenizer> but is smart about inter-word punctuation. The term stream looks very much like the way you parse words and punctuation while reading. The major difference between this tokenizer and most other tokenizers shipped with Lucene is that punctuation is tokenized. This is required for the following taggers to operate properly.

The OpenNLP taggers annotate terms using the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute>.

- <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPTokenizer> segments text into sentences or words. This Tokenizer uses the OpenNLP Sentence Detector and/or Tokenizer classes. When used together, the Tokenizer receives sentences and can do a better job.
- <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPPOSFilter> tags words for Part-of-Speech and <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPChunkerFilter> tags words for Chunking. These tags are assigned as token types. Note that only one of these operations will tag
Since the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> is not stored in the index, it is recommended that one of these filters is used following OpenNLPFilter to enable search against the assigned tags:

- <xref:Lucene.Net.Analysis.Payloads.TypeAsPayloadTokenFilter> copies the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> value to the <xref:Lucene.Net.Analysis.TokenAttributes.IPayloadAttribute>
- <xref:Lucene.Net.Analysis.Miscellaneous.TypeAsSynonymFilter> creates a cloned token at the same position as each tagged token, and copies the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> value to the <xref:Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>, optionally with a customized prefix (so that tags effectively occupy a different namespace from token text).

Named Entity Recognition is also supported by OpenNLP, but there is no OpenNLPNERFilter included. For an implementation, see the [lucenenet-opennlp-mavenreference-demo](https://github.com/NightOwl888/lucenenet-opennlp-mavenreference-demo).

## MavenReference Primer

When a `<PackageReference>` is included for this NuGet package in your SDK-style MSBuild project, it will automatically include transient dependencies to [`opennlp-tools` on maven.org](https://search.maven.org/artifact/org.apache.opennlp/opennlp-tools/1.9.4/bundle). The transient dependency will automatically include a `<MavenReference>` in your MSBuild project.

The `<MavenReference>` item group operates similar to a dependency in Maven. All transitive dependencies are collected and resolved, and then the final output is produced. However, unlike `PackageReference`s, `MavenReference`s are collected by the final output project, and reassessed. That is, each dependent Project within your .NET SDK-style solution contributes its `MavenReference`s to project(s) which include it, and each project makes its own dependency graph. Projects do not contribute their final built assemblies up. They only contribute their dependencies. Allowing each project in a complicated solution to make its own local conflict resolution attempt.

> **NOTE** `<MavenReference>` is only supported on SDK-style MSBuild projects.

## MavenReference Example

This means this package can be combined with other related packages on Maven in your project and they can be accessed using the same path as in Java like a namespace in .NET. For example, you can add a `<MavenReference>` to your project to include a reference to [`opennlp-uima`](https://search.maven.org/artifact/org.apache.opennlp/opennlp-uima/1.9.1/jar). The UIMA (Unstructured Information Management Architecture) integration module is designed to work with the Apache UIMA framework. UIMA is a framework for building applications that analyze unstructured information, and it's often used for processing natural language text. The opennlp-uima module allows you to integrate OpenNLP functionality into UIMA pipelines, leveraging the capabilities of both frameworks.

Here's a basic outline of how you might extend an existing Lucene.NET analyzer to incorporate OpenNLP-UIMA annotators:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lucene.Net.Analysis.OpenNLP" Version="4.8.0-beta00017" />
  </ItemGroup>

  <ItemGroup>
    <MavenReference Include="org.apache.opennlp:opennlp-uima" Version="1.9.1" />
  </ItemGroup>
</Project>
```

```c#
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using org.apache.uima.analysis_engine;
using System.IO;

public class CustomOpenNLPAnalyzer : OpenNLPTokenizerFactory
{
    // ... constructor and other methods ...

    public override Tokenizer Create(AttributeFactory factory, TextReader reader)
    {
        Tokenizer tokenizer = base.Create(factory, reader);

        // Wrap the tokenizer with UIMA annotators
        AnalysisEngineDescription uimaSentenceAnnotator = CreateUIMASentenceAnnotator();
        AnalysisEngineDescription uimaTokenAnnotator = CreateUIMATokenAnnotator();

        // Combine OpenNLP-UIMA annotators with the existing tokenizer
        AnalysisEngine tokenizerAndUIMAAnnotators = CreateAggregate(uimaSentenceAnnotator, uimaTokenAnnotator);

        return new UIMATokenizer(tokenizer, tokenizerAndUIMAAnnotators);
    }

    // ... other methods ...

    private AnalysisEngineDescription CreateUIMASentenceAnnotator() {
        // Create and configure UIMA sentence annotator
        // ...

        return /* UIMA sentence annotator description */;
    }

    private AnalysisEngineDescription CreateUIMATokenAnnotator() {
        // Create and configure UIMA token annotator
        // ...

        return /* UIMA token annotator description */;
    }
}
```

In the above example, `CustomOpenNLPAnalyzer` extends `OpenNLPTokenizerFactory` (assuming that's the analyzer you're using), and it wraps the OpenNLP tokenizer with UIMA annotators. You'll need to replace the placeholder methods (`CreateUIMASentenceAnnotator` and `CreateUIMATokenAnnotator`) with the actual code to create and configure your UIMA annotators. Please note that configuring NLP can be complex. See the [OpenNLP 1.9.4 Manual](https://opennlp.apache.org/docs/1.9.4/manual/opennlp.html) and [OpenNLP UIMA 1.9.4 API Documention](https://opennlp.apache.org/docs/1.9.4/apidocs/opennlp-uima/index.html) for details.

> [!NOTE]
> IKVM (and `<MavenReference>`) does not support Java SE higher than version 8. So it will not be possible to add a `<MavenReference>` to OpenNLP 2.x until support is added for it in IKVM.

For a more complete example, see the [lucenenet-opennlp-mavenreference-demo](https://github.com/NightOwl888/lucenenet-opennlp-mavenreference-demo).
