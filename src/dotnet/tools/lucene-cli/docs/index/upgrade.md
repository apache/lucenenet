# upgrade

### Name

`index-upgrade` - Upgrades all segments of an index from previous Lucene.Net versions to the current segment file format.

### Synopsis

```console
lucene index upgrade [<INDEX_DIRECTORY>] [-d|--delete-prior-commits] [-v|--verbose] [-dir|--directory-type] [?|-h|--help]
```

### Description

This tool keeps only the last commit in an index; for this reason, if the incoming index has more than one commit, the tool refuses to run by default. Specify --delete-prior-commits to override this, allowing the tool to delete all but the last commit. 

Specify an FSDirectory implementation through the --directory-type option to force its use. If not qualified by an AssemblyName, the Lucene.Net.dll assembly will be used. 

> [!WARNING]
> This tool may reorder document IDs! Be sure to make a backup of your index before you use this. Also, ensure you are using the correct version of this utility to match your application's version of Lucene.NET. This operation cannot be reversed.

### Arguments

`INDEX_DIRECTORY`

The directory of the index. If omitted, it defaults to the current working directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-d|--delete-prior-commits`

Deletes prior commits.

`-v|--verbose`

Verbose output.

`-dir|--directory-type <DIRECTORY_TYPE>`

The `FSDirectory` implementation to use. Defaults to the optional `FSDirectory` for your OS platform.

### Examples

Upgrade the index format of the index located at `X:\lucene-index\` to the same version as this tool, using the `SimpleFSDirectory` implementation:

```console
lucene index upgrade X:\lucene-index -dir SimpleFSDirectory
```


Upgrade the index located at `C:\indexes\category-index\` verbosely, deleting all but the last commit:

```console
lucene index upgrade C:\indexes\category-index --verbose --delete-prior-commits
```
