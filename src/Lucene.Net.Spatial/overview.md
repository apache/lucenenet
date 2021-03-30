---
uid: Lucene.Net.Spatial
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

# The Spatial Module for Apache Lucene.NET

The spatial module is new to Lucene.NET 4, replacing the old "Lucene.Net.Contrib" module that came before it. The principle interface to the module is a <xref:Lucene.Net.Spatial.SpatialStrategy> which encapsulates an approach to indexing and searching based on shapes. Different Strategies have different features and performance profiles, which are documented at each Strategy implementation class level. 

For some sample code showing how to use the API, see SpatialExample.cs in the tests. 

The spatial module uses [Spatial4n](https://github.com/NightOwl888/Spatial4n), a .NET port of the ASL licensed [Spatial4j](https://github.com/spatial4j/spatial4j) heavily. Spatial4n is a library with these capabilities:

* Provides shape implementations, namely point, rectangle, and circle. Both geospatial contexts and plain 2D Euclidean/Cartesian contexts are supported. With an additional dependency, it adds polygon and other geometry shape support via integration with [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) (often referred to as NTS). This includes dateline wrap support.
* Shape parsing and serialization, including [Well-Known Text (WKT)](http://en.wikipedia.org/wiki/Well-known_text) (via NTS).
* Distance and other spatial related math calculations. 

> [!NOTE]
> Historical Fact: The new spatial module was once known as Lucene Spatial Playground (LSP) as an external project. In ~March 2012, LSP split into this new module as part of Lucene and Spatial4j externally. A large chunk of the LSP implementation originated as SOLR-2155 which uses trie/prefix-tree algorithms with a geohash encoding. That approach is implemented in <xref:Lucene.Net.Spatial.Prefix.RecursivePrefixTreeStrategy> today. 