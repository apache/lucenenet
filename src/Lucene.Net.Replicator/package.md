---
uid: Lucene.Net.Replicator
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

# Files replication framework

	The
	[Replicator](Replicator.html) allows replicating files between a server and client(s). Producers publish
	[revisions](Revision.html) and consumers update to the latest revision available.
	[ReplicationClient](ReplicationClient.html) is a helper utility for performing the update operation. It can
	be invoked either
	[manually](ReplicationClient.html#updateNow()) or periodically by
	[starting an update thread](ReplicationClient.html#startUpdateThread(long, java.lang.String)).
	[HttpReplicator](http/HttpReplicator.html) can be used to replicate revisions by consumers that reside on
	a different node than the producer.

	The replication framework supports replicating any type of files, with built-in support for a single search index as
	well as an index and taxonomy pair. For a single index, the application should publish an
	[IndexRevision](IndexRevision.html) and set
	[IndexReplicationHandler](IndexReplicationHandler.html) on the client. For an index and taxonomy pair, the
	application should publish an [IndexAndTaxonomyRevision](IndexAndTaxonomyRevision.html) and set 
	[IndexAndTaxonomyReplicationHandler](IndexAndTaxonomyReplicationHandler.html) on the client.

	When the replication client detects that there is a newer revision available, it copies the files of the revision and
	then invokes the handler to complete the operation (e.g. copy the files to the index directory, fsync them, reopen an
	index reader etc.). By default, only files that do not exist in the handler's
	[current revision files](ReplicationClient.ReplicationHandler.html#currentRevisionFiles()) are copied,
	however this can be overridden by extending the client.

	An example usage of the Replicator:

    // ++++++++++++++ SERVER SIDE ++++++++++++++ // 
    IndexWriter publishWriter; // the writer used for indexing
    Replicator replicator = new LocalReplicator();
    replicator.publish(new IndexRevision(publishWriter));

    // ++++++++++++++ CLIENT SIDE ++++++++++++++ // 
    // either LocalReplictor, or HttpReplicator if client and server are on different nodes
    Replicator replicator;

    // callback invoked after handler finished handling the revision and e.g. can reopen the reader.
    Callable<Boolean> callback = null; // can also be null if no callback is needed
    ReplicationHandler handler = new IndexReplicationHandler(indexDir, callback);
    SourceDirectoryFactory factory = new PerSessionDirectoryFactory(workDir);
    ReplicationClient client = new ReplicationClient(replicator, handler, factory);

    // invoke client manually
    client.updateNow();

    // or, periodically
    client.startUpdateThread(100); // check for update every 100 milliseconds