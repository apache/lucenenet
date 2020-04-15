---
uid: Lucene.Net.QueryParsers.Flexible.Precedence.Processors
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


Processors used by Precedence Query Parser

## Lucene Precedence Query Parser Processors

 This package contains the 2 <xref:Lucene.Net.QueryParsers.Flexible.Core.Processors.QueryNodeProcessor>s used by <xref:Lucene.Net.QueryParsers.Flexible.Precedence.PrecedenceQueryParser>. 

 <xref:Lucene.Net.QueryParsers.Flexible.Precedence.Processors.BooleanModifiersQueryNodeProcessor>: this processor is used to apply <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.ModifierQueryNode>s on <xref:Lucene.Net.QueryParsers.Flexible.Core.Nodes.BooleanQueryNode> children according to the boolean type or the default operator. 

 <xref:Lucene.Net.QueryParsers.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline>: this processor pipeline is used by <xref:Lucene.Net.QueryParsers.Flexible.Precedence.PrecedenceQueryParser>. It extends <xref:Lucene.Net.QueryParsers.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline> and rearrange the pipeline so the boolean precedence is processed correctly. Check <xref:Lucene.Net.QueryParsers.Flexible.Precedence.Processors.PrecedenceQueryNodeProcessorPipeline> for more details. 