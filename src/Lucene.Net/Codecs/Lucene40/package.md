---
uid: Lucene.Net.Codecs.Lucene40
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

Lucene 4.0 file format.

# Apache Lucene - Index File Formats


* [Introduction](#introduction)

* [Definitions](#definitions)

   * [Inverted Indexing](#inverted-indexing)

   * [Types of Fields](#types-of-fields)

   * [Segments](#segments)

   * [Document Numbers](#document-numbers)

* [Index Structure Overview](#index-structure-overview)

* [File Naming](#file-naming)

* [Summary of File Extensions](#summary-of-file-extensions)

   * [Lock File](#lock-file)

   * [History](#history)

   * [Limitations](#limitations)


## Introduction

This document defines the index file formats used in this version of Lucene. If you are using a different version of Lucene, please consult the copy of `docs/` that was distributed with the version you are using.

Apache Lucene is written in Java, but several efforts are underway to write [versions of Lucene in other programming languages](http://wiki.apache.org/lucene-java/LuceneImplementations) including this implementation in .NET. If these versions are to remain compatible with Apache Lucene, then a language-independent definition of the Lucene index format is required. This document thus attempts to provide a complete and independent definition of the Apache Lucene file formats.

As Lucene evolves, this document should evolve. Versions of Lucene in different programming languages should endeavor to agree on file formats, and generate new versions of this document.

## Definitions

The fundamental concepts in Lucene are index, document, field and term.

An index contains a sequence of documents.

* A document is a sequence of fields.

* A field is a named sequence of terms.

* A term is a sequence of bytes.

The same sequence of bytes in two different fields is considered a different term. Thus terms are represented as a pair: the string naming the field, and the bytes within the field.

### Inverted Indexing

The index stores statistics about terms in order to make term-based search more efficient. Lucene's index falls into the family of indexes known as an _inverted index._ This is because it can list, for a term, the documents that contain it. This is the inverse of the natural relationship, in which documents list terms.

### Types of Fields

In Lucene, fields may be _stored_, in which case their text is stored in the index literally, in a non-inverted manner. Fields that are inverted are called _indexed_. A field may be both stored and indexed.

The text of a field may be _tokenized_ into terms to be indexed, or the text of a field may be used literally as a term to be indexed. Most fields are tokenized, but sometimes it is useful for certain identifier fields to be indexed literally.

See the [Field](xref:Lucene.Net.Documents.Field) docs for more information on Fields.

### Segments

Lucene indexes may be composed of multiple sub-indexes, or _segments_. Each segment is a fully independent index, which could be searched separately. Indexes evolve by:

1.  Creating new segments for newly added documents.

2.  Merging existing segments.

Searches may involve multiple segments and/or multiple indexes, each index potentially composed of a set of segments.

### Document Numbers

Internally, Lucene refers to documents by an integer _document number_. The first document added to an index is numbered zero, and each subsequent document added gets a number one greater than the previous.

Note that a document's number may change, so caution should be taken when storing these numbers outside of Lucene. In particular, numbers may change in the following situations:

* The numbers stored in each segment are unique only within the segment, and must be converted before they can be used in a larger context. The standard technique is to allocate each segment a range of values, based on the range of numbers used in that segment. To convert a document number from a segment to an external value, the segment's _base_ document number is added. To convert an external value back to a segment-specific value, the segment is identified by the range that the external value is in, and the segment's base value is subtracted. For example two five document segments might be combined, so that the first segment has a base value of zero, and the second of five. Document three from the second segment would have an external value of eight.

* When documents are deleted, gaps are created in the numbering. These are eventually removed as the index evolves through merging. Deleted documents are dropped when segments are merged. A freshly-merged segment thus has no gaps in its numbering.

## Index Structure Overview

Each segment index maintains the following:

* [Segment info](xref:Lucene.Net.Codecs.Lucene40.Lucene40SegmentInfoFormat).
   This contains metadata about a segment, such as the number of documents,
   what files it uses, 

* [Field names](xref:Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosFormat). 
   This contains the set of field names used in the index.

* [Stored Field values](xref:Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat). 
This contains, for each document, a list of attribute-value pairs, where the attributes 
are field names. These are used to store auxiliary information about the document, such as 
its title, url, or an identifier to access a database. The set of stored fields are what is 
returned for each hit when searching. This is keyed by document number.

* [Term dictionary](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat). 
A dictionary containing all of the terms used in all of the
indexed fields of all of the documents. The dictionary also contains the number
of documents which contain the term, and pointers to the term's frequency and
proximity data.

* [Term Frequency data](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat). 
For each term in the dictionary, the numbers of all the
documents that contain that term, and the frequency of the term in that
document, unless frequencies are omitted (IndexOptions.DOCS_ONLY)

* [Term Proximity data](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat). 
For each term in the dictionary, the positions that the
term occurs in each document. Note that this will not exist if all fields in
all documents omit position data.

* [Normalization factors](xref:Lucene.Net.Codecs.Lucene40.Lucene40NormsFormat). 
For each field in each document, a value is stored
that is multiplied into the score for hits on that field.

* [Term Vectors](xref:Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat). 
For each field in each document, the term vector (sometimes
called document vector) may be stored. A term vector consists of term text and
term frequency. To add Term Vectors to your index see the 
[Field](xref:Lucene.Net.Documents.Field) constructors

* [Per-document values](xref:Lucene.Net.Codecs.Lucene40.Lucene40DocValuesFormat). 
Like stored values, these are also keyed by document
number, but are generally intended to be loaded into main memory for fast
access. Whereas stored values are generally intended for summary results from
searches, per-document values are useful for things like scoring factors.

* [Deleted documents](xref:Lucene.Net.Codecs.Lucene40.Lucene40LiveDocsFormat). 
An optional file indicating which documents are deleted.

Details on each of these are provided in their linked pages.

## File Naming

All files belonging to a segment have the same name with varying extensions. The extensions correspond to the different file formats described below. When using the Compound File format (default in 1.4 and greater) these files (except for the Segment info file, the Lock file, and Deleted documents file) are collapsed into a single .cfs file (see below for details)

Typically, all segments in an index are stored in a single directory, although this is not required.

As of version 2.1 (lock-less commits), file names are never re-used (there is one exception, "segments.gen", see below). That is, when any file is saved to the Directory it is given a never before used filename. This is achieved using a simple generations approach. For example, the first segments file is `segments_1`, then `segments_2`, etc. The generation is a sequential long integer represented in alpha-numeric (base 36) form.

## Summary of File Extensions

The following table summarizes the names and extensions of the files in Lucene:

<table cellspacing="1" cellpadding="4">
<tr>
<th>Name</th>
<th>Extension</th>
<th>Brief Description</th>
</tr>
<tr>
<td>[Segments File](xref:Lucene.Net.Index.SegmentInfos)</td>
<td>segments.gen, segments_N</td>
<td>Stores information about a commit point</td>
</tr>
<tr>
<td>[Lock File](#lock-file)</td>
<td>write.lock</td>
<td>The Write lock prevents multiple IndexWriters from writing to the same
file.</td>
</tr>
<tr>
<td>[Segment Info](xref:Lucene.Net.Codecs.Lucene40.Lucene40SegmentInfoFormat)</td>
<td>.si</td>
<td>Stores metadata about a segment</td>
</tr>
<tr>
<td>[Compound File](xref:Lucene.Net.Store.CompoundFileDirectory)</td>
<td>.cfs, .cfe</td>
<td>An optional "virtual" file consisting of all the other index files for
systems that frequently run out of file handles.</td>
</tr>
<tr>
<td>[Fields](xref:Lucene.Net.Codecs.Lucene40.Lucene40FieldInfosFormat)</td>
<td>.fnm</td>
<td>Stores information about the fields</td>
</tr>
<tr>
<td>[Field Index](xref:Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat)</td>
<td>.fdx</td>
<td>Contains pointers to field data</td>
</tr>
<tr>
<td>[Field Data](xref:Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat)</td>
<td>.fdt</td>
<td>The stored fields for documents</td>
</tr>
<tr>
<td>[Term Dictionary](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat)</td>
<td>.tim</td>
<td>The term dictionary, stores term info</td>
</tr>
<tr>
<td>[Term Index](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat)</td>
<td>.tip</td>
<td>The index into the Term Dictionary</td>
</tr>
<tr>
<td>[Frequencies](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat)</td>
<td>.frq</td>
<td>Contains the list of docs which contain each term along with frequency</td>
</tr>
<tr>
<td>[Positions](xref:Lucene.Net.Codecs.Lucene40.Lucene40PostingsFormat)</td>
<td>.prx</td>
<td>Stores position information about where a term occurs in the index</td>
</tr>
<tr>
<td>[Norms](xref:Lucene.Net.Codecs.Lucene40.Lucene40NormsFormat)</td>
<td>.nrm.cfs, .nrm.cfe</td>
<td>Encodes length and boost factors for docs and fields</td>
</tr>
<tr>
<td>[Per-Document Values](xref:Lucene.Net.Codecs.Lucene40.Lucene40DocValuesFormat)</td>
<td>.dv.cfs, .dv.cfe</td>
<td>Encodes additional scoring factors or other per-document information.</td>
</tr>
<tr>
<td>[Term Vector Index](xref:Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat)</td>
<td>.tvx</td>
<td>Stores offset into the document data file</td>
</tr>
<tr>
<td>[Term Vector Documents](xref:Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat)</td>
<td>.tvd</td>
<td>Contains information about each document that has term vectors</td>
</tr>
<tr>
<td>[Term Vector Fields](xref:Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat)</td>
<td>.tvf</td>
<td>The field level info about term vectors</td>
</tr>
<tr>
<td>[Deleted Documents](xref:Lucene.Net.Codecs.Lucene40.Lucene40LiveDocsFormat)</td>
<td>.del</td>
<td>Info about what files are deleted</td>
</tr>
</table>

## Lock File

The write lock, which is stored in the index directory by default, is named
`write.lock`. If the lock directory is different from the index directory then
the write lock will be named `XXXX-write.lock` where XXXX is a unique prefix
derived from the full path to the index directory. When this file is present, a
writer is currently modifying the index (adding or removing documents). This
lock file ensures that only one writer is modifying the index at a time.

## History

Compatibility notes are provided in this document, describing how file formats have changed from prior versions:

* In version 2.1, the file format was changed to allow lock-less commits (ie,
no more commit lock). The change is fully backwards compatible: you can open a
pre-2.1 index for searching or adding/deleting of docs. When the new segments
file is saved (committed), it will be written in the new file format (meaning
no specific "upgrade" process is needed). But note that once a commit has
occurred, pre-2.1 Lucene will not be able to read the index.

* In version 2.3, the file format was changed to allow segments to share a
single set of doc store (vectors & stored fields) files. This allows for
faster indexing in certain cases. The change is fully backwards compatible (in
the same way as the lock-less commits change in 2.1).

* In version 2.4, Strings are now written as true UTF-8 byte sequence, not
Java's modified UTF-8. See [
LUCENE-510](http://issues.apache.org/jira/browse/LUCENE-510) for details.

* In version 2.9, an optional opaque `IDictionary<string, string>` CommitUserData
may be passed to IndexWriter's commit methods (and later retrieved), which is
recorded in the `segments_N` file. See
[LUCENE-1382](http://issues.apache.org/jira/browse/LUCENE-1382) for details. Also,
diagnostics were added to each segment written recording details about why it
was written (due to flush, merge; which OS/JRE was used; etc.). See issue
[LUCENE-1654](http://issues.apache.org/jira/browse/LUCENE-1654) for details.

* In version 3.0, compressed fields are no longer written to the index (they
can still be read, but on merge the new segment will write them, uncompressed).
See issue [LUCENE-1960](http://issues.apache.org/jira/browse/LUCENE-1960) 
for details.

* In version 3.1, segments records the code version that created them. See
[LUCENE-2720](http://issues.apache.org/jira/browse/LUCENE-2720) for details. 
Additionally segments track explicitly whether or not they have term vectors. 
See [LUCENE-2811](http://issues.apache.org/jira/browse/LUCENE-2811) 
for details.

* In version 3.2, numeric fields are written as natively to stored fields
file, previously they were stored in text format only.

* In version 3.4, fields can omit position data while still indexing term
frequencies.

* In version 4.0, the format of the inverted index became extensible via
the [Codec](xref:Lucene.Net.Codecs.Codec) api. Fast per-document storage
(`DocValues`) was introduced. Normalization factors need no longer be a 
single byte, they can be any [NumericDocValues](xref:Lucene.Net.Index.NumericDocValues). 
Terms need not be unicode strings, they can be any byte sequence. Term offsets 
can optionally be indexed into the postings lists. Payloads can be stored in the 
term vectors.

## Limitations

Lucene uses a .NET `int` to refer to document numbers, and the index file format uses an `Int32` on-disk to store document numbers. This is a limitation of both the index file format and the current implementation. Eventually these should be replaced with either `UInt64` values, or better yet, [VInt](xref:Lucene.Net.Store.DataOutput#Lucene_Net_Store_DataOutput_WriteVInt32_System_Int32_) values which have no limit.