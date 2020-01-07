---
uid: Lucene.Net.Codecs
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

Codecs API: API for customization of the encoding and structure of the index.

 The Codec API allows you to customise the way the following pieces of index information are stored: * Postings lists - see <xref:Lucene.Net.Codecs.PostingsFormat> * DocValues - see <xref:Lucene.Net.Codecs.DocValuesFormat> * Stored fields - see <xref:Lucene.Net.Codecs.StoredFieldsFormat> * Term vectors - see <xref:Lucene.Net.Codecs.TermVectorsFormat> * FieldInfos - see <xref:Lucene.Net.Codecs.FieldInfosFormat> * SegmentInfo - see <xref:Lucene.Net.Codecs.SegmentInfoFormat> * Norms - see <xref:Lucene.Net.Codecs.NormsFormat> * Live documents - see <xref:Lucene.Net.Codecs.LiveDocsFormat> 

  For some concrete implementations beyond Lucene's official index format, see
  the [Codecs module]({@docRoot}/../codecs/overview-summary.html).

 Codecs are identified by name through the Java Service Provider Interface. To create your own codec, extend <xref:Lucene.Net.Codecs.Codec> and pass the new codec's name to the super() constructor: public class MyCodec extends Codec { public MyCodec() { super("MyCodecName"); } ... } You will need to register the Codec class so that the {@link java.util.ServiceLoader ServiceLoader} can find it, by including a META-INF/services/org.apache.lucene.codecs.Codec file on your classpath that contains the package-qualified name of your codec. 

 If you just want to customise the <xref:Lucene.Net.Codecs.PostingsFormat>, or use different postings formats for different fields, then you can register your custom postings format in the same way (in META-INF/services/org.apache.lucene.codecs.PostingsFormat), and then extend the default <xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec> and override [#getPostingsFormatForField(String)](xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec) to return your custom postings format. 

 Similarly, if you just want to customise the <xref:Lucene.Net.Codecs.DocValuesFormat> per-field, have a look at [#getDocValuesFormatForField(String)](xref:Lucene.Net.Codecs.Lucene46.Lucene46Codec). 