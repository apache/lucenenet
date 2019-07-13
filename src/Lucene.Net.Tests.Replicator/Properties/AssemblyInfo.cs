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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("418e9d8e-2369-4b52-8d2f-5a987213999b")]

#if !NETSTANDARD
// LUCENENET TODO: 2019-07-14 - Had a hanging test in Lucene.Net.Tests.Replicator 
// on .NET Framwork 4.5 (not sure which one),
// but unable to repeat. Adding this timeout (with at least 10x the time it usually takes
// to run all of these tests) to ensure if we get a hang again, the hanging test will fail
// so we know which test to investigate.
[assembly: NUnit.Framework.Timeout(120000)]

//All we know for certain is that it wasn't any of these tests

//√ TestBasic [1s 220ms]
//√ TestNoUpdateThread [313ms]
//√ TestRecreateTaxonomy [233ms]
//√ TestRestart [380ms]
//√ TestUpdateThread [314ms]
//√ TestNoCommit [26ms]
//√ TestOpen [31ms]
//√ TestRevisionRelease [5ms]
//√ TestSegmentsFileLast [3ms]
#endif
