# Lucene.NET command line interface (CLI) tools

The Lucene.NET command line interface (CLI) is a new cross-platform toolchain with utilities for maintaining Lucene.NET and demos for learning basic Lucene.NET functionality.

## Prerequisites

- [.NET 6.0 Runtime or Higher](https://dotnet.microsoft.com/en-us/download/dotnet)

## Installation

Perform a one-time install of the lucene-cli tool using the following dotnet CLI command:

```console
dotnet tool install lucene-cli -g --version 4.8.0-beta00016
```

> [!NOTE]
> The version of the CLI you install should match the version of Lucene.NET you use.

You may then use the lucene-cli tool to analyze and update Lucene.NET indexes and use its demos.

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
