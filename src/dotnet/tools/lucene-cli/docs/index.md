# Lucene.NET command line interface (CLI) tools

The Lucene.NET command line interface (CLI) is a new cross-platform toolchain with utilities for maintaining Lucene.NET and demos for learning basic Lucene.NET functionality.

## Prerequisites

- [.NET 6.0 Runtime or Higher](https://dotnet.microsoft.com/en-us/download/dotnet) (.NET 8.0 recommended)

## Installation

Perform a one-time install of the lucene-cli tool using the [dotnet tool install](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) command,
which will install the specified version:

```console
dotnet tool install lucene-cli -g --version 4.8.0-beta00016
```

<!--
Note for source readers: The version argument above is updated by the
docs.ps1 script when the docs are built, and this file should have that change
committed when a new version of the CLI is released. This is to help strike
a balance between having a real version number in this file for readers of
the source and not having to manually update the version number in the docs
every time a new version is released. You should still consult the NOTE
below to ensure the version number is correct for the version of Lucene.NET
you are using.
-->

> [!NOTE]
> The version of the CLI you install should match the version of Lucene.NET you use.
> The version can be specified using the [`--version` option](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install#options)
> of the [`dotnet tool install`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) command.
> For a list of available versions, see the [versions tab of the lucene-cli NuGet package](https://www.nuget.org/packages/lucene-cli#versions-body-tab)
> or run the [dotnet tool list](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-list)
> command using the package id `lucene-cli`.

You may then use the lucene-cli tool to analyze and update Lucene.NET indexes and use its demos.

The CLI is configured to [roll-forward](https://learn.microsoft.com/en-us/dotnet/core/versions/selection#control-roll-forward-behavior)
to the next available major version of .NET installed on your machine, if only a newer one than .NET 8 is found.
You can control this behavior by setting the `DOTNET_ROLL_FORWARD` environment variable or `--roll-forward`
command-line argument to `Disable` to prevent rolling forward, or `LatestMajor` to always use the latest
available major version, before running the CLI tool.



## CLI Commands

The following commands are installed:

- [analysis](analysis/index.md)
- [demo](demo/index.md)
- [index](index/index.md)
- [lock](lock/index.md)

## Command structure

CLI command structure consists of the driver ("lucene"), the command, and possibly command arguments and options. You see this pattern in most CLI operations, such as checking a Lucene.NET index for problematic segments and fixing (removing) them:

```console
lucene index check C:\my-index --verbose
lucene index fix C:\my-index
```
