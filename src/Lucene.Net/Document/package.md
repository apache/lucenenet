---
uid: Lucene.Net.Documents
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

The logical representation of a <xref:Lucene.Net.Documents.Document> for indexing and searching.

The document package provides the user level logical representation of content to be indexed and searched. The package also provides utilities for working with <xref:Lucene.Net.Documents.Document>s and <xref:Lucene.Net.Index.IIndexableField>s.

## Document and IndexableField

A <xref:Lucene.Net.Documents.Document> is a collection of <xref:Lucene.Net.Index.IIndexableField>s. A <xref:Lucene.Net.Index.IIndexableField> is a logical representation of a user's content that needs to be indexed or stored. <xref:Lucene.Net.Index.IIndexableField>s have a number of properties that tell Lucene.NET how to treat the content (like indexed, tokenized, stored, etc.) See the <xref:Lucene.Net.Documents.Field> implementation of <xref:Lucene.Net.Index.IIndexableField> for specifics on these properties. 

Note: it is common to refer to <xref:Lucene.Net.Documents.Document>s having <xref:Lucene.Net.Documents.Field>s, even though technically they have <xref:Lucene.Net.Index.IIndexableField>s.

## Working with Documents

First and foremost, a <xref:Lucene.Net.Documents.Document> is something created by the user application. It is your job to create Documents based on the content of the files you are working with in your application (Word, txt, PDF, Excel or any other format.) How this is done is completely up to you. That being said, there are many tools available in other projects that can make the process of taking a file and converting it into a Lucene <xref:Lucene.Net.Documents.Document>. 

The <xref:Lucene.Net.Documents.DateTools> is a utility class to make dates and times searchable (remember, Lucene only searches text). <xref:Lucene.Net.Documents.Int32Field>, <xref:Lucene.Net.Documents.Int64Field>, <xref:Lucene.Net.Documents.SingleField> and <xref:Lucene.Net.Documents.DoubleField> are a special helper class to simplify indexing of numeric values (and also dates) for fast range range queries with <xref:Lucene.Net.Search.NumericRangeQuery> (using a special sortable string representation of numeric values).