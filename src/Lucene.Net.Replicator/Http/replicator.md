# Using the ReplicatorService

Because therer are a number of different hosting frameworks to choose from on .NET, and that they don't implement common
interfaceses or base classes for requests and responses, the ReplicatorService instead provides abstractions so that it can
be integrated easily into any of these framworks.

So to integrate the replicator into any existing hosting framworks, the IReplicationRequest and IReplicationResponse interfaces 
must be implemented for a choosen framwork.

## An AspNetCore Implementation

Below is an example of how these wrappers can be implemented for the AspNetCore framwork.
The example only convers the absolutely minimum needed in order for it to become functional within AspNetCore.

It does not go as far as to implement custom middleware, action results for controllers or anything else, while this
would be a natural to do, such implementations extends beyond the scope of this document.

#### AspNetCore Request Wrapper

The first thing to do is to wrap the AspNetCore Request object in a class that implements the IReplicationRequest interface.
This is very straight forward.

```csharp
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

#### AspNetCore Response Wrapper

Secondly the AspNetCore Response object is wrapped in a class that implements the IReplicationResponse interface.
This is also very straight forward.

```csharp
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

#### AspNetCore Utility Extension Method

This part is not nessesary, however by providing a extension method as a overload to the ReplicatorService Perform method
that instead takes the AspNetCore HttpRequest and HttpResponse response objects, it's easier to call the ReplicatorService
from either AspNetCore Mvc controllers, inside of middleare or for the absolute minimal solution directly in the delegate of
a IApplicationBuilder.Run method.

```csharp
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

Now the implementation can be used wihin AspNetCore in order to service Lucene Replicator requests over http.

In order to enable replication of indexes, the IndewWriter that writes the index should be created with a `SnapshotDeletionPolicy`.

```csharp
IndexWriterConfig config = new IndexWriterConfig(...ver..., new StandardAnalyzer(...ver...));
config.IndexDeletionPolicy = new SnapshotDeletionPolicy(config.IndexDeletionPolicy);
IndexWriter writer = new IndexWriter(FSDirectory.Open("..."), config);
```

For the absolute minimal solution we can wire the ReplicatorService up on the server side as:

```csharp
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

Now in order to publish a Revision call the publish method in the LocalReplicator:

```csharp
IndexWriter writer = ...;
LocalReplicator replicator = ...;
replicator.Publish(new IndexRevision(writer));
```

On the client side create a new HttpReplicator and start replicating, e.g.:

```csharp
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

From here it would be natural to use a SearchManager over the directory in order to get Searchers updated outomatically.
But this cannot be created before the first actual replication as the SearchManager will fail because there is no index.

We can use the onUpdate handler to perform the first initialization in this case.