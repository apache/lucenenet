# Lucene API Extractor

This project is a utility to extract the API from the Lucene source code to confirm API compatibility with Lucene.NET.

While it is intended to be called from the Lucene.Net.ApiCheck project, it can be run independently.
One potential use of running it independently is to use the `hash` action to determine if there are API changes between different versions of Lucene.

## Prerequisites

- Java 21 or later
- Maven (tested with 3.9.9)

### Optional

- IntelliJ IDEA (recommended for development)

## Building

To build the project and run the tests, run the following command from this directory:

```bash
mvn package
```

This will generate a JAR file in the `target` directory that you can run with `java -jar`.

## Usage

### Extract

This action downloads the specified Lucene libraries from Maven Central and extracts their API as JSON.

```bash
java -jar target/lucene-api-extractor-<version>.jar extract [options]
```

#### Options

| Option                                            | Description                                                                                                                                                              | Required/Default |
|---------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------|
| `-lib <coord>, --library <coord>`                 | Lucene library to extract, as a Maven coordinate `groupId:artifactId:version`. May be specified multiple times.                                                          | (Required)       |
| `-dep <coord>, --dependency <coord>`              | Additional Maven dependency to include on the classpath (to satisfy transitive types during reflection). Same format as `-lib`. May be specified multiple times.         | (none)           |
| `-f, --force`                                     | Re-download any libraries even if they already exist in the download directory.                                                                                          | `false`          |
| `-dd <path>, --download-dir <path>`               | The directory to download the Lucene libraries to.                                                                                                                       | `download`       |
| `-o <path>, --output <path>`                      | The file to write the extracted API JSON to. If not provided, prints to standard output.                                                                                 | (stdout)         |
| `--strict`                                        | Fail fast when a class cannot be loaded (e.g. missing transitive types). By default such classes are logged to stderr and skipped.                                       | `false`          |
| `--no-verify-checksum`                            | Skip SHA-1 verification of downloaded jars against Maven Central's published `.sha1` sidecar.                                                                            | (verify on)      |

All progress and diagnostic logs are written to standard error; only the extracted JSON (or, for `hash`, the hex digest) is written to standard output, so `java -jar … extract … | jq .` works out of the box.

#### Example

```bash
java -jar target/lucene-api-extractor-1.0-SNAPSHOT.jar extract \
    -lib org.apache.lucene:lucene-core:4.8.1 \
    -lib org.apache.lucene:lucene-analyzers-common:4.8.1 \
    -o lucene-4.8.1.json
```

### Hash

This action runs the same extraction and prints a SHA-256 hash of the API surface. Useful for quickly detecting API drift between two versions of the same set of libraries.

```bash
java -jar target/lucene-api-extractor-<version>.jar hash [options]
```

#### Options

Same as `extract`, minus `-o`/`--output`. The hash is always printed to standard output.

The hash covers the extracted API keyed by `artifactId` only — the `groupId` and `version` parts of each Maven coordinate are deliberately excluded. This means upgrading `lucene-core` from `4.8.1` → `4.8.2` with an identical API produces the same hash; any change to public/protected types, constructors, methods, fields, or their signatures produces a different hash.
