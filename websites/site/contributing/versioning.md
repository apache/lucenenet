---
uid: contributing/versioning
---

# Versioning Procedure Overview

## Package Version

The version number that is used in the build process is called the _Package Version_, which is the same version number used for the NuGet package. The assembly version, file version, and informational version are all derived from the Package Version during the build process. The Package Version uses the following rules.

- Since Lucene.NET is a port of a [semantically versioned](https://semver.org/) component, our versioning scheme is to use the same version number as Lucene on production release (i.e. `4.8.0`). Any patches thereafter (breaking API change or not) will add a revision number (i.e `4.8.0.1`).

- We are also using pre-release version numbers (i.e. `4.8.0-beta00001`) for all unstable versions up to the production release. When doing pre-releases, revision numbers are not supported.

- **ONLY when changes are updated from a newer Lucene version to Lucene.NET**, the Lucene.NET version number is updated to match the Lucene version (i.e. `4.8.0` > `4.8.1` or `4.8.0` > `5.10.0`).

- Version numbers always progress in the following order

  - Pre-Release (i.e. `4.8.0-beta00001`)
  - Production Release (i.e. `4.8.0`)
  - Production Release Patch N (i.e where N is 1, `4.8.0.1`, where N is 2, `4.8.0.2`, etc)

- Version numbers correspond to GitHub milestones, so when releasing a milestone, the same version number should be used

## Git Tag Version

For legacy reasons, tagging the Git repository to indicate a version uses a different version format, but it must include all of the elements of the Package Version, for example:

```txt
# Pre-Release
Lucene.Net_4_8_0_beta00001

# Production Release
Lucene.Net_4_8_0

# Production Release Patch (breaking API changes included)
Lucene.Net_4_8_0_1

```
