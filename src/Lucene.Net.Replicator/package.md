---
uid: Lucene.Net.Replicator
summary: *content
---

# Files replication framework

The
[Replicator](xref:Lucene.Net.Replicator.IReplicator) allows replicating files between a server and client(s). Producers publish
[revisions](xref:Lucene.Net.Replicator.IRevision) and consumers update to the latest revision available.
[ReplicationClient](xref:Lucene.Net.Replicator.ReplicationClient) is a helper utility for performing the update operation. It can
be invoked either
[manually](xref:Lucene.Net.Replicator.ReplicationClient#Lucene_Net_Replicator_ReplicationClient_UpdateNow) or periodically by
[starting an update thread](xref:Lucene.Net.Replicator.ReplicationClient#Lucene_Net_Replicator_ReplicationClient_StartUpdateThread_System_Int64_System_String_).
[HttpReplicator](xref:Lucene.Net.Replicator.Http.HttpReplicator) can be used to replicate revisions by consumers that reside on
a different node than the producer.

The replication framework supports replicating any type of files, with built-in support for a single search index as
well as an index and taxonomy pair. For a single index, the application should publish an
[IndexRevision](xref:Lucene.Net.Replicator.IndexRevision) and set
[IndexReplicationHandler](xref:Lucene.Net.Replicator.IndexReplicationHandler) on the client. For an index and taxonomy pair, the
application should publish an [IndexAndTaxonomyRevision](xref:Lucene.Net.Replicator.IndexAndTaxonomyRevision) and set
[IndexAndTaxonomyReplicationHandler](xref:Lucene.Net.Replicator.IndexAndTaxonomyReplicationHandler) on the client.

When the replication client detects that there is a newer revision available, it copies the files of the revision and
then invokes the handler to complete the operation (e.g. copy the files to the index directory, sync them, reopen an
index reader etc.). By default, only files that do not exist in the handler's
[current revision files](xref:Lucene.Net.Replicator.IReplicationHandler.html#Lucene_Net_Replicator_IReplicationHandler_CurrentRevisionFiles) are copied,
however this can be overridden by extending the client.

<!-- Old Code Sample - not sure whether this is useful
```cs
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
```
-->

# Using the ReplicatorService

Because there are a number of different hosting frameworks to choose from on .NET and they don't implement common
abstractions for requests and responses, the ReplicatorService provides abstractions so that it can
be integrated easily into any of these frameworks.

To integrate the replicator into an existing hosting framework, the <xref:Lucene.Net.Replicator.Http.Abstractions.IReplicationRequest> and <xref:Lucene.Net.Replicator.Http.Abstractions.IReplicationResponse> interfaces must be implemented for the chosen framework.

## An ASP.NET Core Implementation

Below is an example of how these wrappers can be implemented for the ASP.NET Core framework.
The example only covers the absolute minimum needed in order for it to become functional within ASP.NET Core.

It does not go as far as to implement custom middleware, action results for controllers or anything else, while this
would be a natural to do, such implementations extends beyond the scope of this document.

#### ASP.NET Core Request Wrapper

The first thing to do is to wrap the ASP.NET Core Request object in a class that implements the <xref:Lucene.Net.Replicator.Http.Abstractions.IReplicationRequest> interface.
This is very straight forward.

```cs
// Wrapper class for the Microsoft.AspNetCore.Http.HttpRequest
public class AspNetCoreReplicationRequest : IReplicationRequest
{
    private readonly HttpRequest request;

    // Inject the actual request object in the constructor.
    public AspNetCoreReplicationRequest(HttpRequest request) 
        => this.request = request;

    // Provide the full path relative to the host.
    // In the common case in AspNetCore this should just return the full path, so PathBase + Path are concatenated and returned.
    // 
    // The path expected by the ReplicatorService is {context}/{shard}/{action} where:
    //  - action may be Obtain, Release or Update
    //  - context is the same context that is provided to the ReplicatorService constructor and defaults to '/replicate'
    public string Path 
        => request.PathBase + request.Path;

    // Return values for parameters used by the ReplicatorService
    // The ReplicatorService will call this with:
    // - version: The index revision
    // - sessionid: The ID of the session
    // - source: The source index for the files
    // - filename: The file name
    //
    // In this implementation a exception is thrown in the case that parameters are provided multiple times.
    public string QueryParam(string name) 
        => request.Query[name].SingleOrDefault();
}
```

#### ASP.NET Core Response Wrapper

Secondly the ASP.NET Core Response object is wrapped in a class that implements the <xref:Lucene.Net.Replicator.Http.Abstractions.IReplicationResponse> interface.
This is also very straight forward.

```cs
// Wrapper class for the Microsoft.AspNetCore.Http.HttpResponse
public class AspNetCoreReplicationResponse : IReplicationResponse
{
    private readonly HttpResponse response;
    
    // Inject the actual response object in the constructor.
    public AspNetCoreReplicationResponse(HttpResponse response)
        => this.response = response;

    // Getter and Setter for the http Status code, in case of failure the ReplicatorService will set this
    // Property.
    public int StatusCode
    {
        get => response.StatusCode;
        set => response.StatusCode = value;
    }

    // Return a stream where the ReplicatorService can write to for the response.
    // Depending on the action either the file or the sesssion token will be written to this stream.
    public Stream Body => response.Body;

    // Called when the ReplicatorService is done writing data to the response.
    // Here it is mapped to the flush method on the "body" stream on the response.
    public void Flush() => response.Body.Flush();
}
```

#### ASP.NET Core Utility Extension Method

This part is not nessesary, however by providing a extension method as a overload to the ReplicatorService Perform method
that instead takes the ASP.NET Core HttpRequest and HttpResponse response objects, it's easier to call the ReplicatorService
from either ASP.NET Core MVC controllers, inside of middleare or for the absolute minimal solution directly in the delegate parameter of a IApplicationBuilder.Run() method.

```cs
public static class AspNetCoreReplicationServiceExtentions
{
    // Optionally, provide a extension method for calling the perform method directly using the specific request
    // and response objects from AspNetCore
    public static void Perform(this ReplicationService self, HttpRequest request, HttpResponse response)
        => self.Perform(
            new AspNetCoreReplicationRequest(request),
            new AspNetCoreReplicationResponse(response));
}
```

## Using the Implementation

Now the implementation can be used within ASP.NET Core in order to service Lucene.NET Replicator requests over HTTP.

In order to enable replication of indexes, the <xref:Lucene.Net.Index.IndexWriter> that writes the index should be created with a <xref:Lucene.Net.Index.SnapshotDeletionPolicy>.

```cs
IndexWriterConfig config = new IndexWriterConfig(...ver..., new StandardAnalyzer(...ver...));
config.IndexDeletionPolicy = new SnapshotDeletionPolicy(config.IndexDeletionPolicy);
IndexWriter writer = new IndexWriter(FSDirectory.Open("..."), config);
```

For the absolute minimal solution we can wire the <xref:Lucene.Net.Replicator.Http.ReplicationService> up on the server side as:

```cs
LocalReplicator replicator = new LocalReplicator(); 
ReplicatorService service = new ReplicationService(new Dictionary<string, IReplicator>{
    ["shard_name"] = replicator
}, "/api/replicate");

app.Map("/api/replicate", builder => {
    builder.Run(async context => {
        await Task.Yield();
        service.Perform(context.Request, context.Response); 
    });
});
```

Now in order to publish a [Revision](xref:Lucene.Net.Replicator.IRevision) call the [Publish()](xref:Lucene.Net.Replicator.LocalReplicator#Lucene_Net_Replicator_LocalReplicator_Publish_Lucene_Net_Replicator_IRevision_) method in the <xref:Lucene.Net.Replicator.LocalReplicator>:

```cs
IndexWriter writer = ...;
LocalReplicator replicator = ...;
replicator.Publish(new IndexRevision(writer));
```

On the client side create a new <xref:Lucene.Net.Replicator.Http.HttpReplicator> and start replicating, e.g.:

```cs
IReplicator replicator = new HttpReplicator("http://{host}:{port}/api/replicate/shard_name");
ReplicationClient client = new ReplicationClient(
    replicator, 
    new IndexReplicationHandler(
        FSDirectory.Open(...directory...), 
        () => ...onUpdate...), 
        new PerSessionDirectoryFactory(...temp-working-directory...));

//Now either start the Update Thread or do manual pulls periodically.
client.UpdateNow(); //Manual Pull
client.StartUpdateThread(1000, "Replicator Thread"); //Pull automatically every second if there is any changes.
```

From here it would be natural to use a <xref:Lucene.Net.Search.SearcherManager> over the directory in order to get Searchers updated automatically.
But this cannot be created before the first actual replication as the <xref:Lucene.Net.Search.SearcherManager> will fail because there is no index.

We can use the onUpdate handler to perform the first initialization in this case.
