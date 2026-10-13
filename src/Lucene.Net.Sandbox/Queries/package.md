---
uid: Lucene.Net.Sandbox.Queries
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

Additional queries (some may have caveats or limitations)

## Query types not included from Lucene 4.8.1

The Java Sandbox `RegexQuery` family is not included in this package because it
was superseded by `RegexpQuery` in the main Lucene search package. Use
`Lucene.Net.Search.RegexpQuery` for regular-expression queries instead.

The deprecated `SlowCollated` query types are also not included. For indexed
collation keys, use `CollationKeyAnalyzer` or `ICUCollationKeyAnalyzer` from
the analysis packages. These replacements are supported alternatives to the
old Sandbox types and avoid carrying forward APIs that were removed upstream.
