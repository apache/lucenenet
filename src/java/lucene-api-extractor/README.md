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

To build the project and run the tests, run the following command from the root of the repository:

```bash
mvn package
```

This will generate a JAR file in the `target` directory that you can run with `java -jar`.

## Usage

### Extract

This action extracts the API from the Lucene source code as JSON.

```bash
java -jar lucene-api-extractor-{version}.jar extract [options]
```

#### Options

| Option                                       | Description                                                                         | Required/Default |
|----------------------------------------------|-------------------------------------------------------------------------------------|------------------|
| `-lv <version>, --lucene-version <version>`  | The version of Lucene to extract the API from.                                      | (Required)       |
| `-libs <libraries>, --libraries <libraries>` | A comma-delimited list of Lucene libraries to extract.                              | (Required)       |
| `-f, --force`                                | Re-download any libraries even if they already exist in the download directory.     | `false`          |
| `-dd <path>, --download-dir <path>`          | The directory to download the Lucene libraries to.                                  | `download`       |
| `-o <path>, --output <path>`                 | The file to write the extracted API to. If not provided, prints to standard output. | (none)           |

### Hash

This action generates a SHA-256 hash of the JSON of the API extracted from the Lucene source code.

```bash
java -jar lucene-api-extractor-{version}.jar hash [options]
```

#### Options

| Option                                       | Description                                                                         | Required/Default |
|----------------------------------------------|-------------------------------------------------------------------------------------|------------------|
| `-lv <version>, --lucene-version <version>`  | The version of Lucene to extract the API from.                                      | (Required)       |
| `-libs <libraries>, --libraries <libraries>` | A comma-delimited list of Lucene libraries to extract.                              | (Required)       |
| `-f, --force`                                | Re-download any libraries even if they already exist in the download directory.     | `false`          |
| `-dd <path>, --download-dir <path>`          | The directory to download the Lucene libraries to.                                  | `download`       |
