---
uid: Lucene.Net.Misc
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

## Misc Tools


The misc package has various tools for splitting/merging indices,
changing norms, finding high freq terms, and others.


<!--

LUCENENET specific - we didn't port the NativeUnixDirectory, and it is not clear whether there is any advantage to doing so in .NET.
See: https://github.com/apache/lucenenet/issues/276

## NativeUnixDirectory

__NOTE__: This uses C++ sources (accessible via JNI), which you'll
have to compile on your platform.

<xref:Lucene.Net.Store.NativeUnixDirectory> is a Directory implementation that bypasses the
OS's buffer cache (using direct IO) for any IndexInput and IndexOutput
used during merging of segments larger than a specified size (default
10 MB).  This avoids evicting hot pages that are still in-use for
searching, keeping search more responsive while large merges run.

See [this blog post](http://blog.mikemccandless.com/2010/06/lucene-and-fadvisemadvise.html)
for details.

Steps to build:

*   <tt>cd lucene/misc/</tt>

*   To compile NativePosixUtil.cpp -> libNativePosixUtil.so, run<tt> ant build-native-unix</tt>.

*   <tt>libNativePosixUtil.so</tt> will be located in the <tt>lucene/build/native/</tt> folder

*   Make sure libNativePosixUtil.so is on your LD_LIBRARY_PATH so java can find it (something like <tt>export LD_LIBRARY_PATH=/path/to/dir:$LD_LIBRARY_PATH</tt>, where /path/to/dir contains libNativePosixUtil.so)

*   <tt>ant jar</tt> to compile the java source and put that JAR on your CLASSPATH

NativePosixUtil.cpp/java also expose access to the posix_madvise,
madvise, posix_fadvise functions, which are somewhat more cross
platform than O_DIRECT, however, in testing (see above link), these
APIs did not seem to help prevent buffer cache eviction.
-->