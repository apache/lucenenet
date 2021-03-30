# extract-wikipedia

### Name

`benchmark-extract-wikipedia` - Extracts a downloaded Wikipedia dump into separate files for indexing.

### Synopsis

```console
lucene benchmark extract-wikipedia <INPUT_WIKIPEDIA_FILE> <OUTPUT_DIRECTORY> [-d|--discard-image-only-docs] [?|-h|--help]
```

### Arguments

`INPUT_WIKIPEDIA_FILE`

Input path to a Wikipedia XML file.

`OUTPUT_DIRECTORY`

Path to a directory where the output files will be written.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-d|--discard-image-only-docs`

Tells the extractor to skip WIKI docs that contain only images.

### Example

Extracts the `c:\wiki.xml` file into the `c:\out` directory, skipping any docs that only contain images.

```console
lucene benchmark extract-wikipedia c:\wiki.xml c:\out -d
```
