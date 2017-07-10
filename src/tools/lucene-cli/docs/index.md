# Lucene.Net command line interface (CLI) tools

The Lucene.Net command line interface (CLI) is a new cross-platform toolchain with utilities for maintaining Lucene.Net and demos for learning basic Lucene.Net functionality.

## Installation

LUCENENET TODO

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