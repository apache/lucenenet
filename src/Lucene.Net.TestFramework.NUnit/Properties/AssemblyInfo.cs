/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Support;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDefaultAlias("Lucene.Net.TestFramework.NUnit")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(true)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5f36e3cf-82ac-4d97-af1a-6dabe60e9180")]

// We need InternalsVisibleTo in order to prevent making everything public just for the sake of testing.
// This has broad implications, though because many methods are marked "protected internal", which means other assemblies
// must update overridden methods to match.
// Note that some of the APIs we use to test Lucene.Net are internal because they are only meant to
// make porting tests from Java easier. This means that every Lucene.Net test project requires
// InternalsVisibleTo, now and in the future to keep these APIs from public view.
[assembly: InternalsVisibleTo("Lucene.Net.Tests._A-D, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._E-I, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._J-S, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._T-U, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._U-Z, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Common, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Kuromoji, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Morfologik, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.OpenNLP, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Phonetic, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.SmartCn, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Analysis.Stempel, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Benchmark, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Classification, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Cli, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Codecs, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Demo, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Expressions, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Facet, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Grouping, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Highlighter, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Join, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.ICU, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Memory, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Misc, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Queries, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.QueryParser, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Replicator, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Sandbox, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Spatial, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Suggest, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.TestFramework, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.TestFramework.NUnit, PublicKey=" + AssemblyKeys.PublicKey)]
