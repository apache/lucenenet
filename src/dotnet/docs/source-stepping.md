---
title: How to step into source in the Visual Studio debugger
uid: source-stepping
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

Debuggers can step into the source code, set breakpoints, watch variables, etc. It's easy to drop into Lucene.NET code any time you want to understand what's going on.

If you're getting ready to report a bug in Lucene.NET, figuring out how to create a minimal repro is much easier since you aren't dealing with a black box!

> [!NOTE]
> This feature is enabled using [Source Link](https://github.com/dotnet/sourcelink#readme), which also has support for source stepping in [Visual Studio Code](https://devblogs.microsoft.com/dotnet/improving-debug-time-productivity-with-source-link/#visual-studio-code).

As Source Link downloads files from the internet, Visual Studio has it disabled by default. Enabling it requires changing a few of the Visual Studio settings:

1. Go to **Tools > Options > Debugging > Symbols** and ensure that the `NuGet.org Symbol Server` option is checked. It may also be a good idea to specify a cache directory once you have Source Link set up so Visual Studio won't need to repeatedly download the same source files each time you step into them.

   ![Enabling Source Symbols](https://lucenenet.apache.org/images/contributing/source-link-setup/debugging-with-source-link01.png)

   > [!NOTE]
   > If you are on .NET Framework, you'll also need to check the `Microsoft Symbol Servers` option.

2. Disable `Just My Code` in **Tools > Options > Debugging > General** to allow Visual Studio to debug code outside of your solution. Also, verify that `Enable Source Link support` is enabled.

   ![Enabling Source Link](https://lucenenet.apache.org/images/contributing/source-link-setup/debugging-with-source-link02.png)

   > [!NOTE]
   > If you are on .NET Framework, you'll also need to check `Enable .NET Framework source stepping`.

## Verifying Source Link

1. To confirm Source Link is working, set a breakpoint before or on a line of code that calls a Lucene.NET type and start debugging the application.

   ![Breakpoint at BytesRef](https://lucenenet.apache.org/images/contributing/source-link-setup/debugging-with-source-link03.png)

2. Step into the code, just as you would for any local method (F11 is the default keyboard shortcut). If all is configured correctly, you will be prompted to download the source code file for the type you are stepping into. Click on either `Download Source and Continue Debugging` option to continue.

   ![Download Source and Continue Debugging](https://lucenenet.apache.org/images/contributing/source-link-setup/debugging-with-source-link04.png)

3. After a short pause, The debugger will step into the next line after your breakpoint inside the Lucene.NET source code.

   ![Step into BytesRef](https://lucenenet.apache.org/images/contributing/source-link-setup/debugging-with-source-link05.png)

Congratulations! You can now step into Lucene.NET code to figure stuff out and to help put together a thorough bug report or PR.




