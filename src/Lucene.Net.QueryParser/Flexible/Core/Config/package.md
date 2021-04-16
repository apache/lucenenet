---
uid: Lucene.Net.QueryParsers.Flexible.Core.Config
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


Base classes used to configure the query processing.

## Query Configuration Interfaces

The namespace <tt>Lucene.Net.QueryParsers.Flexible.Core.Config</tt> contains query configuration handler abstract class that all config handlers should extend. 

See <xref:Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler> for a reference implementation. 

The <xref:Lucene.Net.QueryParsers.Flexible.Core.Config.QueryConfigHandler> and <xref:Lucene.Net.QueryParsers.Flexible.Core.Config.FieldConfig> are used in the processors to access config information in a flexible and independent way. See <xref:Lucene.Net.QueryParsers.Flexible.Standard.Processors.TermRangeQueryNodeProcessor> for a reference implementation. 
