# Apache Lucene.Net

Apache Lucene.Net is a C# full-text search engine, a C# port of the popular Apache Lucene project.  Apache Lucene.Net is not a complete application, but rather a code library and API that can easily be used to add search capabilities to applications.

The Apache Lucene.Net web site is at:
  http://lucenenet.apache.org

## Supported Frameworks

### Lucene.Net 4.8.0

- .NET Standard 1.5
- .NET Framework 4.5.1

### Lucene.Net 3.0.3

- .NET Framework 4.0
- .NET Framework 3.5

## Status

Latest Stable Version: Lucene.Net 3.0.3

Working toward Lucene.Net 4.8.0 (currently in BETA)

## Download

[![NuGet version](https://badge.fury.io/nu/Lucene.Net.svg)](https://www.nuget.org/packages/Lucene.Net/)

```
PM> Install-Package Lucene.Net
```

Lucene.Net is now divided into several sub-packages. See the [complete list of Lucene.Net sub-packages on NuGet.org](https://www.nuget.org/packages?q=lucene.net)

Our [continuous integration feed](https://myget.org/gallery/lucene-net-ci) is available on MyGet, for those who want to live on the edge.

## Documentation

[Lucene.Net WIKI](https://cwiki.apache.org/confluence/display/LUCENENET/Lucene.Net)

We don't yet have API documentation for Lucene.Net 4.8.0 (contributions welcome), but the API is similar to [Lucene 4.8.0](https://lucene.apache.org/core/4_8_0/).

### Legacy Versions

- [Lucene.Net 3.0.3 API Documentation](http://incubator.apache.org/lucene.net/docs/3.0.3/Index.html)
- [Lucene.Net 2.9.4 API Documentation](http://incubator.apache.org/lucene.net/docs/2.9.4/Index.html)

## How to Contribute

Lucene is a very large project (over 350,000 executable lines of code) and we welcome any and all help to maintain such an effort.

### Join Mailing Lists

[How to Join/Unsubscribe to/from mailing lists](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists)

### Ask a Question

If you have a general how-to question or need help from the Lucene.Net community, please email the Apache Lucene.Net-User mailing list by sending a message to:

[user@lucenenet.apache.org](mailto:user@lucenenet.apache.org)

We recommend you join the [user mailing list](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) to stay looped into all user discussions.

### Report a Bug

To report a bug, please use the [JIRA issue tracker](https://issues.apache.org/jira/browse/LUCENENET-574?jql=project%20%3D%20LUCENENET%20AND%20status%20%3D%20Open). You can signup for a JIRA account [here](https://cwiki.apache.org/confluence/signup.action) (it just takes a minute).

### Start a Discussion

To start a development discussion regarding technical features of Lucene.Net, please email the Apache Lucene.Net-Developer mailing list by sending a message to: 

[dev@lucenenet.apache.org](mailto:dev@lucenenet.apache.org)

We recommend you join both the [user and dev mailing lists](https://cwiki.apache.org/confluence/display/LUCENENET/Mailing+Lists) to stay looped in to all user and developer discussions.

### Submit a Pull Request

Before you start working on a pull request, please read our [Contributing](https://github.com/apache/lucenenet/blob/master/CONTRIBUTING.md) guide.

If you plan to submit multiple pull requests, please submit an [Individual Contributor License](https://cwiki.apache.org/confluence/display/LUCENENET/Individual+Contributor+License), or for individual pull requests, just submit the request and in the description state that the code is your original work and you license it under the [Apache License v2](http://www.apache.org/licenses/LICENSE-2.0).

## Build

To build the source, clone or download the repository. From the repository root, execute:

```
> build -pv:4.8.0.1000
```

This will build, version, and create NuGet `.nupkg` packages in the directory `/src/release/NuGetPackages/`. You can setup Visual Studio to read these packages like any NuGet feed by following these steps:

1. In Visual Studio, right-click the solution in Solution Explorer, and choose "Manage NuGet Packages for Solution"
2. Click the gear icon next to the Package sources dropdown.
3. Click the `+` icon (for add)
4. Give the source a name such as `Lucene.Net Local Packages`
5. Click the `...` button next to the Source field, and choose the `/src/release/NuGetPackages` folder on your local system.
6. Click Ok

Then all you need to do is choose the `Lucene.Net Local Packages` feed from the dropdown (in the NuGet Package Manager) and you can search for, install, and update the NuGet packages just as you can with any Internet-based feed.
