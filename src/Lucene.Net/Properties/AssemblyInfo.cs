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

using Lucene.Net.Support;
using System;
using System.Diagnostics;
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
[assembly: InternalsVisibleTo("Lucene.Net.Tests._A-I, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._J-U, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests._U-Z, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Misc, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.ICU, PublicKey=" + AssemblyKeys.PublicKey)] // For Analysis.Util.TestSegmentingTokenizerBase
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Misc, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.QueryParser, PublicKey=" + AssemblyKeys.PublicKey)]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Cli, PublicKey=" + AssemblyKeys.PublicKey)] // For lucene-cli


