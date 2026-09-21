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

The OpenNLP Tokenizer behavior is similar to the <xref:Lucene.Net.Analysis.Core.WhitespaceTokenizer> but is smart about inter-word punctuation. The term stream looks very much like the way you parse words and punctuation while reading. The major difference between this tokenizer and most other tokenizers shipped with Lucene is that punctuation is tokenized. This is required for the following taggers to operate properly.

The OpenNLP taggers annotate terms using the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute>.

- <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPTokenizer> segments text into sentences or words. This Tokenizer uses the OpenNLP Sentence Detector and/or Tokenizer classes. When used together, the Tokenizer receives sentences and can do a better job.
- <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPPOSFilter> tags words for Part-of-Speech and <xref:Lucene.Net.Analysis.OpenNlp.OpenNLPChunkerFilter> tags words for Chunking. These tags are assigned as token types. Note that only one of these operations will tag
Since the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> is not stored in the index, it is recommended that one of these filters is used following OpenNLPFilter to enable search against the assigned tags:

- <xref:Lucene.Net.Analysis.Payloads.TypeAsPayloadTokenFilter> copies the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> value to the <xref:Lucene.Net.Analysis.TokenAttributes.IPayloadAttribute>
- <xref:Lucene.Net.Analysis.Miscellaneous.TypeAsSynonymFilter> creates a cloned token at the same position as each tagged token, and copies the <xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute> value to the <xref:Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>, optionally with a customized prefix (so that tags effectively occupy a different namespace from token text).

Named Entity Recognition is also supported by OpenNLP, but there is no OpenNLPNERFilter included.

## Underlying NLP library

This module is built on [NOpenNLP](https://github.com/nopennlp/nopennlp), a C# port of Apache OpenNLP 1.9.5. NOpenNLP is referenced as an ordinary NuGet package, so using this module requires nothing beyond a `<PackageReference>`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lucene.Net.Analysis.OpenNLP" Version="[VERSION]" />
  </ItemGroup>
</Project>
```

NOpenNLP ports the `opennlp-tools` module, keeping the upstream OpenNLP type and member names with .NET casing conventions applied, so the [OpenNLP Manual](https://opennlp.apache.org/docs/1.9.4/manual/opennlp.html) and Java examples generally carry over. Its API reference is at [nopennlp.github.io/nopennlp](https://nopennlp.github.io/nopennlp/).
