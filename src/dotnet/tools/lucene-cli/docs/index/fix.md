# fix

### Name

`index-fix` - Fixes an index by removing problematic segments.

### Synopsis

```console
lucene index fix [<INDEX_DIRECTORY>] [-v|--verbose] [-c|--cross-check-term-vectors] [-dir|--directory-type] [--dry-run] [?|-h|--help]
```

### Description

Basic tool to write a new segments file that removes reference to problematic segments. As this tool checks every byte in the index, on a large index it can take quite a long time to run.

> [!WARNING] 
> This command should only be used on an emergency basis as it will cause documents (perhaps many) to be permanently removed from the index. Always make a backup copy of your index before running this! Do not run this tool on an index that is actively being written to. You have been warned!

### Arguments

`INDEX_DIRECTORY`

The directory of the index. If omitted, it defaults to the current working directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-v|--verbose`

Enables verbose output.

`-c|--cross-check-term-vectors`

Cross check term vectors.

`-dir|--directory-type <DIRECTORY_TYPE>`

The `FSDirectory` implementation to use. If omitted, it defaults to the optimal `FSDirectory` for your OS platform.

`--dry-run`

Doesn't change the index, but reports any actions that would be taken if this option were not supplied.

### Examples

Check what a fix operation would do if run on the index located at `X:\product-index\`, using verbose output:

<code>lucene index fix X:\product-index --verbose --dry-run</code>


Fix the index located at `X:\product-index` and cross check term vectors:

```console
lucene index fix X:\product-index -c
```
