<!--
 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache License, Version 2.0 (the
 "License"); you may not use this file except in compliance
 with the License.  You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing,
 software distributed under the License is distributed on an
 "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the License for the
 specific language governing permissions and limitations
 under the License.
-->

# Index compatibility harness (Lucene 4.8.1 <-> Lucene.NET)

This self-contained Maven project proves **two-way** index/codec compatibility
between Apache Lucene 4.8.1 (Java) and Lucene.NET.

Each runtime can write an index that the **other** runtime opens, validates with
`CheckIndex` (the codec integrity gate, which verifies the per-file checksums the
codec wrote), and then reads back to confirm the contents match. We do **not**
do a byte-for-byte file comparison: some header fields legitimately differ
between runtimes (e.g. `java.vendor`), so `CheckIndex` + semantic read back is
the common approach.

The shared, deterministic document set is defined once, in two mirror
implementations that must stay in sync:

- Java: [`CompatDocs.java`](src/main/java/org/apache/lucenenet/compat/CompatDocs.java)
- .NET: `TestJavaCompatibility.cs` in `src/Lucene.Net.Tests/Index`

## Requirements

- A JDK (8 or newer). Lucene 4.8.1 targets Java 7; we compile to release 8.
- No system Maven needed: use the bundled wrapper `./mvnw` (or `mvnw.cmd` on Windows).
- The .NET SDK, to build/run the Lucene.NET side.

## No committed fixtures

Indexes are **never** committed to the repo. Every index is generated fresh into
the gitignored `work/` folder on demand:

- `work/java/`   - indexes written by Java (read by .NET)
- `work/dotnet/` - indexes written by .NET (read by Java)

## Running

The easiest path is the driver script from this directory, which runs **both**
directions end to end:

```sh
./run-compat.sh          # macOS / Linux
.\run-compat.ps1         # Windows (PowerShell)
```

### Direction 1: Java writes, .NET reads

```sh
./mvnw -q compile exec:java        # writes work/java/index.481.{cfs,nocfs}
```

Then run the .NET `TestJavaCompatibility` tests (see the driver script) pointed at
`work/java`. In .NET, a **missing** Java index makes the cross-runtime test
**inconclusive** (skipped), because the JDK may not be present in every
environment.

### Direction 2: .NET writes, Java reads

First have .NET write its index into `work/dotnet` (the driver does this), then:

```sh
./mvnw -q test -Dlucenenet.index.dir=work/dotnet/index.481.nocfs
```

In Java, a **missing** .NET index makes the test **fail** (not skip): when Java is
asked to read a .NET index, the absence of that index means the pipeline that was
supposed to produce it is broken.
