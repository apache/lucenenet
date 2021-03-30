# list-high-freq-terms

### Name

`index-list-high-freq-terms` - Lists the top *N* most frequent terms by document frequency.

### Synopsis

```console
lucene index list-high-freq-terms [<INDEX_DIRECTORY>] [-t|--total-term-frequency] [-n|--number-of-terms] [-f|--field] [?|-h|--help]
```

### Description

Extracts the top *N* most frequent terms (by document frequency) from an existing Lucene index and reports their
document frequency.

### Arguments

`INDEX_DIRECTORY`

The directory of the index. If omitted, it defaults to the current working directory.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-t|--total-term-frequency`

Specifies that both the document frequency and term frequency are reported, ordered by descending total term frequency.

`-n|--number-of-terms <NUMBER>`

The number of terms to consider. If omitted, defaults to 100.

`-f|--field <FIELD>`

The field to consider. If omitted, considers all fields.

### Examples

List the high frequency terms in the index located at `F:\product-index\` on the `description` field, reporting both document frequency and term frequency:

```console
lucene index list-high-freq-terms F:\product-index --total-term-frequency --field description
```

List the high frequency terms in the index located at `C:\lucene-index\` on the `name` field, tracking 30 terms:

```console
lucene index list-high-freq-terms C:\lucene-index --f name -n 30
```
