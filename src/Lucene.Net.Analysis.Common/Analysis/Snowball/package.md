---
uid: Lucene.Net.Analysis.Snowball
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

<xref:Lucene.Net.Analysis.TokenFilter> and <xref:Lucene.Net.Analysis.Analyzer> implementations that use Snowball
stemmers.

 This project provides pre-compiled version of the Snowball stemmers based on revision 500 of the Tartarus Snowball repository, together with classes integrating them with the Lucene search engine. 

 A few changes has been made to the static Snowball code and compiled stemmers: 

*   Class SnowballProgram is made abstract and contains new abstract method stem() to avoid reflection in Lucene filter class SnowballFilter.

*   All use of StringBuffers has been refactored to StringBuilder for speed.

*   Snowball BSD license header has been added to the Java classes to avoid having RAT adding ASL headers.

 See the Snowball [home page](http://snowball.tartarus.org/) for more information about the algorithms. 

 __IMPORTANT NOTICE ON BACKWARDS COMPATIBILITY!__ 

 An index created using the Snowball module in Lucene 2.3.2 and below might not be compatible with the Snowball module in Lucene 2.4 or greater. 

 For more information about this issue see: https://issues.apache.org/jira/browse/LUCENE-1142 