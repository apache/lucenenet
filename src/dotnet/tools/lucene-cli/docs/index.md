# Lucene.Net command line interface (CLI) tools

The Lucene.Net command line interface (CLI) is a new cross-platform toolchain with utilities for maintaining Lucene.Net and demos for learning basic Lucene.Net functionality.

## Prerequisites

- [.NET Core 2.0 Runtime](https://www.microsoft.com/net/download/core#/runtime)

## Installation

Download the binaries from the [Apache Lucene.Net Distribution](http://www.apache.org/dyn/closer.cgi). Unzip the `lucene-cli.zip` file to a local directory and add that directory to the PATH environment variable of the local system.

## CLI Commands

The following commands are installed:

- [analysis](analysis/index.md)
- [demo](demo/index.md)
- [index](index/index.md)
- [lock](lock/index.md)

## Command structure

CLI command structure consists of the driver ("dotnet lucene-cli.dll"), the command, and possibly command arguments and options. You see this pattern in most CLI operations, such as checking a Lucene.Net index for problematic segments and fixing (removing) them:

```
dotnet lucene-cli.dll index check C:\my-index --verbose
dotnet lucene-cli.dll index fix C:\my-index
```