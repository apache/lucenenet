# Lucene.NET API Check Utility

This utility checks the Lucene.NET API against the Lucene API for any incompatibilities.
It is intended to be used by Lucene.NET contributors, and is not intended to be used by Lucene.NET end users.

## How it works

The Java side of the API surface is produced by the [java-api-extractor](https://github.com/paulirwin/java-api-extractor)
tool, which lives in its own repository. This utility shells out to a pre-built copy of that tool (a Java jar)
to extract the Lucene Java API as JSON, then compares it against the reflected Lucene.NET API.

## Running

Use the `apicheck.ps1` script at the root of the repository. It downloads the pinned
`java-api-extractor` release jar and runs the comparison:

```powershell
./apicheck.ps1
```

Pass `-clean` to force a fresh download of the jar and a clean rebuild of the .NET project.

### Requirements

- A Java 21+ runtime on your `PATH` (the extractor targets Java 21).
- The [GitHub CLI](https://cli.github.com/) (`gh`), installed and authenticated (`gh auth login`).
  The script uses it to download the extractor's release jar.

### Running the .NET tool directly

If you already have a copy of the extractor jar, you can run this tool directly and point it at the jar
with `-j`:

```powershell
dotnet run --project src/dotnet/Lucene.Net.ApiCheck -- report -j <path-to-jar> -c apicheck-config.jsonc -o <output-dir> -d <download-dir>
```

Use the `diff` command instead of `report` to emit the raw diff JSON rather than the HTML report.
