# split

### Name

`index-split` - Splits an index into multiple equal parts.

### Synopsis

```console
lucene index split <OUTPUT_DIRECTORY> <INPUT_DIRECTORY>[ <INPUT_DIRECTORY_2>...] [-n|--number-of-parts] [-s|--sequential] [?|-h|--help]
```

### Description

Splits the input index into multiple equal parts. The method employed here uses `IndexWriter.AddIndexes(IndexReader[])` where the input data comes from the input index with artificially applied deletes to the document ids that fall outside the selected partition.

Deletes are only applied to a buffered list of deleted documents and don't affect the source index. This tool works also with read-only indexes.

The disadvantage of this tool is that source index needs to be read as many times as there are parts to be created. The multiple passes may be slow.

> [!NOTE]
> This tool is unaware of documents added automatically via `IndexWriter.AddDocuments(IEnumerable<IEnumerable<IIndexableField>>, Analyzer)` or `IndexWriter.UpdateDocuments(Term, IEnumerable<IEnumerable<IIndexableField>>, Analyzer)`, which means it can easily break up such document groups.

### Arguments

`OUTPUT_DIRECTORY`

Path to output directory to contain partial indexes.

`INPUT_DIRECTORY, INPUT_DIRECTORY_2`

The path of the source index, which can have deletions and can have multiple segments (or multiple readers). Multiple values can be supplied separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-n|--number-of-parts <NUMBER>`

The number of parts (output indices) to produce. If omitted, defaults to 2.

`-s|--sequential`

Sequential doc-id range split (default is round-robin).

### Example

Split the index located at `X:\old-index\` sequentially, placing the resulting 2 indices into the `X:\new-index\` directory:

```console
lucene index split X:\new-index X:\old-index --sequential
```


Split the index located at `T:\in\` into 4 parts and place them into the `T:\out\` directory:

```console
lucene index split T:\out T:\in -n 4
```
