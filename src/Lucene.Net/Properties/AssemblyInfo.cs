/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDefaultAlias("Lucene.Net")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(true)]

// We need InternalsVisibleTo in order to prevent making everything public just for the sake of testing.
// This has broad implications because many methods are marked "protected internal", which means other assemblies
// must update overridden methods to match.
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Common, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Kuromoji, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Morfologik, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Nori, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.OpenNLP, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Phonetic, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.SmartCn, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Analysis.Stempel, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Benchmark, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Classification, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Codecs, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Demo, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Expressions, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Facet, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Grouping, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Highlighter, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.ICU, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Join, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Memory, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Misc, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Queries, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.QueryParser, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Replicator, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Sandbox, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Spatial, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Suggest, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._A-D, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._E-I, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._J-S, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._T-U, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._U-Z, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework.MSTest, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework.NUnit, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework.xUnit, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Common, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Kuromoji, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Phonetic, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Benchmark, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Expressions, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Facet, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Grouping, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Highlighter, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.ICU, PublicKey=" + AssemblyKeys.PublicKey)] // For Analysis.Util.TestSegmentingTokenizerBase
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Misc, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.QueryParser, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Cli, PublicKey=" + AssemblyKeys.PublicKey)] // For lucene-cli
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Replicator, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Spatial, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Suggest, PublicKey=" + AssemblyKeys.PublicKey)]


