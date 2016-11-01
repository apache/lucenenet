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

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("Apache Lucene.Net")]
[assembly: AssemblyDescription("The Apache Software Foundation Lucene.Net a full-text search engine library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Apache Software Foundation")]
[assembly: AssemblyProduct("Lucene.Net")]
[assembly: AssemblyCopyright("Copyright 2006 - 2014 The Apache Software Foundation")]
[assembly: AssemblyTrademark("Copyright 2006 - 2014 The Apache Software Foundation")]
[assembly: AssemblyDefaultAlias("Lucene.Net")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(true)]

[assembly: AssemblyInformationalVersionAttribute("4.8.0")]

// for testing
//[assembly: InternalsVisibleTo("Lucene.Net.Test, PublicKey=002400000480000094000000060200000024000052534131000400000100010075a07ce602f88e" +
//                                                         "f263c7db8cb342c58ebd49ecdcc210fac874260b0213fb929ac3dcaf4f5b39744b800f99073eca" +
//                                                         "72aebfac5f7284e1d5f2c82012a804a140f06d7d043d83e830cdb606a04da2ad5374cc92c0a495" +
//                                                         "08437802fb4f8fb80a05e59f80afb99f4ccd0dfe44065743543c4b053b669509d29d332cd32a0c" +
//                                                         "b1e97e84")]

// LUCENENET NOTE: For now it is not possible to use a SNK because we have unmanaged references in Analysis.Common.
// However, we still need InternalsVisibleTo in order to prevent making everything public just for the sake of testing.
// This has broad implications, though because many methods are marked "protected internal", which means other assemblies
// must update overridden methods to match.
[assembly: InternalsVisibleTo("Lucene.Net.Tests")]
[assembly: InternalsVisibleTo("Lucene.Net.TestFramework")]
[assembly: InternalsVisibleTo("Lucene.Net.Misc")]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("4.8.0")]


//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]
//[assembly: AssemblyKeyName("")]
