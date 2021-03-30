# copy-segments

### Name

`index-copy-segments` - Copies segments from one index to another index.

### Synopsis

```console
lucene index copy-segments <INPUT_DIRECTORY> <OUTPUT_DIRECTORY> <SEGMENT>[ <SEGMENT_2>...] [?|-h|--help]
```

### Description

This tool does file-level copying of segments files. This means it's unable to split apart a single segment into multiple segments. For example if your index is a single segment, this tool won't help.  Also, it does basic file-level copying (using simple FileStream) so it will not work with non FSDirectory Directory implementations.

### Arguments

`INPUT_DIRECTORY`

The directory of the index to copy.

`OUTPUT_DIRECTORY`

The directory of the destination index.

`SEGMENT, SEGMENT_2`

The segments to copy, separated by a space.

### Options

`?|-h|--help`

Prints out a short help for the command.

### Example

Copy the `_71_Lucene41_0` segment from the index located at `X:\lucene-index` to the index located at `X:\output`:

```console
lucene index copy-segments X:\lucene-index X:\output _71_Lucene41_0
```

