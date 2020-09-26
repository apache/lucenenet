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

using NUnit.Framework;
using System.Reflection;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyDefaultAlias("Lucene.Net.Tests")]
[assembly: AssemblyCulture("")]



// LUCENENET specific - only allow tests in this assembly to run one at a time
// to prevent polluting shared state.
[assembly: LevelOfParallelism(1)]

// LUCENENET specific - time out test projects at 55 minutes to allow the results
// to be uploaded before the 60 minute Azure DevOps job cutoff for easier troubleshooting
[assembly: Timeout(3300000)]
