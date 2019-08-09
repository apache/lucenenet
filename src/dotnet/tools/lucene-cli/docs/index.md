# Lucene.Net command line interface (CLI) tools

The Lucene.Net command line interface (CLI) is a new cross-platform toolchain with utilities for maintaining Lucene.Net and demos for learning basic Lucene.Net functionality.

## Prerequisites

- [.NET Core 2.1 Runtime](https://www.microsoft.com/net/download/core#/runtime)

## Installation

Perform a one-time install of the lucene-cli tool using the following dotnet CLI command:

```
dotnet tool install lucene-cli -g --version 4.8.0-beta00006
```

You may then use the lucene-cli tool to analyze and update Lucene.Net indexes and use its demos.

## CLI Commands

The following commands are installed:

- [analysis](analysis/index.md)
- [demo](demo/index.md)
- [index](index/index.md)
- [lock](lock/index.md)

## Command structure

CLI command structure consists of the driver ("lucene"), the command, and possibly command arguments and options. You see this pattern in most CLI operations, such as checking a Lucene.Net index for problematic segments and fixing (removing) them:

```
lucene index check C:\my-index --verbose
lucene index fix C:\my-index
```